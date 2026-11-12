namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.Wheels
{
    using System;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using TelemetryManagement.Settings;

    public class InsideOutsideTyreTempGraphViewModel : AbstractWheelsGraphViewModel
    {
        public override string Title => "Inside / Outside Tyre Temps";
        protected override string YUnits => Temperature.GetUnitSymbol(UnitsCollection.TemperatureUnits);
        protected override double YTickInterval => 1; //Math.Round(Temperature.FromCelsius(25).GetValueInUnits(TemperatureUnits));
        protected override bool CanYZoom => true;
        protected override Func<SimulatorSourceInfo, WheelInfo, CarPropertiesDto, double> ExtractorFunction => (_, x, __) => Math.Round(Math.Abs(x.LeftTyreTemp.ActualQuantity.GetValueInUnits(UnitsCollection.TemperatureUnits) - x.RightTyreTemp.ActualQuantity.GetValueInUnits(UnitsCollection.TemperatureUnits)), 2);
    }
}