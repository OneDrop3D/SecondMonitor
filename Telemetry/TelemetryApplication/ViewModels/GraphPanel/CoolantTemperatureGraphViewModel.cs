namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel
{
    using System.Collections.Generic;
    using DataExtractor;
    using DataModel.BasicProperties;
    using DataModel.Telemetry;
    using TelemetryManagement.Settings;

    public class CoolantTemperatureGraphViewModel : AbstractSingleSeriesGraphViewModel
    {
        public CoolantTemperatureGraphViewModel(IEnumerable<ISingleSeriesDataExtractor> dataExtractors) : base(dataExtractors)
        {
        }

        public override string Title => "Coolant Temperature";
        protected override string YUnits => Temperature.GetUnitSymbol(UnitsCollection.TemperatureUnits);
        protected override double YTickInterval => 10;
        protected override bool CanYZoom => true;

        protected override double GetYValue(TimedTelemetrySnapshot value, CarPropertiesDto carPropertiesDto)
        {
            return value.PlayerData.CarInfo.WaterSystemInfo.OptimalWaterTemperature.ActualQuantity.GetValueInUnits(UnitsCollection.TemperatureUnits);
        }
    }
}