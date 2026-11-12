namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Providers
{
    using System.Collections.Generic;
    using System.Linq;
    using Controllers.Settings;
    using Controllers.Synchronization;
    using Extractors;
    using SecondMonitor.ViewModels.Colors;
    using SecondMonitor.ViewModels.Colors.Extensions;
    using Settings.DTO;
    using TelemetryManagement.DTO;
    using ViewModels.AggregatedCharts;
    using ViewModels.AggregatedCharts.ScatterPlot;
    using ViewModels.LoadedLapCache;

    public abstract class AbstractGearsChartProvider : AbstractAggregatedChartProvider
    {
        private readonly AbstractGearFilteredScatterPlotExtractor _dataExtractor;
        private readonly IDataPointSelectionSynchronization _dataPointSelectionSynchronization;
        private readonly ISettingsController _settingsController;

        protected AbstractGearsChartProvider(ILoadedLapsCache loadedLapsCache, AbstractGearFilteredScatterPlotExtractor dataExtractor, IDataPointSelectionSynchronization dataPointSelectionSynchronization, ISettingsController settingsController) : base(loadedLapsCache)
        {
            _dataExtractor = dataExtractor;
            _dataPointSelectionSynchronization = dataPointSelectionSynchronization;
            _settingsController = settingsController;
        }

        public override IReadOnlyCollection<IAggregatedChartViewModel> CreateAggregatedChartViewModels(AggregatedChartSettingsDto aggregatedChartSettings)
        {
            IEnumerable<IGrouping<int, LapTelemetryDto>> lapsStintGrouping = GetLapsGrouped(aggregatedChartSettings);
            if (aggregatedChartSettings.StintRenderingKind == StintRenderingKind.SingleChart)
            {
                return CreateChartForAllStints(lapsStintGrouping);
            }
            else
            {
                return CreateChartForEachStint(lapsStintGrouping, aggregatedChartSettings);
            }
        }

        public virtual void DidCreateScatterPlotAllGear(ScatterPlot scatterPlot, IEnumerable<LapTelemetryDto> loadedLaps)
        {
        }

        public virtual void ScatterPlotAllGearSeriesTitle(ref string title, IEnumerable<LapTelemetryDto> loadedLaps, int gear)
        {
        }

        private IReadOnlyCollection<IAggregatedChartViewModel> CreateChartForAllStints(IEnumerable<IGrouping<int, LapTelemetryDto>> lapsStintGrouping)
        {
            IGrouping<int, LapTelemetryDto>[] lapsInStints = lapsStintGrouping as IGrouping<int, LapTelemetryDto>[] ?? lapsStintGrouping.ToArray();
            string title = BuildTitleForAllStints(lapsInStints);
            CompositeAggregatedChartsViewModel viewModel = new CompositeAggregatedChartsViewModel() { Title = title };
            if (lapsInStints.Length == 0)
            {
                return new List<IAggregatedChartViewModel>() { viewModel };
            }

            int maxGear = lapsInStints.SelectMany(x => x).SelectMany(x => x.DataPoints).Where(x => !string.IsNullOrWhiteSpace(x.PlayerData.CarInfo.CurrentGear) && x.PlayerData.CarInfo.CurrentGear != "R" && x.PlayerData.CarInfo.CurrentGear != "N").Max(x => int.Parse(x.PlayerData.CarInfo.CurrentGear));

            ScatterPlotChartViewModel mainViewModel = new ScatterPlotChartViewModel(_dataPointSelectionSynchronization) { Title = "All Gear", HideNotSelectedPoints = _settingsController.TelemetrySettings.AggregatedChartSettings.HideNotSelectedPoints };
            mainViewModel.FromModel(CreateScatterPlotAllGear(lapsInStints, maxGear));
            viewModel.MainAggregatedChartViewModel = mainViewModel;

            for (int i = 1; i <= maxGear; i++)
            {
                ScatterPlot scatterPlot = CreateScatterPlot(lapsInStints, i);
                if (scatterPlot.ScatterPlotSeries.Count == 0)
                {
                    continue;
                }

                ScatterPlotChartViewModel child = new ScatterPlotChartViewModel(_dataPointSelectionSynchronization) { Title = $"Gear {i}", HideNotSelectedPoints = _settingsController.TelemetrySettings.AggregatedChartSettings.HideNotSelectedPoints };
                child.FromModel(scatterPlot);
                viewModel.AddChildAggregatedChildViewModel(child);
            }

            return new List<IAggregatedChartViewModel>() { viewModel };
        }

        private IReadOnlyCollection<IAggregatedChartViewModel> CreateChartForEachStint(IEnumerable<IGrouping<int, LapTelemetryDto>> lapsStintGrouping, AggregatedChartSettingsDto aggregatedChartSettings)
        {
            List<IAggregatedChartViewModel> charts = new List<IAggregatedChartViewModel>();

            foreach (IGrouping<int, LapTelemetryDto> lapsGrouped in lapsStintGrouping)
            {
                string title = BuildChartTitle(lapsGrouped, aggregatedChartSettings);

                var gearsAsString = lapsGrouped.SelectMany(x => x.DataPoints).Where(x => !string.IsNullOrWhiteSpace(x.PlayerData.CarInfo.CurrentGear) && x.PlayerData.CarInfo.CurrentGear != "R" && x.PlayerData.CarInfo.CurrentGear != "N").ToList();
                if (gearsAsString.Count == 0)
                {
                    continue;
                }

                int maxGear = gearsAsString.Max(x => int.Parse(x.PlayerData.CarInfo.CurrentGear));

                CompositeAggregatedChartsViewModel viewModel = new CompositeAggregatedChartsViewModel() { Title = title };

                ScatterPlotChartViewModel mainViewModel = new ScatterPlotChartViewModel(_dataPointSelectionSynchronization) { Title = "All Gear", HideNotSelectedPoints = _settingsController.TelemetrySettings.AggregatedChartSettings.HideNotSelectedPoints };
                mainViewModel.FromModel(CreateScatterPlotAllGear(lapsGrouped, maxGear));

                viewModel.MainAggregatedChartViewModel = mainViewModel;

                for (int i = 1; i <= maxGear; i++)
                {
                    ScatterPlot scatterPlot = CreateScatterPlot(lapsGrouped, i);
                    if (scatterPlot.ScatterPlotSeries.Count == 0)
                    {
                        continue;
                    }

                    ScatterPlotChartViewModel child = new ScatterPlotChartViewModel(_dataPointSelectionSynchronization) { Title = $"Gear {i}", HideNotSelectedPoints = _settingsController.TelemetrySettings.AggregatedChartSettings.HideNotSelectedPoints };
                    child.FromModel(scatterPlot);
                    viewModel.AddChildAggregatedChildViewModel(child);
                }

                charts.Add(viewModel);
            }

            return charts;
        }

        protected ScatterPlot CreateScatterPlot(IEnumerable<LapTelemetryDto> loadedLaps, int gear)
        {
            AxisDefinition xAxis = new AxisDefinition(_dataExtractor.XMajorTickSize, _dataExtractor.XMajorTickSize / 5, _dataExtractor.XUnit);
            AxisDefinition yAxis = new AxisDefinition(_dataExtractor.YMajorTickSize, _dataExtractor.YMajorTickSize / 4, _dataExtractor.YUnit);
            ScatterPlot scatterPlot = new ScatterPlot($"Gear: {gear}", xAxis, yAxis);

            scatterPlot.AddScatterPlotSeries(_dataExtractor.ExtractSeriesForGear(loadedLaps, gear.ToString()));
            return scatterPlot;
        }

        protected ScatterPlot CreateScatterPlotAllGear(IEnumerable<LapTelemetryDto> loadedLaps, int maxGear)
        {
            AxisDefinition xAxis = new AxisDefinition(_dataExtractor.XMajorTickSize, _dataExtractor.XMajorTickSize / 5, _dataExtractor.XUnit);
            AxisDefinition yAxis = new AxisDefinition(_dataExtractor.YMajorTickSize, _dataExtractor.YMajorTickSize / 4, _dataExtractor.YUnit);
            ScatterPlot scatterPlot = new ScatterPlot("All Gears (O/T)", xAxis, yAxis);

            for (int i = 1; i <= maxGear; i++)
            {
                var seriesTitle = $"Gear {i}";
                ScatterPlotAllGearSeriesTitle(ref seriesTitle, loadedLaps, i);
                scatterPlot.AddScatterPlotSeries(_dataExtractor.ExtractSeriesForGear(loadedLaps, i.ToString(), true, seriesTitle));
            }

            DidCreateScatterPlotAllGear(scatterPlot, loadedLaps);

            return scatterPlot;
        }

        private ScatterPlot CreateScatterPlotAllGear(IEnumerable<IGrouping<int, LapTelemetryDto>> lapsInStints, int maxGear)
        {
            IColorPaletteProvider colorPaletteProvider = new BasicColorPaletteProvider();
            AxisDefinition xAxis = new AxisDefinition(_dataExtractor.XMajorTickSize, _dataExtractor.XMajorTickSize / 5, _dataExtractor.XUnit);
            AxisDefinition yAxis = new AxisDefinition(_dataExtractor.YMajorTickSize, _dataExtractor.YMajorTickSize / 4, _dataExtractor.YUnit);
            ScatterPlot scatterPlot = new ScatterPlot("All Gears (O/T)", xAxis, yAxis);
            foreach (IGrouping<int, LapTelemetryDto> stint in lapsInStints)
            {
                string seriesTitle = $"Laps: {string.Join(", ", stint.Select(x => x.LapSummary.CustomDisplayName))} - Stint: {stint.Key}";
                scatterPlot.AddScatterPlotSeries(_dataExtractor.ExtractSeriesForAllGears(stint, true, seriesTitle, colorPaletteProvider.GetNext().ToOxyColor()));
            }

            DidCreateScatterPlotAllGear(scatterPlot, lapsInStints.SelectMany(group => group));

            return scatterPlot;
        }

        private ScatterPlot CreateScatterPlot(IEnumerable<IGrouping<int, LapTelemetryDto>> lapsInStints, int gear)
        {
            IColorPaletteProvider colorPaletteProvider = new BasicColorPaletteProvider();
            AxisDefinition xAxis = new AxisDefinition(_dataExtractor.XMajorTickSize, _dataExtractor.XMajorTickSize / 5, _dataExtractor.XUnit);
            AxisDefinition yAxis = new AxisDefinition(_dataExtractor.YMajorTickSize, _dataExtractor.YMajorTickSize / 4, _dataExtractor.YUnit);
            ScatterPlot scatterPlot = new ScatterPlot($"Gear: {gear}", xAxis, yAxis);

            foreach (IGrouping<int, LapTelemetryDto> stint in lapsInStints)
            {
                string seriesTitle = $"Laps: {string.Join(", ", stint.Select(x => x.LapSummary.CustomDisplayName))} - Stint: {stint.Key}";
                scatterPlot.AddScatterPlotSeries(_dataExtractor.ExtractSeriesForGear(stint, gear.ToString(), seriesTitle, colorPaletteProvider.GetNext().ToOxyColor()));
            }

            return scatterPlot;
        }
    }
}