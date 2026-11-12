namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.AggregatedCharts.ScatterPlot
{
    using Controllers.Synchronization;

    public class FilteredScatterPlotChartViewModel : ScatterPlotChartViewModel
    {
        private string _quantityName;
        private string _quantityUnit;
        private double _minimalValue;
        private double _maximumValue;

        public FilteredScatterPlotChartViewModel(IDataPointSelectionSynchronization dataPointSelectionSynchronization) : base(dataPointSelectionSynchronization)
        {
        }

        public FilteredScatterPlotChartViewModel()
        {
        }

        public string QuantityName
        {
            get => _quantityName;
            set => SetProperty(ref _quantityName, value);
        }

        public string QuantityUnit
        {
            get => _quantityUnit;
            set => SetProperty(ref _quantityUnit, value);
        }

        public double MinimalValue
        {
            get => _minimalValue;
            set => SetProperty(ref _minimalValue, value);
        }

        public double MaximumValue
        {
            get => _maximumValue;
            set => SetProperty(ref _maximumValue, value);
        }
    }
}