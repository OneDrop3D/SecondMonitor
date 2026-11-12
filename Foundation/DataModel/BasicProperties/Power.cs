namespace SecondMonitor.DataModel.BasicProperties
{
    using System;
    using System.Xml.Serialization;
    using ProtoBuf;
    using Units;

    [ProtoContract]
    [Serializable]
    public class Power : IQuantity
    {
        public Power()
        {
            InKw = 0;
        }

        private Power(double inKw)
        {
            InKw = inKw;
        }

        [XmlAttribute]
        [ProtoMember(1, IsRequired = true)]
        public double InKw { get; set; }

        [XmlIgnore]
        public double InHorsePower
        {
            get => InKw * 1.34102;
            set => InKw = value / 1.34102;
        }

        public bool IsZero => InKw == 0;
        public double RawValue => InKw;

        public static Power FromKw(double inKw)
        {
            return new Power(inKw);
        }

        public static string GetUnitSymbol(PowerUnits powerUnits)
        {
            return powerUnits switch
            {
                PowerUnits.KW => "KW",
                PowerUnits.HP => "HP",
                _ => throw new ArgumentOutOfRangeException(nameof(powerUnits), powerUnits, null)
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

        public double GetValueInUnit(PowerUnits powerUnits)
        {
            return powerUnits switch
            {
                PowerUnits.KW => InKw,
                PowerUnits.HP => InHorsePower,
                _ => throw new ArgumentOutOfRangeException(nameof(powerUnits), powerUnits, null)
            };
        }
    }
}