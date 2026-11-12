namespace SecondMonitor.Timing.Api.Data
{
    using SecondMonitor.DataModel.Snapshot.Systems;

    public class HybridSystemData
    {
        public HybridSystemData(HybridSystem hybridSystem)
        {
            HybridSystemMode = (int)hybridSystem.HybridSystemMode;
            HybridSystemModeName = hybridSystem.HybridSystemMode.ToString();
            HybridChargeChange = (int)hybridSystem.HybridChargeChange;
            HybridChargeChangeName = hybridSystem.HybridChargeChange.ToString();
            RemainingChargePercentage = hybridSystem.RemainingChargePercentage;
            RemainingPerLapChargePercentage = hybridSystem.RemainingPerLapChargePercentage ?? 0;
            HasRemainingPerLapChargePercentage = hybridSystem.RemainingPerLapChargePercentage.HasValue;
            IsElectricOnly = hybridSystem.IsElectricOnly;
        }

        public int HybridSystemMode { get; }

        public string HybridSystemModeName { get; }

        public int HybridChargeChange { get; }

        public string HybridChargeChangeName { get; }

        public double RemainingChargePercentage { get; }

        public double RemainingPerLapChargePercentage { get; }

        public bool HasRemainingPerLapChargePercentage { get; }

        public bool IsElectricOnly { get; }
    }
}
