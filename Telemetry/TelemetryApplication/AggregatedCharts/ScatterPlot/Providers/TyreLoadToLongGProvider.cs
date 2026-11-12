namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Providers
{
    using Controllers.Settings;
    using Controllers.Synchronization;
    using Extractors;
    using ViewModels.LoadedLapCache;

    public class TyreLoadToLongGProvider : AbstractWheelChartProvider
    {
        public TyreLoadToLongGProvider(TyreLoadToLongGExtractor dataExtractor, ILoadedLapsCache loadedLaps, IDataPointSelectionSynchronization dataPointSelectionSynchronization, ISettingsController settingsController) : base(dataExtractor, loadedLaps, dataPointSelectionSynchronization, settingsController)
        {
        }

        public override string ChartName => "Tyre Load / Longitudinal Acceleration ";
    }
}