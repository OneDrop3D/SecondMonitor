namespace SecondMonitor.TelemetryPresentation.Controls.LapsSummary
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using OpenWindow;
    using Telemetry.TelemetryApplication.ViewModels.LapPicker;
    using Telemetry.TelemetryApplication.ViewModels.OpenWindow;

    /// <summary>
    /// Interaction logic for LapSelectionControl.xaml
    /// </summary>
    public partial class LapSelectionControl : UserControl
    {
        private OpenWindow _openWindow;
        private OpenWindow _addWindow;

        public LapSelectionControl()
        {
            InitializeComponent();
        }

        private void OpenButton_OnClick(object sender, RoutedEventArgs e)
        {
            IOpenWindowViewModel openWindowViewModel = ((LapSelectionViewModel)DataContext).OpenWindowViewModel;

            if (_openWindow == null)
            {
                openWindowViewModel.IsOpenWindowVisible = true;
                _openWindow = new OpenWindow { WindowStartupLocation = WindowStartupLocation.CenterScreen, Owner = Window.GetWindow(this), DataContext = openWindowViewModel };
                _openWindow.Closed += OpenWindowOnClosed;
                openWindowViewModel.RefreshRecentCommand.Execute(null);
                _openWindow.Show();
                return;
            }

            openWindowViewModel.RefreshRecentCommand.Execute(null);
            openWindowViewModel.IsOpenWindowVisible = true;
            _openWindow.Focus();
        }

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            IOpenWindowViewModel openWindowViewModel = ((LapSelectionViewModel)DataContext).AddWindowViewModel;

            if (_addWindow == null)
            {
                openWindowViewModel.IsOpenWindowVisible = true;
                _addWindow = new OpenWindow { WindowStartupLocation = WindowStartupLocation.CenterScreen, Owner = Window.GetWindow(this), DataContext = openWindowViewModel };
                _addWindow.Closed += AddWindowOnClosed;
                openWindowViewModel.RefreshRecentCommand.Execute(null);
                _addWindow.Show();
                return;
            }

            openWindowViewModel.RefreshRecentCommand.Execute(null);
            openWindowViewModel.IsOpenWindowVisible = true;
            _addWindow.Focus();
        }

        private void OpenWindowOnClosed(object sender, EventArgs e)
        {
            _openWindow.DataContext = null;
            _openWindow.Closed -= OpenWindowOnClosed;
            _openWindow = null;
        }

        private void AddWindowOnClosed(object sender, EventArgs e)
        {
            _addWindow.DataContext = null;
            _addWindow.Closed -= OpenWindowOnClosed;
            _addWindow = null;
        }
    }
}
