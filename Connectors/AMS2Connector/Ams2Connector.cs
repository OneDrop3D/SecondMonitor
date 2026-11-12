namespace SecondMonitor.AMS2Connector
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using DataConvertor;
    using DataModel;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using Foundation.Connectors;
    using Foundation.Connectors.SharedMemory;
    using NLog;
    using PluginsConfiguration.Common.Controller;
    using SharedMemory;

    public class Ams2Connector : AbstractGameConnector
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly string[] Ams2Executables = { "AMS2", "AMS2AVX" };
        private static readonly string SharedMemoryName = "$pcars2$";

        private readonly MappedBuffer<Ams2SharedMemory> _sharedMemory;
        private readonly Ams2DataConvertor _ams2DataConvertor;
        private readonly Stopwatch _stopwatch;
        private Ams2SharedMemory _previousTickMemory;

        private bool _isConnected;

        private Ams2SessionType _lastRawAms2SessionType;
        private SessionType _lastSessionType;

        public Ams2Connector(IPluginSettingsProvider pluginSettingsProvider)
            : base(Ams2Executables)
        {
            _sharedMemory = new MappedBuffer<Ams2SharedMemory>(SharedMemoryName);

            _ams2DataConvertor = new Ams2DataConvertor(pluginSettingsProvider.Ams2Configuration);
            _lastRawAms2SessionType = Ams2SessionType.SessionInvalid;
            _lastSessionType = SessionType.Na;
            _stopwatch = new Stopwatch();
            _previousTickMemory = default;
        }

        protected override string ConnectorName => SimulatorsNameMap.Ams2SimName;

        protected internal TimeSpan SessionTime => _stopwatch?.Elapsed ?? TimeSpan.Zero;

        public override bool IsConnected => _isConnected;

        protected override void OnConnection()
        {
            ResetConnector();

            try
            {
                _sharedMemory.Connect();
                _isConnected = true;
            }
            catch (Exception)
            {
                Disconnect();
                throw;
            }
        }

        protected override void ResetConnector()
        {
            _stopwatch.Restart();
            _lastRawAms2SessionType = Ams2SessionType.SessionInvalid;
            _lastSessionType = SessionType.Na;
        }

        protected override async Task DaemonMethod(CancellationToken cancellationToken)
        {
            while (!ShouldDisconnect)
            {
                await Task.Delay(TickTime, cancellationToken).ConfigureAwait(false);
                Ams2SharedMemory rawData = ReadAllBuffers();

                if (!_stopwatch.IsRunning && ((GameState)rawData.mGameState == GameState.GameInGamePlaying || (GameState)rawData.mGameState == GameState.GameInGameInMenuTimeTicking))
                {
                    _stopwatch.Start();
                }

                if (_stopwatch.IsRunning && ((GameState)rawData.mGameState != GameState.GameInGamePlaying && (GameState)rawData.mGameState != GameState.GameInGameInMenuTimeTicking))
                {
                    _stopwatch.Stop();
                }

                // This a state that sometimes occurs when saving pit preset during race
                // This state is ignored, otherwise it would trigger a session reset
                if (rawData.mSessionState == 0 && (rawData.mGameState == 2 || rawData.mGameState == 3))
                {
                    continue;
                }

                SimulatorDataSet dataSet = _ams2DataConvertor.CreateSimulatorDataSet(ref rawData, TimeSpan.FromMilliseconds(_stopwatch.ElapsedMilliseconds));

                if (CheckSessionStarted(rawData, dataSet))
                {
                    _stopwatch.Restart();
                    dataSet.SessionInfo.SessionTime = _stopwatch.Elapsed;
                    RaiseSessionStartedEvent(dataSet);
                }

                _previousTickMemory = rawData;

                _ams2DataConvertor.LastSessionTime = dataSet.SessionInfo.SessionTime;
                RaiseDataLoadedEvent(dataSet);

                if (!IsProcessRunning())
                {
                    ShouldDisconnect = true;
                }
            }

            Disconnect();
            RaiseDisconnectedEvent();
        }

        private static void Log(ref Ams2SharedMemory sharedMemory, string label)
        {
            StringBuilder sb = new(label);
            sb.AppendLine();
            sb.AppendLine($"Game State: {((GameState)sharedMemory.mGameState).ToString()}");
            sb.AppendLine($"Session State: {((Ams2SessionType)sharedMemory.mSessionState).ToString()}");
            sb.AppendLine($"Race State: {((RaceState)sharedMemory.mRaceState).ToString()}");
            sb.AppendLine($"Participants: {sharedMemory.mNumParticipants}");
            sb.AppendLine($"Seq Number: {sharedMemory.mSequenceNumber}");
            sb.AppendLine($"Laps: {sharedMemory.mLapsInEvent}");
            sb.AppendLine($"Time: {sharedMemory.mSessionDuration}");
            Logger.Info(sb.ToString);
        }

        private Ams2SharedMemory ReadAllBuffers()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected");
            }

            Ams2SharedMemory ams2data;
            do
            {
                ams2data = _sharedMemory.GetMappedDataUnSynchronized();
            } while (ams2data.mSequenceNumber % 2 != 0);

            return ams2data;
        }

        private void Disconnect()
        {
            _sharedMemory.Disconnect();
            _isConnected = false;
            _previousTickMemory = default;
        }

        private bool CheckSessionStarted(Ams2SharedMemory rawData, SimulatorDataSet dataSet)
        {
            if (_lastRawAms2SessionType != (Ams2SessionType)rawData.mSessionState || _lastSessionType != dataSet.SessionInfo.SessionType)
            {
                Logger.Info($"Session doesn match, last packet session: {_lastSessionType}, current session: {dataSet.SessionInfo.SessionType} ");
                Log(ref _previousTickMemory, "Previous: ");
                Log(ref rawData, "Current: ");
                _lastSessionType = dataSet.SessionInfo.SessionType;
                _lastRawAms2SessionType = (Ams2SessionType)rawData.mSessionState;
                return true;
            }

            return false;
        }
    }
}