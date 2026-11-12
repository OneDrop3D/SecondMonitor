namespace SecondMonitor.DataModel.Snapshot.Drivers
{
    using System;
    using ProtoBuf;

    [ProtoContract]
    [Serializable]
    public class RatingInfo
    {
        [ProtoMember(1)]
        public int Rating { get; set; }

        [ProtoMember(2)]
        public bool IsFilled { get; set; }

        [ProtoMember(3)]
        public int RacesCompleted { get; set; }

        [ProtoMember(4)]
        public double Reputation { get; set; }
    }
}