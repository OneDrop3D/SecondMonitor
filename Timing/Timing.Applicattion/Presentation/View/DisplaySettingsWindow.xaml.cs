namespace SecondMonitor.Timing.Application.Presentation.View
{
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Navigation;

    /// <summary>
    /// Interaction logic for DisplaySettingsWindow.xaml
    /// </summary>
    public partial class DisplaySettingsWindow : Window
    {
        public DisplaySettingsWindow()
        {
            InitializeComponent();
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
