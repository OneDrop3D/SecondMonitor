namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces.Providers.BuildIn.ChartSet
{
    using System;
    using Controllers.MainWindow.GraphPanel;
    using SecondMonitor.ViewModels.Layouts;
    using SecondMonitor.ViewModels.Settings.Model.Layout;

    public class DifferentialChartSetProvider : AbstractChartSetProvider
    {
        public static readonly Guid Guid = Guid.Parse("22521cfd-58b9-4c4d-b882-872e7f696523");
        public override Guid ChartSetGuid => Guid;
        public override string ChartSetName => "Differential";
        protected override LayoutDescription GetLayoutDescription()
        {
            var content = RowsDefinitionSettingBuilder.Create().WithGridSplitters().
                WithNamedContent(SeriesChartNames.LapTimeChartName, SizeKind.Manual, 250).
                WithNamedContent(SeriesChartNames.SpeedChartName, SizeKind.Manual, 350).
                WithNamedContent(SeriesChartNames.CombinedPedalsChartName, SizeKind.Manual, 300).
                WithNamedContent(SeriesChartNames.RearTyresSlip, SizeKind.Manual, 300).
                WithNamedContent(SeriesChartNames.RearTyresRps, SizeKind.Manual, 300).
                WithNamedContent(SeriesChartNames.RearTyresRpsDiff, SizeKind.Manual, 300).
                WithNamedContent(SeriesChartNames.FrontTyresSlip, SizeKind.Manual, 300).
                WithNamedContent(SeriesChartNames.FrontTyresRps, SizeKind.Manual, 300).
                WithNamedContent(SeriesChartNames.FrontTyresRpsDiff, SizeKind.Manual, 300).
                Build();

            return new LayoutDescription()
            {
                RootElement = content
            };
        }
    }
}
