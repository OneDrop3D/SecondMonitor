namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.MainWindow.GraphPanel
{
    using System;
    using System.Windows;
    using SecondMonitor.ViewModels.Layouts;

    public class ChartWithProviderAndWindow : ChartWithProvider
    {
        public ChartWithProviderAndWindow(string providerName, ViewModelContainer wrapperViewModel, bool refreshOnCarSettingsChange, Window window, Guid groupIdentifier) : base(providerName, wrapperViewModel, refreshOnCarSettingsChange)
        {
            Window = window;
            GroupIdentifier = groupIdentifier;
        }

        public Window Window { get; }
        public Guid GroupIdentifier { get; }
    }
}