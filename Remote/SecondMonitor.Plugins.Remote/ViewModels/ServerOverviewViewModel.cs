namespace SecondMonitor.Plugins.Remote.ViewModels
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows.Threading;
    using Extension;
    using LiteNetLib;
    using SecondMonitor.Remote.ViewModels;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    public class ServerOverviewViewModel : AbstractViewModel, IServerOverviewViewModel
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly Dictionary<string, IClientViewModel> _ipClientDictionary;

        private List<string> _availableIps;
        private bool _isRunning;
        private ObservableCollection<IClientViewModel> _connectedClients;

        private double _throttleInput;
        private double _clutchInput;
        private double _brakeInput;
        private string _connectedSimulator;

        public ServerOverviewViewModel(IViewModelFactory viewModelFactory, INetworkStatsViewModel networkStatsViewModel)
        {
            _viewModelFactory = viewModelFactory;
            _ipClientDictionary = new Dictionary<string, IClientViewModel>();
            ConnectedClients = new ObservableCollection<IClientViewModel>();
            NetworkStatsViewModel = networkStatsViewModel;
        }

        public List<string> AvailableIps
        {
            get => _availableIps;
            set => this.SetProperty(ref _availableIps, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set => this.SetProperty(ref _isRunning, value);
        }

        public double ThrottleInput
        {
            get => _throttleInput;
            set => this.SetProperty(ref _throttleInput, value);
        }

        public double ClutchInput
        {
            get => _clutchInput;
            set => this.SetProperty(ref _clutchInput, value);
        }

        public double BrakeInput
        {
            get => _brakeInput;
            set => this.SetProperty(ref _brakeInput, value);
        }

        public string ConnectedSimulator
        {
            get => _connectedSimulator;
            set => this.SetProperty(ref _connectedSimulator, value);
        }

        public ObservableCollection<IClientViewModel> ConnectedClients
        {
            get => _connectedClients;
            private set => this.SetProperty(ref _connectedClients, value);
        }

        public INetworkStatsViewModel NetworkStatsViewModel
        {
            get;
        }

        public void AddClient(NetPeer netPeer)
        {
            if (!Dispatcher.CurrentDispatcher.CheckAccess())
            {
                Dispatcher.CurrentDispatcher.Invoke(() => AddClient(netPeer));
                return;
            }

            RemoveClient(netPeer);
            IClientViewModel newViewModel = _viewModelFactory.Create<IClientViewModel>();
            newViewModel.FromModel(netPeer);
            _connectedClients.Add(newViewModel);
            _ipClientDictionary[netPeer.GetIdentifier()] = newViewModel;
        }

        public void RemoveClient(NetPeer netPeer)
        {
            if (!Dispatcher.CurrentDispatcher.CheckAccess())
            {
                Dispatcher.CurrentDispatcher.Invoke(() => RemoveClient(netPeer));
                return;
            }

            if (!_ipClientDictionary.ContainsKey(netPeer.GetIdentifier()))
            {
                return;
            }

            IClientViewModel removedViewModel = _ipClientDictionary[netPeer.GetIdentifier()];
            _ipClientDictionary.Remove(netPeer.GetIdentifier());
            _connectedClients.Remove(removedViewModel);
        }

        public void SampleStats(NetStatistics netStatistics)
        {
            if (!Dispatcher.CurrentDispatcher.CheckAccess())
            {
                Dispatcher.CurrentDispatcher.Invoke(() => SampleStats(netStatistics));
                return;
            }

            NetworkStatsViewModel.FromModel(netStatistics);
        }
    }
}