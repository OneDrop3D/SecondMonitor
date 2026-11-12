namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.Wheels
{
    using System;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using TelemetryManagement.Settings;

    public class BrakeTemperaturesGraphViewModel : AbstractWheelsGraphViewModel
    {
        public override string Title => "Brake Temperature";
        protected override string YUnits => Temperature.GetUnitSymbol(UnitsCollection.TemperatureUnits);
        protected override double YTickInterval => 100;
        protected override bool CanYZoom => true;
        protected override Func<SimulatorSourceInfo, WheelInfo, CarPropertiesDto, double> ExtractorFunction => (_, x, __) => x.BrakeTemperature.ActualQuantity.GetValueInUnits(UnitsCollection.TemperatureUnits);
    }
}