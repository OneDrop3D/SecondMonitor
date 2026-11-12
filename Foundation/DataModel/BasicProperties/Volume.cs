namespace SecondMonitor.DataModel.BasicProperties
{
    using System;
    using System.Xml.Serialization;
    using Newtonsoft.Json;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public sealed class Volume : IQuantity
    {
        public Volume()
        {
            InLiters = -1;
        }

        private Volume(double valueInLiters)
        {
            InLiters = valueInLiters;
        }

        [ProtoMember(1, IsRequired = true)]
        public double InLiters { get; set; }

        [JsonIgnore]
        [XmlIgnore]
        public double InUsGallons => InLiters * 0.264172;

        [JsonIgnore]
        [XmlIgnore]
        public bool IsZero => InLiters == -1;

        [JsonIgnore]
        [XmlIgnore]
        public double RawValue => InLiters;

        #region Operators

        public static Volume operator +(Volume left, Volume right)
        {
            return FromLiters(left.InLiters + right.InLiters);
        }

        public static Volume operator -(Volume left, Volume right)
        {
            return FromLiters(left.InLiters - right.InLiters);
        }

        public static Volume operator *(Volume left, Volume right)
        {
            return FromLiters(left.InLiters * right.InLiters);
        }

        public static Volume operator *(Volume left, double right)
        {
            return FromLiters(left.InLiters * right);
        }

        public static Volume operator *(Volume left, int right)
        {
            return FromLiters(left.InLiters * right);
        }

        public static Volume operator *(int left, Volume right)
        {
            return FromLiters(left * right.InLiters);
        }

        public static Volume operator /(Volume left, Volume right)
        {
            return FromLiters(left.InLiters / right.InLiters);
        }

        public static Volume operator /(Volume left, double right)
        {
            return FromLiters(left.InLiters / right);
        }

        public static bool operator ==(Volume left, Volume right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (ReferenceEquals(left, null))
            {
                return false;
            }

            if (ReferenceEquals(right, null))
            {
                return false;
            }

            return left.InLiters == right.InLiters;
        }

        public static bool operator !=(Volume left, Volume right)
        {
            return !(left == right);
        }

        public static bool operator <(Volume left, Volume right)
        {
            return left.InLiters < right.InLiters;
        }

        public static bool operator >(Volume left, Volume right)
        {
            return left.InLiters > right.InLiters;
        }

        public static bool operator <=(Volume left, Volume right)
        {
            return left.InLiters <= right.InLiters;
        }

        public static bool operator >=(Volume left, Volume right)
        {
            return left.InLiters >= right.InLiters;
        }

        #endregion

        public static string GetUnitSymbol(VolumeUnits units)
        {
            return units switch
            {
                VolumeUnits.Liters => "L",
                VolumeUnits.UsGallons => "gal",
                _ => throw new ArgumentException("Unable to return symbol fir" + units.ToString())
            };
        }

        public static Volume FromLiters(double volumeInLiters)
        {
            return new Volume(volumeInLiters);
        }

        public static Volume FromWholeLiters(double volumeInLiters)
        {
            return new Volume(Math.Ceiling(volumeInLiters));
        }

        public static Volume FromUsGallons(double volumeInGallons)
        {
            return Volume.FromLiters(volumeInGallons / 0.264172);
        }

        public double GetValueInUnits(VolumeUnits units)
        {
            return units switch
            {
                VolumeUnits.Liters => InLiters,
                VolumeUnits.UsGallons => InUsGallons,
                _ => throw new ArgumentException("Unable to return value in" + units.ToString())
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

        public string GetValueInUnits(VolumeUnits units, int decimalPlaces)
        {
            return units switch
            {
                VolumeUnits.Liters => InLiters.ToString($"F{decimalPlaces}"),
                VolumeUnits.UsGallons => InUsGallons.ToString($"F{decimalPlaces}"),
                _ => throw new ArgumentException("Unable to return value in" + units.ToString())
            };
        }

        public override bool Equals(object obj)
        {
            var volume = obj as Volume;
            return volume != null && InLiters == volume.InLiters;
        }

        public override int GetHashCode()
        {
            return InLiters.GetHashCode();
        }

        public static Volume FromUnits(double valueInUnits, VolumeUnits volumeUnits)
        {
            return volumeUnits switch
            {
                VolumeUnits.Liters => FromLiters(valueInUnits),
                VolumeUnits.UsGallons => FromUsGallons(valueInUnits),
                _ => throw new ArgumentOutOfRangeException(nameof(volumeUnits), volumeUnits, null)
            };
        }
    }
}