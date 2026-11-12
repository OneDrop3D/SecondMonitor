namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.Wheels
{
    using System;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using TelemetryManagement.Settings;

    public class SuspensionVelocityGraphViewModel : AbstractWheelsGraphViewModel
    {
        public override string Title => "Suspension Velocity";

        protected override string YUnits => Velocity.GetUnitSymbol(UnitsCollection.VelocityUnitsSmall);

        protected override double YTickInterval => Math.Round(Velocity.FromMs(0.05).GetValueInUnits(UnitsCollection.VelocityUnitsSmall), 2);

        protected override bool CanYZoom => true;

        protected override Func<SimulatorSourceInfo, WheelInfo, CarPropertiesDto, double> ExtractorFunction => (_, x, __) => x.SuspensionVelocity?.GetValueInUnits(UnitsCollection.VelocityUnitsSmall) ?? 0;
    }
}