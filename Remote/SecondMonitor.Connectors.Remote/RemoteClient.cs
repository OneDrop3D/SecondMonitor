namespace SecondMonitor.Connectors.Remote
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using DataModel.Snapshot;
    using Factories;
    using LiteNetLib;
    using LiteNetLib.Utils;
    using Models;
    using NLog;
    using ProtoBuf;
    using SecondMonitor.Remote;
    using SecondMonitor.Remote.Adapter;
    using SecondMonitor.Remote.Models;
    using ViewModels;
    using ConnectionState = LiteNetLib.ConnectionState;

    internal class RemoteClient : IRemoteClient
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IRemoteConnectorConfiguration _remoteConnectorConfiguration;
        private readonly EventBasedNetListener _listener;
        private readonly NetManager _client;
        private readonly RemoteConnectorOptionsViewModel _optionsViewModel;
        private readonly IDatagramPayloadUnPackerFactory _payloadUnPackerFactory;
        private readonly Dictionary<IPEndPoint, IDatagramPayloadUnPacker> _datagramPayloadUnPackers;
        private readonly bool _isMultiClientEnabled;

        private readonly SessionManager _sessionManager = new SessionManager();
        private readonly object _lockObject;
        private Task _clientCheckLoopTask;
        private IPEndPoint _pendingPeerEndPoint;

        public RemoteClient(IRemoteConnectorConfiguration remoteConnectorConfiguration, RemoteConnectorOptionsViewModel optionsViewModel, IDatagramPayloadUnPackerFactory payloadUnPackerFactory)
        {
            _lockObject = new object();
            _listener = new EventBasedNetListener();
            _listener.ConnectionRequestEvent += request => request.AcceptIfKey(DatagramPayload.Version4);
            _client = new NetManager(_listener, new PacketCompressionLayer())
            {
                BroadcastReceiveEnabled = true,
                EnableStatistics = true,
            };
            _remoteConnectorConfiguration = remoteConnectorConfiguration;
            _optionsViewModel = optionsViewModel;
            _payloadUnPackerFactory = payloadUnPackerFactory;
            _datagramPayloadUnPackers = new Dictionary<IPEndPoint, IDatagramPayloadUnPacker>();
            _pendingPeerEndPoint = null;
            _isMultiClientEnabled = _remoteConnectorConfiguration.IsMultiClientEnabled;

            Logger.Info($"Multiclient is set to {_isMultiClientEnabled}");
        }

        public event EventHandler Disconnected;

        public event EventHandler<SimulatorDataSet> SessionStarted;
        public event EventHandler<SimulatorDataSet> DataLoaded;

        private bool IsConnected { get; set; }

        public bool TryConnect()
        {
            Logger.Info("Remote Connector Is Enabled");

            if (_clientCheckLoopTask?.Status != TaskStatus.Running)
            {
                StartConnector();
            }

            lock (_lockObject)
            {
                if (_remoteConnectorConfiguration.FindInLanPort.HasValue)
                {
                    _client.TryConnectUsingDiscovery(_remoteConnectorConfiguration.FindInLanPort.Value);
                }
            }

            foreach (var serverPeerEndPoint in _remoteConnectorConfiguration.ServerPeers.ToList())
            {
                ConnectToPeer(serverPeerEndPoint);
            }

            Thread.Sleep(200);

            lock (_lockObject)
            {
                if (_client.ConnectedPeerList.All(x => x.ConnectionState != ConnectionState.Connected))
                {
                    Logger.Info("Connection state is still in progress");
                    return false;
                }

                foreach (var serverPeer in _client.ConnectedPeerList.Where(x => x.ConnectionState == ConnectionState.Connected).ToList())
                {
                    Logger.Info($"Connected to {serverPeer.Address}:{serverPeer.Port}");
                }
            }

            return true;
        }

        public void StartClientLoop()
        {
            IsConnected = true;
        }

        public void SwitchToPeer(IPEndPoint endPoint)
        {
            _pendingPeerEndPoint = endPoint;
        }

        public void ConnectToPeer(IPEndPoint endPoint)
        {
            lock (_lockObject)
            {
                var serverPeer = _client.ConnectedPeerList.FirstOrDefault(x => x.Equals(endPoint));

                if (serverPeer?.ConnectionState == ConnectionState.Connected)
                {
                    Logger.Info($"Connected to {endPoint.Address}:{endPoint.Address}");
                    _sessionManager.AddPeer(serverPeer);
                    _optionsViewModel.AddPeer(serverPeer);
                    return;
                }

                if (serverPeer?.ConnectionState == ConnectionState.Outgoing)
                {
                    Logger.Info($"Trying to Connect to to Server {endPoint.Address}:{endPoint.Port}, server peer is In Progress");
                    _sessionManager.AddPeer(serverPeer);
                    _optionsViewModel.AddPeer(serverPeer);
                    return;
                }

                try
                {
                    Logger.Info($"Trying to Connecto to Server {endPoint.Address}:{endPoint.Port}");
                    var newPeer = _client.Connect(endPoint, DatagramPayload.Version4);
                    _sessionManager.AddPeer(newPeer);
                    _optionsViewModel.AddPeer(newPeer);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }

        private void StartConnector()
        {
            lock (_lockObject)
            {
                _client.Start();
                _listener.PeerConnectedEvent += EventBasedNetListenerOnPeerConnectedEvent;
                _listener.PeerDisconnectedEvent += EventBasedNetListenerOnPeerDisconnectedEvent;
                _listener.NetworkReceiveEvent += EventBasedNetListenerOnNetworkReceiveEvent;
                _listener.NetworkReceiveUnconnectedEvent += EventBasedNetListenerOnNetworkReceiveUnconnectedEvent;
                _clientCheckLoopTask = ClientCheckLoop();
            }
        }

        private void EventBasedNetListenerOnNetworkReceiveUnconnectedEvent(IPEndPoint remoteEndPoint, NetDataReader reader, UnconnectedMessageType messageType)
        {
            if (messageType != UnconnectedMessageType.BasicMessage)
            {
                return;
            }

            string discoveryMessage = reader.GetString();

            Logger.Info($"Discovery ResponseFrom from {remoteEndPoint.Address}:{remoteEndPoint.Port}, Message:{discoveryMessage}");
            if (discoveryMessage != DatagramPayload.Version4)
            {
                Logger.Info("Version Do not Match - Ignoring");
                return;
            }

            Logger.Info("Version Do Match - Will be used as server");
            Logger.Info($"Trying to Connecto to Server {remoteEndPoint.Address}:{remoteEndPoint.Port}");
            lock (_lockObject)
            {
                var newPeer = _client.Connect(remoteEndPoint, DatagramPayload.Version4);
                _sessionManager.AddPeer(newPeer);
                _optionsViewModel.AddPeer(newPeer);
            }
        }

        private void EventBasedNetListenerOnNetworkReceiveEvent(NetPeer peer, NetDataReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            //Not the used connector = end
            if (!IsConnected)
            {
                return;
            }

            DatagramPayload payload;
            using (MemoryStream memoryStream = new(reader.RawData, reader.UserDataOffset, reader.UserDataSize))
            {
                payload = Serializer.Deserialize<DatagramPayload>(memoryStream);
            }

            if (!_datagramPayloadUnPackers.TryGetValue(peer, out IDatagramPayloadUnPacker datagramPayloadUnPacker))
            {
                datagramPayloadUnPacker = _payloadUnPackerFactory.Create();
                _datagramPayloadUnPackers.Add(peer, datagramPayloadUnPacker);
            }

            SimulatorDataSet simulatorDataSet = datagramPayloadUnPacker.UnpackDatagramPayload(payload);

            _optionsViewModel.UpdatePeer(peer, simulatorDataSet.Source, simulatorDataSet.PlayerInfo?.DriverLongName ?? string.Empty, simulatorDataSet.SessionInfo?.TrackInfo?.TrackFullName ?? string.Empty);

            if (!_isMultiClientEnabled)
            {
                HandlePayloadSimple(payload, simulatorDataSet);
                return;
            }

            bool sessionRestarted = false;
            if (_pendingPeerEndPoint?.Equals(peer) == true)
            {
                sessionRestarted = _sessionManager.SwitchToPeer(peer, simulatorDataSet);
                _pendingPeerEndPoint = null;
            }

            if (payload.PayloadKind == DatagramPayloadKind.SessionStart || !_sessionManager.HasActiveSession || sessionRestarted)
            {
                if (_sessionManager.SessionStartedReceived(peer, simulatorDataSet))
                {
                    SessionStarted?.Invoke(this, simulatorDataSet);
                    _optionsViewModel.SetActivePeer(peer);
                }
            }
            else if (payload.PayloadKind == DatagramPayloadKind.Normal && _sessionManager.DataSetReceived(peer, simulatorDataSet))
            {
                DataLoaded?.Invoke(this, simulatorDataSet);
                _optionsViewModel.SetActivePeer(peer);
            }
            else if (_sessionManager.IsActivePeerInDifferentSession(peer, simulatorDataSet))
            {
                Logger.Info("Peer was detected in different session");
                SessionStarted?.Invoke(this, simulatorDataSet);
                _optionsViewModel.SetActivePeer(peer);
            }
        }

        private void HandlePayloadSimple(DatagramPayload payload, SimulatorDataSet simulatorDataSet)
        {
            switch (payload.PayloadKind)
            {
                case DatagramPayloadKind.HearthBeat:
                case DatagramPayloadKind.Normal:
                    DataLoaded?.Invoke(this, simulatorDataSet);
                    break;
                case DatagramPayloadKind.SessionStart:
                    SessionStarted?.Invoke(this, simulatorDataSet);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void EventBasedNetListenerOnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectinfo)
        {
            if (disconnectinfo.Reason == DisconnectReason.ConnectionFailed)
            {
                return;
            }

            _optionsViewModel.UpdatePeerState(peer);
            Logger.Info($"Server Disconnected - {disconnectinfo.Reason}");

            lock (_lockObject)
            {
                if (_client.ConnectedPeerList.All(x => x.ConnectionState == ConnectionState.Disconnected))
                {
                    Disconnected?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void EventBasedNetListenerOnPeerConnectedEvent(NetPeer peer)
        {
            _sessionManager.AddPeer(peer);
            _optionsViewModel.AddPeer(peer);
        }

        private async Task ClientCheckLoop()
        {
            try
            {
                var sampleStopwatch = Stopwatch.StartNew();
                while (true)
                {
                    await Task.Delay(5);
                    lock (_client)
                    {
                        if (_client.PoolCount > 0)
                        {
                            _client.PollEvents();
                        }

                        if (sampleStopwatch.Elapsed.Milliseconds > 500)
                        {
                            foreach (var peer in _client.ConnectedPeerList.ToList())
                            {
                                _optionsViewModel.SamplePeerStats(peer);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "RemoteClient CheckLoop fell over");
            }
            finally
            {
                Logger.Info("Disconnecting RemoteClient due to error");
                lock (_lockObject)
                {
                    _client.DisconnectAll();
                }

                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}