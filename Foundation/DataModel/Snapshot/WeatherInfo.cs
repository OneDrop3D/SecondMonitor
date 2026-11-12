namespace SecondMonitor.DataModel.Snapshot
{
    using System;

    using BasicProperties;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public sealed class WeatherInfo
    {
        public WeatherInfo()
        {
        }

        [ProtoMember(1)]
        public Temperature AirTemperature { get; set; } = Temperature.FromCelsius(-1);

        [ProtoMember(2)]
        public Temperature TrackTemperature { get; set; } = Temperature.FromCelsius(-1);

        [ProtoMember(3, IsRequired = true)]
        public int RainIntensity { get; set; } = 0;

        [ProtoMember(4, IsRequired = true)]
        public int TrackWetness { get; set; } = 0;

        [ProtoMember(5, IsRequired = true)]
        public double WindDirectionFrom { get; set; } = 0;

        [ProtoMember(6)]
        public Velocity WindSpeed { get; set; } = Velocity.Zero;

        [ProtoMember(7, IsRequired = true)]
        public bool HasWindInformation { get; set; } = false;
    }
}
