namespace SecondMonitor.Telemetry.TelemetryApplication.DataAdapters
{
    using System.Linq;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Telemetry;
    using TelemetryManagement.Settings;

    public class DownforceAdapter : IQuantityAdapter<Force>
    {
        private readonly TyreLoadAdapter _tyreLoadAdapter;

        public DownforceAdapter(TyreLoadAdapter tyreLoadAdapter)
        {
            _tyreLoadAdapter = tyreLoadAdapter;
        }

        public bool IsQuantityComputed(SimulatorSourceInfo sourceInfo)
        {
            return !sourceInfo.TelemetryInfo.ContainsOverallDownforce;
        }

        public Force GetQuantity(TelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            if (!IsQuantityComputed(snapshot.SimulatorSourceInfo))
            {
                return snapshot.PlayerData?.CarInfo?.OverallDownForce ?? Force.GetFromNewtons(0);
            }

            return Force.GetFromNewtons(snapshot.PlayerData.CarInfo.WheelsInfo.AllWheels.
                Select(x => _tyreLoadAdapter.GetQuantityFromWheel(snapshot.SimulatorSourceInfo, x, carPropertiesDto).InNewtons).Sum());
        }
    }
}