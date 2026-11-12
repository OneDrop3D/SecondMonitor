namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.LineSeries.Providers
{
    using System.Collections.Generic;
    using System.Linq;
    using Controllers.Settings;
    using Filter;
    using Histogram;
    using Histogram.Extractors;
    using SecondMonitor.ViewModels.Factory;
    using Settings.DTO;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;
    using ViewModels.AggregatedCharts;
    using ViewModels.AggregatedCharts.LineSeries;
    using ViewModels.LoadedLapCache;

    public class SuspensionVelocityLineSeriesProvider : AbstractAggregatedChartProvider
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly SuspensionVelocityHistogramDataExtractor _suspensionVelocityHistogramDataExtractor;
        private readonly SuspensionVelocityFilter _suspensionVelocityFilter;
        private readonly ISettingsController _settingsController;

        public SuspensionVelocityLineSeriesProvider(ILoadedLapsCache loadedLapsCache, IViewModelFactory viewModelFactory, SuspensionVelocityHistogramDataExtractor suspensionVelocityHistogramDataExtractor, 
            SuspensionVelocityFilter suspensionVelocityFilter, ISettingsController settingsController) : base(loadedLapsCache)
        {
            _viewModelFactory = viewModelFactory;
            _suspensionVelocityHistogramDataExtractor = suspensionVelocityHistogramDataExtractor;
            _suspensionVelocityFilter = suspensionVelocityFilter;
            _settingsController = settingsController;
        }

        public override string ChartName => "Suspension Velocity Series";
        public override AggregatedChartKind Kind => AggregatedChartKind.LineSeries;
        public override IReadOnlyCollection<IAggregatedChartViewModel> CreateAggregatedChartViewModels(AggregatedChartSettingsDto aggregatedChartSettings)
        {
            List<IAggregatedChartViewModel> charts = new List<IAggregatedChartViewModel>();
            var groupedByStint = GetLapsGrouped(aggregatedChartSettings).ToList();

            if (groupedByStint.Count == 0)
            {
                return charts;
            }

            CarWithChartPropertiesDto carWithChartPropertiesDto = _settingsController.CarSettingsController.CurrentCarWithChartProperties;
            _suspensionVelocityFilter.MinimumVelocity = carWithChartPropertiesDto.ChartsProperties.SuspensionVelocityHistogram.Minimum.GetValueInUnits(_suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            _suspensionVelocityFilter.MaximumVelocity = carWithChartPropertiesDto.ChartsProperties.SuspensionVelocityHistogram.Maximum.GetValueInUnits(_suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            _suspensionVelocityFilter.VelocityUnits = _suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall;
            double bandSize = carWithChartPropertiesDto.ChartsProperties.SuspensionVelocityHistogram.BandSize.GetValueInUnits(_suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);

            foreach (IGrouping<int, LapTelemetryDto> lapsInStint in groupedByStint)
            {
                string title = BuildChartTitle(lapsInStint, aggregatedChartSettings);

                LineSeriesHistogramsChartViewModel lineSeriesHistogramsChartViewModel = _viewModelFactory.Create<LineSeriesHistogramsChartViewModel>();
                lineSeriesHistogramsChartViewModel.Title = title;

                FillHistogramViewmodel(lapsInStint.ToList(), bandSize, lineSeriesHistogramsChartViewModel);

                charts.Add(lineSeriesHistogramsChartViewModel);
            }

            return charts;
        }

        protected void FillHistogramViewmodel(IReadOnlyCollection<LapTelemetryDto> loadedLaps, double bandSize, LineSeriesHistogramsChartViewModel lineSeriesHistogramsChartViewModel)
        {
            Histogram flHistogram = ExtractFlHistogram(loadedLaps, bandSize);
            Histogram frHistogram = ExtractFrHistogram(loadedLaps, bandSize);
            Histogram rlHistogram = ExtractRlHistogram(loadedLaps, bandSize);
            Histogram rrHistogram = ExtractRrHistogram(loadedLaps, bandSize);

            lineSeriesHistogramsChartViewModel.AddSeries(flHistogram);
            lineSeriesHistogramsChartViewModel.AddSeries(frHistogram);
            lineSeriesHistogramsChartViewModel.AddSeries(rlHistogram);
            lineSeriesHistogramsChartViewModel.AddSeries(rrHistogram);
        }

        protected virtual Histogram ExtractFlHistogram(IReadOnlyCollection<LapTelemetryDto> loadedLaps, double bandSize)
        {
            _suspensionVelocityFilter.FilterFrontLeft();
            return _suspensionVelocityHistogramDataExtractor.ExtractHistogramFrontLeft(loadedLaps, bandSize, new[] { _suspensionVelocityFilter });
        }

        protected virtual Histogram ExtractFrHistogram(IReadOnlyCollection<LapTelemetryDto> loadedLaps, double bandSize)
        {
            _suspensionVelocityFilter.FilterFrontRight();
            return _suspensionVelocityHistogramDataExtractor.ExtractHistogramFrontRight(loadedLaps, bandSize, new[] { _suspensionVelocityFilter });
        }

        protected virtual Histogram ExtractRlHistogram(IReadOnlyCollection<LapTelemetryDto> loadedLaps, double bandSize)
        {
            _suspensionVelocityFilter.FilterRearLeft();
            return _suspensionVelocityHistogramDataExtractor.ExtractHistogramRearLeft(loadedLaps, bandSize, new[] { _suspensionVelocityFilter });
        }

        protected virtual Histogram ExtractRrHistogram(IReadOnlyCollection<LapTelemetryDto> loadedLaps, double bandSize)
        {
            _suspensionVelocityFilter.FilterRearRight();
            return _suspensionVelocityHistogramDataExtractor.ExtractHistogramRearRight(loadedLaps, bandSize, new[] { _suspensionVelocityFilter });
        }
    }
}