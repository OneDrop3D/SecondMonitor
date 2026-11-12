namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Providers
{
    using Controllers.Settings;
    using Extractors;
    using SecondMonitor.ViewModels.Factory;
    using ViewModels.LoadedLapCache;

    public class SpeedToLatGProvider : AbstractStintScatterPlotProvider
    {
        public SpeedToLatGProvider(ILoadedLapsCache loadedLapsCache, SpeedToLatGAllPointsExtractor dataExtractor, ISettingsController settingsController, IViewModelFactory viewModelFactory)
            : base(loadedLapsCache, dataExtractor, settingsController, viewModelFactory)
        {
        }

        public override string ChartName => "Lat vs Speed";
        public override AggregatedChartKind Kind => AggregatedChartKind.ScatterPlot;
    }
}