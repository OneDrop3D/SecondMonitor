namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.DualSeries
{
    using System;
    using System.Collections.Generic;
    using DataModel.Telemetry;
    using OxyPlot;
    using OxyPlot.Series;

    public class CombinedPedalsGraphViewModel : AbstractDualSeriesGraphViewModel
    {
        public override string Title => "Throttle & Brake";
        protected override string YUnits => "%";
        protected override double YTickInterval => 20;
        protected override bool CanYZoom => true;

        protected override string Series1Title => "Throttle";
        protected override string Series2Title => "Brake";

        protected override Func<TimedTelemetrySnapshot, double> Series1ExtractFunc => (x) => x.InputInfo?.ThrottlePedalPosition * 100 ?? 0;
        protected override Func<TimedTelemetrySnapshot, double> Series2ExtractFunc => (x) => x.InputInfo?.BrakePedalPosition * 100 ?? 0;
        protected override OxyColor Series1Color => OxyColors.Green;
        protected override OxyColor Series2Color => OxyColors.Red;

        protected override void OnSeriesCreated(List<LineSeries> lineSeries)
        {
            base.OnSeriesCreated(lineSeries);
            YMaximum = 100;
            YMinimum = 0;
        }
    }
}