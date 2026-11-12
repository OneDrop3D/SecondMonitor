namespace SecondMonitor.DataModel.Summary.FuelStrategy
{
    using System;
    using System.Xml.Serialization;

    using SecondMonitor.DataModel.Summary.FuelStrategy.PitWindowStrategy;
    using SecondMonitor.DataModel.Summary.FuelStrategy.TankToTankStrategy;
    using SecondMonitor.DataModel.Summary.FuelStrategy.TimedStintStrategy;
    using SecondMonitor.DataModel.Summary.FuelStrategy.VirtualEnergyStrategy;

    [Serializable]
    public class ClassFuelStrategy
    {
        [XmlAttribute]
        public string SimName { get; set; } = string.Empty;

        [XmlAttribute]
        public string ClassId { get; set; } = string.Empty;

        [XmlAttribute]
        public string LastSelectedStrategy { get; set; } = string.Empty;

        [XmlAttribute]
        public int FuelMultiplier { get; set; } = 1;

        public TankToTankStrategySettings TankToTankStrategySettings { get; set; } = new();

        public PitWindowStrategySettings PitWindowStrategySettings { get; set; } = new();

        public TimedStintStrategySettings TimedStintStrategySettings { get; set; } = new();

        public VirtualTankToTankStrategySettings VirtualTankToTankStrategySettings { get; set; } = new();
    }
}
