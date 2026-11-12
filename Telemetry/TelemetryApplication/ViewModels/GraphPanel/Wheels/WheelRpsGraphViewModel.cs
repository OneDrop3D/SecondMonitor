namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.Wheels
{
    using System;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using TelemetryManagement.Settings;

    public class WheelRpsGraphViewModel : AbstractWheelsGraphViewModel
    {
        public override string Title => "Tyre Rps";
        protected override string YUnits => "Rad / s";
        protected override double YTickInterval => 100;
        protected override bool CanYZoom => true;
        protected override Func<SimulatorSourceInfo, WheelInfo, CarPropertiesDto, double> ExtractorFunction => (_, x, __) => x.Rps;
    }
}