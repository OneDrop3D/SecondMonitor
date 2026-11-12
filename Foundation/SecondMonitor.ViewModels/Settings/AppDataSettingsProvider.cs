namespace SecondMonitor.ViewModels.Settings
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Threading;
    using Factory;
    using ViewModel;

    public class AppDataSettingsProvider : ISettingsProvider
    {
        private const string MapFolder = "TrackMaps";
        private const string RatingsFolder = "Ratings";
        private const string SettingsFolder = "Settings";
        private const string TelemetryFolder = "Telemetry";
        private const string ContentFolder = "Content";
        private const string RecordsFolder = "Records";
        private const string ChampionshipsFolder = "Championships";
        private const string ChampionshipsCalendarsFolder = "Calendars";

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SecondMonitor\\settings.json");

        private readonly IViewModelFactory _viewModelFactory;
        private readonly DisplaySettingsLoader _displaySettingsLoader;

        private readonly Lazy<DisplaySettingsViewModel> _displaySettingsLazy;

        public AppDataSettingsProvider(IViewModelFactory viewModelFactory, DisplaySettingsLoader displaySettingsLoader)
        {
            _viewModelFactory = viewModelFactory;
            _displaySettingsLoader = displaySettingsLoader;
            _displaySettingsLazy = new Lazy<DisplaySettingsViewModel>(LoadSettings, LazyThreadSafetyMode.PublicationOnly);
        }

        public event EventHandler<PropertyChangedEventArgs> DisplaySettingsPropertyChanged;
        public DisplaySettingsViewModel DisplaySettingsViewModel => _displaySettingsLazy.Value;

        public string TelemetryRepositoryPath => Path.Combine(DisplaySettingsViewModel.ReportingSettingsViewModel.ExportDirectoryReplacedSpecialDirs, TelemetryFolder);

        public string MapRepositoryPath => Path.Combine(DisplaySettingsViewModel.ReportingSettingsViewModel.ExportDirectoryReplacedSpecialDirs, MapFolder);
        public string RatingsRepositoryPath => Path.Combine(DisplaySettingsViewModel.ReportingSettingsViewModel.ExportDirectoryReplacedSpecialDirs, RatingsFolder);

        public string SimulatorContentRepository => Path.Combine(DisplaySettingsViewModel.ReportingSettingsViewModel.ExportDirectoryReplacedSpecialDirs, ContentFolder);
        public string TrackRecordsPath => Path.Combine(DisplaySettingsViewModel.ReportingSettingsViewModel.ExportDirectoryReplacedSpecialDirs, RecordsFolder);
        public string CarSpecificationPath => Path.Combine(DisplaySettingsViewModel.ReportingSettingsViewModel.ExportDirectoryReplacedSpecialDirs, SettingsFolder);
        public string ChampionshipRepositoryPath => Path.Combine(DisplaySettingsViewModel.ReportingSettingsViewModel.ExportDirectoryReplacedSpecialDirs, ChampionshipsFolder);
        public string ChampionshipCalendarsPath => Path.Combine(DisplaySettingsViewModel.ReportingSettingsViewModel.ExportDirectoryReplacedSpecialDirs, ChampionshipsCalendarsFolder);

        public string DriversPresentationFolder => Path.Combine(DisplaySettingsViewModel.ReportingSettingsViewModel.ExportDirectoryReplacedSpecialDirs, SettingsFolder);

        private DisplaySettingsViewModel LoadSettings()
        {
            DisplaySettingsViewModel displaySettingsViewModel = _viewModelFactory.Create<DisplaySettingsViewModel>();
            displaySettingsViewModel.FromModel(_displaySettingsLoader.LoadDisplaySettingsFromFileSafe(SettingsPath));

            displaySettingsViewModel.PropertyChanged += OnDisplaySettingsChange;
            displaySettingsViewModel.SessionOptionsViewModel.PracticeSessionDisplayOptionsView.PropertyChanged += OnDisplaySettingsChange;
            displaySettingsViewModel.SessionOptionsViewModel.RaceSessionDisplayOptionsView.PropertyChanged += OnDisplaySettingsChange;
            displaySettingsViewModel.SessionOptionsViewModel.QualificationSessionDisplayOptionsView.PropertyChanged += OnDisplaySettingsChange;
            displaySettingsViewModel.LayoutSettingsViewModel.PropertyChanged += OnDisplaySettingsChange;

            return displaySettingsViewModel;
        }

        private void OnDisplaySettingsChange(object sender, PropertyChangedEventArgs e)
        {
            DisplaySettingsPropertyChanged?.Invoke(sender, e);
        }
    }
}