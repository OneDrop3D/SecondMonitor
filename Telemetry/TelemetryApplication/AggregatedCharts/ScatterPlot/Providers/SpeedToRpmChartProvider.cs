namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Providers
{
    using System.Collections.Generic;
    using System.Linq;
    using Controllers.Settings;
    using Controllers.Synchronization;
    using Extractors;
    using OxyPlot;
    using OxyPlot.Annotations;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Telemetry;
    using SecondMonitor.Telemetry.TelemetryManagement.DTO;
    using ViewModels.LoadedLapCache;

    public class SpeedToRpmChartProvider : AbstractGearsChartProvider
    {
        private readonly SpeedToRpmScatterPlotExtractor _speedToRpmScatterPlotExtractor;
        public SpeedToRpmChartProvider(ILoadedLapsCache loadedLapsCache, SpeedToRpmScatterPlotExtractor speedToRpmScatterPlotExtractor, IDataPointSelectionSynchronization dataPointSelectionSynchronization, ISettingsController settingsController)
            : base(loadedLapsCache, speedToRpmScatterPlotExtractor, dataPointSelectionSynchronization, settingsController)
        {
            _speedToRpmScatterPlotExtractor = speedToRpmScatterPlotExtractor;
        }

        public override string ChartName => "Speed vs RPM";
        public override AggregatedChartKind Kind => AggregatedChartKind.ScatterPlot;

        public override void DidCreateScatterPlotAllGear(ScatterPlot scatterPlot, IEnumerable<LapTelemetryDto> loadedLaps)
        {
            List<LapTelemetryDto> lapTelemetryDtos = loadedLaps.ToList();
            base.DidCreateScatterPlotAllGear(scatterPlot, lapTelemetryDtos);

            List<TimedTelemetrySnapshot> dataPoints = lapTelemetryDtos
                .SelectMany(lap => lap.DataPoints)
                .Where(dp => dp?.PlayerData is { CarInfo: not null })
                .Where(dp => dp.PlayerData.CarInfo.EngineTorque != null).ToList();

            double? maxTorqueRpm = dataPoints.Count > 0 ? dataPoints.MaxBy(dp => dp.PlayerData.CarInfo.EngineTorque.InNm)
                ?.PlayerData.CarInfo.EngineRpm : null;

            dataPoints = lapTelemetryDtos
                .SelectMany(lap => lap.DataPoints)
                .Where(dp => dp?.PlayerData is { CarInfo: not null })
                .Where(dp => dp.PlayerData.CarInfo.EnginePower != null).ToList();

            double? maxPowerRpm = dataPoints.Count > 0 ? dataPoints.MaxBy(dp => dp.PlayerData.CarInfo.EnginePower.InKw)
                ?.PlayerData.CarInfo.EngineRpm : null;

            int? maxEngineRpm = lapTelemetryDtos
                .SelectMany(lap => lap.DataPoints ?? new List<TimedTelemetrySnapshot>())
                .Where(dp => dp?.PlayerData != null && dp.PlayerData.CarInfo != null)
                .MaxBy(dp => dp.PlayerData.CarInfo.EngineRpm)
                ?.PlayerData.CarInfo.EngineRpm;

            if (maxTorqueRpm.HasValue)
            {
                scatterPlot.AddAnnotation(new LineAnnotation() { Slope = 0, Intercept = maxTorqueRpm.Value, Color = OxyColors.ForestGreen, StrokeThickness = 1, LineStyle = LineStyle.Dash });
            }

            if (maxPowerRpm.HasValue)
            {
                scatterPlot.AddAnnotation(new LineAnnotation() { Slope = 0, Intercept = maxPowerRpm.Value, Color = OxyColors.Red, StrokeThickness = 1, LineStyle = LineStyle.Dash });
            }

            if (maxEngineRpm.HasValue)
            {
                scatterPlot.AddAnnotation(new LineAnnotation() { Slope = 0, Intercept = maxEngineRpm.Value, Color = OxyColors.Red, StrokeThickness = 1, LineStyle = LineStyle.Solid });
            }
        }

        public override void ScatterPlotAllGearSeriesTitle(ref string title, IEnumerable<LapTelemetryDto> loadedLaps, int gear)
        {
            List<LapTelemetryDto> lapTelemetryDtos = loadedLaps.ToList();
            base.ScatterPlotAllGearSeriesTitle(ref title, lapTelemetryDtos, gear);

            if (lapTelemetryDtos.Count == 0)
            {
                return;
            }

            List<TimedTelemetrySnapshot> eligiblePoints = lapTelemetryDtos
                .SelectMany(lap => lap.DataPoints ?? new List<TimedTelemetrySnapshot>())
                .Where(dp => dp?.PlayerData is { CarInfo: not null })
                .Where(dp => dp.PlayerData.CarInfo.CurrentGear == gear.ToString())
                .Where(dp => dp.InputInfo.ThrottlePedalPosition > 0.05).ToList();

            if (eligiblePoints.Count == 0)
            {
                return;
            }

            double averagePowerInKw = eligiblePoints.Average(dp => dp.PlayerData.CarInfo.EnginePower.InKw);
            title += $": {Power.FromKw(averagePowerInKw).GetValueInUnit(_speedToRpmScatterPlotExtractor.PowerUnits):F2} {Power.GetUnitSymbol(_speedToRpmScatterPlotExtractor.PowerUnits)}";
        }
    }
}