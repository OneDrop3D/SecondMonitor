namespace SecondMonitor.WindowsControls.WPF.DonateButton
{
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for HelpButton.xaml
    /// </summary>
    public partial class DonateButton : UserControl
    {
        private const string DonateLink = @"https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=7VHPBA575PP9E&source=url";
        public DonateButton()
        {
            InitializeComponent();
        }

        private void HelpButtonClick(object sender, RoutedEventArgs e)
        {
            //Uri url = new Uri(HelpUrl);
            Process.Start(new ProcessStartInfo(DonateLink) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
