namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Extractors
{
    using System.Collections.Generic;
    using Controllers.Settings;
    using DataModel.BasicProperties;
    using DataModel.Telemetry;
    using Filter;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.Settings;

    public class LatAccelerationToSteeringInputExtractor : AbstractScatterPlotExtractor
    {
        private readonly SpeedFilter _speedFilter;

        public LatAccelerationToSteeringInputExtractor(ISettingsProvider settingsProvider, SpeedFilter speedFilter, ISettingsController settingsController)
            : base(settingsProvider, new List<ITelemetryFilter>() { speedFilter }, settingsController)
        {
            _speedFilter = speedFilter;
        }

        public override string YUnit => "%";
        public override string XUnit => "G";
        public override double XMajorTickSize => 1;
        public override double YMajorTickSize => 50;

        public Velocity MinimalSpeed
        {
            get => _speedFilter.MinimalSpeed;
            set => _speedFilter.MinimalSpeed = value;
        }

        public Velocity MaximumSpeed
        {
            get => _speedFilter.MaximumSpeed;
            set => _speedFilter.MaximumSpeed = value;
        }

        protected override double GetXValue(TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return snapshot.PlayerData.CarInfo.Acceleration.XinG;
        }

        protected override double GetYValue(TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return snapshot.InputInfo.SteeringInput * 100;
        }
    }
}