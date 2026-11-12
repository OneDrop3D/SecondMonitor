namespace SecondMonitor.Connectors.Remote.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using LiteNetLib;

    internal class Session
    {
        private readonly HashSet<IPEndPoint> _pendingPeerEndpoints;
        private readonly Dictionary<string, VehicleEntry> _trackedEntries;

        private VehicleEntry _activeEntry;

        public Session(SimulatorDataSet dataSet)
        {
            TrackName = dataSet.SessionInfo.TrackInfo.TrackFullName;
            SessionType = dataSet.SessionInfo.SessionType;
            SimulatorSource = dataSet.Source;
            _pendingPeerEndpoints = new HashSet<IPEndPoint>();
            _trackedEntries = new Dictionary<string, VehicleEntry>();
        }

        public string TrackName { get; }

        public SessionType SessionType { get; }

        public string SimulatorSource { get; }

        public IEnumerable<IPEndPoint> DriverPeers => _pendingPeerEndpoints.Union(_trackedEntries.Values.SelectMany(x => x.DriverPeers));
        public bool HasAnyPeer => _activeEntry != null;

        public bool SessionStartedReceived(NetPeer netPeer, DriverInfo playerInfo)
        {
            if (string.IsNullOrWhiteSpace(playerInfo.DriverSessionId))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(playerInfo.CarName))
            {
                return false;
            }

            if (_pendingPeerEndpoints.Contains(netPeer))
            {
                _pendingPeerEndpoints.Remove(netPeer);
            }

            if (_trackedEntries.TryGetValue(playerInfo.DriverSessionId, out var vehicleEntry))
            {
                vehicleEntry.ChangeDriver(netPeer, playerInfo);
            }
            else
            {
                vehicleEntry = new VehicleEntry(netPeer, playerInfo);
                _trackedEntries.Add(playerInfo.DriverSessionId, vehicleEntry);
            }

            if (_activeEntry == null)
            {
                _activeEntry = vehicleEntry;
            }

            return _activeEntry == vehicleEntry;
        }

        public bool DataSetReceived(NetPeer netPeer, DriverInfo playerInfo)
        {
            if (!Contains(netPeer) || playerInfo.DriverSessionId == string.Empty)
            {
                return false;
            }

            if (!_trackedEntries.TryGetValue(playerInfo.DriverSessionId, out var vehicleEntry))
            {
                return false;
            }

            if (!vehicleEntry.Contains(netPeer))
            {
                vehicleEntry.ChangeDriver(netPeer, playerInfo);
            }

            return _activeEntry == vehicleEntry;
        }

        public bool Matches(SimulatorDataSet dataSet)
        {
            return SessionType == dataSet.SessionInfo.SessionType && TrackName == dataSet.SessionInfo.TrackInfo.TrackFullName && SimulatorSource == dataSet.Source;
        }

        public bool Contains(NetPeer netPeer)
        {
            return _pendingPeerEndpoints.Contains(netPeer) || _trackedEntries.Values.Any(x => x.Contains(netPeer));
        }

        public bool IsActivePeer(NetPeer netPeer)
        {
            return _activeEntry?.Contains(netPeer) ?? false;
        }

        public bool Remove(NetPeer netPeer)
        {
            if (_pendingPeerEndpoints.Remove(netPeer))
            {
                return true;
            }

            var vehicleEntry = _trackedEntries.Values.SingleOrDefault(x => x.Contains(netPeer));

            return vehicleEntry?.RemovePeer(netPeer) ?? false;
        }

        public bool AddPeer(NetPeer netPeer)
        {
            return !_trackedEntries.Values.Any(x => x.Contains(netPeer)) && _pendingPeerEndpoints.Add(netPeer);
        }

        public void SwitchActivePeer(NetPeer peer)
        {
            if (_pendingPeerEndpoints.Contains(peer))
            {
                return;
            }

            _activeEntry = _trackedEntries.Values.Single(x => x.Contains(peer));
        }
    }
}