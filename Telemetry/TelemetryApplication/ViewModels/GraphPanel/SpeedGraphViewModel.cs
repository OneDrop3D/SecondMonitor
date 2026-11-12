namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel
{
    using System.Collections.Generic;
    using DataExtractor;
    using DataModel.BasicProperties;
    using DataModel.Telemetry;
    using TelemetryManagement.Settings;

    public class SpeedGraphViewModel : AbstractSingleSeriesGraphViewModel
    {
        public SpeedGraphViewModel(IEnumerable<ISingleSeriesDataExtractor> dataExtractors) : base(dataExtractors)
        {
        }

        public override string Title => "Speed";
        protected override string YUnits => Velocity.GetUnitSymbol(UnitsCollection.VelocityUnits);
        protected override double YTickInterval => 50;
        protected override bool CanYZoom => true;

        protected override double GetYValue(TimedTelemetrySnapshot value, CarPropertiesDto carPropertiesDto)
        {
            return value.PlayerData.Speed.GetValueInUnits(UnitsCollection.VelocityUnits);
        }

        /*protected override void UpdateYMaximum(LapTelemetryDto lapTelemetry)
        {
            double newMax = lapTelemetry.TimedTelemetrySnapshots.Max(x => x.PlayerData.Speed.GetValueInUnits(VelocityUnits)) * 1.1;
            if (newMax > YMaximum)
            {
                YMaximum = newMax;
            }
        }*/
    }
}