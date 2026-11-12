namespace SecondMonitor.Telemetry.TelemetryApplication.DataAdapters
{
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using TelemetryManagement.Settings;

    public class TyreLoadAdapter : IWheelQuantityAdapter<Force>
    {
        public bool IsQuantityComputed(SimulatorSourceInfo simulatorSource)
        {
            return !simulatorSource.TelemetryInfo.ContainsTyreLoad;
        }

        public Force GetQuantityFromWheel(SimulatorSourceInfo simulatorSource, WheelInfo wheelInfo, CarPropertiesDto carPropertiesDto)
        {
            return IsQuantityComputed(simulatorSource) ? ComputeTyreLoad(wheelInfo, carPropertiesDto.GetPropertiesByWheelKind(wheelInfo.WheelKind), simulatorSource.TelemetryInfo.IsSuspensionTravelInverted) : wheelInfo.TyreLoad;
        }

        private static Force ComputeTyreLoad(WheelInfo wheelInfo, WheelPropertiesDto wheelProperties, bool inverseForce)
        {
            return inverseForce
                ? Force.GetFromNewtons((wheelInfo.SuspensionTravel.InMillimeter - wheelProperties.NeutralSuspensionTravel.InMillimeter) * -wheelProperties.SprintStiffnessPerMm.InNewtons)
                : Force.GetFromNewtons((wheelInfo.SuspensionTravel.InMillimeter - wheelProperties.NeutralSuspensionTravel.InMillimeter) * wheelProperties.SprintStiffnessPerMm.InNewtons);
        }
    }
}