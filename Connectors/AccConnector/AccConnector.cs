namespace SecondMonitor.AccConnector
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using DataModel;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using Foundation.Connectors;
    using Foundation.Connectors.SharedMemory;
    using NLog;
    using PluginsConfiguration.Common.Controller;
    using Repository;
    using SecondMonitor.PluginsConfiguration.Common.DataModel;
    using SharedMemory;
    using UDPData;
    using SessionPhase = DataModel.Snapshot.SessionPhase;

    public class AccConnector : AbstractGameConnector
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly string[] AcExecutables = { "AC2-Win64-Shipping" };
        private readonly MappedBuffer<SPageFilePhysics> _physicsBuffer;
        private readonly MappedBuffer<SPageFileGraphic> _graphicsBuffer;
        private readonly MappedBuffer<SPageFileStatic> _staticBuffer;
        private readonly AccUdpConnector _accUdpConnector;
        private readonly AccDataConverter _accDataConverter;
        private bool _errorShown;

        private TimeSpan _lastSessionTimeRaw;
        private RaceSessionType _rawLastSessionType;
        private SessionType _lastSessionType;
        private SessionPhase _lastSessionPhase;

        private bool _isConnected;
        private DateTime _connectionTime = DateTime.MinValue;
        private SimulatorDataSet _lastDataSet;
        private int _lastSessionIndex;
        private CancellationTokenSource _udpConnectorCancelationTokeSource;

        public AccConnector(IPluginSettingsProvider pluginSettingsProvider, AccSettingsRepository settingsRepository)
            : base(AcExecutables)
        {
            AccConfiguration accConfiguration = pluginSettingsProvider.AccConfiguration;
            _physicsBuffer = new MappedBuffer<SPageFilePhysics>(AccShared.SharedMemoryNamePhysics);
            _graphicsBuffer = new MappedBuffer<SPageFileGraphic>(AccShared.SharedMemoryNameGraphic);
            _staticBuffer = new MappedBuffer<SPageFileStatic>(AccShared.SharedMemoryNameStatic);
            _accDataConverter = new AccDataConverter(accConfiguration.CustomName, settingsRepository);
            _rawLastSessionType = RaceSessionType.Replay;
            _lastSessionType = SessionType.Na;
            _lastSessionPhase = SessionPhase.Countdown;
            _accUdpConnector = new AccUdpConnector(accConfiguration);
            _lastSessionIndex = -1;
        }

        protected override string ConnectorName => SimulatorsNameMap.AccSimName;

        public override bool IsConnected => _isConnected;

        protected override void OnConnection()
        {
            if (_errorShown)
            {
                return;
            }

            ResetConnector();
            if (_connectionTime == DateTime.MinValue)
            {
                _connectionTime = DateTime.Now;
            }

            _isConnected = true;
        }

        protected override void ResetConnector()
        {
            _lastSessionTimeRaw = TimeSpan.MaxValue;
            _rawLastSessionType = RaceSessionType.Race;
            _lastSessionType = SessionType.Na;
            _lastSessionPhase = SessionPhase.Countdown;
            _lastDataSet = null;
        }

        protected override async Task DaemonMethod(CancellationToken cancellationToken)
        {
            _connectionTime = DateTime.MinValue;
            _udpConnectorCancelationTokeSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            while (!ShouldDisconnect && _physicsBuffer.IsConnected == false && _graphicsBuffer.IsConnected == false && _staticBuffer.IsConnected == false)
            {
                SimulatorDataSet dataSet = new SimulatorDataSet(SimulatorsNameMap.AccSimName) { SessionInfo = { SessionTime = DateTime.Now - _connectionTime } };
                RaiseDataLoadedEvent(dataSet);
                try
                {
                    _physicsBuffer.Connect();
                    _graphicsBuffer.Connect();
                    _staticBuffer.Connect();
                    _isConnected = true;
                }

                //ACC creates buffers at session start, this is correct
                catch (FileNotFoundException)
                {
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }

                await Task.Delay(2000, _udpConnectorCancelationTokeSource.Token).ConfigureAwait(false);
                if (!IsProcessRunning())
                {
                    _accUdpConnector.Disconnect();
                    Disconnect();
                    RaiseDisconnectedEvent();
                    return;
                }
            }

            Task connectTask = _accUdpConnector.ConnectAsync(_udpConnectorCancelationTokeSource.Token);
            await Task.WhenAny(connectTask, Task.Delay(20000, cancellationToken));

            if (!_accUdpConnector.IsConnected)
            {
                ShouldDisconnect = true;
                Logger.Info("ACC Session was detected, but the application is unable to connect to the ACC UDP Broadcast server, please check if the ACC broadcast server is enabled.");
            }

            _udpConnectorCancelationTokeSource.Cancel();
            try
            {
                await connectTask;
            }
            catch (OperationCanceledException)
            {
            }

            while (!ShouldDisconnect)
            {
                await Task.Delay(TickTime, cancellationToken).ConfigureAwait(false);
                FullUdpData fullUdpData = _accUdpConnector.FullUdpData;
                if (!fullUdpData.IsFilled)
                {
                    continue;
                }

                AccShared acData = ReadAllBuffers();
                SimulatorDataSet dataSet = _accDataConverter.CreateSimulatorDataSet(acData, fullUdpData);

                if (CheckSessionStarted(acData, dataSet, fullUdpData))
                {
                    _accDataConverter.Reset();
                    RaiseSessionStartedEvent(dataSet);
                }

                RaiseDataLoadedEvent(dataSet);
                _lastDataSet = dataSet;

                if (!IsProcessRunning())
                {
                    ShouldDisconnect = true;
                }
            }

            _accUdpConnector.Disconnect();
            Disconnect();
            RaiseDisconnectedEvent();
        }

        private void Disconnect()
        {
            _physicsBuffer.Disconnect();
            _graphicsBuffer.Disconnect();
            _staticBuffer.Disconnect();
            _isConnected = false;
        }

        private AccShared ReadAllBuffers()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected");
            }

            AccShared data = new AccShared()
            {
                AcsGraphic = _graphicsBuffer.GetMappedDataUnSynchronized(),
                AcsPhysics = _physicsBuffer.GetMappedDataUnSynchronized(),
                AcsStatic = _staticBuffer.GetMappedDataUnSynchronized()
            };

            //NO Player in session hack

            /*var graphicsTemp = data.AcsGraphic;
            graphicsTemp.playerCarID = -1;
            data.AcsGraphic = graphicsTemp;*/

            return data;
        }

        private bool CheckSessionStarted(AccShared acData, SimulatorDataSet dataSet, FullUdpData fullUdpData)
        {
            if (_lastSessionTimeRaw > fullUdpData.LastRealtimeUpdate.SessionTime)
            {
                _lastSessionTimeRaw = fullUdpData.LastRealtimeUpdate.SessionTime;
                return true;
            }

            _lastSessionTimeRaw = fullUdpData.LastRealtimeUpdate.SessionTime;

            if (_lastSessionIndex != fullUdpData.LastRealtimeUpdate.SessionIndex)
            {
                _lastSessionIndex = fullUdpData.LastRealtimeUpdate.SessionIndex;
                return true;
            }

            if (_rawLastSessionType != fullUdpData.LastRealtimeUpdate.SessionType || _lastSessionType != dataSet.SessionInfo.SessionType)
            {
                _lastSessionType = dataSet.SessionInfo.SessionType;
                _rawLastSessionType = fullUdpData.LastRealtimeUpdate.SessionType;
                _lastSessionPhase = dataSet.SessionInfo.SessionPhase;
                _lastSessionTimeRaw = fullUdpData.LastRealtimeUpdate.SessionTime;
                Logger.Info("Session restart cause - Session Type Change");
                return true;
            }

            if (_lastDataSet == null)
            {
                return false;
            }

            if ((dataSet.SessionInfo.SessionTime - _lastDataSet.SessionInfo.SessionTime).TotalSeconds < -5)
            {
                return true;
            }

            return false;
        }
    }
}