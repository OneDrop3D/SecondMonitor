namespace SecondMonitor.DataModel.BasicProperties
{
    using System;
    using System.Xml.Serialization;
    using Newtonsoft.Json;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public class Velocity : IQuantity
    {
        public Velocity()
        {
            InMs = 0;
        }

        private Velocity(double ms)
        {
            InMs = ms;
        }

        public static Velocity Zero => FromMs(0);

        [XmlIgnore]
        [JsonIgnore]
        public double InKph
        {
            get => InMs * 3.6;
            set => InMs = value / 3.6;
        }

        [ProtoMember(1, IsRequired = true)]
        [XmlAttribute]
        public double InMs { get; set; }

        [JsonIgnore]
        public double InMph => InMs * 2.23694;

        [JsonIgnore]
        public bool IsZero => this == Zero;

        [JsonIgnore]
        public double RawValue => InMs;

        [JsonIgnore]
        public double InFps => InMs * 3.28084;

        [JsonIgnore]
        public double InInPerSecond => InMs * 39.3701;

        [JsonIgnore]
        public double InCentimeterPerSecond => InMs * 100;

        [JsonIgnore]
        public double InMillimeterPerSecond => InMs * 1000;

        public static bool operator <(Velocity v1, Velocity v2)
        {
            return v1.InMs < v2.InMs;
        }

        public static bool operator >(Velocity v1, Velocity v2)
        {
            return v1.InMs > v2.InMs;
        }

        public static bool operator <=(Velocity v1, Velocity v2)
        {
            return v1.InMs <= v2.InMs;
        }

        public static bool operator >=(Velocity v1, Velocity v2)
        {
            return v1.InMs >= v2.InMs;
        }

        public static Velocity operator -(Velocity v1, Velocity v2)
        {
            return FromMs(v1.InMs - v2.InMs);
        }

        public static Velocity FromMs(double inMs)
        {
            return new Velocity(inMs);
        }

        public static Velocity FromKph(double inKph)
        {
            return new Velocity(inKph / 3.6);
        }

        public static Velocity FromMph(double inMph)
        {
            return new Velocity(inMph / 2.237);
        }

        public static string GetUnitSymbol(VelocityUnits units)
        {
            return units switch
            {
                VelocityUnits.Kph => "Kph",
                VelocityUnits.Mph => "Mph",
                VelocityUnits.Ms => "Ms",
                VelocityUnits.Fps => "fps",
                VelocityUnits.CmPerSecond => "cm/s",
                VelocityUnits.InPerSecond => "In/s",
                VelocityUnits.MMPerSecond => "mm/s",
                _ => throw new ArgumentOutOfRangeException(nameof(units), units, null)
            };
        }

        public static Velocity FromUnits(double value, VelocityUnits units)
        {
            return units switch
            {
                VelocityUnits.Kph => FromKph(value),
                VelocityUnits.Mph => FromMph(value),
                VelocityUnits.Ms => FromMs(value),
                VelocityUnits.Fps => FromMs(value / 3.28084),
                VelocityUnits.CmPerSecond => FromMs(value / 100),
                VelocityUnits.InPerSecond => FromMs(value / 39.3701),
                VelocityUnits.MMPerSecond => FromMs(value / 1000),
                _ => throw new ArgumentException("Unable to return value in" + units)
            };
        }

        protected bool Equals(Velocity other)
        {
            return InMs.Equals(other.InMs);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Velocity)obj);
        }

        public bool Equals(IQuantity other, IQuantity tolerance)
        {
            if (other.GetType() != GetType())
            {
                return false;
            }

            return Math.Abs(RawValue - other.RawValue) < tolerance.RawValue;
        }

        public override int GetHashCode()
        {
            return InMs.GetHashCode();
        }

        public double GetValueInUnits(VelocityUnits units)
        {
            return units switch
            {
                VelocityUnits.Kph => InKph,
                VelocityUnits.Mph => InMph,
                VelocityUnits.Ms => InMs,
                VelocityUnits.Fps => InFps,
                VelocityUnits.CmPerSecond => InCentimeterPerSecond,
                VelocityUnits.InPerSecond => InInPerSecond,
                VelocityUnits.MMPerSecond => InMillimeterPerSecond,
                _ => throw new ArgumentException("Unable to return value in" + units)
            };
        }

        public string GetValueInUnits(VelocityUnits units, int decimalPlaces)
        {
            return GetValueInUnits(units).ToString($"F{decimalPlaces}");
        }
    }
}