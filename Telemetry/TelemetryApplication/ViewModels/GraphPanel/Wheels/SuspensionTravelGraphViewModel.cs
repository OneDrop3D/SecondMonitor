namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.Wheels
{
    using System;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using TelemetryManagement.Settings;

    public class SuspensionTravelGraphViewModel : AbstractWheelsGraphViewModel
    {
        public override string Title => "Suspension Travel";
        protected override string YUnits => Distance.GetUnitsSymbol(UnitsCollection.DistanceUnitsVerySmall);
        protected override double YTickInterval => Math.Round(Distance.FromMeters(0.01).GetByUnit(UnitsCollection.DistanceUnitsVerySmall), 2);
        protected override bool CanYZoom => true;
        protected override Func<SimulatorSourceInfo, WheelInfo, CarPropertiesDto, double> ExtractorFunction => (_, x, __) => x.SuspensionTravel?.GetByUnit(UnitsCollection.DistanceUnitsVerySmall) ?? 0;
    }
}