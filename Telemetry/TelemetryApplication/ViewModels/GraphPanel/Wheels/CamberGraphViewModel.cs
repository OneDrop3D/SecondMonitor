namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.Wheels
{
    using System;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using TelemetryManagement.Settings;

    public class CamberGraphViewModel : AbstractWheelsGraphViewModel
    {
        public override string Title => "Camber";
        protected override string YUnits => Angle.GetUnitsSymbol(UnitsCollection.AngleUnits);
        protected override double YTickInterval => Angle.GetFromDegrees(1).GetValueInUnits(UnitsCollection.AngleUnits);
        protected override bool CanYZoom => true;
        protected override Func<SimulatorSourceInfo, WheelInfo, CarPropertiesDto, double> ExtractorFunction => (_, x, __) => x?.Camber?.GetValueInUnits(UnitsCollection.AngleUnits) ?? 0.0;
    }
}