namespace SecondMonitor.Plugins.Remote.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using LiteNetLib;
    using LiteNetLib.Utils;
    using NLog;
    using ProtoBuf;
    using SecondMonitor.Remote;
    using SecondMonitor.Remote.Adapter;
    using SecondMonitor.Remote.Models;
    using ViewModels;

    internal class BroadCastServerController : IBroadCastServerController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IBroadcastServerConfiguration _broadcastServerConfiguration;
        private readonly IServerOverviewViewModel _serverOverviewViewModel;
        private readonly IDatagramPayloadPacker _datagramPayloadPacker;
        private readonly Queue<NetPeer> _newPeers;
        private readonly object _queueLock = new object();

        private EventBasedNetListener _eventBasedNetListener;
        private NetManager _server;
        private Task _checkLoop;
        private CancellationTokenSource _serverCheckLoopSource;
        private Stopwatch _lastPackedStopWatch;

        public BroadCastServerController(
            IBroadcastServerConfiguration broadcastServerConfiguration,
            IServerOverviewViewModel serverOverviewViewModel,
            IDatagramPayloadPacker datagramPayloadPacker)
        {
            _newPeers = new Queue<NetPeer>();
            _broadcastServerConfiguration = broadcastServerConfiguration;
            _serverOverviewViewModel = serverOverviewViewModel;
            _datagramPayloadPacker = datagramPayloadPacker;
        }

        public Task StartControllerAsync()
        {
            StartListeningServer();
            return Task.CompletedTask;
        }

        public async Task StopControllerAsync()
        {
            await StopListeningServer();
        }

        public void SendSessionStartedPackage(SimulatorDataSet simulatorDataSet)
        {
            _lastPackedStopWatch = Stopwatch.StartNew();
            DatagramPayload payload = _datagramPayloadPacker.CreateSessionStartedPayload(simulatorDataSet);
            SendPackage(payload);
        }

        public void SendRegularDataPackage(SimulatorDataSet simulatorDataSet)
        {
            if (!_datagramPayloadPacker.IsMinimalPackageDelayPassed())
            {
                return;
            }

            lock (_queueLock)
            {
                if (_newPeers.Any())
                {
                    DatagramPayload initialPayload = _datagramPayloadPacker.CreateHandshakeDatagramPayload();
                    while (_newPeers.Count > 0)
                    {
                        SendPackage(initialPayload, _newPeers.Dequeue());
                    }
                }
            }

            _lastPackedStopWatch.Restart();
            DatagramPayload payload = _datagramPayloadPacker.CreateRegularDatagramPayload(simulatorDataSet);
            SendPackage(payload);
        }

        private void StartListeningServer()
        {
            _eventBasedNetListener = new EventBasedNetListener();
            _eventBasedNetListener.ConnectionRequestEvent += request => request.AcceptIfKey(DatagramPayload.Version4);
            _server = new NetManager(_eventBasedNetListener, new PacketCompressionLayer())
            {
                BroadcastReceiveEnabled = true,
                EnableStatistics = true
            };

            SubscribeEventBasedListener();
            int port = _broadcastServerConfiguration.Port;

            Logger.Info($"Starting Listening Server on port:{port}");
            if (!_server.Start(port))
            {
                Logger.Info($"Unable to start server, unknown error");
                return;
            }

            _serverOverviewViewModel.IsRunning = true;
            Logger.Info($"Server Started:{port}");

            _serverCheckLoopSource = new CancellationTokenSource();
            _checkLoop = ServerLoop(_serverCheckLoopSource.Token);
            _lastPackedStopWatch = Stopwatch.StartNew();
        }

        private async Task StopListeningServer()
        {
            Logger.Info("Stopping Listening Server");
            UnSubscribeEventBasedListener();
            _server.Stop();
            
            if (_serverCheckLoopSource == null)
            {
                return;
            }

            _serverCheckLoopSource.Cancel();

            try
            {
                await _checkLoop;
            }
            catch (TaskCanceledException)
            {
            }
        }

        private void SubscribeEventBasedListener()
        {
            if (_eventBasedNetListener == null)
            {
                return;
            }

            _eventBasedNetListener.PeerConnectedEvent += EventBasedNetListenerOnPeerConnectedEvent;
            _eventBasedNetListener.PeerDisconnectedEvent += EventBasedNetListenerOnPeerDisconnectedEvent;
            _eventBasedNetListener.NetworkReceiveEvent += EventBasedNetListenerOnNetworkReceiveEvent;
            _eventBasedNetListener.NetworkReceiveUnconnectedEvent += EventBasedNetListenerOnNetworkReceiveUnconnectedEvent;
            _eventBasedNetListener.NetworkErrorEvent += EventBasedNetListenerOnNetworkErrorEvent;
        }

        private void UnSubscribeEventBasedListener()
        {
            if (_eventBasedNetListener == null)
            {
                return;
            }

            _eventBasedNetListener.PeerConnectedEvent -= EventBasedNetListenerOnPeerConnectedEvent;
            _eventBasedNetListener.PeerDisconnectedEvent -= EventBasedNetListenerOnPeerDisconnectedEvent;
            _eventBasedNetListener.NetworkReceiveEvent -= EventBasedNetListenerOnNetworkReceiveEvent;
            _eventBasedNetListener.NetworkReceiveUnconnectedEvent -= EventBasedNetListenerOnNetworkReceiveUnconnectedEvent;
            _eventBasedNetListener.NetworkErrorEvent -= EventBasedNetListenerOnNetworkErrorEvent;
        }

        private void EventBasedNetListenerOnNetworkErrorEvent(IPEndPoint endpoint, SocketError socketErrorCode)
        {
            Logger.Info($"Socket Error -  {endpoint.Address}:{endpoint.Port} - Error Code :{socketErrorCode}");
        }

        private void EventBasedNetListenerOnNetworkReceiveUnconnectedEvent(IPEndPoint remoteEndPoint, NetDataReader reader, UnconnectedMessageType messageType)
        {
            if (messageType != UnconnectedMessageType.Broadcast)
            {
                return;
            }

            string discoveryMessage = reader.GetString();
            Logger.Info($"Discovery Request from {remoteEndPoint.Address}:{remoteEndPoint.Port}, Message:{discoveryMessage}");

            if (discoveryMessage != DatagramPayload.Version4)
            {
                Logger.Info("Versions do not match - ignored");
                return;
            }

            NetDataWriter response = new NetDataWriter();
            response.Put(DatagramPayload.Version4);
            _server.SendBroadcast(response, remoteEndPoint.Port);
        }

        private void EventBasedNetListenerOnNetworkReceiveEvent(NetPeer peer, NetDataReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            Logger.Info($"Data:{reader.GetString()}");
        }

        private void EventBasedNetListenerOnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Logger.Info($"Client Disconnected: {peer.Address}:{peer.Port}");
            _serverOverviewViewModel.RemoveClient(peer);
        }

        private void EventBasedNetListenerOnPeerConnectedEvent(NetPeer peer)
        {
            Logger.Info($"New Client Connected: {peer.Address}:{peer.Port}");
            _serverOverviewViewModel.AddClient(peer);
            lock (_queueLock)
            {
                _newPeers.Enqueue(peer);
            }
        }

        private async Task ServerLoop(CancellationToken token)
        {
            try
            {
                var statsStopwatch = Stopwatch.StartNew();
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(5, token);
                    _server.PollEvents();
                    if (_lastPackedStopWatch.Elapsed.TotalMilliseconds > 1000)
                    {
                        SendKeepAlivePacket();
                        _lastPackedStopWatch.Restart();
                    }

                    if (statsStopwatch.Elapsed.TotalMilliseconds > 500)
                    {
                        _serverOverviewViewModel.SampleStats(_server.Statistics);
                        statsStopwatch.Restart();
                    }
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private void SendPackage(DatagramPayload payload)
        {
            UpdateViewModelInputs(payload.InputInfo, payload.Source);
            NetDataWriter package = new NetDataWriter();
            package.Put(SerializeDatagramPayload(payload));
            _server.SendToAll(package, DeliveryMethod.ReliableOrdered);
        }

        private void SendPackage(DatagramPayload payload, NetPeer peer)
        {
            NetDataWriter package = new NetDataWriter();
            package.Put(SerializeDatagramPayload(payload));
            peer.Send(package, DeliveryMethod.ReliableOrdered);
        }

        private byte[] SerializeDatagramPayload(DatagramPayload payload)
        {
            byte[] payloadBytes;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                Serializer.Serialize(memoryStream, payload);
                payloadBytes = memoryStream.ToArray();
            }

            return payloadBytes;
        }

        private void UpdateViewModelInputs(InputInfo inputInfo, string source)
        {
            if (inputInfo == null)
            {
                return;
            }

            _serverOverviewViewModel.ThrottleInput = inputInfo.ThrottlePedalPosition * 100;
            _serverOverviewViewModel.ClutchInput = inputInfo.ClutchPedalPosition * 100;
            _serverOverviewViewModel.BrakeInput = inputInfo.BrakePedalPosition * 100;
            _serverOverviewViewModel.ConnectedSimulator = source == "Remote" ? "None" : source;
        }

        private void SendKeepAlivePacket()
        {
            DatagramPayload payload = _datagramPayloadPacker.CreateHearthBeatDatagramPayload();
            SendPackage(payload);
        }
    }
}