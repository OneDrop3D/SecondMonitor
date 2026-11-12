namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.MainWindow.GraphPanel
{
    using SecondMonitor.ViewModels.Layouts;

    public class ChartWithProvider
    {
        public ChartWithProvider(string providerName, ViewModelContainer wrapperViewModel, bool refreshOnCarSettingsChange)
        {
            ProviderName = providerName;
            WrapperViewModel = wrapperViewModel;
            RefreshOnCarSettingsChange = refreshOnCarSettingsChange;
        }

        public string ProviderName { get; }

        public bool RefreshOnCarSettingsChange { get; }

        public ViewModelContainer WrapperViewModel { get; }
    }
}