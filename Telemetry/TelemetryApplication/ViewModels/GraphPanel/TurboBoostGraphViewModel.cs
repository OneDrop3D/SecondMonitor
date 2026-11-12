namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel
{
    using System.Collections.Generic;
    using DataExtractor;
    using DataModel.BasicProperties;
    using DataModel.Telemetry;
    using TelemetryManagement.Settings;

    public class TurboBoostGraphViewModel : AbstractSingleSeriesGraphViewModel
    {
        public TurboBoostGraphViewModel(IEnumerable<ISingleSeriesDataExtractor> dataExtractors) : base(dataExtractors)
        {
        }

        public override string Title => "Turbo Boost";
        protected override string YUnits => Pressure.GetUnitSymbol(UnitsCollection.PressureUnits);
        protected override double YTickInterval => Pressure.FromBar(0.50).GetValueInUnits(UnitsCollection.PressureUnits);
        protected override bool CanYZoom => true;
        /*protected override void UpdateYMaximum(LapTelemetryDto lapTelemetry)
        {
            double newYMax = lapTelemetry.TimedTelemetrySnapshots.Max(x => x.PlayerData?.CarInfo?.TurboPressure?.GetValueInUnits(PressureUnits) ?? 0.0);
            if (newYMax > YMaximum)
            {
                YMaximum = newYMax;
            }
        }*/

        protected override double GetYValue(TimedTelemetrySnapshot value, CarPropertiesDto carPropertiesDto)
        {
            return value.PlayerData?.CarInfo?.TurboPressure?.GetValueInUnits(UnitsCollection.PressureUnits) ?? 0.0;
        }
    }
}