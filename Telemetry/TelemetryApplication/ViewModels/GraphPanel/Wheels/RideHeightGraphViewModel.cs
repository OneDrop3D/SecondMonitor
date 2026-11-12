namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.Wheels
{
    using System;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using TelemetryManagement.Settings;

    public class RideHeightGraphViewModel : AbstractWheelsGraphViewModel
    {
        public override string Title => "Ride Height - Wheels";
        protected override string YUnits => Distance.GetUnitsSymbol(UnitsCollection.DistanceUnitsVerySmall);
        protected override double YTickInterval => Math.Round(Distance.FromMeters(0.01).GetByUnit(UnitsCollection.DistanceUnitsVerySmall), 2);
        protected override bool CanYZoom => true;
        protected override Func<SimulatorSourceInfo, WheelInfo, CarPropertiesDto, double> ExtractorFunction => (_, x, __) => x.RideHeight?.GetByUnit(UnitsCollection.DistanceUnitsVerySmall) ?? 0;
    }
}