namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.DualSeries
{
    using System;
    using DataModel.Telemetry;
    using OxyPlot;

    public class FrontWheelsRps : AbstractDualSeriesGraphViewModel
    {
        public override string Title => "Front Tyres Rps";
        protected override string YUnits => "Rad / s";
        protected override double YTickInterval => 100;
        protected override bool CanYZoom => true;
        protected override string Series1Title => "Left";
        protected override string Series2Title => "Right";
        protected override Func<TimedTelemetrySnapshot, double> Series1ExtractFunc => x => x.PlayerData.CarInfo.WheelsInfo.FrontLeft.Rps;
        protected override Func<TimedTelemetrySnapshot, double> Series2ExtractFunc => x => x.PlayerData.CarInfo.WheelsInfo.FrontRight.Rps;
        protected override OxyColor Series1Color => OxyColors.Red;
        protected override OxyColor Series2Color => OxyColors.Green;
    }
}