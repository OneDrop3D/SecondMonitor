namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Providers
{
    using Controllers.Settings;
    using Controllers.Settings.Car;
    using DataModel.BasicProperties;
    using Extractors;
    using SecondMonitor.ViewModels.Factory;
    using ViewModels.AggregatedCharts.ScatterPlot;
    using ViewModels.LoadedLapCache;

    public class LatAccelerationToSteeringInputProvider : GenericStintScatterPlotProvider<ScatterPlot, FilteredScatterPlotChartViewModel>
    {
        private readonly LatAccelerationToSteeringInputExtractor _dataExtractor;
        private readonly ICarSettingsController _carSettingsController;

        public LatAccelerationToSteeringInputProvider(ILoadedLapsCache loadedLapsCache, LatAccelerationToSteeringInputExtractor dataExtractor, ISettingsController settingsController, IViewModelFactory viewModelFactory)
            : base(loadedLapsCache, dataExtractor, settingsController, viewModelFactory)
        {
            _dataExtractor = dataExtractor;
            _carSettingsController = settingsController.CarSettingsController;
        }

        public override string ChartName => AggregatedChartNames.LatAccToSteeringInput;
        public override AggregatedChartKind Kind => AggregatedChartKind.ScatterPlot;

        protected override void OnNewViewModel(ScatterPlot scatterPlot, FilteredScatterPlotChartViewModel scatterPlotChartViewModel)
        {
            scatterPlotChartViewModel.QuantityName = "Speeds: ";
            scatterPlotChartViewModel.QuantityUnit = Velocity.GetUnitSymbol(_dataExtractor.VelocityUnits);
            scatterPlotChartViewModel.MinimalValue = _dataExtractor.MinimalSpeed.GetValueInUnits(_dataExtractor.VelocityUnits);
            scatterPlotChartViewModel.MaximumValue = _dataExtractor.MaximumSpeed.GetValueInUnits(_dataExtractor.VelocityUnits);
        }

        protected override void PreviewScatterPlotCreation()
        {
            base.PreviewScatterPlotCreation();
            if (_carSettingsController.CurrentCarWithChartProperties?.ChartsProperties?.LatAccToSteeringChartProperties == null)
            {
                return;
            }

            var chartProperties = _carSettingsController.CurrentCarWithChartProperties.ChartsProperties.LatAccToSteeringChartProperties;

            _dataExtractor.MinimalSpeed = chartProperties.MinimumSpeed;
            _dataExtractor.MaximumSpeed = chartProperties.MaximumSpeed;
        }

        protected override void PreviewRefreshViewModel(FilteredScatterPlotChartViewModel scatterPlotChartViewModel)
        {
            base.PreviewRefreshViewModel(scatterPlotChartViewModel);

            if (_carSettingsController.CurrentCarWithChartProperties?.ChartsProperties?.LatAccToSteeringChartProperties == null)
            {
                return;
            }

            var chartProperties = _carSettingsController.CurrentCarWithChartProperties.ChartsProperties.LatAccToSteeringChartProperties;

            chartProperties.MinimumSpeed = Velocity.FromUnits(scatterPlotChartViewModel.MinimalValue, _dataExtractor.VelocityUnits);
            chartProperties.MaximumSpeed = Velocity.FromUnits(scatterPlotChartViewModel.MaximumValue, _dataExtractor.VelocityUnits);
        }
    }
}