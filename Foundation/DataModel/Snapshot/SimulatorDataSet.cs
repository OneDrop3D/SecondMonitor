namespace SecondMonitor.DataModel.Snapshot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BasicProperties;
    using Drivers;

    [Serializable]
    public sealed class SimulatorDataSet
    {
        public SimulatorDataSet() : this(string.Empty)
        {
        }

        public SimulatorDataSet(string source)
        {
            Source = source;
            InputInfo = new InputInfo();
            SessionInfo = new SessionInfo();
            DriversInfo = Array.Empty<DriverInfo>();
            PlayerInfo = new DriverInfo();
            LeaderInfo = new DriverInfo();
        }

        public string Source { get; set; }

        public InputInfo InputInfo { get; set; }

        public SessionInfo SessionInfo { get; set; }

        public DriverInfo[] DriversInfo { get; set; }

        public DriverInfo PlayerInfo { get; set; }

        public DriverInfo LeaderInfo { get; set; }

        public SimulatorSourceInfo SimulatorSourceInfo { get; set; } = new SimulatorSourceInfo();

        public void Accept(ISimulatorDataSetVisitor simulatorDataSetVisitor)
        {
            simulatorDataSetVisitor.Visit(this);
        }

        public IReadOnlyCollection<DriverInfo> GetDriversInfoByMultiClass()
        {
            return SessionInfo.IsMultiClass ? DriversInfo.Where(x => x.CarClassId == PlayerInfo.CarClassId).ToList() : DriversInfo.ToList();
        }

        public bool IsWetSession()
        {
            return SessionInfo.WeatherInfo.RainIntensity > 0 || SessionInfo.WeatherInfo.TrackWetness > 0 ||
                   (PlayerInfo?.InPits == false && PlayerInfo.CarInfo.WheelsInfo.AllWheels.Any(x =>
                       x.TyreType.Equals("wet", StringComparison.CurrentCultureIgnoreCase) || x.TyreType.Equals("Intermediate", StringComparison.CurrentCultureIgnoreCase)));
        }
    }
}