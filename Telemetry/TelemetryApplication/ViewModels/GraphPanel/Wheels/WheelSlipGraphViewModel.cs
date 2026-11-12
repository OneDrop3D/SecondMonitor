namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.Wheels
{
    using System;
    using System.Collections.Generic;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using DataModel.Telemetry;
    using OxyPlot;
    using OxyPlot.Series;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;

    public class WheelSlipGraphViewModel : AbstractWheelsGraphViewModel
    {
        public override string Title => "Wheel Slip";
        protected override string YUnits => string.Empty;
        protected override double YTickInterval => 0.5;
        protected override bool CanYZoom => true;
        protected override Func<SimulatorSourceInfo, WheelInfo, CarPropertiesDto, double> ExtractorFunction => (_, x, __) => x.Slip;

        protected override List<LineSeries> GetLineSeries(LapSummaryDto lapSummary, CarPropertiesDto carPropertiesDto, List<TimedTelemetrySnapshot> dataPoints, OxyColor color)
        {
            var lineSeries = base.GetLineSeries(lapSummary, carPropertiesDto, dataPoints, color);
            YMaximum = 0.5;
            YMinimum = -0.5;
            return lineSeries;
        }
    }
}