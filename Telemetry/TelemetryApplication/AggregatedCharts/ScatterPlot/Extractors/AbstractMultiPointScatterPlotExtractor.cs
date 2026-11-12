namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Extractors
{
    using System.Collections.Generic;
    using System.Linq;
    using Controllers.Settings;

    using DataModel.Telemetry;
    using Filter;
    using OxyPlot;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;

    public abstract class AbstractMultiPointScatterPlotExtractor : AbstractScatterPlotExtractor
    {
        private readonly ISettingsController _settingsController;

        protected AbstractMultiPointScatterPlotExtractor(ISettingsProvider settingsProvider, ISettingsController settingsController) : base(settingsProvider, settingsController)
        {
            _settingsController = settingsController;
        }

        public ScatterPlotSeries ExtractMultiPointSeries(IEnumerable<LapTelemetryDto> loadedLaps, IReadOnlyCollection<ITelemetryFilter> filters, string seriesTitle, OxyColor color)
        {
            ScatterPlotSeries newSeries = new ScatterPlotSeries(color, seriesTitle);
            foreach (LapTelemetryDto lapTelemetryDto in loadedLaps)
            {
                var timedTelemetrySnapshots = lapTelemetryDto.DataPoints.Where(x => filters.All(y => y.Accepts(x))).ToArray();
                if (timedTelemetrySnapshots.Length == 0)
                {
                    continue;
                }

                CarPropertiesDto carPropertiesDto = _settingsController.CarSettingsController.GetCarProperties(lapTelemetryDto);
                timedTelemetrySnapshots.ForEach(x => newSeries.AddDataPoints(GetDataPoints(x, carPropertiesDto).Select(y => (y.X, y: y.Y, x))));
            }

            return newSeries.DataPoints.Count > 0 ? newSeries : null;
        }

        protected abstract IEnumerable<(double X, double Y)> GetDataPoints(TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto);
    }
}