namespace SecondMonitor.TelemetryPresentation.Controls.LapsSummary
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    public class LapSelectionTemplateSelectorConverter : IValueConverter
    {
        public DataTemplate ListViewDataTemplate { get; set; }
        public DataTemplate TreeViewDataTemplate { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool areLapsGrouped))
            {
                return ListViewDataTemplate;
            }

            return areLapsGrouped ? TreeViewDataTemplate : ListViewDataTemplate;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}