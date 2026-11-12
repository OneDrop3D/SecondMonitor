namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.Wheels
{
    using System;
    using System.Linq;
    using DataAdapters;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;

    public class TyreLoadGraphViewModel : AbstractWheelsGraphViewModel
    {
        private readonly TyreLoadAdapter _tyreLoadAdapter;
        private string _title = "Tyre Load";

        public TyreLoadGraphViewModel(TyreLoadAdapter tyreLoadAdapter)
        {
            _tyreLoadAdapter = tyreLoadAdapter;
        }

        public override string Title => _title;

        protected override string YUnits => Force.GetUnitSymbol(UnitsCollection.ForceUnits);

        protected override double YTickInterval => Force.GetFromNewtons(2500).GetValueInUnits(UnitsCollection.ForceUnits);

        protected override bool CanYZoom => true;

        protected override Func<SimulatorSourceInfo, WheelInfo, CarPropertiesDto, double> ExtractorFunction => (sourceInfo, x, carProp) => _tyreLoadAdapter.GetQuantityFromWheel(sourceInfo, x, carProp).GetValueInUnits(UnitsCollection.ForceUnits);

        public override bool IsCarSettingsDependant => true;

        protected override void PreviewOnLapLoaded(LapTelemetryDto lapTelemetryDto, CarPropertiesDto carPropertiesDto)
        {
            var firstDataPoint = lapTelemetryDto.DataPoints.FirstOrDefault();
            string newTitle;
            if (firstDataPoint == null || !_tyreLoadAdapter.IsQuantityComputed(firstDataPoint.SimulatorSourceInfo))
            {
                newTitle = "Tyre Load";
            }
            else
            {
                newTitle = "Suspension Load (C)";
            }

            if (newTitle != _title)
            {
                _title = newTitle;
                NotifyPropertyChanged(nameof(Title));
            }

            base.PreviewOnLapLoaded(lapTelemetryDto, carPropertiesDto);
        }
    }
}