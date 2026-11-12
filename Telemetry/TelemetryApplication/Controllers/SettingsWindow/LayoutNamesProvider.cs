namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.SettingsWindow
{
    using System.Collections.Generic;
    using System.Linq;
    using AggregatedCharts;
    using MainWindow.GraphPanel;
    using SecondMonitor.ViewModels.Layouts.Factory;

    public class LayoutNamesProvider : ILayoutElementNamesProvider
    {
        public List<string> GetAllowableContentNames()
        {
            return SeriesChartNames.AllSeriesCharts.OrderBy(x => x).Concat(AggregatedChartNames.AllChartNames.OrderBy(x => x)).ToList();
        }
    }
}