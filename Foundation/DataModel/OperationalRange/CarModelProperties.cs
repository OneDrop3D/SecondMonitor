namespace SecondMonitor.DataModel.OperationalRange
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    using BasicProperties;

    [Serializable]
    public sealed class CarModelProperties
    {
        public CarModelProperties()
        {
            TyreCompoundsProperties = new List<TyreCompoundProperties>();
            WheelRotation = 540;
            FrontStaticRideHeight = Distance.ZeroDistance;
            RearStaticRideHeight = Distance.ZeroDistance;
            MotionRatio = 1;
            FrontWheelDiameter = Distance.FromMeters(0.4572);
            RearWheelDiameter = Distance.FromMeters(0.4572);
            CarCustomName = string.Empty;
        }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public bool OriginalContainsOptimalTemperature { get; set; }

        [XmlAttribute]
        public int WheelRotation { get; set; }

        [XmlAttribute]
        public string CarCustomName { get; set; }

        public Temperature OptimalBrakeTemperature { get; set; }
        public Temperature OptimalBrakeTemperatureWindow { get; set; }

        public Distance FrontStaticRideHeight { get; set; }

        public Distance RearStaticRideHeight { get; set; }

        public double MotionRatio { get; set; }

        public Distance FrontWheelDiameter { get; set; }

        public Distance RearWheelDiameter { get; set; }

        [XmlAttribute]
        public bool WasWheelDiameterSet { get; set; }

        public List<TyreCompoundProperties> TyreCompoundsProperties { get; set; }

        public TyreCompoundProperties GetTyreCompound(string compoundName)
        {
            return TyreCompoundsProperties.FirstOrDefault(x => x.CompoundName == compoundName);
        }

        public void AddTyreCompound(TyreCompoundProperties newCompound)
        {
            TyreCompoundsProperties.RemoveAll(x => x.CompoundName == newCompound.CompoundName);
            TyreCompoundsProperties.Add(newCompound);
        }
    }
}