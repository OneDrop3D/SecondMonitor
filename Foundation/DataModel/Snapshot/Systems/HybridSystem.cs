namespace SecondMonitor.DataModel.Snapshot.Systems
{
    using ProtoBuf;

    [ProtoContract]
    public class HybridSystem
    {
        [ProtoMember(1, IsRequired = true)]
        public HybridSystemMode HybridSystemMode { get; set; } = HybridSystemMode.UnAvailable;

        [ProtoMember(2, IsRequired = true)]
        public HybridChargeChange HybridChargeChange { get; set; } = HybridChargeChange.Unavailable;

        /// <summary>
        /// 0 - 1 Range
        /// </summary>
        [ProtoMember(3, IsRequired = true)]
        public double RemainingChargePercentage { get; set; } = 0;

        [ProtoMember(4, IsRequired = true)]
        public double? RemainingPerLapChargePercentage { get; set; } = null;

        [ProtoMember(5, IsRequired = true)]
        public bool IsElectricOnly { get; set; } = false;
    }
}
