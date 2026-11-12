namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.AggregatedCharts.ScatterPlot
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using OxyPlot.Series;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Colors.Extensions;

    public class LegendViewModel : AbstractViewModel
    {
        private readonly ObservableCollection<LegendItemViewModel> _legendItems;
        private bool _isVisible;
        private bool _isExpanded;

        public LegendViewModel()
        {
            _legendItems = new ObservableCollection<LegendItemViewModel>();
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public IReadOnlyCollection<LegendItemViewModel> LegendItems => _legendItems;

        public void AddItem(ScatterSeries scatterSeries, bool isChecked)
        {
            LegendItemViewModel newViewModel = new LegendItemViewModel(scatterSeries, scatterSeries.MarkerFill.FromOxyColor(), scatterSeries.Title, isChecked);
            _legendItems.Add(newViewModel);
        }

        public void RemoveItem(ScatterSeries scatterSeries)
        {
            _legendItems.Where(x => x.ScatterSeries == scatterSeries).ToList().ForEach(x => _legendItems.Remove(x));
        }

        public void RemoveAll()
        {
            _legendItems.Clear();
        }
    }
}