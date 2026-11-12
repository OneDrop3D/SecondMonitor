namespace SecondMonitor.DataModel.BasicProperties
{
    using System;
    using System.Xml.Serialization;
    using Newtonsoft.Json;
    using ProtoBuf;

    [Serializable]
    [ProtoContract]
    public sealed class Pressure : IQuantity
    {
        public Pressure()
        {
            InKpa = 0;
        }

        private Pressure(double valueInKpa)
        {
            InKpa = valueInKpa;
        }

        [JsonIgnore]
        [XmlIgnore]
        public static Pressure Zero => new Pressure();

        [XmlAttribute]
        [ProtoMember(1, IsRequired = true)]
        public double InKpa { get; set; }

        [JsonIgnore]
        [XmlIgnore]
        public double InAtmospheres
        {
            get => InKpa / 101.3;
            set => InKpa = value * 101.3;
        }

        [JsonIgnore]
        [XmlIgnore]
        public double InBars
        {
            get => InKpa * 0.01;
            set => InKpa = value / 0.01;
        }

        [JsonIgnore]
        [XmlIgnore]
        public double InPsi
        {
            get => InKpa * 0.145038;
            set => InKpa = value / 0.145038;
        }

        [JsonIgnore]
        [XmlIgnore]
        public bool IsZero => InKpa == -1;

        [JsonIgnore]
        [XmlIgnore]
        public double RawValue => InKpa;

        public static string GetUnitSymbol(PressureUnits units)
        {
            return units switch
            {
                PressureUnits.Kpa => " KPa",
                PressureUnits.Atmosphere => " ATM",
                PressureUnits.Bar => "Bar",
                PressureUnits.Psi => "Psi",
                _ => throw new ArgumentException("Unable to return symbol fir" + units.ToString())
            };
        }

        public static Pressure FromKiloPascals(double pressureInKpa)
        {
            return new Pressure(pressureInKpa);
        }

        public static Pressure FromPsi(double pressureInPsi)
        {
            return new Pressure(pressureInPsi / 0.145038);
        }

        public static Pressure FromAtm(double pressure)
        {
            return new Pressure(pressure * 101.3);
        }

        public static Pressure FromBar(double pressure)
        {
            return new Pressure(pressure / 0.01);
        }

        public static Pressure GetFromUnits(double value, PressureUnits units)
        {
            return units switch
            {
                PressureUnits.Kpa => FromKiloPascals(value),
                PressureUnits.Atmosphere => FromAtm(value),
                PressureUnits.Bar => FromBar(value),
                PressureUnits.Psi => FromPsi(value),
                _ => throw new ArgumentException("Unable to return value in" + units.ToString())
            };
        }

        public void UpdateValue(double value, PressureUnits units)
        {
            switch (units)
            {
                case PressureUnits.Kpa:
                    InKpa = value;
                    return;
                case PressureUnits.Atmosphere:
                    InAtmospheres = value;
                    return;
                case PressureUnits.Bar:
                    InBars = value;
                    return;
                case PressureUnits.Psi:
                    InPsi = value;
                    return;
            }

            throw new ArgumentException("Unable to return symbol fir" + units.ToString());
        }

        public double GetValueInUnits(PressureUnits units)
        {
            return units switch
            {
                PressureUnits.Kpa => InKpa,
                PressureUnits.Atmosphere => InAtmospheres,
                PressureUnits.Bar => InBars,
                PressureUnits.Psi => InPsi,
                _ => throw new ArgumentException("Unable to return value in" + units.ToString())
            };
        }

        public string GetValueInUnits(PressureUnits units, int decimalPlaces)
        {
            return units switch
            {
                PressureUnits.Kpa => InKpa.ToString($"F{decimalPlaces}"),
                PressureUnits.Atmosphere => InAtmospheres.ToString($"F{decimalPlaces}"),
                PressureUnits.Bar => InBars.ToString($"F{decimalPlaces}"),
                PressureUnits.Psi => InPsi.ToString($"F{decimalPlaces}"),
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
    }
}