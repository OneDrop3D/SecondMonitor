namespace SecondMonitor.DataModel.BasicProperties
{
    using System;
    using System.Xml.Serialization;
    using Newtonsoft.Json;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public sealed class Distance : IQuantity
    {
        private readonly bool _isZero;

        public Distance()
        {
        }

        private Distance(double inMeters, bool isZero = false)
        {
            InMeters = inMeters;
            _isZero = isZero;
        }

        [XmlIgnore]
        [JsonIgnore]
        public static Distance ZeroDistance => new Distance(0, true);

        [XmlAttribute]
        [ProtoMember(1, IsRequired = true)]
        public double InMeters { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public double InKilometers => InMeters / 1000;

        [XmlIgnore]
        [JsonIgnore]
        public double InMiles => InKilometers / 1.609344;

        [XmlIgnore]
        [JsonIgnore]
        public double InInches => InMeters * 39.3701;

        [XmlIgnore]
        [JsonIgnore]
        public double InCentimeters => InMeters * 100;

        [XmlIgnore]
        [JsonIgnore]
        public double InYards => InMeters * 1.09361;

        [XmlIgnore]
        [JsonIgnore]
        public double InFeet => InMeters * 3.28084;

        [XmlIgnore]
        [JsonIgnore]
        public double InMillimeter => InMeters * 1000;

        [JsonIgnore]
        public bool IsZero => InMeters == 0;

        [JsonIgnore]
        public double RawValue => InMeters;

        public static bool operator <(Distance d1, Distance d2)
        {
            if (d1 is null || d2 is null)
            {
                return false;
            }

            return d1.InMeters < d2.InMeters;
        }

        public static bool operator >(Distance d1, Distance d2)
        {
            if (d1 is null || d2 is null)
            {
                return false;
            }

            return d1.InMeters > d2.InMeters;
        }

        public static bool operator ==(Distance dist1, Distance dist2)
        {
            return dist1?._isZero == dist2?._isZero && dist1?.InMeters == dist2?.InMeters;
        }

        public static bool operator !=(Distance dist1, Distance dist2)
        {
            return !(dist1 == dist2);
        }

        public static Distance operator -(Distance d1, Distance d2)
        {
            return FromMeters(d1.InMeters - d2.InMeters);
        }

        public static Distance operator *(Distance d1, double d)
        {
            if (d1 is null)
            {
                return null;
            }

            return FromMeters(d1.InMeters * d);
        }

        public static Distance operator *(Distance d1, Distance d2)
        {
            if (d1 is null || d2 is null)
            {
                return null;
            }

            return FromMeters(d1.InMeters * d2.InMeters);
        }

        public static Distance FromMeters(double distanceInM)
        {
            return new Distance(distanceInM);
        }

        public static Distance FromInches(double distanceInInches)
        {
            return new Distance(distanceInInches / 39.3701);
        }

        public static Distance FromCentimeters(double distanceInCm)
        {
            return new Distance(distanceInCm / 100);
        }

        public bool Equals(IQuantity other, IQuantity tolerance)
        {
            if (other.GetType() != typeof(Distance))
            {
                return false;
            }

            return Math.Abs(RawValue - other.RawValue) < tolerance.RawValue;
        }

        public double GetByUnit(DistanceUnits distanceUnits)
        {
            return distanceUnits switch
            {
                DistanceUnits.Meters => InMeters,
                DistanceUnits.Kilometers => InKilometers,
                DistanceUnits.Miles => InMiles,
                DistanceUnits.Feet => InFeet,
                DistanceUnits.Inches => InInches,
                DistanceUnits.Centimeter => InCentimeters,
                DistanceUnits.Yards => InYards,
                DistanceUnits.Millimeter => InMillimeter,
                _ => throw new ArgumentException($"Distance units {distanceUnits} is not known")
            };
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

            return obj.GetType() == GetType() && Equals((Distance)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_isZero.GetHashCode() * 397) ^ InMeters.GetHashCode();
            }
        }

        private bool Equals(Distance other)
        {
            return _isZero == other._isZero && InMeters.Equals(other.InMeters);
        }

        public static string GetUnitsSymbol(DistanceUnits distanceUnits)
        {
            return distanceUnits switch
            {
                DistanceUnits.Meters => "m",
                DistanceUnits.Kilometers => "Km",
                DistanceUnits.Miles => "mi",
                DistanceUnits.Feet => "ft",
                DistanceUnits.Inches => "in",
                DistanceUnits.Centimeter => "cm",
                DistanceUnits.Yards => "yd",
                DistanceUnits.Millimeter => "mm",
                _ => throw new ArgumentException($"Distance units {nameof(distanceUnits)} is unknown.")
            };
        }

        public static Distance CreateByUnits(double value, DistanceUnits distanceUnits)
        {
            return distanceUnits switch
            {
                DistanceUnits.Meters => FromMeters(value),
                DistanceUnits.Kilometers => FromMeters(value * 1000),
                DistanceUnits.Miles => FromMeters(value * 1609.34),
                DistanceUnits.Feet => FromMeters(value * 0.3047992424196),
                DistanceUnits.Yards => FromMeters(value * 0.9144),
                DistanceUnits.Inches => FromMeters(value * 0.0254),
                DistanceUnits.Centimeter => FromMeters(value / 100),
                DistanceUnits.Millimeter => FromMeters(value / 1000),
                _ => throw new ArgumentException($"Distance units {nameof(distanceUnits)} is unknown.")
            };
        }
    }
}