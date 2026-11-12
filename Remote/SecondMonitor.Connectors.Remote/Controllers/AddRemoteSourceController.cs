namespace SecondMonitor.Connectors.Remote.Controllers
{
    using System.Linq;
    using System.Windows;
    using Contracts.Commands;
    using LiteNetLib;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.PluginsSettings;
    using View;
    using ViewModels;

    internal class AddRemoteSourceController
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly IRemoteConnectorConfiguration _remoteConnectorConfiguration;
        private readonly IHostAddressValidator _hostAddressValidator;
        private readonly IRemoteClient _remoteClient;

        private AddRemoteSourceViewModel _model;
        private AddRemoteSourceDialog _dialog;

        public AddRemoteSourceController(IViewModelFactory viewModelFactory, IRemoteConnectorConfiguration remoteConnectorConfiguration, IHostAddressValidator hostAddressValidator, IRemoteClient remoteClient)
        {
            _viewModelFactory = viewModelFactory;
            _remoteConnectorConfiguration = remoteConnectorConfiguration;
            _hostAddressValidator = hostAddressValidator;
            _remoteClient = remoteClient;
        }

        public void ShowDialog(RemoteConnectorWindow window)
        {
            _model = _viewModelFactory.Create<AddRemoteSourceViewModel>();
            _model.Port = _remoteConnectorConfiguration.ServerPeers.First().Port;
            _model.ConnectCommand = new RelayCommand(TryConnect);

            _dialog = new AddRemoteSourceDialog
            {
                Owner = window,
                DataContext = _model
            };

            var dialogResult = _dialog.ShowDialog();

            if (dialogResult != true)
            {
                return;
            }

            var endpoint = NetUtils.MakeEndPoint(_model.HostAddress, _model.Port);
            _remoteClient.ConnectToPeer(endpoint);
        }

        private void TryConnect()
        {
            _model.HostAddressIsValid = _hostAddressValidator.IsValidHostAddress(_model.HostAddress);

            if (_model.HostAddressIsValid)
            {
                _dialog.DialogResult = true;
            }
            else
            {
                MessageBox.Show("Invalid Host Address", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}