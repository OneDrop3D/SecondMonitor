namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.Chassis
{
    using System.Linq;
    using DataAdapters;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;

    public class FrontRearTyreLoadsGraphViewModel : AbstractChassisGraphViewModel
    {
        private readonly TyreLoadAdapter _tyreLoadAdapter;
        private string _title = "Tyre Loads (F/R)";

        public FrontRearTyreLoadsGraphViewModel(TyreLoadAdapter tyreLoadAdapter)
        {
            _tyreLoadAdapter = tyreLoadAdapter;
        }

        public override string Title => _title;
        protected override string YUnits => Force.GetUnitSymbol(UnitsCollection.ForceUnits);
        protected override double YTickInterval => Force.GetFromNewtons(1000).GetValueInUnits(UnitsCollection.ForceUnits);
        protected override bool CanYZoom => true;
        public override bool IsCarSettingsDependant => true;

        protected override double GetFrontValue(SimulatorSourceInfo simulatorSourceInfo, CarInfo carInfo, CarPropertiesDto carPropertiesDto)
        {
            return _tyreLoadAdapter.GetQuantityFromWheel(simulatorSourceInfo, carInfo.WheelsInfo.FrontLeft, carPropertiesDto).GetValueInUnits(UnitsCollection.ForceUnits) +
                   _tyreLoadAdapter.GetQuantityFromWheel(simulatorSourceInfo, carInfo.WheelsInfo.FrontRight, carPropertiesDto).GetValueInUnits(UnitsCollection.ForceUnits);
        }

        protected override double GetRearValue(SimulatorSourceInfo simulatorSourceInfo, CarInfo carInfo, CarPropertiesDto carPropertiesDto)
        {
            return _tyreLoadAdapter.GetQuantityFromWheel(simulatorSourceInfo, carInfo.WheelsInfo.RearLeft, carPropertiesDto).GetValueInUnits(UnitsCollection.ForceUnits) +
                   _tyreLoadAdapter.GetQuantityFromWheel(simulatorSourceInfo, carInfo.WheelsInfo.RearRight, carPropertiesDto).GetValueInUnits(UnitsCollection.ForceUnits);
        }

        protected override void PreviewOnLapLoaded(LapTelemetryDto lapTelemetryDto, CarPropertiesDto carPropertiesDto)
        {
            var firstDataPoint = lapTelemetryDto.DataPoints.FirstOrDefault();
            string newTitle;
            if (firstDataPoint == null || !_tyreLoadAdapter.IsQuantityComputed(firstDataPoint.SimulatorSourceInfo))
            {
                newTitle = "Tyre Loads (F/R)";
            }
            else
            {
                newTitle = "Suspension Loads (F/R) (C)";
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