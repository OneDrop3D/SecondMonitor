namespace SecondMonitor.DataModel.Snapshot.Drivers
{
    using System;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public sealed class DriverTimingInfo
    {
        /// <summary>
        ///     How far ahead the car in front of this driver is
        /// </summary>
        [ProtoMember(1, IsRequired = true)]
        public TimeSpan GapAhead { get; set; }

        /// <summary>
        ///     How far behind the car behind of this driver is
        /// </summary>
        [ProtoMember(2, IsRequired = true)]
        public TimeSpan GapBehind { get; set; }

        /// <summary>
        ///     Gap to Player
        /// </summary>
        [ProtoMember(3, IsRequired = true)]
        public TimeSpan GapToPlayer { get; set; }

        [ProtoMember(4, IsRequired = true)]
        public TimeSpan LastLapTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// <remarks>Unreliable, as it is only filled when the sim directly provides current lap time.</remarks>
        /// </summary>
        [ProtoMember(5, IsRequired = true)]
        public TimeSpan CurrentLapTime { get; set; } = TimeSpan.Zero;

        [ProtoMember(6, IsRequired = true)]
        public int CurrentSector { get; set; } = 0;

        [ProtoMember(7, IsRequired = true)]
        public TimeSpan CurrentSectorTime { get; set; } = TimeSpan.Zero;

        [ProtoMember(8, IsRequired = true)]
        public TimeSpan LastSector1Time { get; set; } = TimeSpan.Zero;

        [ProtoMember(9, IsRequired = true)]
        public TimeSpan LastSector2Time { get; set; } = TimeSpan.Zero;

        [ProtoMember(10, IsRequired = true)]
        public TimeSpan LastSector3Time { get; set; } = TimeSpan.Zero;

        [ProtoMember(11, IsRequired = true)]
        public TimeSpan BestLapTime { get; set; } = TimeSpan.Zero;

        [ProtoMember(12, IsRequired = true)]
        public int BestLapTimeNumber { get; set; } = -1;
    }
}