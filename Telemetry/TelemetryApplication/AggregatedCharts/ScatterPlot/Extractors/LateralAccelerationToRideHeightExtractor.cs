namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Extractors
{
    using System.Collections.Generic;
    using Controllers.Settings;
    using DataModel.Snapshot.Systems;
    using DataModel.Telemetry;
    using Filter;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.Settings;

    public class LateralAccelerationToRideHeightExtractor : HorizontalAccelerationToRideHeightExtractor
    {
        public LateralAccelerationToRideHeightExtractor(ISettingsProvider settingsProvider, IEnumerable<ITelemetryFilter> filters, ISettingsController settingsController)
            : base(settingsProvider, filters, settingsController)
        {
        }

        protected override double GetXWheelValue(WheelInfo wheelInfo, TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return snapshot.PlayerData.CarInfo.Acceleration.XinG;
        }
    }
}