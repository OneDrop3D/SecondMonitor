namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces.Providers.BuildIn.ChartSet
{
    using System;
    using AggregatedCharts;
    using Controllers.MainWindow.GraphPanel;
    using SecondMonitor.ViewModels.Layouts;
    using SecondMonitor.ViewModels.Settings.Model.Layout;

    public class BrakesChartSetProvider : AbstractChartSetProvider
    {
        public static readonly Guid Guid = Guid.Parse("ca5fef56-4ff0-4f10-8851-7f7ddfd38064");
        public override Guid ChartSetGuid => Guid;
        public override string ChartSetName => "Braking";
        protected override LayoutDescription GetLayoutDescription()
        {
            var content = RowsDefinitionSettingBuilder.Create().WithGridSplitters().
                WithNamedContent(SeriesChartNames.SpeedChartName, SizeKind.Manual, 350).
                WithNamedContent(SeriesChartNames.BrakeChartName, SizeKind.Manual, 300).
                WithNamedContent(SeriesChartNames.LongGChartName, SizeKind.Manual, 300).
                WithNamedContent(SeriesChartNames.WheelRpsChartName, SizeKind.Manual, 300).
                WithNamedContent(SeriesChartNames.BrakesTemperatureFrontRearChartName, SizeKind.Manual, 300).
                WithNamedContent(AggregatedChartNames.WheelSlipBrakingChartName, SizeKind.Manual, 800).
                Build();

            return new LayoutDescription()
            {
                RootElement = content
            };
        }
    }
}