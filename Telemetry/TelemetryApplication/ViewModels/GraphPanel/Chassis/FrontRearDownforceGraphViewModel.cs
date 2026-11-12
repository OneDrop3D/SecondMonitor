namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.Chassis
{
    using System.Linq;
    using DataAdapters;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;

    public class FrontRearDownForceGraphViewModel : AbstractChassisGraphViewModel
    {
        private readonly DownforceChassisAdapter _downforceChassisAdapter;
        private string _title = "Front - Rear DownForce";

        public FrontRearDownForceGraphViewModel(DownforceChassisAdapter downforceChassisAdapter)
        {
            _downforceChassisAdapter = downforceChassisAdapter;
        }

        public override string Title => _title;
        protected override string YUnits => Force.GetUnitSymbol(UnitsCollection.ForceUnits);
        protected override double YTickInterval => Force.GetFromNewtons(1000).GetValueInUnits(UnitsCollection.ForceUnits);
        protected override bool CanYZoom => true;
        public override bool IsCarSettingsDependant => true;

        protected override double GetFrontValue(SimulatorSourceInfo simulatorSourceInfo, CarInfo carInfo, CarPropertiesDto carPropertiesDto)
        {
            return _downforceChassisAdapter.GetQuantityFront(simulatorSourceInfo, carInfo, carPropertiesDto).GetValueInUnits(UnitsCollection.ForceUnits);
        }

        protected override double GetRearValue(SimulatorSourceInfo simulatorSourceInfo, CarInfo carInfo, CarPropertiesDto carPropertiesDto)
        {
            return _downforceChassisAdapter.GetQuantityBack(simulatorSourceInfo, carInfo, carPropertiesDto).GetValueInUnits(UnitsCollection.ForceUnits);
        }

        protected override void PreviewOnLapLoaded(LapTelemetryDto lapTelemetryDto, CarPropertiesDto carPropertiesDto)
        {
            var firstDataPoint = lapTelemetryDto.DataPoints.FirstOrDefault();
            string newTitle;
            if (firstDataPoint == null || !_downforceChassisAdapter.IsQuantityComputed(firstDataPoint.SimulatorSourceInfo))
            {
                newTitle = "Front - Rear DownForce";
            }
            else
            {
                newTitle = "Front - Rear DownForce (C)";
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