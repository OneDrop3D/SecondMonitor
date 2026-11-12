namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.DualSeries
{
    using System;
    using DataModel.Telemetry;
    using OxyPlot;

    public class RearWheelsSlip : AbstractDualSeriesGraphViewModel
    {
        public override string Title => "Rear Tyres Slip";
        protected override string YUnits => string.Empty;
        protected override double YTickInterval => 0.5;
        protected override bool CanYZoom => true;
        protected override string Series1Title => "Left";
        protected override string Series2Title => "Right";
        protected override Func<TimedTelemetrySnapshot, double> Series1ExtractFunc => x => x.PlayerData.CarInfo.WheelsInfo.RearLeft.Slip;
        protected override Func<TimedTelemetrySnapshot, double> Series2ExtractFunc => x => x.PlayerData.CarInfo.WheelsInfo.RearRight.Slip;
        protected override OxyColor Series1Color => OxyColors.Red;
        protected override OxyColor Series2Color => OxyColors.Green;
    }
}