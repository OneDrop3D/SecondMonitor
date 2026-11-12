namespace SecondMonitor.Connectors.Remote.Models
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using DataModel.Snapshot.Drivers;
    using LiteNetLib;
    internal class VehicleEntry
    {
        private readonly Dictionary<IPEndPoint, DriverSource> _driverSources;
        public VehicleEntry(NetPeer netPeer, DriverInfo playerInfo)
        {
            EntryId = playerInfo.DriverSessionId;
            CarName = playerInfo.CarName;
            _driverSources = new Dictionary<IPEndPoint, DriverSource>();

            var currentDriver = new DriverSource(netPeer, playerInfo, this);
            _driverSources.Add(netPeer, currentDriver);
            CurrentDriver = currentDriver;
        }

        public string EntryId { get; }
        public string CarName { get; }
        public DriverSource CurrentDriver { get; private set; }

        public IEnumerable<IPEndPoint> DriverPeers => _driverSources.Keys;

        public void ChangeDriver(NetPeer netPeer, DriverInfo playerInfo)
        {
            if (_driverSources.ContainsKey(netPeer))
            {
                return;
            }

            var currentDriver = new DriverSource(netPeer, playerInfo, this);
            _driverSources.Add(netPeer, currentDriver);
            CurrentDriver = currentDriver;
        }

        public bool Contains(NetPeer netPeer)
        {
            return _driverSources.ContainsKey(netPeer);
        }

        public bool IsActivePeer(NetPeer netPeer)
        {
            throw new NotSupportedException();
        }

        public bool RemovePeer(NetPeer netPeer)
        {
            return _driverSources.Remove(netPeer);
        }
    }
}