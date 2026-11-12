namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces.Providers.BuildIn.ChartSet
{
    using System;
    using AggregatedCharts;
    using Controllers.MainWindow.GraphPanel;
    using SecondMonitor.ViewModels.Layouts;
    using SecondMonitor.ViewModels.Settings.Model.Layout;

    public class RideHeightChartSetProvider : AbstractChartSetProvider
    {
        public static readonly Guid Guid = Guid.Parse("42a2001d-8686-4551-8d35-81ffbf9b45ad");
        public override Guid ChartSetGuid => Guid;
        public override string ChartSetName => "Ride Height";
        protected override LayoutDescription GetLayoutDescription()
        {
            var content = RowsDefinitionSettingBuilder.Create().WithGridSplitters().
                WithNamedContent(SeriesChartNames.SpeedChartName, SizeKind.Manual, 200).
                WithNamedContent(SeriesChartNames.RideHeightChartName, SizeKind.Manual, 200).
                WithNamedContent(AggregatedChartNames.RideHeightHistogramChartName, SizeKind.Manual, 800).
                WithContent(
                    ColumnsDefinitionSettingBuilder.Create().
                        WithNamedContent(AggregatedChartNames.RideHeightToLateralAccChartName, SizeKind.Remaining, 1).
                        WithNamedContent(AggregatedChartNames.RideHeightToLongitudinalAccChartName, SizeKind.Remaining, 1).Build(),
                    SizeKind.Manual, 800).
                Build();

            return new LayoutDescription()
            {
                RootElement = content
            };
        }
    }
}