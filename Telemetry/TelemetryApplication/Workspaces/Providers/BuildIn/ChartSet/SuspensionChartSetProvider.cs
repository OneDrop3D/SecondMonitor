namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces.Providers.BuildIn.ChartSet
{
    using System;
    using AggregatedCharts;
    using Controllers.MainWindow.GraphPanel;
    using SecondMonitor.ViewModels.Layouts;
    using SecondMonitor.ViewModels.Settings.Model.Layout;

    public class SuspensionChartSetProvider : AbstractChartSetProvider
    {
        public static readonly Guid Guid = Guid.Parse("8ab5261c-0723-4192-a912-ddeed8883418");
        public override Guid ChartSetGuid => Guid;
        public override string ChartSetName => "Suspension";
        protected override LayoutDescription GetLayoutDescription()
        {
            var content = RowsDefinitionSettingBuilder.Create().WithGridSplitters().
                WithNamedContent(SeriesChartNames.SpeedChartName, SizeKind.Manual, 250).
                WithNamedContent(SeriesChartNames.SuspensionTravelChartName, SizeKind.Manual, 250).
                WithNamedContent(SeriesChartNames.SuspensionVelocityChartName, SizeKind.Manual, 250).
                WithNamedContent(AggregatedChartNames.SuspensionVelocityHistogramChartName, SizeKind.Manual, 1000).
                WithNamedContent(AggregatedChartNames.SuspensionVelocityLineSeriesChartName, SizeKind.Manual, 500).
                WithNamedContent(AggregatedChartNames.SuspensionTravelToSpeedChartName, SizeKind.Manual, 800).
                WithNamedContent(AggregatedChartNames.LatAccelerationToFrontRollAngleChartName, SizeKind.Manual, 500).
                WithNamedContent(AggregatedChartNames.LatAccelerationToRearRollAngleChartName, SizeKind.Manual, 500).
                WithNamedContent(AggregatedChartNames.RearToFrontRollAngleChartName, SizeKind.Manual, 1000).
                Build();

            return new LayoutDescription()
            {
                RootElement = content
            };
        }
    }
}