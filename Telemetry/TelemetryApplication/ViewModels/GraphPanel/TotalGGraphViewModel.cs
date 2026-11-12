namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel
{
    using System;
    using System.Collections.Generic;
    using DataExtractor;
    using DataModel.Telemetry;
    using TelemetryManagement.Settings;

    public class TotalGGraphViewModel : AbstractSingleSeriesGraphViewModel
    {
        public TotalGGraphViewModel(IEnumerable<ISingleSeriesDataExtractor> dataExtractors) : base(dataExtractors)
        {
        }

        public override string Title => "Total Acceleration";
        protected override string YUnits => "Gs";
        protected override double YTickInterval => 1;
        protected override bool CanYZoom => true;
        /*protected override void UpdateYMaximum(LapTelemetryDto lapTelemetry)
        {
            YMinimum = -3;
            YMaximum = 3;
        }*/

        protected override double GetYValue(TimedTelemetrySnapshot value, CarPropertiesDto carPropertiesDto)
        {
            return Math.Abs(value.PlayerData.CarInfo.Acceleration.XinG) + Math.Abs(value.PlayerData.CarInfo.Acceleration.ZinG);
        }
    }
}