namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces.Providers.BuildIn.ChartSet
{
    using System;
    using AggregatedCharts;
    using Controllers.MainWindow.GraphPanel;
    using SecondMonitor.ViewModels.Layouts;
    using SecondMonitor.ViewModels.Settings.Model.Layout;

    public class EngineChartSetProvider : AbstractChartSetProvider
    {
        public static readonly Guid Guid = Guid.Parse("93ced1cb-660d-4665-a8f1-ad6518703f11");
        public override Guid ChartSetGuid => Guid;
        public override string ChartSetName => "Engine";
        protected override LayoutDescription GetLayoutDescription()
        {
            var rpmColumns = ColumnsDefinitionSettingBuilder.Create().
                WithNamedContent(AggregatedChartNames.SpeedToRpmChartName, SizeKind.Remaining, 2).
                WithNamedContent(AggregatedChartNames.PowerCurveChartName, SizeKind.Remaining, 1).Build();

            var content = RowsDefinitionSettingBuilder.Create().
                WithNamedContent(SeriesChartNames.CoolantTemperature, SizeKind.Manual, 200).
                WithNamedContent(SeriesChartNames.OilTemperature, SizeKind.Manual, 200).
                WithNamedContent(AggregatedChartNames.RpmHistogramChartName, SizeKind.Manual, 600).
                WithContent(rpmColumns, SizeKind.Manual, 900).
                WithNamedContent(AggregatedChartNames.WheelSlipAccelerationChartName, SizeKind.Manual, 900).
                Build();

            var columns = ColumnsDefinitionSettingBuilder.Create().WithContent(content, SizeKind.Remaining, 1).Build();

            return new LayoutDescription()
            {
                RootElement = columns
            };
        }
    }
}