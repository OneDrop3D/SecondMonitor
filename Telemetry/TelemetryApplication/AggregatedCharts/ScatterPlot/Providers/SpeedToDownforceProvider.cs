namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Providers
{
    using Controllers.Settings;
    using Extractors;
    using SecondMonitor.ViewModels.Factory;
    using ViewModels.LoadedLapCache;

    public class SpeedToDownforceProvider : AbstractStintScatterPlotProvider
    {
        public SpeedToDownforceProvider(SpeedToDownforceExtractor dataExtractor, ILoadedLapsCache loadedLapsCache, ISettingsController settingsController, IViewModelFactory viewModelFactory)
            : base(loadedLapsCache, dataExtractor, settingsController, viewModelFactory)
        {
        }

        public override string ChartName => "Downforce / Speed";
        public override AggregatedChartKind Kind => AggregatedChartKind.ScatterPlot;
        public override bool IsUsingCarProperties => true;
    }
}