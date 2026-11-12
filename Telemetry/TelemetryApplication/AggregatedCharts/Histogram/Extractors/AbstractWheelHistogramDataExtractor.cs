namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.Histogram.Extractors
{
    using System;
    using System.Collections.Generic;
    using Controllers.Settings;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using DataModel.Telemetry;
    using Filter;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;

    public abstract class AbstractWheelHistogramDataExtractor : AbstractHistogramDataExtractor
    {
        protected AbstractWheelHistogramDataExtractor(ISettingsProvider settingsProvider, ISettingsController settingsController) : base(settingsProvider, settingsController)
        {
        }

        protected abstract Func<SimulatorSourceInfo, WheelInfo, CarPropertiesDto, double> WheelValueExtractor { get; }

        public Histogram ExtractHistogramFrontLeft(IEnumerable<LapTelemetryDto> loadedLaps, double bandSize, IReadOnlyCollection<ITelemetryFilter> filters)
        {
            double ExtractFunc(TimedTelemetrySnapshot x, CarPropertiesDto carPropertiesDto) => WheelValueExtractor(x.SimulatorSourceInfo, x.PlayerData.CarInfo.WheelsInfo.FrontLeft, carPropertiesDto);
            return ExtractHistogram(loadedLaps, ExtractFunc, filters, bandSize, "Front Left");
        }

        public Histogram ExtractHistogramFrontRight(IEnumerable<LapTelemetryDto> loadedLaps, double bandSize, IReadOnlyCollection<ITelemetryFilter> filters)
        {
            double ExtractFunc(TimedTelemetrySnapshot x, CarPropertiesDto carPropertiesDto) => WheelValueExtractor(x.SimulatorSourceInfo, x.PlayerData.CarInfo.WheelsInfo.FrontRight, carPropertiesDto);
            return ExtractHistogram(loadedLaps, ExtractFunc, filters, bandSize, "Front Right");
        }

        public Histogram ExtractHistogramRearLeft(IEnumerable<LapTelemetryDto> loadedLaps, double bandSize, IReadOnlyCollection<ITelemetryFilter> filters)
        {
            double ExtractFunc(TimedTelemetrySnapshot x, CarPropertiesDto carPropertiesDto) => WheelValueExtractor(x.SimulatorSourceInfo, x.PlayerData.CarInfo.WheelsInfo.RearLeft, carPropertiesDto);
            return ExtractHistogram(loadedLaps, ExtractFunc, filters, bandSize, "Rear Left");
        }

        public Histogram ExtractHistogramRearRight(IEnumerable<LapTelemetryDto> loadedLaps, double bandSize, IReadOnlyCollection<ITelemetryFilter> filters)
        {
            double ExtractFunc(TimedTelemetrySnapshot x, CarPropertiesDto carPropertiesDto) => WheelValueExtractor(x.SimulatorSourceInfo, x.PlayerData.CarInfo.WheelsInfo.RearRight, carPropertiesDto);
            return ExtractHistogram(loadedLaps, ExtractFunc, filters, bandSize, "Rear Right");
        }
    }
}