namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Providers
{
    using Controllers.Settings;
    using Controllers.Synchronization;
    using Extractors;
    using ViewModels.LoadedLapCache;

    public class TyreLoadToLatGProvider : AbstractWheelChartProvider
    {
        public TyreLoadToLatGProvider(TyreLoadToLatGExtractor dataExtractor, ILoadedLapsCache loadedLaps, IDataPointSelectionSynchronization dataPointSelectionSynchronization, ISettingsController settingsController) : base(dataExtractor, loadedLaps, dataPointSelectionSynchronization, settingsController)
        {
        }

        public override string ChartName => "Tyre Load / Lateral Acceleration";
    }
}