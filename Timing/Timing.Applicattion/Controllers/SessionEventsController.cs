namespace SecondMonitor.Timing.Application.Controllers
{
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Contracts.SimSettings;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using NLog;
    using ViewModels.SessionEvents;

    public class SessionEventsController : ISessionEventsController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly ISimSettingsFactory _settingsFactory;
        private bool _isStarted;
        private SessionType _lastSessionType;
        private Stopwatch _periodicCheckStopwatch;
        private string _lastTrackName;
        private string _lastPlayersCar;
        private string _lastPlayersClass;
        private string _lastSimulatorName;
        private FlagKind _activeFlags;
        private bool _lastPlayerCausingYellow;
        private bool _wasPlayerInSession;
        private DriverFinishStatus _lastPlayerFinishStatus;

        public SessionEventsController(ISessionEventProvider sessionEventProvider, ISimSettingsFactory settingsFactory)
        {
            _lastSimulatorName = string.Empty;
            _sessionEventProvider = sessionEventProvider;
            _settingsFactory = settingsFactory;
        }

        public Task StartControllerAsync()
        {
            ResetProperties();
            _sessionEventProvider.SetCurrentSimulatorSettings(_settingsFactory.Create(string.Empty));
            _isStarted = true;
            _periodicCheckStopwatch = Stopwatch.StartNew();
            return Task.CompletedTask;
        }

        public Task StopControllerAsync()
        {
            _isStarted = false;
            return Task.CompletedTask;
        }

        public void Visit(SimulatorDataSet simulatorDataSet)
        {
            if (!_isStarted)
            {
                return;
            }

            if (_lastSimulatorName != simulatorDataSet.Source)
            {
                _sessionEventProvider.SetCurrentSimulatorSettings(_settingsFactory.Create(simulatorDataSet.Source));
                _sessionEventProvider.NotifySimulatorChanged(simulatorDataSet);
                _lastSimulatorName = simulatorDataSet.Source;
            }

            if (_lastSessionType != simulatorDataSet.SessionInfo.SessionType)
            {
                _sessionEventProvider.NotifySessionTypeChanged(simulatorDataSet);
                _lastSessionType = simulatorDataSet.SessionInfo.SessionType;
                Logger.Info($"Session Type Change Detected : new session Type is {_lastSessionType}");
            }

            CheckPeriodicProperties(simulatorDataSet);
        }

        public void Reset()
        {
            if (!_isStarted)
            {
                return;
            }

            ResetProperties();
        }

        public void UpdateLastDataSet(SimulatorDataSet simulatorDataSet)
        {
            _sessionEventProvider.SetLastDataSet(simulatorDataSet);
        }

        private void CheckPeriodicProperties(SimulatorDataSet simulatorDataSet)
        {
            if (simulatorDataSet.PlayerInfo.FinishStatus != _lastPlayerFinishStatus)
            {
                _sessionEventProvider.NotifyPlayerFinishStateChanged(simulatorDataSet);
                _lastPlayerFinishStatus = simulatorDataSet.PlayerInfo.FinishStatus;
            }

            if (simulatorDataSet.SessionInfo.ActiveFlags != _activeFlags || simulatorDataSet.PlayerInfo.IsCausingYellow != _lastPlayerCausingYellow)
            {
                _lastPlayerCausingYellow = simulatorDataSet.PlayerInfo.IsCausingYellow;
                _activeFlags = simulatorDataSet.SessionInfo.ActiveFlags;
                _sessionEventProvider.NotifyFlagStateChanged(simulatorDataSet);
            }

            CheckPlayerInSession(simulatorDataSet);

            if (_periodicCheckStopwatch.ElapsedMilliseconds < 1000)
            {
                return;
            }

            if (simulatorDataSet.PlayerInfo != null)
            {
                CheckPlayerProperties(simulatorDataSet);
            }

            _periodicCheckStopwatch.Restart();

            if (_lastTrackName != simulatorDataSet.SessionInfo.TrackInfo.TrackFullName && !string.IsNullOrEmpty(simulatorDataSet.SessionInfo.TrackInfo.TrackFullName))
            {
                Logger.Info($"Track Change Detected : new Track is {simulatorDataSet.SessionInfo.TrackInfo.TrackFullName}");
                _sessionEventProvider.NotifyTrackChanged(simulatorDataSet);
                _lastTrackName = simulatorDataSet.SessionInfo.TrackInfo.TrackFullName;
            }
        }

        private void CheckPlayerInSession(SimulatorDataSet simulatorDataSet)
        {
            bool isPLayerInSession = !string.IsNullOrEmpty(simulatorDataSet.PlayerInfo.DriverSessionId);
            if (!isPLayerInSession && _wasPlayerInSession)
            {
                _sessionEventProvider.NotifyPlayerLeftSession(simulatorDataSet);
            }

            _wasPlayerInSession = isPLayerInSession;
        }

        private void CheckPlayerProperties(SimulatorDataSet simulatorDataSet)
        {
            if ((_lastPlayersCar != simulatorDataSet.PlayerInfo.CarName || _lastPlayersClass != simulatorDataSet.PlayerInfo.CarClassName)
                && !string.IsNullOrWhiteSpace(simulatorDataSet.PlayerInfo.CarName) && !string.IsNullOrWhiteSpace(simulatorDataSet.PlayerInfo.CarClassName))
            {
                _sessionEventProvider.NotifyPlayerPropertiesChanged(simulatorDataSet);
                _lastPlayersCar = simulatorDataSet.PlayerInfo.CarName;
                _lastPlayersClass = simulatorDataSet.PlayerInfo.CarClassName;
                Logger.Info($"Players Car Change detected, new car is  {_lastPlayersCar}, of class {_lastPlayersClass}");
            }
        }

        private void ResetProperties()
        {
            _activeFlags = FlagKind.None;
            _lastTrackName = string.Empty;
            _lastPlayersCar = string.Empty;
            _lastPlayersClass = string.Empty;
            _lastPlayerFinishStatus = DriverFinishStatus.Na;
            _wasPlayerInSession = false;
        }
    }
}