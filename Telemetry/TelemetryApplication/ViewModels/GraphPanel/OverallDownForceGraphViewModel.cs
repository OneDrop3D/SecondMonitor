namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel
{
    using System.Collections.Generic;
    using System.Linq;
    using DataAdapters;
    using DataExtractor;
    using DataModel.BasicProperties;
    using DataModel.Telemetry;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;

    public class OverallDownForceGraphViewModel : AbstractSingleSeriesGraphViewModel
    {
        private readonly DownforceAdapter _downforceAdapter;
        private string _title = "Overall DownForce";

        public OverallDownForceGraphViewModel(IEnumerable<ISingleSeriesDataExtractor> dataExtractors, DownforceAdapter downforceAdapter) : base(dataExtractors)
        {
            _downforceAdapter = downforceAdapter;
        }

        public override string Title => _title;
        protected override string YUnits => Force.GetUnitSymbol(UnitsCollection.ForceUnits);
        protected override double YTickInterval => Force.GetFromNewtons(1000).GetValueInUnits(UnitsCollection.ForceUnits);
        protected override bool CanYZoom => true;
        public override bool IsCarSettingsDependant => true;

        protected override void PreviewOnLapLoaded(LapTelemetryDto lapTelemetryDto, CarPropertiesDto carPropertiesDto)
        {
            var firstDataPoint = lapTelemetryDto.DataPoints.FirstOrDefault();
            string newTitle;
            if (firstDataPoint == null || !_downforceAdapter.IsQuantityComputed(firstDataPoint.SimulatorSourceInfo))
            {
                newTitle = "Overall DownForce";
            }
            else
            {
                newTitle = "Overall DownForce (C)";
            }

            if (newTitle != _title)
            {
                _title = newTitle;
                NotifyPropertyChanged(nameof(Title));
            }

            base.PreviewOnLapLoaded(lapTelemetryDto, carPropertiesDto);
        }

        protected override double GetYValue(TimedTelemetrySnapshot value, CarPropertiesDto carPropertiesDto)
        {
            return _downforceAdapter.GetQuantity(value, carPropertiesDto).GetValueInUnits(UnitsCollection.ForceUnits);
        }
    }
}