namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces.Providers.BuildIn.ChartSet
{
    using System;
    using Controllers.MainWindow.GraphPanel;
    using SecondMonitor.ViewModels.Layouts;
    using SecondMonitor.ViewModels.Settings.Model.Layout;

    public class CompareChartSetProvider : AbstractChartSetProvider
    {
        public static readonly Guid Guid = Guid.Parse("ff694a34-f23d-42f9-956d-60d9ad076e74");
        public override Guid ChartSetGuid => Guid;
        public override string ChartSetName => "Compare";
        protected override LayoutDescription GetLayoutDescription()
        {
            var content = RowsDefinitionSettingBuilder.Create().WithGridSplitters().
                WithNamedContent(SeriesChartNames.LapTimeChartName, SizeKind.Manual, 250).
                WithNamedContent(SeriesChartNames.SpeedChartName, SizeKind.Manual, 350).
                WithNamedContent(SeriesChartNames.EngineRpmChartName, SizeKind.Manual, 200).
                WithNamedContent(SeriesChartNames.GearChartName, SizeKind.Manual, 150).
                WithNamedContent(SeriesChartNames.SteeringAngleChartName, SizeKind.Manual, 300).
                WithNamedContent(SeriesChartNames.ThrottleChartName, SizeKind.Manual, 300).
                WithNamedContent(SeriesChartNames.BrakeChartName, SizeKind.Manual, 300).
                Build();

            return new LayoutDescription()
            {
                RootElement = content
            };
        }
    }
}