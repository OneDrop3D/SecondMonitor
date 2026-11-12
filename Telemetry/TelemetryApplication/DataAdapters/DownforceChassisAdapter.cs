namespace SecondMonitor.Telemetry.TelemetryApplication.DataAdapters
{
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using TelemetryManagement.Settings;

    public class DownforceChassisAdapter : IChassisQuantityAdapter<Force>
    {
        private readonly TyreLoadAdapter _tyreLoadAdapter;

        public DownforceChassisAdapter(TyreLoadAdapter tyreLoadAdapter)
        {
            _tyreLoadAdapter = tyreLoadAdapter;
        }

        public bool IsQuantityComputed(SimulatorSourceInfo simulatorSource)
        {
            return !simulatorSource.TelemetryInfo.ContainsFrontRearDownforce;
        }

        public Force GetQuantityFront(SimulatorSourceInfo simulatorSource, CarInfo carInfo, CarPropertiesDto carPropertiesDto)
        {
            return IsQuantityComputed(simulatorSource) ?
                Force.GetFromNewtons(_tyreLoadAdapter.GetQuantityFromWheel(simulatorSource, carInfo.WheelsInfo.FrontLeft, carPropertiesDto).InNewtons
                                     + _tyreLoadAdapter.GetQuantityFromWheel(simulatorSource, carInfo.WheelsInfo.FrontRight, carPropertiesDto).InNewtons)
                : carInfo?.FrontDownForce ?? Force.GetFromNewtons(0);
        }

        public Force GetQuantityBack(SimulatorSourceInfo simulatorSource, CarInfo carInfo, CarPropertiesDto carPropertiesDto)
        {
            return IsQuantityComputed(simulatorSource) ?
                Force.GetFromNewtons(_tyreLoadAdapter.GetQuantityFromWheel(simulatorSource, carInfo.WheelsInfo.RearLeft, carPropertiesDto).InNewtons
                                     + _tyreLoadAdapter.GetQuantityFromWheel(simulatorSource, carInfo.WheelsInfo.RearRight, carPropertiesDto).InNewtons) :
                carInfo?.RearDownForce ?? Force.GetFromNewtons(0);
        }
    }
}