namespace SecondMonitor.DataModel.BasicProperties
{
    using System;
    using System.Xml.Serialization;

    using Newtonsoft.Json;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public class Force : IQuantity
    {
        public Force()
        {
            IsZero = true;
            InNewtons = 0;
        }

        private Force(double inNewtons)
        {
            IsZero = false;
            InNewtons = inNewtons;
        }

        [ProtoMember(1, IsRequired = true)]
        public double InNewtons { get; set; }

        [JsonIgnore]
        [XmlIgnore]
        public double InPoundForce => 0.224809 * InNewtons;

        [JsonIgnore]
        [XmlIgnore]
        public Force Zero => new Force();

        [JsonIgnore]
        [XmlIgnore]
        public bool IsZero { get; }

        [JsonIgnore]
        [XmlIgnore]
        public double RawValue => InNewtons;

        public static string GetUnitSymbol(ForceUnits units)
        {
            return units switch
            {
                ForceUnits.Newtons => "N",
                ForceUnits.PoundForce => "lbf",
                _ => throw new ArgumentOutOfRangeException(nameof(units), units, null)
            };
        }

        public static Force GetFromNewtons(double force)
        {
            return new Force(force);
        }

        public Force FromNewtons(double force)
        {
            return new Force(force);
        }

        public double GetValueInUnits(ForceUnits units)
        {
            return units switch
            {
                ForceUnits.Newtons => InNewtons,
                ForceUnits.PoundForce => InPoundForce,
                _ => throw new ArgumentOutOfRangeException(nameof(units), units, null)
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
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            if (!(obj is Force force))
            {
                return false;
            }

            return InNewtons == force.InNewtons;
        }

        protected bool Equals(Force other)
        {
            return InNewtons.Equals(other.InNewtons) && IsZero == other.IsZero;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (InNewtons.GetHashCode() * 397) ^ IsZero.GetHashCode();
            }
        }
    }
}