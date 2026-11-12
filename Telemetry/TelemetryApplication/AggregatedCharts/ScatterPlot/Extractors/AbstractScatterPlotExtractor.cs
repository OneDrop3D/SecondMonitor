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

    public abstract class AbstractScatterPlotExtractor : AbstractTelemetryDataExtractor
    {
        private readonly ISettingsController _settingsController;
        private readonly IList<ITelemetryFilter> _filters;

        protected AbstractScatterPlotExtractor(ISettingsProvider settingsProvider, ISettingsController settingsController) : this(settingsProvider, new List<ITelemetryFilter>(), settingsController)
        {
        }

        protected AbstractScatterPlotExtractor(ISettingsProvider settingsProvider, IList<ITelemetryFilter> filters, ISettingsController settingsController) : base(settingsProvider, settingsController)
        {
            _filters = filters;
            _settingsController = settingsController;
        }

        public abstract string YUnit { get; }
        public abstract string XUnit { get; }
        public abstract double XMajorTickSize { get; }
        public abstract double YMajorTickSize { get; }

        public ScatterPlotSeries ExtractSeries(IEnumerable<LapTelemetryDto> loadedLaps, CarPropertiesDto carPropertiesDto, string seriesTitle, OxyColor color)
        {
            return ExtractSeries(loadedLaps, Enumerable.Empty<ITelemetryFilter>(), seriesTitle, color);
        }

        public ScatterPlotSeries ExtractSeries(IEnumerable<LapTelemetryDto> loadedLaps, IEnumerable<ITelemetryFilter> filters, string seriesTitle, OxyColor color)
        {
            List<ITelemetryFilter> allFilters = filters.Concat(_filters).ToList();

            ScatterPlotSeries newSeries = new ScatterPlotSeries(color, seriesTitle);
            foreach (LapTelemetryDto lapTelemetryDto in loadedLaps)
            {
                var timedTelemetrySnapshots = lapTelemetryDto.DataPoints.Where(x => allFilters.All(y => y.Accepts(x))).ToArray();
                if (timedTelemetrySnapshots.Length == 0)
                {
                    continue;
                }

                CarPropertiesDto carPropertiesDto = _settingsController.CarSettingsController.GetCarProperties(lapTelemetryDto);
                timedTelemetrySnapshots.ForEach(x => newSeries.AddDataPoint(GetXValue(x, carPropertiesDto), GetYValue(x, carPropertiesDto), x));
            }

            return newSeries.DataPoints.Count > 0 ? newSeries : null;
        }

        protected abstract double GetXValue(TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto);
        protected abstract double GetYValue(TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto);
    }
}