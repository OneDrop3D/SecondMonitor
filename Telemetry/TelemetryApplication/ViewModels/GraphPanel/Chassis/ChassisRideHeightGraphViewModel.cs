namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.Chassis
{
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using TelemetryManagement.Settings;

    public class ChassisRideHeightGraphViewModel : AbstractChassisGraphViewModel
    {
        public override string Title => "Ride Height - Chassis";
        protected override string YUnits => Distance.GetUnitsSymbol(UnitsCollection.DistanceUnitsVerySmall);
        protected override double YTickInterval => Distance.FromMeters(0.02).GetByUnit(UnitsCollection.DistanceUnitsVerySmall);

        protected override double GetFrontValue(SimulatorSourceInfo simulatorSourceInfo, CarInfo carInfo, CarPropertiesDto carPropertiesDto)
        {
            return carInfo.FrontHeight?.GetByUnit(UnitsCollection.DistanceUnitsVerySmall) ?? 0;
        }

        protected override double GetRearValue(SimulatorSourceInfo simulatorSourceInfo, CarInfo carInfo, CarPropertiesDto carPropertiesDto)
        {
            return carInfo.RearHeight?.GetByUnit(UnitsCollection.DistanceUnitsVerySmall) ?? 0;
        }
    }
}