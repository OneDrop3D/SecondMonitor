namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces.Providers.BuildIn.ChartSet
{
    using System;
    using AggregatedCharts;
    using Controllers.MainWindow.GraphPanel;
    using SecondMonitor.ViewModels.Layouts;
    using SecondMonitor.ViewModels.Settings.Model.Layout;

    public class SteeringChartSetProvider : AbstractChartSetProvider
    {
        public static readonly Guid Guid = Guid.Parse("de3f49e3-9d83-4bb2-8cb1-55986c71755a");
        public override Guid ChartSetGuid => Guid;
        public override string ChartSetName => "Turning";
        protected override LayoutDescription GetLayoutDescription()
        {
            var content = RowsDefinitionSettingBuilder.Create().WithGridSplitters().
                WithNamedContent(SeriesChartNames.SpeedChartName, SizeKind.Manual, 250).
                WithNamedContent(SeriesChartNames.SteeringInputChartName, SizeKind.Manual, 250).
                WithNamedContent(SeriesChartNames.LatGChartName, SizeKind.Manual, 250).
                WithNamedContent(SeriesChartNames.CrossTyreLoad, SizeKind.Manual, 250).
                WithNamedContent(AggregatedChartNames.LatAccToSteeringInput, SizeKind.Manual, 1000).
                Build();

            return new LayoutDescription()
            {
                RootElement = content
            };
        }
    }
}