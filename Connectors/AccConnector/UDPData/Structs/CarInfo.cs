namespace SecondMonitor.AccConnector.UDPData.Structs
{
    using System.Collections.Generic;

    public class CarInfo
    {
        public CarInfo(ushort carIndex)
        {
            CarIndex = carIndex;
        }

        public ushort CarIndex { get; }
        public byte CarModelType { get; internal set; }
        public string TeamName { get; internal set; }
        public int RaceNumber { get; internal set; }
        public byte CupCategory { get; internal set; }
        public int CurrentDriverIndex { get; internal set; }
        public IList<DriverInfo> Drivers { get; } = new List<DriverInfo>();
        public NationalityEnum Nationality { get; internal set; }

        internal void AddDriver(DriverInfo driverInfo)
        {
            Drivers.Add(driverInfo);
        }

        public string GetCurrentDriverName()
        {
            if (CurrentDriverIndex < Drivers.Count)
            {
                return Drivers[CurrentDriverIndex].LastName;
            }

            return "nobody(?)";
        }
    }
}
