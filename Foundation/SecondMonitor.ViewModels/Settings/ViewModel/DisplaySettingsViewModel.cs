namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using System.Windows.Input;
    using Contracts.Commands;
    using DataModel.BasicProperties;
    using DataModel.BasicProperties.FuelConsumption;
    using DataModel.BasicProperties.Units;
    using Factory;
    using Layouts;
    using Model;
    using Overrides;
    using PluginsSettings;

    using SecondMonitor.Contracts;

    public class DisplaySettingsViewModel : AbstractViewModel<DisplaySettings>
    {
        private readonly IViewModelFactory _viewModelFactory;

        private readonly PitStopTimeDisplayMap _pitStopTimeDisplayMap;

        private IUomOverride _activeUomOverride;
        private VelocityUnits _velocityUnits;
        private TemperatureUnits _temperatureUnits;
        private PressureUnits _pressureUnits;
        private VolumeUnits _volumeUnits;
        private FuelCalculationScope _fuelCalculationScope;
        private int _paceLaps;
        private int _refreshRate;
        private bool _scrollToPlayer;
        private ReportingSettingsViewModel _reportingSettingsViewModel;
        private bool _animateDriverPosition;
        private bool _animateDeltaTimes;

        private MapDisplaySettingsViewModel _mapDisplaySettingsViewModel;
        private TelemetrySettingsViewModel _telemetrySettingsViewModel;
        private MultiClassDisplayKind _multiClassDisplayKind;
        private ForceUnits _forceUnits;
        private AngleUnits _angleUnits;
        private PowerUnits _powerUnits;
        private TorqueUnits _torqueUnits;
        private bool _isGapVisualizationEnabled;
        private double _minimalGapForVisualization;
        private double _gapHeightForOneSecond;
        private double _maximumGapHeight;
        private RatingSettingsViewModel _ratingSettingsViewModel;
        private PitBoardSettingsViewModel _pitBoardSettingsViewModel;
        private TrackRecordsSettingsViewModel _trackRecordsSettingsViewModel;
        private bool _enablePedalInformation;
        private bool _enableTemperatureInformation;
        private bool _enableNonTemperatureInformation;
        private bool _enableCamberVisualization;
        private string _customResourcesPath;
        private int _driversUpdatedPerTick;
        private bool _isHwAccelerationEnabled;
        private LayoutSettingsViewModel _layoutSettingsViewModel;
        private IViewModel _layoutEditorViewModel;
        private bool _isForceSingleClassEnabled;
        private string _selectedPitStopDisplayMode;
        private bool _showPitStopTimeRelative;

        private int _maximumExtraFuel;
        private int _minimumExtraFuel;
        private int _racesToTrainFuel;
        private int _extraRaceLaps;
        private int _extraRaceMinutes;
        private int _extraContingencyFuel;
        private bool _overrideToPsiInAcc;
        private bool _paceContainsLapsToCatchEstimation;
        private int _gapClosingMaximumPositionDifference;
        private double _gapClosingOvertakeTimeLost;
        private int _gapClosingExtraLaps;
        private bool _gapComputeOnlyOnLapStart;
        private PitStopTimeDisplayKind _pitStopTimeDisplayKind;
        private WindowLocationSetting _windowLocationSetting;
        private WindowLocationSetting _mapWindowLocationSettings;
        private Func<LayoutDescription> _defaultLayoutCallback;
        private double _gapClosingInitialTime;
        private int _defaultRaceLaps = 0;
        private int _defaultRaceMinutes = 0;
        private bool _isRecentLowestSpeedEnabled;
        private bool _isRecentHighestSpeedEnabled;
        private int _lowestHighestSpeedTimeout;
        private int _hybridModeRelativeInFront;
        private int _hybridModeRelativeInBehind;
        private bool _isHybridModeGapEnabled;
        private int _hybridModeGapHeight;
        private bool _isBottomingOutIndicationEnabled;
        private Distance _bottomingOutHeight;
        private HumanReadableItem<SessionBestInformationKind> _selectedSessionBestInformationKind;

        public DisplaySettingsViewModel(IViewModelFactory viewModelFactory, List<IUomOverride> uomOverrides)
        {
            SessionBestInformationMap sessionBestInformationMap = new();
            _viewModelFactory = viewModelFactory;
            uomOverrides.ForEach(x => x.ActivationStateChanged += OnUomOverridesActivationStateChanged);
            SelectCustomResourceFileCommand = new RelayCommand(SelectCustomResourceFile);
            SessionOptionsViewModel = viewModelFactory.Create<SessionsOptionsViewModel>();
            PluginsConfigurationViewModel = viewModelFactory.Create<IPluginsConfigurationViewModel>();
            PitEstimationSettingsViewModel = viewModelFactory.Create<PitEstimationSettingsViewModel>();
            _pitStopTimeDisplayMap = new PitStopTimeDisplayMap();
            AllowedPitStopDisplayModes = _pitStopTimeDisplayMap.GetAllHumanReadableValue().ToArray();
            PracticeFuelSettings = viewModelFactory.Create<SessionFuelCalculationSettingsViewModel>();
            QualificationFuelSettings = viewModelFactory.Create<SessionFuelCalculationSettingsViewModel>();
            DashboardSettingsViewModel = viewModelFactory.Create<DashboardSettingsViewModel>();
            WeatherInfoSettings = viewModelFactory.Create<WeatherInfoSettingsViewModel>();
            AllSessionBestInformationKind = sessionBestInformationMap.GetAllItems().ToList();
            SimsAutoDeleteSettingsViewModel = viewModelFactory.Create<SimsAutoDeleteSettingsViewModel>();
            ChampionshipSettingViewModel = viewModelFactory.Create<ChampionshipSettingViewModel>();
            RestApiSettingsViewModel = viewModelFactory.Create<RestApiSettingsViewModel>();
            DeltaLapSettingViewModel = viewModelFactory.Create<DeltaLapSettingViewModel>();
            TimedRaceSessionFuelSettings = viewModelFactory.Create<RaceSessionFuelCalculationSettingsViewModel>();
            LapRaceSessionFuelSetting = viewModelFactory.Create<RaceSessionFuelCalculationSettingsViewModel>();
        }

        public WeatherInfoSettingsViewModel WeatherInfoSettings { get; }

        public SessionFuelCalculationSettingsViewModel PracticeFuelSettings { get; }

        public SessionFuelCalculationSettingsViewModel QualificationFuelSettings { get; }

        public RaceSessionFuelCalculationSettingsViewModel TimedRaceSessionFuelSettings { get; }

        public RaceSessionFuelCalculationSettingsViewModel LapRaceSessionFuelSetting { get; }

        public DashboardSettingsViewModel DashboardSettingsViewModel { get; }

        public SimsAutoDeleteSettingsViewModel SimsAutoDeleteSettingsViewModel { get; }

        public ICommand OpenLogDirectoryCommand => new RelayCommand(OpenLogDirectory);

        public PitEstimationSettingsViewModel PitEstimationSettingsViewModel { get; }

        public ChampionshipSettingViewModel ChampionshipSettingViewModel { get; }

        public RestApiSettingsViewModel RestApiSettingsViewModel { get; }

        public TelemetrySettingsViewModel TelemetrySettingsViewModel
        {
            get => _telemetrySettingsViewModel;
            set => SetProperty(ref _telemetrySettingsViewModel, value);
        }

        public TemperatureUnits TemperatureUnits
        {
            get => _temperatureUnits;
            set => SetProperty(ref _temperatureUnits, value);
        }

        public string[] AllowedPitStopDisplayModes { get; }

        public string SelectedPitStopDisplayMode
        {
            get => _selectedPitStopDisplayMode;
            set
            {
                SetProperty(ref _selectedPitStopDisplayMode, value);
                PitStopTimeDisplayKind = _pitStopTimeDisplayMap.FromHumanReadable(value);
            }
        }

        public PitStopTimeDisplayKind PitStopTimeDisplayKind
        {
            get => _pitStopTimeDisplayKind;
            private set => SetProperty(ref _pitStopTimeDisplayKind, value);
        }

        public MultiClassDisplayKind MultiClassDisplayKind
        {
            get => _multiClassDisplayKind;
            set => SetProperty(ref _multiClassDisplayKind, value);
        }

        public bool OverrideToPsiInAcc
        {
            get => _overrideToPsiInAcc;
            set => SetProperty(ref _overrideToPsiInAcc, value);
        }

        public PressureUnits DisplayPressureUnits => _activeUomOverride?.GetPressureUnits(this, _pressureUnits) ?? _pressureUnits;

        public bool IsRecentLowestSpeedEnabled
        {
            get => _isRecentLowestSpeedEnabled;
            set => SetProperty(ref _isRecentLowestSpeedEnabled, value);
        }

        public bool IsRecentHighestSpeedEnabled
        {
            get => _isRecentHighestSpeedEnabled;
            set => SetProperty(ref _isRecentHighestSpeedEnabled, value);
        }

        public int LowestHighestSpeedTimeout
        {
            get => _lowestHighestSpeedTimeout;
            set => SetProperty(ref _lowestHighestSpeedTimeout, value);
        }

        public PressureUnits PressureUnits
        {
            get => _pressureUnits;
            set => SetProperty(ref _pressureUnits, value);
        }

        public VolumeUnits VolumeUnits
        {
            get => _volumeUnits;
            set => SetProperty(ref _volumeUnits, value);
        }

        public bool IsBottomingOutIndicationEnabled
        {
            get => _isBottomingOutIndicationEnabled;
            set => SetProperty(ref _isBottomingOutIndicationEnabled, value);
        }

        public Distance BottomingOutHeight
        {
            get => _bottomingOutHeight;
            set => SetProperty(ref _bottomingOutHeight, value);
        }

        public VelocityUnits VelocityUnits
        {
            get => _velocityUnits;
            set
            {
                SetProperty(ref _velocityUnits, value);
                NotifyPropertyChanged(nameof(DistanceUnits));
                NotifyPropertyChanged(nameof(FuelPerDistanceUnits));
            }
        }

        public ForceUnits ForceUnits
        {
            get => _forceUnits;
            set => SetProperty(ref _forceUnits, value);
        }

        public AngleUnits AngleUnits
        {
            get => _angleUnits;
            set => SetProperty(ref _angleUnits, value);
        }

        public TorqueUnits TorqueUnits
        {
            get => _torqueUnits;
            set => SetProperty(ref _torqueUnits, value);
        }

        public PowerUnits PowerUnits
        {
            get => _powerUnits;
            set => SetProperty(ref _powerUnits, value);
        }

        public FuelCalculationScope FuelCalculationScope
        {
            get => _fuelCalculationScope;
            set => SetProperty(ref _fuelCalculationScope, value);
        }

        public SessionsOptionsViewModel SessionOptionsViewModel { get; }

        public bool EnablePedalInformation
        {
            get => _enablePedalInformation;
            set => SetProperty(ref _enablePedalInformation, value);
        }

        public bool EnableTemperatureInformation
        {
            get => _enableTemperatureInformation;
            set => SetProperty(ref _enableTemperatureInformation, value);
        }

        public bool EnableNonTemperatureInformation
        {
            get => _enableNonTemperatureInformation;
            set => SetProperty(ref _enableNonTemperatureInformation, value);
        }

        public DistanceUnits DistanceUnits
        {
            get
            {
                return VelocityUnits switch
                {
                    VelocityUnits.Kph => DistanceUnits.Kilometers,
                    VelocityUnits.Mph => DistanceUnits.Miles,
                    VelocityUnits.Ms => DistanceUnits.Meters,
                    _ => DistanceUnits.Kilometers
                };
            }
        }

        public DistanceUnits DistanceUnitsSmall
        {
            get
            {
                return VelocityUnits switch
                {
                    VelocityUnits.Kph => DistanceUnits.Meters,
                    VelocityUnits.Mph => DistanceUnits.Yards,
                    VelocityUnits.Ms => DistanceUnits.Meters,
                    _ => DistanceUnits.Meters
                };
            }
        }

        public DistanceUnits DistanceUnitsVerySmall
        {
            get
            {
                return VelocityUnits switch
                {
                    VelocityUnits.Kph => DistanceUnits.Millimeter,
                    VelocityUnits.Mph => DistanceUnits.Inches,
                    VelocityUnits.Ms => DistanceUnits.Millimeter,
                    _ => DistanceUnits.Millimeter
                };
            }
        }

        public VelocityUnits VelocityUnitsVerySmall
        {
            get
            {
                return VelocityUnits switch
                {
                    VelocityUnits.Kph => VelocityUnits.MMPerSecond,
                    VelocityUnits.Mph => VelocityUnits.InPerSecond,
                    VelocityUnits.Ms => VelocityUnits.MMPerSecond,
                    VelocityUnits.Fps => VelocityUnits.InPerSecond,
                    VelocityUnits.CmPerSecond => VelocityUnits.MMPerSecond,
                    VelocityUnits.InPerSecond => VelocityUnits.InPerSecond,
                    _ => VelocityUnits.MMPerSecond
                };
            }
        }

        public FuelPerDistanceUnits FuelPerDistanceUnits
        {
            get
            {
                return VelocityUnits switch
                {
                    VelocityUnits.Kph => FuelPerDistanceUnits.LitersPerHundredKm,
                    VelocityUnits.Mph => FuelPerDistanceUnits.MilesPerGallon,
                    VelocityUnits.Ms => FuelPerDistanceUnits.LitersPerHundredKm,
                    _ => FuelPerDistanceUnits.LitersPerHundredKm
                };
            }
        }

        public bool ShowPitStopTimeRelative
        {
            get => _showPitStopTimeRelative;
            set => SetProperty(ref _showPitStopTimeRelative, value);
        }

        public int PaceLaps
        {
            get => _paceLaps;
            set => SetProperty(ref _paceLaps, value);
        }

        public bool PaceContainsLapsToCatchEstimation
        {
            get => _paceContainsLapsToCatchEstimation;
            set => SetProperty(ref _paceContainsLapsToCatchEstimation, value);
        }

        public int RefreshRate
        {
            get => _refreshRate;
            set => SetProperty(ref _refreshRate, value);
        }

        public bool ScrollToPlayer
        {
            get => _scrollToPlayer;
            set => SetProperty(ref _scrollToPlayer, value);
        }

        public bool AnimateDriversPosition
        {
            get => _animateDriverPosition;
            set => SetProperty(ref _animateDriverPosition, value);
        }

        public bool AnimateDeltaTimes
        {
            get => _animateDeltaTimes;
            set => SetProperty(ref _animateDeltaTimes, value);
        }

        public ReportingSettingsViewModel ReportingSettingsViewModel
        {
            get => _reportingSettingsViewModel;
            set => SetProperty(ref _reportingSettingsViewModel, value);
        }

        public MapDisplaySettingsViewModel MapDisplaySettingsViewModel
        {
            get => _mapDisplaySettingsViewModel;
            set => SetProperty(ref _mapDisplaySettingsViewModel, value);
        }

        public bool IsGapVisualizationEnabled
        {
            get => _isGapVisualizationEnabled;
            set => SetProperty(ref _isGapVisualizationEnabled, value);
        }

        public double MinimalGapForVisualization
        {
            get => _minimalGapForVisualization;
            set => SetProperty(ref _minimalGapForVisualization, value);
        }

        public double GapHeightForOneSecond
        {
            get => _gapHeightForOneSecond;
            set => SetProperty(ref _gapHeightForOneSecond, value);
        }

        public double MaximumGapHeight
        {
            get => _maximumGapHeight;
            set => SetProperty(ref _maximumGapHeight, value);
        }

        public RatingSettingsViewModel RatingSettingsViewModel
        {
            get => _ratingSettingsViewModel;
            set => SetProperty(ref _ratingSettingsViewModel, value);
        }

        public PitBoardSettingsViewModel PitBoardSettingsViewModel
        {
            get => _pitBoardSettingsViewModel;
            set => SetProperty(ref _pitBoardSettingsViewModel, value);
        }

        public TrackRecordsSettingsViewModel TrackRecordsSettingsViewModel
        {
            get => _trackRecordsSettingsViewModel;
            set => SetProperty(ref _trackRecordsSettingsViewModel, value);
        }

        public WindowLocationSetting WindowLocationSetting
        {
            get => _windowLocationSetting;
            set => SetProperty(ref _windowLocationSetting, value);
        }

        public WindowLocationSetting MapWindowLocationSettings
        {
            get => _mapWindowLocationSettings;
            set => SetProperty(ref _mapWindowLocationSettings, value);
        }

        public bool EnableCamberVisualization
        {
            get => _enableCamberVisualization;
            set => SetProperty(ref _enableCamberVisualization, value);
        }

        public string CustomResourcesPath
        {
            get => _customResourcesPath;
            set => SetProperty(ref _customResourcesPath, value);
        }

        public int DriversUpdatedPerTick
        {
            get => _driversUpdatedPerTick;
            set => SetProperty(ref _driversUpdatedPerTick, value);
        }

        public bool IsHwAccelerationEnabled
        {
            get => _isHwAccelerationEnabled;
            set => SetProperty(ref _isHwAccelerationEnabled, value);
        }

        public LayoutSettingsViewModel LayoutSettingsViewModel
        {
            get => _layoutSettingsViewModel;
            set => SetProperty(ref _layoutSettingsViewModel, value);
        }

        public IViewModel LayoutEditorViewModel
        {
            get => _layoutEditorViewModel;
            set => SetProperty(ref _layoutEditorViewModel, value);
        }

        public Func<LayoutDescription> DefaultLayoutCallback
        {
            get => _defaultLayoutCallback;
            set => SetProperty(ref _defaultLayoutCallback, value);
        }

        public bool IsForceSingleClassEnabled
        {
            get => _isForceSingleClassEnabled;
            set => SetProperty(ref _isForceSingleClassEnabled, value);
        }

        public IPluginsConfigurationViewModel PluginsConfigurationViewModel { get; }

        public ICommand SelectCustomResourceFileCommand { get; }

        public int MaximumExtraFuel
        {
            get => _maximumExtraFuel;
            set => SetProperty(ref _maximumExtraFuel, value);
        }

        public int MinimumExtraFuel
        {
            get => _minimumExtraFuel;
            set => SetProperty(ref _minimumExtraFuel, value);
        }

        public int RacesToTrainFuel
        {
            get => _racesToTrainFuel;
            set => SetProperty(ref _racesToTrainFuel, value);
        }

        public int ExtraRaceLaps
        {
            get => _extraRaceLaps;
            set => SetProperty(ref _extraRaceLaps, value);
        }

        public int ExtraRaceMinutes
        {
            get => _extraRaceMinutes;
            set => SetProperty(ref _extraRaceMinutes, value);
        }

        public int ExtraContingencyFuel
        {
            get => _extraContingencyFuel;
            set => SetProperty(ref _extraContingencyFuel, value);
        }

        public int GapClosingMaximumPositionDifference
        {
            get => _gapClosingMaximumPositionDifference;
            set => SetProperty(ref _gapClosingMaximumPositionDifference, value);
        }

        public double GapClosingOvertakeTimeLost
        {
            get => _gapClosingOvertakeTimeLost;
            set => SetProperty(ref _gapClosingOvertakeTimeLost, value);
        }

        public int GapClosingExtraLaps
        {
            get => _gapClosingExtraLaps;
            set => SetProperty(ref _gapClosingExtraLaps, value);
        }

        public double GapClosingInitialTime
        {
            get => _gapClosingInitialTime;
            set => SetProperty(ref _gapClosingInitialTime, value);
        }

        public bool GapComputeOnlyOnLapStart
        {
            get => _gapComputeOnlyOnLapStart;
            set => SetProperty(ref _gapComputeOnlyOnLapStart, value);
        }

        public int DefaultRaceLaps
        {
            get => _defaultRaceLaps;
            set => SetProperty(ref _defaultRaceLaps, value);
        }

        public int DefaultRaceMinutes
        {
            get => _defaultRaceMinutes;
            set => SetProperty(ref _defaultRaceMinutes, value);
        }

        public int HybridModeRelativeInFront
        {
            get => _hybridModeRelativeInFront;
            set => SetProperty(ref _hybridModeRelativeInFront, value);
        }

        public int HybridModeRelativeInBehind
        {
            get => _hybridModeRelativeInBehind;
            set => SetProperty(ref _hybridModeRelativeInBehind, value);
        }

        public bool IsHybridModeGapEnabled
        {
            get => _isHybridModeGapEnabled;
            set => SetProperty(ref _isHybridModeGapEnabled, value);
        }

        public int HybridModeGapHeight
        {
            get => _hybridModeGapHeight;
            set => SetProperty(ref _hybridModeGapHeight, value);
        }

        public HumanReadableItem<SessionBestInformationKind> SelectedSessionBestInformationKind
        {
            get => _selectedSessionBestInformationKind;
            set => SetProperty(ref _selectedSessionBestInformationKind, value);
        }

        public List<HumanReadableItem<SessionBestInformationKind>> AllSessionBestInformationKind { get; }

        public DeltaLapSettingViewModel DeltaLapSettingViewModel { get; }

        protected override void ApplyModel(DisplaySettings settings)
        {
            TemperatureUnits = settings.TemperatureUnits;
            PressureUnits = settings.PressureUnits;
            OverrideToPsiInAcc = settings.OverrideToPsiInAcc;
            VolumeUnits = settings.VolumeUnits;
            VelocityUnits = settings.VelocityUnits;
            FuelCalculationScope = settings.FuelCalculationScope;
            PaceLaps = settings.PaceLaps;
            PaceContainsLapsToCatchEstimation = settings.PaceContainsLapsToCatchEstimation;
            RefreshRate = settings.RefreshRate;
            ScrollToPlayer = settings.ScrollToPlayer;
            AnimateDeltaTimes = settings.AnimateDeltaTimes;
            AnimateDriversPosition = settings.AnimateDriversPosition;
            MultiClassDisplayKind = settings.MultiClassDisplayKind;
            ForceUnits = settings.ForceUnits;
            AngleUnits = settings.AngleUnits;

            IsGapVisualizationEnabled = settings.IsGapVisualizationEnabled;
            MinimalGapForVisualization = settings.MinimalGapForVisualization;
            MaximumGapHeight = settings.MaximumGapHeight;
            GapHeightForOneSecond = settings.GapHeightForOneSecond;

            MapDisplaySettingsViewModel = new MapDisplaySettingsViewModel();
            MapDisplaySettingsViewModel.FromModel(settings.MapDisplaySettings);

            SessionOptionsViewModel.FromModel(settings);

            ReportingSettingsViewModel = new ReportingSettingsViewModel();
            ReportingSettingsViewModel.FromModel(settings.ReportingSettings);

            TelemetrySettingsViewModel = _viewModelFactory.CreateAndApply<TelemetrySettingsViewModel, TelemetrySettings>(settings.TelemetrySettings);
            WindowLocationSetting = settings.WindowLocationSetting;
            MapWindowLocationSettings = settings.MapWindowLocationSettings;

            RatingSettingsViewModel = new RatingSettingsViewModel();
            RatingSettingsViewModel.FromModel(settings.RatingSettings);

            PitBoardSettingsViewModel = new PitBoardSettingsViewModel();
            PitBoardSettingsViewModel.FromModel(settings.PitBoardSettings);

            TrackRecordsSettingsViewModel = new TrackRecordsSettingsViewModel();
            TrackRecordsSettingsViewModel.FromModel(settings.TrackRecordsSettings);

            PowerUnits = settings.PowerUnits;
            TorqueUnits = settings.TorqueUnits;

            EnablePedalInformation = settings.EnablePedalInformation;
            EnableNonTemperatureInformation = settings.EnableNonTemperatureInformation;
            EnableTemperatureInformation = settings.EnableTemperatureInformation;
            EnableCamberVisualization = settings.EnableCamberVisualization;
            CustomResourcesPath = settings.CustomResourcesPath;
            DriversUpdatedPerTick = settings.DriversUpdatedPerTick;
            IsHwAccelerationEnabled = settings.IsHwAccelerationEnabled;

            LayoutSettingsViewModel = new LayoutSettingsViewModel();
            LayoutSettingsViewModel.FromModel(settings.LayoutSettings);

            IsForceSingleClassEnabled = settings.IsForceSingeClassEnabled;
            SelectedPitStopDisplayMode = _pitStopTimeDisplayMap.ToHumanReadable(settings.PitStopTimeDisplayKind);
            ShowPitStopTimeRelative = settings.ShowPitStopTimeRelative;

            PitEstimationSettingsViewModel.FromModel(settings.PitEstimationSettings);

            MaximumExtraFuel = settings.MaximumExtraFuel;
            MinimumExtraFuel = settings.MinimumExtraFuel;
            RacesToTrainFuel = settings.RacesToTrainFuel;

            DefaultRaceLaps = settings.DefaultRaceLaps;
            DefaultRaceMinutes = settings.DefaultRaceMinutes;

            ExtraContingencyFuel = settings.ExtraContingencyFuel;
            ExtraRaceLaps = settings.ExtraRaceLaps;
            ExtraRaceMinutes = settings.ExtraRaceMinutes;
            GapClosingExtraLaps = settings.GapClosingExtraLaps;
            GapClosingOvertakeTimeLost = settings.GapClosingOvertakeTimeLost;
            GapClosingMaximumPositionDifference = settings.GapClosingMaximumPositionDifference;
            GapClosingInitialTime = settings.GapClosingInitialTime;
            GapComputeOnlyOnLapStart = settings.GapComputeOnlyOnLapStart;
            PracticeFuelSettings.FromModel(settings.PracticeFuelSettings);
            QualificationFuelSettings.FromModel(settings.QualificationFuelSettings);

            IsRecentLowestSpeedEnabled = settings.IsRecentLowestSpeedEnabled;
            IsRecentHighestSpeedEnabled = settings.IsRecentHighestSpeedEnabled;
            LowestHighestSpeedTimeout = settings.LowestHighestSpeedTimeout;

            HybridModeRelativeInFront = settings.HybridModeRelativeInFront;
            HybridModeRelativeInBehind = settings.HybridModeRelativeInBehind;
            HybridModeGapHeight = settings.HybridModeGapHeight;
            IsHybridModeGapEnabled = settings.IsHybridModeGapEnabled;
            DashboardSettingsViewModel.FromModel(settings.DashboardSettings);
            WeatherInfoSettings.FromModel(settings.WeatherInfoSettings);
            BottomingOutHeight = settings.BottomingOutHeight;
            IsBottomingOutIndicationEnabled = settings.IsBottomingOutIndicationEnabled;
            SelectedSessionBestInformationKind = AllSessionBestInformationKind.Single(x => x.Value == settings.SessionBestInformationKind);
            SimsAutoDeleteSettingsViewModel.FromModel(settings.SimsAutoDeleteSettings);
            ChampionshipSettingViewModel.FromModel(settings.ChampionshipSettings);
            RestApiSettingsViewModel.FromModel(settings.RestApiSettings);
            DeltaLapSettingViewModel.FromModel(settings.DeltaLapSetting);
            TimedRaceSessionFuelSettings.FromModel(settings.TimedSessionFuelSettings);
            LapRaceSessionFuelSetting.FromModel(settings.LapSessionFuelSetting);
        }

        public override DisplaySettings SaveToNewModel()
        {
            var displaySettings = new DisplaySettings()
            {
                TemperatureUnits = TemperatureUnits,
                PressureUnits = PressureUnits,
                OverrideToPsiInAcc = OverrideToPsiInAcc,
                VolumeUnits = VolumeUnits,
                VelocityUnits = VelocityUnits,
                FuelCalculationScope = FuelCalculationScope,
                PaceLaps = PaceLaps,
                PaceContainsLapsToCatchEstimation = PaceContainsLapsToCatchEstimation,
                RefreshRate = RefreshRate,
                ScrollToPlayer = ScrollToPlayer,
                ReportingSettings = ReportingSettingsViewModel.ToModel(),
                AnimateDriversPosition = AnimateDriversPosition,
                AnimateDeltaTimes = AnimateDeltaTimes,
                MapDisplaySettings = MapDisplaySettingsViewModel.SaveToNewModel(),
                TelemetrySettings = TelemetrySettingsViewModel.SaveToNewModel(),
                MultiClassDisplayKind = MultiClassDisplayKind,
                ForceUnits = ForceUnits,
                AngleUnits = AngleUnits,
                IsGapVisualizationEnabled = IsGapVisualizationEnabled,
                MinimalGapForVisualization = MinimalGapForVisualization,
                MaximumGapHeight = MaximumGapHeight,
                GapHeightForOneSecond = GapHeightForOneSecond,
                WindowLocationSetting = WindowLocationSetting,
                MapWindowLocationSettings = MapWindowLocationSettings,
                RatingSettings = RatingSettingsViewModel.SaveToNewModel(),
                PitBoardSettings = PitBoardSettingsViewModel.SaveToNewModel(),
                TrackRecordsSettings = TrackRecordsSettingsViewModel.SaveToNewModel(),
                PowerUnits = PowerUnits,
                TorqueUnits = TorqueUnits,
                EnablePedalInformation = EnablePedalInformation,
                EnableTemperatureInformation = EnableTemperatureInformation,
                EnableNonTemperatureInformation = EnableNonTemperatureInformation,
                EnableCamberVisualization = EnableCamberVisualization,
                CustomResourcesPath = CustomResourcesPath,
                DriversUpdatedPerTick = DriversUpdatedPerTick,
                IsHwAccelerationEnabled = IsHwAccelerationEnabled,
                LayoutSettings = LayoutSettingsViewModel.SaveToNewModel(),
                IsForceSingeClassEnabled = IsForceSingleClassEnabled,
                PitStopTimeDisplayKind = _pitStopTimeDisplayMap.FromHumanReadable(SelectedPitStopDisplayMode),
                ShowPitStopTimeRelative = ShowPitStopTimeRelative,
                PitEstimationSettings = PitEstimationSettingsViewModel.SaveToNewModel(),
                MaximumExtraFuel = MaximumExtraFuel,
                MinimumExtraFuel = MinimumExtraFuel,
                RacesToTrainFuel = RacesToTrainFuel,
                ExtraContingencyFuel = ExtraContingencyFuel,
                ExtraRaceLaps = ExtraRaceLaps,
                ExtraRaceMinutes = ExtraRaceMinutes,
                DefaultRaceLaps = DefaultRaceLaps,
                DefaultRaceMinutes = DefaultRaceMinutes,
                GapClosingExtraLaps = GapClosingExtraLaps,
                GapClosingMaximumPositionDifference = GapClosingMaximumPositionDifference,
                GapClosingOvertakeTimeLost = GapClosingOvertakeTimeLost,
                GapClosingInitialTime = GapClosingInitialTime,
                PracticeFuelSettings = PracticeFuelSettings.SaveToNewModel(),
                QualificationFuelSettings = QualificationFuelSettings.SaveToNewModel(),
                IsRecentLowestSpeedEnabled = IsRecentLowestSpeedEnabled,
                IsRecentHighestSpeedEnabled = IsRecentHighestSpeedEnabled,
                LowestHighestSpeedTimeout = LowestHighestSpeedTimeout,
                HybridModeRelativeInFront = HybridModeRelativeInFront,
                HybridModeRelativeInBehind = HybridModeRelativeInBehind,
                HybridModeGapHeight = HybridModeGapHeight,
                IsHybridModeGapEnabled = IsHybridModeGapEnabled,
                DashboardSettings = DashboardSettingsViewModel.SaveToNewModel(),
                GapComputeOnlyOnLapStart = GapComputeOnlyOnLapStart,
                WeatherInfoSettings = WeatherInfoSettings.SaveToNewModel(),
                BottomingOutHeight = BottomingOutHeight,
                IsBottomingOutIndicationEnabled = IsBottomingOutIndicationEnabled,
                SessionBestInformationKind = SelectedSessionBestInformationKind.Value,
                SimsAutoDeleteSettings = SimsAutoDeleteSettingsViewModel.SaveToNewModel(),
                ChampionshipSettings = ChampionshipSettingViewModel.SaveToNewModel(),
                RestApiSettings = RestApiSettingsViewModel.SaveToNewModel(),
                DeltaLapSetting = DeltaLapSettingViewModel.SaveToNewModel(),
                LapSessionFuelSetting = LapRaceSessionFuelSetting.SaveToNewModel(),
                TimedSessionFuelSettings = TimedRaceSessionFuelSettings.SaveToNewModel(),
            };

            SessionOptionsViewModel.ApplyToModel(displaySettings);
            return displaySettings;
        }

        private void SelectCustomResourceFile()
        {
            using (OpenFileDialog fbd = new OpenFileDialog())
            {
                fbd.DefaultExt = ".xaml";
                fbd.Filter = @"xaml Files (*.xaml)|*.xaml";
                fbd.CheckFileExists = true;
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.FileName))
                {
                    CustomResourcesPath = fbd.FileName;
                }
            }
        }

        private void OnUomOverridesActivationStateChanged(object sender, ActivationStateEventArgs e)
        {
            if (sender is not IUomOverride senderUomOverride)
            {
                return;
            }

            if (e.IsActive)
            {
                _activeUomOverride = senderUomOverride;
                NotifyPropertyChanged(string.Empty);
                return;
            }

            if (senderUomOverride != _activeUomOverride)
            {
                return;
            }

            _activeUomOverride = null;
            NotifyPropertyChanged(string.Empty);
        }

        private void OpenLogDirectory()
        {
            string reportDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SecondMonitor");
            Task.Run(
                () => { Process.Start(new ProcessStartInfo("explorer.exe", reportDirectory) { UseShellExecute = true }); });
        }

        public LayoutDescription GetDefaultLayout()
        {
            if (DefaultLayoutCallback == null)
            {
                throw new InvalidOperationException("Default Layout callback not initialized.");
            }

            return DefaultLayoutCallback();
        }

        public UnitsCollection GetUnitsCollection()
        {
            return new UnitsCollection()
            {
                DistanceUnitsVerySmall = DistanceUnitsVerySmall,
                DistanceUnits = DistanceUnitsSmall,
                VelocityUnits = VelocityUnits,
                VelocityUnitsSmall = VelocityUnitsVerySmall,
                TemperatureUnits = TemperatureUnits,
                PressureUnits = DisplayPressureUnits,
                AngleUnits = AngleUnits,
                ForceUnits = ForceUnits,
            };
        }
    }
}