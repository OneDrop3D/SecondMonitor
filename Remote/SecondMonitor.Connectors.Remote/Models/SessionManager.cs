namespace SecondMonitor.Connectors.Remote.Models
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using DataModel.Snapshot;
    using LiteNetLib;
    internal class SessionManager
    {
        private readonly HashSet<IPEndPoint> _pendingPeerEndpoints;

        private Session _activeSession;

        public SessionManager()
        {
            _pendingPeerEndpoints = new HashSet<IPEndPoint>();
        }

        public bool HasActiveSession => _activeSession != null;

        public bool AddPeer(NetPeer netPeer)
        {
            if (HasActiveSession && _activeSession.Contains(netPeer))
            {
                return false;
            }

            return _pendingPeerEndpoints.Add(netPeer);
        }

        public bool SessionStartedReceived(NetPeer netPeer, SimulatorDataSet dataSet)
        {
            if (!_pendingPeerEndpoints.Contains(netPeer) && !_activeSession.Contains(netPeer))
            {
                return false;
            }

            if (!HasActiveSession)
            {
                _activeSession = new Session(dataSet);
                _pendingPeerEndpoints.Remove(netPeer);
            }

            if (_activeSession.IsActivePeer(netPeer))
            {
                _pendingPeerEndpoints.UnionWith(_activeSession.DriverPeers);
                _activeSession = new Session(dataSet);
            }

            if (!_activeSession.Matches(dataSet))
            {
                if (_activeSession.Remove(netPeer))
                {
                    _pendingPeerEndpoints.Remove(netPeer);
                }

                return false;
            }

            if (!_activeSession.Contains(netPeer))
            {
                _pendingPeerEndpoints.Remove(netPeer);
                _activeSession.AddPeer(netPeer);
            }

            return _activeSession.SessionStartedReceived(netPeer, dataSet.PlayerInfo);
        }

        public bool DataSetReceived(NetPeer netPeer, SimulatorDataSet dataSet)
        {
            if (!HasActiveSession)
            {
                return SessionStartedReceived(netPeer, dataSet);
            }

            return _activeSession.Matches(dataSet) && _activeSession.DataSetReceived(netPeer, dataSet.PlayerInfo);
        }

        public bool SwitchToPeer(NetPeer peer, SimulatorDataSet dataSet)
        {
            if (_pendingPeerEndpoints.Contains(peer))
            {
                _pendingPeerEndpoints.UnionWith(_activeSession.DriverPeers);
                _activeSession = new Session(dataSet);
                _pendingPeerEndpoints.Remove(peer);
                return true;
            }

            if (_activeSession.Contains(peer))
            {
                _activeSession.SwitchActivePeer(peer);
                return false;
            }

            throw new Exception("Peer isn't tracked by Session Manager");
        }

        public bool IsActivePeerInDifferentSession(NetPeer peer, SimulatorDataSet dataSet)
        {
            if (!HasActiveSession)
            {
                return false;
            }

            if (_activeSession.HasAnyPeer && (!_activeSession.IsActivePeer(peer) || !_activeSession.Matches(dataSet)))
            {
                return false;
            }

            _activeSession = new Session(dataSet);
            _activeSession.AddPeer(peer);
            return _activeSession.SessionStartedReceived(peer, dataSet.PlayerInfo);
        }
    }
}