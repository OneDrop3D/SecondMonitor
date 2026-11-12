namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Providers
{
    using Controllers.Settings;
    using Controllers.Synchronization;
    using Extractors;
    using ViewModels.LoadedLapCache;

    public class RideHeightToLateralAccProvider : AbstractWheelChartProvider
    {
        public RideHeightToLateralAccProvider(LateralAccelerationToRideHeightExtractor dataExtractor, ILoadedLapsCache loadedLaps, IDataPointSelectionSynchronization dataPointSelectionSynchronization, ISettingsController settingsController) : base(dataExtractor, loadedLaps, dataPointSelectionSynchronization, settingsController)
        {
        }

        public override string ChartName => "Ride Height / Lateral Acceleration";
    }
}