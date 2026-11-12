namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Providers
{
    using Controllers.Settings;
    using Extractors;
    using SecondMonitor.ViewModels.Factory;
    using ViewModels.AggregatedCharts.ScatterPlot;
    using ViewModels.LoadedLapCache;

    public abstract class AbstractStintScatterPlotProvider : GenericStintScatterPlotProvider<ScatterPlot, ScatterPlotChartViewModel>
    {
        protected AbstractStintScatterPlotProvider(ILoadedLapsCache loadedLapsCache, AbstractScatterPlotExtractor dataExtractor, ISettingsController settingsController, IViewModelFactory viewModelFactory) : base(loadedLapsCache, dataExtractor,
            settingsController, viewModelFactory)
        {
        }

        protected override void OnNewViewModel(ScatterPlot scatterPlot, ScatterPlotChartViewModel scatterPlotChartViewModel)
        {
        }
    }
}