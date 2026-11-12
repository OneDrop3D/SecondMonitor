namespace SecondMonitor.ViewModels.Settings.Model
{
    using System;
    using DataModel.BasicProperties;
    using DataModel.BasicProperties.Units;

    using SecondMonitor.DataModel.Dtos;
    using SecondMonitor.ViewModels.Settings.Model.Layout;

    [Serializable]
    public class DisplaySettings : IDtoWithVersion
    {
        public DisplaySettings()
        {
        }

        public int Version { get; set; } = 1;

        public TemperatureUnits TemperatureUnits { get; set; } = TemperatureUnits.Celsius;

        public bool OverrideToPsiInAcc { get; set; } = true;

        public PressureUnits PressureUnits { get; set; } = PressureUnits.Kpa;

        public VolumeUnits VolumeUnits { get; set; } = VolumeUnits.Liters;

        public VelocityUnits VelocityUnits { get; set; } = VelocityUnits.Kph;

        public ForceUnits ForceUnits { get; set; } = ForceUnits.Newtons;

        public AngleUnits AngleUnits { get; set; } = AngleUnits.Degrees;

        public MultiClassDisplayKind MultiClassDisplayKind { get; set; } = MultiClassDisplayKind.ClassFirst;

        public int SessionBestSwitchAfterSeconds { get; set; } = 20;

        public SessionBestInformationKind SessionBestInformationKind { get; set; } = SessionBestInformationKind.BestOfClass;

        public DeltaLapSetting DeltaLapSetting { get; set; } = new DeltaLapSetting();

        public FuelCalculationScope FuelCalculationScope { get; set; } = FuelCalculationScope.Lap;

        public TorqueUnits TorqueUnits { get; set; } = TorqueUnits.Nm;

        public PowerUnits PowerUnits { get; set; } = PowerUnits.KW;

        public PitStopTimeDisplayKind PitStopTimeDisplayKind { get; set; } = PitStopTimeDisplayKind.Both;

        public bool ShowPitStopTimeRelative { get; set; } = true;

        public int PaceLaps { get; set; } = 3;

        public bool PaceContainsLapsToCatchEstimation { get; set; } = true;

        public int RefreshRate { get; set; } = 300;

        public bool ScrollToPlayer { get; set; } = true;

        public bool AnimateDriversPosition { get; set; } = false;

        public bool AnimateDeltaTimes { get; set; } = false;

        public bool IsGapVisualizationEnabled { get; set; } = false;

        public double MinimalGapForVisualization { get; set; } = 2;

        public double GapHeightForOneSecond { get; set; } = 25;

        public double MaximumGapHeight { get; set; } = 150;

        public bool EnablePedalInformation { get; set; } = true;

        public bool EnableTemperatureInformation { get; set; } = true;

        public bool EnableNonTemperatureInformation { get; set; } = true;

        public bool IsRecentLowestSpeedEnabled { get; set; } = true;

        public bool IsRecentHighestSpeedEnabled { get; set; } = true;

        public int LowestHighestSpeedTimeout { get; set; } = 10;

        public SessionOptions PracticeOptions { get; set; } = new() { OrderingDisplayMode = DriverOrderKind.AbsoluteByClass, TimesDisplayMode = DisplayModeEnum.Absolute, SessionName = "Practice" };

        public SessionOptions QualificationOptions { get; set; } = new() { OrderingDisplayMode = DriverOrderKind.AbsoluteByClass, TimesDisplayMode = DisplayModeEnum.Absolute, SessionName = "Quali" };

        public SessionOptions RaceOptions { get; set; } = new() { OrderingDisplayMode = DriverOrderKind.Hybrid, TimesDisplayMode = DisplayModeEnum.Relative, SessionName = "Race" };

        public ReportingSettings ReportingSettings { get; set; } = new();

        public MapDisplaySettings MapDisplaySettings { get; set; } = new();

        public TelemetrySettings TelemetrySettings { get; set; } = new();

        public WindowLocationSetting WindowLocationSetting { get; set; }

        public WindowLocationSetting MapWindowLocationSettings { get; set; }

        public RatingSettings RatingSettings { get; set; } = new();

        public PitBoardSettings PitBoardSettings { get; set; } = new();

        public TrackRecordsSettings TrackRecordsSettings { get; set; } = new();

        public DashboardSettings DashboardSettings { get; set; } = new();

        public bool EnableCamberVisualization { get; set; } = false;

        public string CustomResourcesPath { get; set; } = string.Empty;

        public int DriversUpdatedPerTick { get; set; } = 10;

        public bool IsHwAccelerationEnabled { get; set; } = false;

        public LayoutSettings LayoutSettings { get; set; } = new();

        public bool IsForceSingeClassEnabled { get; set; } = false;

        public PitEstimationSettings PitEstimationSettings { get; set; } = new();

        public int MaximumExtraFuel { get; set; } = 15;

        public int MinimumExtraFuel { get; set; } = 5;

        public int RacesToTrainFuel { get; set; } = 5;

        public int DefaultRaceLaps { get; set; } = 0;

        public int DefaultRaceMinutes { get; set; } = 0;

        public int ExtraRaceLaps { get; set; } = 1;
        public int ExtraRaceMinutes { get; set; } = 0;
        public int ExtraContingencyFuel { get; set; } = 5;
        public int GapClosingExtraLaps { get; set; } = 5;
        public double GapClosingOvertakeTimeLost { get; set; } = 2.0;
        public int GapClosingMaximumPositionDifference { get; set; } = 4;
        public double GapClosingInitialTime { get; set; } = 5.0;
        public bool GapComputeOnlyOnLapStart { get; set; } = true;

        public SessionFuelCalculationSettings PracticeFuelSettings { get; set; } = new SessionFuelCalculationSettings(true, true, 0, 20);
        public SessionFuelCalculationSettings QualificationFuelSettings { get; set; } = new SessionFuelCalculationSettings(true, true, 4, 1);

        public RaceSessionFuelCalculationSettings TimedSessionFuelSettings { get; set; } = new RaceSessionFuelCalculationSettings(0, 0);

        public RaceSessionFuelCalculationSettings LapSessionFuelSetting { get; set; } = new RaceSessionFuelCalculationSettings(0, 5);

        public int HybridModeRelativeInFront { get; set; } = 8;
        public int HybridModeRelativeInBehind { get; set; } = 8;
        public bool IsHybridModeGapEnabled { get; set; } = true;
        public int HybridModeGapHeight { get; set; } = 30;

        public WeatherInfoSettings WeatherInfoSettings { get; set; } = new WeatherInfoSettings();

        public bool IsBottomingOutIndicationEnabled { get; set; } = true;

        public Distance BottomingOutHeight { get; set; } = Distance.FromCentimeters(0.1);

        public SimsAutoDeleteSettings SimsAutoDeleteSettings { get; set; } = new();

        public ChampionshipSettings ChampionshipSettings { get; set; } = new();

        public RestApiSettings RestApiSettings { get; set; } = new();
    }
}
