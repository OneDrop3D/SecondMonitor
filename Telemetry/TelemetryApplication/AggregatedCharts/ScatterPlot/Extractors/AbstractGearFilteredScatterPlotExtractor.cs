namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Extractors
{
    using System.Collections.Generic;
    using System.Linq;
    using Controllers.Settings;
    using Filter;
    using OxyPlot;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.DTO;

    public abstract class AbstractGearFilteredScatterPlotExtractor : AbstractScatterPlotExtractor
    {
        protected static readonly Dictionary<string, OxyColor> ColorMap = new Dictionary<string, OxyColor>()
        {
            { "N", OxyColor.Parse("#ff9900") },
            { "1", OxyColor.Parse("#99ffcc") },
            { "2", OxyColor.Parse("#0066ff") },
            { "3", OxyColor.Parse("#33cccc") },
            { "4", OxyColor.Parse("#E5FBE4") },
            { "5", OxyColor.Parse("#ff531a") },
            { "6", OxyColor.Parse("#1aff1a") },
            { "7", OxyColor.Parse("#0000ff") },
            { "8", OxyColor.Parse("#cccc00") },
        };

        private readonly IReadOnlyCollection<ITelemetryFilter> _allFilters;
        private readonly IGearTelemetryFilter _gearTelemetryFilter;

        protected AbstractGearFilteredScatterPlotExtractor(ISettingsProvider settingsProvider, IEnumerable<ITelemetryFilter> filters, IGearTelemetryFilter gearTelemetryFilter, ISettingsController settingsController)
            : base(settingsProvider, settingsController)
        {
            _allFilters = filters.Append(gearTelemetryFilter).ToList();
            _gearTelemetryFilter = gearTelemetryFilter;
        }

        public ScatterPlotSeries ExtractSeriesForGear(IEnumerable<LapTelemetryDto> loadedLaps, string gear)
        {
            return ExtractSeriesForGear(loadedLaps, gear, false, $"Gear {gear}");
        }

        public ScatterPlotSeries ExtractSeriesForGear(IEnumerable<LapTelemetryDto> loadedLaps, string gear, bool onThrottle, string title)
        {
            if (!ColorMap.TryGetValue(gear, out OxyColor color))
            {
                color = OxyColors.Azure;
            }

            return ExtractSeriesForGear(loadedLaps, gear, onThrottle, title, color);
        }

        public ScatterPlotSeries ExtractSeriesForAllGears(IEnumerable<LapTelemetryDto> loadedLaps, string title, OxyColor color)
        {
            _gearTelemetryFilter.FilterGear = string.Empty;
            _gearTelemetryFilter.OnThrottle = false;
            return ExtractSeries(loadedLaps, _allFilters, title, color);
        }

        public ScatterPlotSeries ExtractSeriesForGear(IEnumerable<LapTelemetryDto> loadedLaps, string gear, string title, OxyColor color)
        {
            _gearTelemetryFilter.FilterGear = gear;
            _gearTelemetryFilter.OnThrottle = false;
            return ExtractSeries(loadedLaps, _allFilters, title, color);
        }

        public ScatterPlotSeries ExtractSeriesForAllGears(IEnumerable<LapTelemetryDto> loadedLaps, bool onThrottle, string title, OxyColor color)
        {
            _gearTelemetryFilter.FilterGear = string.Empty;
            _gearTelemetryFilter.OnThrottle = onThrottle;
            return ExtractSeries(loadedLaps, _allFilters, title, color);
        }

        public ScatterPlotSeries ExtractSeriesForGear(IEnumerable<LapTelemetryDto> loadedLaps, string gear, bool onThrottle, string title, OxyColor color)
        {
            _gearTelemetryFilter.FilterGear = gear;
            _gearTelemetryFilter.OnThrottle = onThrottle;
            return ExtractSeries(loadedLaps, _allFilters, title, color);
        }
    }
}