namespace SecondMonitor.DataModel.BasicProperties
{
    using System;
    using System.Xml.Serialization;
    using Newtonsoft.Json;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public class Angle : IQuantity
    {
        public Angle()
        {
            InDegrees = 0;
            IsZero = true;
        }

        private Angle(double inDegrees)
        {
            InDegrees = inDegrees;
            IsZero = false;
        }

        [ProtoMember(1, IsRequired = true)]
        [XmlAttribute]
        public double InDegrees { get; set; }

        [JsonIgnore]
        public double InRadians => 0.0174533 * InDegrees;

        [JsonIgnore]
        public double InMilliRadians => InRadians * 1000;

        [XmlIgnore]
        public bool IsZero { get; private set; }

        public double RawValue => InDegrees;

        public static string GetUnitsSymbol(AngleUnits angleUnits)
        {
            return angleUnits switch
            {
                AngleUnits.Degrees => "°",
                AngleUnits.Radians => "rad",
                AngleUnits.MilliRadians => "mil",
                _ => throw new ArgumentOutOfRangeException(nameof(angleUnits), angleUnits, null)
            };
        }

        public static Angle GetFromDegrees(double angle)
        {
            return new Angle(angle);
        }

        public static Angle GetFromRadians(double angle)
        {
            return new Angle(angle / 0.0174533);
        }

        public static Angle GetFromValue(double value, AngleUnits units)
        {
            return units switch
            {
                AngleUnits.Degrees => GetFromDegrees(value),
                AngleUnits.Radians => GetFromRadians(value),
                AngleUnits.MilliRadians => GetFromRadians(value / 1000),
                _ => throw new ArgumentOutOfRangeException(nameof(units), units, null)
            };
        }

        public double GetValueInUnits(AngleUnits angleUnits)
        {
            return angleUnits switch
            {
                AngleUnits.Degrees => InDegrees,
                AngleUnits.Radians => InRadians,
                AngleUnits.MilliRadians => InMilliRadians,
                _ => throw new ArgumentOutOfRangeException(nameof(angleUnits), angleUnits, null)
            };
        }

        public bool Equals(IQuantity other, IQuantity tolerance)
        {
            if (other.GetType() != GetType())
            {
                return false;
            }

            return Math.Abs(RawValue - other.RawValue) < tolerance.RawValue;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        protected bool Equals(Angle other)
        {
            return InDegrees.Equals(other.InDegrees) && IsZero == other.IsZero;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (InDegrees.GetHashCode() * 397) ^ IsZero.GetHashCode();
            }
        }
    }
}