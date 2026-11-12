namespace SecondMonitor.DataModel.Snapshot.Systems
{
    using System;

    using BasicProperties;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public sealed class FuelInfo
    {
        public FuelInfo()
        {
            FuelCapacity = new Volume();
            FuelRemaining = new Volume();
            FuelPressure = new Pressure();
            VirtualTank = new Percentage();
        }

        [ProtoMember(1)]
        public Volume FuelCapacity { get; set; }

        [ProtoMember(2)]
        public Volume FuelRemaining { get; set; }

        [ProtoMember(3)]
        public Pressure FuelPressure { get; set; }

        [ProtoMember(4)]
        public Percentage VirtualTank { get; set; }

        [ProtoMember(5)]
        public bool HasVirtualTank { get; set; }
    }
}
