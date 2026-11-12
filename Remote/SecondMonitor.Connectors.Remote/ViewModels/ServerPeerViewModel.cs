namespace SecondMonitor.Connectors.Remote.ViewModels
{
    using System;
    using System.Windows.Input;
    using LiteNetLib;
    using SecondMonitor.Remote.ViewModels;
    using SecondMonitor.ViewModels;

    internal enum ConnectionState
    {
          Connected,
          Disconnected,
          Connecting,
          Disconnecting
    }

    internal class ServerPeerViewModel : AbstractViewModel
    {
        private int _id;
        private string _endpoint;
        private string _driverName;
        private string _trackName;
        private ConnectionState _connectionState;
        private bool _isActive;
        private ICommand _makeActiveCommand;
        private string _simulatorSource;

        public ServerPeerViewModel(INetworkStatsViewModel networkNetworkStats)
        {
            NetworkStats = networkNetworkStats;
            SimulatorSource = "Unknown";
        }

        public int Id
        {
            get => _id;
            set => this.SetProperty(ref _id, value);
        }

        public string EndPoint
        {
            get => _endpoint;
            set => this.SetProperty(ref _endpoint, value);
        }

        public string SimulatorSource
        {
            get => _simulatorSource;
            set => this.SetProperty(ref _simulatorSource, value);
        }

        public string DriverName
        {
            get => _driverName;
            set => this.SetProperty(ref _driverName, value);
        }

        public string TrackName
        {
            get => _trackName;
            set => this.SetProperty(ref _trackName, value);
        }

        public ConnectionState ConnectionState
        {
            get => _connectionState;
            private set
            {
                this.SetProperty(ref _connectionState, value);
                this.NotifyPropertyChanged(nameof(CanSwitchToSource));
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                this.SetProperty(ref _isActive, value);
                this.NotifyPropertyChanged(nameof(CanSwitchToSource));
            }
        }

        public ICommand MakeActiveCommand
        {
            get => _makeActiveCommand;
            set => this.SetProperty(ref _makeActiveCommand, value);
        }

        public INetworkStatsViewModel NetworkStats
        {
            get;
        }

        public bool CanSwitchToSource { get => !_isActive && ConnectionState == ConnectionState.Connected && DriverName != string.Empty; }

        public void UpdatePeerState(NetPeer peer)
        {
            switch (peer.ConnectionState)
            {
                case LiteNetLib.ConnectionState.Connected:
                    ConnectionState = ConnectionState.Connected;
                    break;
                case LiteNetLib.ConnectionState.Disconnected:
                    ConnectionState = ConnectionState.Disconnected;
                    break;
                case LiteNetLib.ConnectionState.Outgoing:
                    ConnectionState = ConnectionState.Connecting;
                    break;
                case LiteNetLib.ConnectionState.ShutdownRequested:
                    ConnectionState = ConnectionState.Disconnecting;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SamplePeerStats(NetPeer peer)
        {
            NetworkStats.FromModel(peer.Statistics);
        }
    }
}