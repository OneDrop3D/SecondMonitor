namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Providers
{
    using Controllers.Settings;
    using Controllers.Synchronization;
    using Extractors;
    using ViewModels.LoadedLapCache;

    public class CamberToLateralGChartProvider : AbstractWheelChartProvider
    {
        public CamberToLateralGChartProvider(CamberToLateralGExtractor dataExtractor, ILoadedLapsCache loadedLaps, IDataPointSelectionSynchronization dataPointSelectionSynchronization, ISettingsController settingsController) : base(dataExtractor, loadedLaps, dataPointSelectionSynchronization, settingsController)
        {
        }

        public override string ChartName => "Camber / Lateral Acceleration";

        protected override AxisDefinition CreateYAxisDefinition()
        {
            var axisDefinition = base.CreateYAxisDefinition();
            axisDefinition.Minimum = -8;
            axisDefinition.Maximum = 1;
            axisDefinition.UseCustomRange = true;
            return axisDefinition;
        }
    }
}