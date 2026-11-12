namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.AggregatedCharts
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class CompositeAggregatedChartsViewModel : AbstractAggregatedChartViewModel
    {
        private readonly ObservableCollection<IAggregatedChartViewModel> _childAggregatedChartViewModels;
        private IAggregatedChartViewModel _mainAggregatedChartViewModel;

        public CompositeAggregatedChartsViewModel()
        {
            _childAggregatedChartViewModels = new ObservableCollection<IAggregatedChartViewModel>();
        }

        public IAggregatedChartViewModel MainAggregatedChartViewModel
        {
            get => _mainAggregatedChartViewModel;
            set => SetProperty(ref _mainAggregatedChartViewModel, value);
        }

        public IReadOnlyCollection<IAggregatedChartViewModel> ChildAggregatedChartViewModels => _childAggregatedChartViewModels;

        public void AddChildAggregatedChildViewModel(IAggregatedChartViewModel aggregatedChartViewModel)
        {
            _childAggregatedChartViewModels.Add(aggregatedChartViewModel);
        }
    }
}