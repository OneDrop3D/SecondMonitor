namespace SecondMonitor.DataModel.BasicProperties
{
    using System;
    using System.Xml.Serialization;

    using Newtonsoft.Json;

    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public class Percentage : IQuantity
    {
        public Percentage() : this(0)
        {
        }

        private Percentage(double inRatio)
        {
            InRatio = inRatio;
        }

        /// <summary>
        /// 0-1 Range
        /// </summary>
        [ProtoMember(1, IsRequired = true)]
        public double InRatio { get; set; }

        /// <summary>
        /// 0-100 Range
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public double InPercentage => InRatio * 100;

        [XmlIgnore]
        [JsonIgnore]
        public bool IsZero => InRatio != 0;

        [XmlIgnore]
        [JsonIgnore]
        public double RawValue => InRatio;

        public static Percentage FromRatio(double ratio)
        {
            return new Percentage(ratio);
        }
        
        public static Percentage FromPercentage(double percentage)
        {
            return new Percentage(percentage / 100);
        }

        public bool Equals(IQuantity other, IQuantity tolerance)
        {
            if (other.GetType() != GetType())
            {
                return false;
            }

            return Math.Abs(RawValue - other.RawValue) < tolerance.RawValue;
        }

        protected bool Equals(Percentage other)
        {
            return InRatio.Equals(other.InRatio);
        }

        #region Operators

        public static Percentage operator +(Percentage left, Percentage right)
        {
            return FromRatio(left.InRatio + right.InRatio);
        }

        public static Percentage operator -(Percentage left, Percentage right)
        {
            return FromRatio(left.InRatio - right.InRatio);
        }

        public static Percentage operator *(Percentage left, Percentage right)
        {
            return FromRatio(left.InRatio * right.InRatio);
        }

        public static Percentage operator *(Percentage left, double right)
        {
            return FromRatio(left.InRatio * right);
        }

        public static Percentage operator *(Percentage left, int right)
        {
            return FromRatio(left.InRatio * right);
        }

        public static Percentage operator *(int left, Percentage right)
        {
            return FromRatio(left * right.InRatio);
        }

        public static Percentage operator /(Percentage left, Percentage right)
        {
            return FromRatio(left.InRatio / right.InRatio);
        }

        public static Percentage operator /(Percentage left, double right)
        {
            return FromRatio(left.InRatio / right);
        }

        public static bool operator ==(Percentage left, Percentage right)
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

            return left.InRatio == right.InRatio;
        }

        public static bool operator !=(Percentage left, Percentage right)
        {
            return !(left == right);
        }

        public static bool operator <(Percentage left, Percentage right)
        {
            return left.InRatio < right.InRatio;
        }

        public static bool operator >(Percentage left, Percentage right)
        {
            return left.InRatio > right.InRatio;
        }

        public static bool operator <=(Percentage left, Percentage right)
        {
            return left.InRatio <= right.InRatio;
        }

        public static bool operator >=(Percentage left, Percentage right)
        {
            return left.InRatio >= right.InRatio;
        }

        #endregion

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

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((Percentage)obj);
        }

        public override int GetHashCode()
        {
            return InRatio.GetHashCode();
        }
    }
}
