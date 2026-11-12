namespace SecondMonitor.DataModel.BasicProperties.Units
{
    using System;
    using System.Xml.Serialization;
    using ProtoBuf;

    [ProtoContract]
    [Serializable]
    public class Torque : IQuantity
    {
        public Torque()
        {
            InNm = 0;
        }

        private Torque(double inNm)
        {
            InNm = inNm;
        }

        [XmlIgnore]
        public bool IsZero => InNm == 0;

        [XmlIgnore]
        public double RawValue => InNm;

        [XmlAttribute]
        [ProtoMember(1, IsRequired = true)]
        public double InNm { get; set; }

        [XmlIgnore]
        public double InPoundFeet
        {
            get => InNm * 0.73756;
            set => InNm = value / 0.73756;
        }

        public static string GetUnitSymbol(TorqueUnits torqueUnits)
        {
            return torqueUnits switch
            {
                TorqueUnits.Nm => "Nm",
                TorqueUnits.lbf => "lb.-ft.",
                _ => throw new ArgumentOutOfRangeException(nameof(torqueUnits), torqueUnits, null)
            };
        }

        public static Torque FromNm(double nm)
        {
            return new Torque(nm);
        }

        public double GetValueInUnit(TorqueUnits torqueUnits)
        {
            return torqueUnits switch
            {
                TorqueUnits.Nm => InNm,
                TorqueUnits.lbf => InPoundFeet,
                _ => throw new ArgumentOutOfRangeException(nameof(torqueUnits), torqueUnits, null)
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
    }
}