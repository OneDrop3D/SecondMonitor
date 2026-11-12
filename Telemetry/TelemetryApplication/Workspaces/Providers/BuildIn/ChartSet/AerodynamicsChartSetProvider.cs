namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces.Providers.BuildIn.ChartSet
{
    using System;
    using AggregatedCharts;
    using Controllers.MainWindow.GraphPanel;
    using SecondMonitor.ViewModels.Layouts;
    using SecondMonitor.ViewModels.Settings.Model.Layout;

    public class AerodynamicsChartSetProvider : AbstractChartSetProvider
    {
        public static readonly Guid Guid = Guid.Parse("e6fcacd7-9a3c-4234-ab58-83f4eed78ee7");
        public override Guid ChartSetGuid => Guid;
        public override string ChartSetName => "Aerodynamics";
        protected override LayoutDescription GetLayoutDescription()
        {
            var content = RowsDefinitionSettingBuilder.Create().WithGridSplitters().
                WithNamedContent(SeriesChartNames.SpeedChartName, SizeKind.Manual, 350).
                WithNamedContent(SeriesChartNames.DownforceOverallChartName, SizeKind.Manual, 300).
                WithContent(
                    ColumnsDefinitionSettingBuilder.Create().
                        WithNamedContent(SeriesChartNames.DownforceFrontRearChartName, SizeKind.Remaining, 1).
                        WithNamedContent(SeriesChartNames.RakeChartName, SizeKind.Remaining, 1).Build(),
                    SizeKind.Manual, 300).

                WithContent(
                    ColumnsDefinitionSettingBuilder.Create().
                        WithNamedContent(AggregatedChartNames.SpeedToDownforceChartName, SizeKind.Remaining, 1).
                        WithNamedContent(AggregatedChartNames.SpeedToRakeChartName, SizeKind.Remaining, 1).Build(),
                    SizeKind.Manual, 500).
                WithNamedContent(AggregatedChartNames.RideHeightToSpeedChartName, SizeKind.Manual, 800).
                Build();

            return new LayoutDescription()
            {
                RootElement = content
            };
        }
    }
}