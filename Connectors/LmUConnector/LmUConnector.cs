namespace SecondMonitor.LmUConnector
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using NLog;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.Foundation.Connectors;
    using SecondMonitor.Foundation.Connectors.SharedMemory;
    using SecondMonitor.Foundation.Connectors.Visitor;
    using SecondMonitor.LmUConnector.SharedMemory;
    using SecondMonitor.LmUConnector.SharedMemory.lmuData;

    // Based on https://github.com/TheIronWolfModding/rf2SharedMemoryMapPlugin
    public class LmUConnector : AbstractGameConnector
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly string[] lmUExecutables = { "Le Mans Ultimate" };
        private readonly LmUDataConverter _lmUDataConverter;
        private readonly MappedBuffer<LmULayout> _lmuBuffer = new MappedBuffer<LmULayout>(LmuConstants.LmuTelemetryFilename);
        private readonly SessionTimeInterpolator _sessionTimeInterpolator;

        private DateTime _connectionTime = DateTime.MinValue;
        private int _rawLastSessionType = int.MinValue;
        private SessionPhase _lastSessionPhase;
        private SessionType _lastSessionType;
        private bool _isConnected;
        private TimeSpan _previousSessionTime;
        private SimulatorDataSet _lastDataSet;
        private LmUObjectOut _lastLmuObject;

        public LmUConnector()
            : base(lmUExecutables)
        {
            TickTime = 16;
            _sessionTimeInterpolator = new SessionTimeInterpolator(TimeSpan.FromMilliseconds(190));
            _lmUDataConverter = new LmUDataConverter(_sessionTimeInterpolator);
        }

        public override bool IsConnected => _isConnected;

        protected override string ConnectorName => "LM Ultimate";

        protected override void OnConnection()
        {
            ResetConnector();
            if (_connectionTime == DateTime.MinValue)
            {
                _connectionTime = DateTime.Now;
            }

            try
            {
                _lmuBuffer.Connect();
                _isConnected = true;
            }
            catch (Exception)
            {
                Disconnect();
                throw;
            }
        }

        private void Disconnect()
        {
            _lmuBuffer.Disconnect();
            _isConnected = false;
        }

        protected override void ResetConnector()
        {
            _rawLastSessionType = int.MinValue;
            _lastSessionType = SessionType.Na;
            _lastSessionPhase = SessionPhase.Countdown;
            _previousSessionTime = TimeSpan.MinValue;
        }

        protected override async Task DaemonMethod(CancellationToken cancellationToken)
        {
            _connectionTime = DateTime.MinValue;
            while (!ShouldDisconnect)
            {
                await Task.Delay(TickTime, cancellationToken).ConfigureAwait(false);
                LoadLmuPluginData();
                try
                {
                    _lastDataSet = _lmUDataConverter.CreateSimulatorDataSet(_lastLmuObject);
                }
                catch (LMUInvalidPackageException ex)
                {
                    if (ex.InnerException != null && ex.InnerException is not LMUInvalidPackageException)
                    {
                        Logger.Error(ex, "LMUInvalid Package Caused by Exception");
                    }

                    continue;
                }

                if (CheckSessionStarted())
                {
                    _sessionTimeInterpolator.Reset();
                    RaiseSessionStartedEvent(_lastDataSet);
                }

                _previousSessionTime = _lastDataSet.SessionInfo.SessionTime;
                RaiseDataLoadedEvent(_lastDataSet);

                if (!IsProcessRunning())
                {
                    ShouldDisconnect = true;
                }
            }

            Disconnect();
            RaiseDisconnectedEvent();
        }

        private void LoadLmuPluginData()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected");
            }

            _lastLmuObject = _lmuBuffer.GetMappedDataUnSynchronized().ObjectOut;
        }

        private bool CheckSessionStarted()
        {
            if (_previousSessionTime.TotalSeconds - 1 > _lastDataSet.SessionInfo.SessionTime.TotalSeconds)
            {
                Logger.Info($"New Session - Session time is less. Oldtime : {_previousSessionTime.TotalSeconds} - New Time: {_lastDataSet.SessionInfo.SessionTime.TotalSeconds}");
                return true;
            }

            if (_rawLastSessionType != _lastLmuObject.Scoring.mScoringInfo.mSession || _lastSessionType != _lastDataSet.SessionInfo.SessionType)
            {
                Logger.Info("New Session - Session type doesn't match");
                _lastSessionType = _lastDataSet.SessionInfo.SessionType;
                _rawLastSessionType = _lastLmuObject.Scoring.mScoringInfo.mSession;
                _lastSessionPhase = _lastDataSet.SessionInfo.SessionPhase;
                return true;
            }

            if (_lastDataSet.SessionInfo.SessionPhase != _lastSessionPhase && _lastSessionPhase != SessionPhase.Green && _lastDataSet.SessionInfo.SessionPhase != SessionPhase.Countdown)
            {
                Logger.Info(
                    $"New Session - Session phase doesn't match. LastRawSessionPhase: {_rawLastSessionType}, CurrentSessionPhase: {_lastLmuObject.Scoring.mScoringInfo.mSession} ");
                _lastSessionType = _lastDataSet.SessionInfo.SessionType;
                _rawLastSessionType = _lastLmuObject.Scoring.mScoringInfo.mSession;
                _lastSessionPhase = _lastDataSet.SessionInfo.SessionPhase;
                return true;
            }

            return false;
        }
    }
}
