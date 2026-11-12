namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.Wheels
{
    using System;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using TelemetryManagement.Settings;

    public class TyrePressuresGraphViewModel : AbstractWheelsGraphViewModel
    {
        public override string Title => "Tyre Pressures";
        protected override string YUnits => Pressure.GetUnitSymbol(UnitsCollection.PressureUnits);
        protected override double YTickInterval => Pressure.FromKiloPascals(20).GetValueInUnits(UnitsCollection.PressureUnits);
        protected override bool CanYZoom => true;
        protected override Func<SimulatorSourceInfo, WheelInfo, CarPropertiesDto, double> ExtractorFunction => (_, x, __) => x.TyrePressure.ActualQuantity.GetValueInUnits(UnitsCollection.PressureUnits);
    }
}