namespace SecondMonitor.Connectors.Remote.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Net;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Threading;
    using Contracts.Commands;
    using LiteNetLib;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    internal class RemoteConnectorOptionsViewModel : AbstractViewModel
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly Dictionary<IPEndPoint, ServerPeerViewModel> _serverPeers;

        private ServerPeerViewModel _activeServerPeer;
        private ICommand _addNewServer;

        public RemoteConnectorOptionsViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            ServerPeers = new ObservableCollection<ServerPeerViewModel>();
            _serverPeers = new Dictionary<IPEndPoint, ServerPeerViewModel>();
        }

        public ServerPeerViewModel ActiveServerPeer
        {
            get => _activeServerPeer;
            set => this.SetProperty(ref _activeServerPeer, value);
        }

        public ObservableCollection<ServerPeerViewModel> ServerPeers { get; }
        public Action<IPEndPoint> SwitchActivePeer { get; set; }

        public ICommand AddNewServer
        {
            get => _addNewServer;
            set => this.SetProperty(ref _addNewServer, value);
        }

        public void SetActivePeer(NetPeer netPeer)
        {
            if (!_serverPeers.TryGetValue(netPeer, out var activeServerPeer))
            {
                return;
            }

            if (ActiveServerPeer != null)
            {
                ActiveServerPeer.IsActive = false;
            }

            ActiveServerPeer = activeServerPeer;
            ActiveServerPeer.IsActive = true;
        }

        public void AddPeer(NetPeer netPeer)
        {
            if (!Dispatcher.CurrentDispatcher.CheckAccess())
            {
                Dispatcher.CurrentDispatcher.Invoke(() => AddPeer(netPeer));
                return;
            }

            var peerEndpoint = netPeer.ToString();

            var peerId = netPeer.Id;
            if (!_serverPeers.TryGetValue(netPeer, out var peerViewModel))
            {
                peerViewModel = _viewModelFactory.Create<ServerPeerViewModel>();
                _serverPeers.Add(netPeer, peerViewModel);
                ServerPeers.Add(peerViewModel);
            }

            peerViewModel.Id = peerId;
            peerViewModel.EndPoint = peerEndpoint;
            peerViewModel.MakeActiveCommand = new RelayCommand(() => SwitchActivePeer(netPeer));
            peerViewModel.UpdatePeerState(netPeer);
        }

        public void UpdatePeer(NetPeer peer, string simulatorSource, string playerInfoDriverLongName, string trackInfoTrackFullName)
        {
            if (!Dispatcher.CurrentDispatcher.CheckAccess())
            {
                Dispatcher.CurrentDispatcher.Invoke(() => UpdatePeer(peer, simulatorSource, playerInfoDriverLongName, trackInfoTrackFullName));
                return;
            }

            if (!_serverPeers.TryGetValue(peer, out var existingPeer))
            {
                return;
            }

            existingPeer.UpdatePeerState(peer);

            existingPeer.SimulatorSource = simulatorSource ?? "Unknown";
            existingPeer.DriverName = playerInfoDriverLongName;
            existingPeer.TrackName = trackInfoTrackFullName;
        }

        public void UpdatePeerState(NetPeer peer)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => UpdatePeerState(peer));
                return;
            }

            if (!_serverPeers.TryGetValue(peer, out var existingPeer))
            {
                return;
            }

            existingPeer.UpdatePeerState(peer);
        }

        public void SamplePeerStats(NetPeer peer)
        {
            if (!_serverPeers.TryGetValue(peer, out var existingPeer))
            {
                return;
            }

            existingPeer.SamplePeerStats(peer);
        }
    }
}