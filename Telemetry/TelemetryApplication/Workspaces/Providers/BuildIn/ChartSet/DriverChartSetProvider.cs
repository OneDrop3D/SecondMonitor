namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces.Providers.BuildIn.ChartSet
{
    using System;
    using Controllers.MainWindow.GraphPanel;
    using SecondMonitor.ViewModels.Layouts;
    using SecondMonitor.ViewModels.Settings.Model.Layout;

    public class DriverChartSetProvider : AbstractChartSetProvider
    {
        public static readonly Guid Guid = Guid.Parse("bf22e691-5796-40da-ac7e-e0d939985251");
        public override Guid ChartSetGuid => Guid;
        public override string ChartSetName => "Driver";
        protected override LayoutDescription GetLayoutDescription()
        {
            var content = RowsDefinitionSettingBuilder.Create().WithGridSplitters().
                WithNamedContent(SeriesChartNames.SpeedChartName, SizeKind.Manual, 350).
                WithNamedContent(SeriesChartNames.EngineRpmChartName, SizeKind.Manual, 200).
                WithNamedContent(SeriesChartNames.GearChartName, SizeKind.Manual, 150).
                WithNamedContent(SeriesChartNames.SteeringAngleChartName, SizeKind.Manual, 300).
                WithNamedContent(SeriesChartNames.CombinedPedalsChartName, SizeKind.Manual, 300).
                WithNamedContent(SeriesChartNames.LatGChartName, SizeKind.Manual, 300).
                WithNamedContent(SeriesChartNames.LongGChartName, SizeKind.Manual, 300).
                WithNamedContent(SeriesChartNames.TotalGChartName, SizeKind.Manual, 300).
                Build();

            return new LayoutDescription()
            {
                RootElement = content
            };
        }
    }
}