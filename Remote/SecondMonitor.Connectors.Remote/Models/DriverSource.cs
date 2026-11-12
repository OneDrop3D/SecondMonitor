namespace SecondMonitor.Connectors.Remote.Models
{
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using LiteNetLib;
    using ConnectionState = ViewModels.ConnectionState;
    internal class DriverSource
    {
        public DriverSource(NetPeer netPeer, DriverInfo playerInfo, VehicleEntry vehicleEntry)
        {
            ConnectionState = netPeer.ConnectionState.Convert();

            DriverName = playerInfo.DriverLongName;
            VehicleEntry = vehicleEntry;
        }

        public ConnectionState ConnectionState
        {
            get;
            private set;
        }

        public string DriverName { get; set; }

        public VehicleEntry VehicleEntry { get; set; }

        public bool DataSetReceived(NetPeer netPeer, SimulatorDataSet dataSet)
        {
            ConnectionState = netPeer.ConnectionState.Convert();

            DriverName = dataSet.PlayerInfo.DriverLongName == string.Empty ? null : dataSet.PlayerInfo.DriverLongName;

            return VehicleEntry != null;
        }
    }
}