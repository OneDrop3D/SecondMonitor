namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel
{
    using SecondMonitor.ViewModels;

    public class ChartWrapperViewModel : AbstractViewModel
    {
        private IViewModel _chartViewModel;

        public ChartWrapperViewModel()
        {
        }

        public ChartWrapperViewModel(IViewModel chartViewModel)
        {
            _chartViewModel = chartViewModel;
        }

        public IViewModel ChartViewModel
        {
            get => _chartViewModel;
            set => SetProperty(ref _chartViewModel, value);
        }
    }
}