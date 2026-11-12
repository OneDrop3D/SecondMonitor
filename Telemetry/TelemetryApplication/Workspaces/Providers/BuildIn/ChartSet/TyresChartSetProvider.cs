namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces.Providers.BuildIn.ChartSet
{
    using System;
    using AggregatedCharts;
    using Controllers.MainWindow.GraphPanel;
    using SecondMonitor.ViewModels.Layouts;
    using SecondMonitor.ViewModels.Settings.Model.Layout;

    public class TyresChartSetProvider : AbstractChartSetProvider
    {
        public static readonly Guid Guid = Guid.Parse("ab5373d2-164d-4f7e-a32e-c17b99ce8c88");
        public override Guid ChartSetGuid => Guid;
        public override string ChartSetName => "Tyres";

        protected override LayoutDescription GetLayoutDescription()
        {
            var frontTyresTemperatures = ColumnsDefinitionSettingBuilder.Create()
                .WithNamedContent(SeriesChartNames.LeftFrontTyreTempChartName, SizeKind.Remaining, 1)
                .WithNamedContent(SeriesChartNames.RightFrontTyreTempChartName, SizeKind.Remaining, 1).Build();

            var rearTyresTemperatures = ColumnsDefinitionSettingBuilder.Create()
                .WithNamedContent(SeriesChartNames.LeftRearTyreTempChartName, SizeKind.Remaining, 1)
                .WithNamedContent(SeriesChartNames.RightRearTyreTempChartName, SizeKind.Remaining, 1).Build();

            var tyresPressures = ColumnsDefinitionSettingBuilder.Create()
                .WithNamedContent(SeriesChartNames.OutsideInsideTyresTempChartName, SizeKind.Remaining, 1)
                .WithNamedContent(SeriesChartNames.TyrePressuresChartName, SizeKind.Remaining, 1).Build();

            var content = RowsDefinitionSettingBuilder.Create().WithGridSplitters()
                .WithContent(frontTyresTemperatures, SizeKind.Manual, 250)
                .WithContent(rearTyresTemperatures, SizeKind.Manual, 250)
                .WithContent(tyresPressures, SizeKind.Manual, 250)
                .WithNamedContent(SeriesChartNames.TyreLoadChartName, SizeKind.Manual, 250)
                .WithNamedContent(AggregatedChartNames.TyreLoadHistogramChartName, SizeKind.Manual, 1000)
                .WithNamedContent(SeriesChartNames.CamberChartName, SizeKind.Manual, 250)
                .WithNamedContent(AggregatedChartNames.CamberHistogramChartName, SizeKind.Manual, 1000)
                .Build();

            return new LayoutDescription()
            {
                RootElement = content
            };
        }
    }
}