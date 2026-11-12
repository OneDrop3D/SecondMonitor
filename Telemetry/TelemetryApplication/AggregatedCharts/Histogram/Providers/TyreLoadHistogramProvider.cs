namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.Histogram.Providers
{
    using System.Collections.Generic;
    using System.Linq;
    using DataAdapters;
    using Extractors;
    using Filter;
    using SecondMonitor.ViewModels.Factory;
    using ViewModels.AggregatedCharts.Histogram;
    using ViewModels.LoadedLapCache;

    public class TyreLoadHistogramProvider : AbstractWheelHistogramProvider<WheelsHistogramChartViewModel, HistogramChartWithStatisticsViewModel>
    {
        private readonly ILoadedLapsCache _loadedLapsCache;
        private readonly TyreLoadAdapter _tyreLoadAdapter;

        public TyreLoadHistogramProvider(TyreLoadHistogramExtractor abstractWheelHistogramDataExtractor, ILoadedLapsCache loadedLapsCache, IViewModelFactory viewModelFactory, IEnumerable<IWheelTelemetryFilter> filters, TyreLoadAdapter tyreLoadAdapter) : base(abstractWheelHistogramDataExtractor, loadedLapsCache, viewModelFactory, filters)
        {
            _loadedLapsCache = loadedLapsCache;
            _tyreLoadAdapter = tyreLoadAdapter;
        }

        public override string ChartName => "Tyre Load";
        public override AggregatedChartKind Kind => AggregatedChartKind.Histogram;
        protected override bool ResetCommandVisible => false;
        public override bool IsUsingCarProperties => true;
        protected override void OnNewViewModel(WheelsHistogramChartViewModel newViewModel)
        {
            var loadedLaps = _loadedLapsCache.LoadedLaps;
            if (loadedLaps.Count == 0)
            {
                return;
            }

            var firstDataPoint = loadedLaps.FirstOrDefault()?.DataPoints?.FirstOrDefault();
            if (firstDataPoint == null)
            {
                return;
            }

            if (_tyreLoadAdapter.IsQuantityComputed(firstDataPoint.SimulatorSourceInfo))
            {
                newViewModel.Title = "Suspension Load (C)";
            }
        }
    }
}