namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Extractors
{
    using System;
    using System.Collections.Generic;
    using Controllers.Settings;
    using DataModel.BasicProperties;
    using DataModel.Snapshot.Systems;
    using DataModel.Telemetry;
    using Filter;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.Settings;

    public class SpeedToRakeExtractor : AbstractScatterPlotExtractor
    {
        public SpeedToRakeExtractor(ISettingsProvider settingsProvider, IList<ITelemetryFilter> filters, ISettingsController settingsController) : base(settingsProvider, filters, settingsController)
        {
        }

        public override string YUnit => Distance.GetUnitsSymbol(DistanceUnitsSmall);
        public override string XUnit => Velocity.GetUnitSymbol(VelocityUnits);
        public override double XMajorTickSize => VelocityUnits == VelocityUnits.Mph ? Velocity.FromMph(50).GetValueInUnits(VelocityUnits) : Velocity.FromKph(50).GetValueInUnits(VelocityUnits);
        public override double YMajorTickSize => Math.Round(Distance.FromMeters(0.05).GetByUnit(DistanceUnitsSmall));

        protected override double GetXValue(TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return snapshot.PlayerData.Speed.GetValueInUnits(VelocityUnits);
        }

        protected override double GetYValue(TimedTelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            double frontHeight = 0;
            double rearHeight = 0;
            CarInfo carInfo = snapshot.PlayerData.CarInfo;
            if (carInfo.FrontHeight != null && carInfo.RearHeight != null && !carInfo.FrontHeight.IsZero)
            {
                rearHeight = carInfo.RearHeight.GetByUnit(DistanceUnitsSmall);
                frontHeight = carInfo.FrontHeight.GetByUnit(DistanceUnitsSmall);
            }
            else if (carInfo.WheelsInfo?.FrontLeft?.RideHeight != null)
            {
                Wheels wheels = carInfo.WheelsInfo;
                frontHeight = (wheels.FrontLeft.RideHeight.GetByUnit(DistanceUnitsSmall) + wheels.FrontRight.RideHeight.GetByUnit(DistanceUnitsSmall)) / 2;
                rearHeight = (wheels.RearLeft.RideHeight.GetByUnit(DistanceUnitsSmall) + wheels.RearRight.RideHeight.GetByUnit(DistanceUnitsSmall)) / 2;
            }

            double rake = rearHeight - frontHeight;
            return rake;
        }
    }
}