namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Providers
{
    using Controllers.Settings;
    using Controllers.Synchronization;
    using Extractors;
    using ViewModels.LoadedLapCache;

    public class SpeedToSuspensionTravelProvider : AbstractWheelChartProvider
    {
        public SpeedToSuspensionTravelProvider(SpeedToSuspensionTravelExtractor dataExtractor, ILoadedLapsCache loadedLaps, IDataPointSelectionSynchronization dataPointSelectionSynchronization, ISettingsController settingsController) : base(dataExtractor, loadedLaps, dataPointSelectionSynchronization, settingsController)
        {
        }

        public override string ChartName => "Suspension Travel / Speed";
    }
}