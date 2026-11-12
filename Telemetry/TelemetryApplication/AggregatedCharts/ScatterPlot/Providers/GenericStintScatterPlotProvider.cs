namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Providers
{
    using System.Collections.Generic;
    using System.Linq;
    using Contracts.Commands;
    using Controllers.Settings;
    using Extractors;
    using Filter;
    using OxyPlot;
    using SecondMonitor.ViewModels.Colors;
    using SecondMonitor.ViewModels.Colors.Extensions;
    using SecondMonitor.ViewModels.Factory;
    using Settings.DTO;
    using TelemetryManagement.DTO;
    using ViewModels.AggregatedCharts;
    using ViewModels.AggregatedCharts.ScatterPlot;
    using ViewModels.LoadedLapCache;

    public abstract class GenericStintScatterPlotProvider<T, TV> : AbstractAggregatedChartProvider where T : ScatterPlot, new() where TV : ScatterPlotChartViewModel
    {
        private readonly AbstractScatterPlotExtractor _dataExtractor;
        private readonly ISettingsController _settingsController;
        private readonly IViewModelFactory _viewModelFactory;

        protected GenericStintScatterPlotProvider(ILoadedLapsCache loadedLapsCache, AbstractScatterPlotExtractor dataExtractor, ISettingsController settingsController, IViewModelFactory viewModelFactory) : base(loadedLapsCache)
        {
            _dataExtractor = dataExtractor;
            _settingsController = settingsController;
            _viewModelFactory = viewModelFactory;
        }

        public bool IsLegendVisible { get; set; }

        public override IReadOnlyCollection<IAggregatedChartViewModel> CreateAggregatedChartViewModels(AggregatedChartSettingsDto aggregatedChartSettings)
        {
            IEnumerable<IGrouping<int, LapTelemetryDto>> lapsStintGrouping = GetLapsGrouped(aggregatedChartSettings);
            return aggregatedChartSettings.StintRenderingKind == StintRenderingKind.SingleChart ? CreateChartForAllStints(lapsStintGrouping.ToList()) : CreateChartForEachStint(lapsStintGrouping, aggregatedChartSettings);
        }

        protected abstract void OnNewViewModel(T scatterPlot, TV scatterPlotChartViewModel);

        protected virtual void PreviewRefreshViewModel(TV scatterPlotChartViewModel)
        {
        }

        protected virtual void PreviewScatterPlotCreation()
        {
        }

        private IReadOnlyCollection<IAggregatedChartViewModel> CreateChartForAllStints(ICollection<IGrouping<int, LapTelemetryDto>> lapsStintGrouping)
        {
            string title = BuildTitleForAllStints(lapsStintGrouping);
            PreviewScatterPlotCreation();
            AxisDefinition xAxis = new AxisDefinition(_dataExtractor.XMajorTickSize, _dataExtractor.XMajorTickSize / 4, _dataExtractor.XUnit);
            AxisDefinition yAxis = new AxisDefinition(_dataExtractor.YMajorTickSize, _dataExtractor.YMajorTickSize / 4, _dataExtractor.YUnit);
            T scatterPlot = new T()
            {
                Title = title,
                XAxis = xAxis,
                YAxis = yAxis,
            };

            IColorPaletteProvider colorPaletteProvider = new BasicColorPaletteProvider();
            foreach (IGrouping<int, LapTelemetryDto> lapsInStintGroup in lapsStintGrouping)
            {
                string seriesTitle = $"Laps: {string.Join(", ", lapsInStintGroup.Select(x => x.LapSummary.CustomDisplayName))} - Stint: {lapsInStintGroup.Key}";
                scatterPlot.AddScatterPlotSeries(_dataExtractor.ExtractSeries(lapsInStintGroup, Enumerable.Empty<ITelemetryFilter>().ToList(), seriesTitle, colorPaletteProvider.GetNext().ToOxyColor()));
            }

            string scatterPlotCharName = ChartName;
            if (_dataExtractor.IsUsingComputedData)
            {
                scatterPlot.Title += " (C)";
                scatterPlotCharName += " (C)";
            }

            TV viewModel = CreateViewModel(scatterPlotCharName, scatterPlot);
            return new[] { viewModel };
        }

        protected IReadOnlyCollection<IAggregatedChartViewModel> CreateChartForEachStint(IEnumerable<IGrouping<int, LapTelemetryDto>> lapsStintGrouping, AggregatedChartSettingsDto aggregatedChartSettings)
        {
            List<IAggregatedChartViewModel> charts = new List<IAggregatedChartViewModel>();
            PreviewScatterPlotCreation();
            foreach (IGrouping<int, LapTelemetryDto> lapsInStintGroup in lapsStintGrouping)
            {
                string title = BuildChartTitle(lapsInStintGroup, aggregatedChartSettings);

                AxisDefinition xAxis = new AxisDefinition(_dataExtractor.XMajorTickSize, _dataExtractor.XMajorTickSize / 4, _dataExtractor.XUnit);
                AxisDefinition yAxis = new AxisDefinition(_dataExtractor.YMajorTickSize, _dataExtractor.YMajorTickSize / 4, _dataExtractor.YUnit);
                T scatterPlot = new T()
                {
                    Title = title,
                    XAxis = xAxis,
                    YAxis = yAxis,
                    IsLegendVisible = IsLegendVisible
                };

                scatterPlot.AddScatterPlotSeries(_dataExtractor.ExtractSeries(lapsInStintGroup, Enumerable.Empty<ITelemetryFilter>().ToList(), title, OxyColors.Green));

                string scatterPlotCharName = ChartName;
                if (_dataExtractor.IsUsingComputedData)
                {
                    scatterPlot.Title += " (C)";
                    scatterPlotCharName += " (C)";
                }

                TV viewModel = CreateViewModel(scatterPlotCharName, scatterPlot);
                charts.Add(viewModel);
            }

            return charts;
        }

        private TV CreateViewModel(string scatterPlotCharName, T scatterPlot)
        {
            TV viewModel = _viewModelFactory.Create<TV>();
            viewModel.Title = scatterPlotCharName;
            viewModel.HideNotSelectedPoints = _settingsController.TelemetrySettings.AggregatedChartSettings.HideNotSelectedPoints;
            viewModel.FromModel(scatterPlot);
            viewModel.RefreshCommand = new RelayCommand(() => RefreshViewModel(viewModel));
            OnNewViewModel(scatterPlot, viewModel);
            return viewModel;
        }

        private void RefreshViewModel(TV viewModel)
        {
            PreviewRefreshViewModel(viewModel);
        }
    }
}