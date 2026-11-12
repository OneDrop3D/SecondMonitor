namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using DataModel.BasicProperties;
    using Model;

    public class TelemetrySettingsViewModel : AbstractViewModel<TelemetrySettings>
    {
        private readonly IDialogService _dialogService;
        private bool _isTelemetryLoggingEnabled;
        private int _loggingInterval;
        private int _maxSessionsKept;
        private bool _logInvalidLaps;
        private bool _logBestForEachDriver;
        private bool _logOpponentsBest;
        private bool _logOnlyPlayerClass;
        private int _logOnlyTop;
        private bool _logInPractice;
        private bool _logInQualification;
        private bool _logInRace;
        private bool _logOpponentsAll;
        private bool _compressFiles;
        private bool _isSaveAlsoAsXMlEnabled;
        private bool _isSaveAlsoAsJsonEnabled;

        public TelemetrySettingsViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        public bool IsTelemetryLoggingEnabled
        {
            get => _isTelemetryLoggingEnabled;
            set => SetProperty(ref _isTelemetryLoggingEnabled, value);
        }

        public int LoggingInterval
        {
            get => _loggingInterval;
            set => SetProperty(ref _loggingInterval, value);
        }

        public int MaxSessionsKept
        {
            get => _maxSessionsKept;
            set => SetProperty(ref _maxSessionsKept, value);
        }

        public bool LogInvalidLaps
        {
            get => _logInvalidLaps;
            set => SetProperty(ref _logInvalidLaps, value);
        }

        public bool LogOpponentsBest
        {
            get => _logOpponentsBest;
            set => SetProperty(ref _logOpponentsBest, value);
        }

        public bool LogBestForEachDriver
        {
            get => _logBestForEachDriver;
            set => SetProperty(ref _logBestForEachDriver, value);
        }

        public bool LogOnlyPlayerClass
        {
            get => _logOnlyPlayerClass;
            set => SetProperty(ref _logOnlyPlayerClass, value);
        }

        public int LogOnlyTop
        {
            get => _logOnlyTop;
            set => SetProperty(ref _logOnlyTop, value);
        }

        public bool LogInPractice
        {
            get => _logInPractice;
            set => SetProperty(ref _logInPractice, value);
        }

        public bool LogInQualification
        {
            get => _logInQualification;
            set => SetProperty(ref _logInQualification, value);
        }

        public bool LogInRace
        {
            get => _logInRace;
            set => SetProperty(ref _logInRace, value);
        }

        public bool CompressFiles
        {
            get => _compressFiles;
            set => SetProperty(ref _compressFiles, value);
        }

        public bool IsSaveAlsoAsXMlEnabled
        {
            get => _isSaveAlsoAsXMlEnabled;
            set => SetProperty(ref _isSaveAlsoAsXMlEnabled, value);
        }

        public bool IsSaveAlsoAsJsonEnabled
        {
            get => _isSaveAlsoAsJsonEnabled;
            set => SetProperty(ref _isSaveAlsoAsJsonEnabled, value);
        }

        public bool LogOpponentsAll
        {
            get => _logOpponentsAll;
            set
            {
                if (value)
                {
                    SetProperty(ref _logOpponentsAll, _dialogService.ShowYesNoDialog("Info", "Logging All Laps will significantly increase disk space used by individual telemetry sessions. Do you wish to enable it?"));
                    return;
                }

                SetProperty(ref _logOpponentsAll, false);
            }
        }

        protected override void ApplyModel(TelemetrySettings model)
        {
            IsTelemetryLoggingEnabled = model.IsTelemetryLoggingEnabled;
            LoggingInterval = model.LoggingInterval;
            MaxSessionsKept = model.MaxSessionsKept;
            LogInvalidLaps = model.LogInvalidLaps;
            LogOpponentsBest = model.LogOpponentsBest;
            LogBestForEachDriver = model.LogBestForEachDriver;
            LogOnlyPlayerClass = model.LogOnlyPlayerClass;
            LogInPractice = model.LogInPractice;
            LogInQualification = model.LogInQualification;
            LogInRace = model.LogInRace;
            LogOnlyTop = model.LogOnlyTop;
            _logOpponentsAll = model.LogOpponentsAll;
            _compressFiles = model.CompressFiles;
            IsSaveAlsoAsXMlEnabled = model.IsSaveAlsoAsXMlEnabled;
            IsSaveAlsoAsJsonEnabled = model.IsSaveAlsoAsJsonEnabled;
        }

        public override TelemetrySettings SaveToNewModel()
        {
            return new TelemetrySettings()
            {
                IsTelemetryLoggingEnabled = IsTelemetryLoggingEnabled,
                LoggingInterval = LoggingInterval,
                MaxSessionsKept = MaxSessionsKept,
                LogInvalidLaps = LogInvalidLaps,
                LogOpponentsBest = LogOpponentsBest,
                LogBestForEachDriver = LogBestForEachDriver,
                LogOnlyPlayerClass = LogOnlyPlayerClass,
                LogInPractice = LogInPractice,
                LogInQualification = LogInQualification,
                LogInRace = LogInRace,
                LogOnlyTop = LogOnlyTop,
                LogOpponentsAll = LogOpponentsAll,
                CompressFiles = CompressFiles,
                IsSaveAlsoAsXMlEnabled = IsSaveAlsoAsXMlEnabled,
                IsSaveAlsoAsJsonEnabled = IsSaveAlsoAsJsonEnabled,
            };
        }

        public bool IsTelemetryEnabledForSession(SessionType sessionType)
        {
            return (sessionType == SessionType.Practice && LogInPractice) ||
                (sessionType == SessionType.Qualification && LogInQualification) ||
                (sessionType == SessionType.Race && LogInRace);
        }
    }
}