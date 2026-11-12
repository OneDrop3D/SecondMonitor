namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.AggregatedCharts.ScatterPlot
{
    using DataModel.BasicProperties;
    using OxyPlot.Series;
    using SecondMonitor.ViewModels;

    public class LegendItemViewModel : AbstractViewModel
    {
        private bool _isChecked;

        public LegendItemViewModel(ScatterSeries scatterSeries, ColorDto seriesColor, string title, bool isChecked)
        {
            ScatterSeries = scatterSeries;
            _isChecked = isChecked;
            SeriesColor = seriesColor;
            Title = title ?? string.Empty;
        }

        public LegendItemViewModel(ScatterSeries scatterSeries, ColorDto seriesColor, string title) : this(scatterSeries, seriesColor, title, false)
        {
        }

        public ScatterSeries ScatterSeries { get; }

        public ColorDto SeriesColor { get; set; }
        public string Title { get; set; }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                SetProperty(ref _isChecked, value);
                ToggleScatterPlotVisibility();
            }
        }

        private void ToggleScatterPlotVisibility()
        {
            ScatterSeries.IsVisible = IsChecked;
            ScatterSeries.PlotModel.InvalidatePlot(false);
        }
    }
}