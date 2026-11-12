namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel
{
    using System.Collections.Generic;
    using DataExtractor;
    using DataModel.Telemetry;
    using TelemetryManagement.Settings;

    public class RearTyresSpeedDifference : AbstractSingleSeriesGraphViewModel
    {
        public RearTyresSpeedDifference(IEnumerable<ISingleSeriesDataExtractor> dataExtractors) : base(dataExtractors)
        {
        }

        public override string Title => "Rear Tyres RPS Difference";
        protected override string YUnits => "%";
        protected override double YTickInterval => 5;
        protected override bool CanYZoom => true;

        protected override double GetYValue(TimedTelemetrySnapshot value, CarPropertiesDto carPropertiesDto)
        {
            double leftRps = value.PlayerData.CarInfo.WheelsInfo.RearLeft.Rps;
            double rightRps = value.PlayerData.CarInfo.WheelsInfo.RearRight.Rps;

            return ((leftRps - rightRps) / (leftRps + rightRps) / 2) * 100;
        }
    }
}