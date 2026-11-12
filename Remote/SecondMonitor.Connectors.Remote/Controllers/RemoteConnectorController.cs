namespace SecondMonitor.Connectors.Remote.Controllers
{
    using System.Windows;
    using Contracts.Commands;
    using View;
    using ViewModels;

    internal class RemoteConnectorController
    {
        private readonly RemoteConnectorOptionsViewModel _optionsViewModel;
        private readonly IRemoteClient _remoteClient;
        private readonly AddRemoteSourceController _addRemoteSourceController;

        private RemoteConnectorWindow _remoteConnectorWindow;

        public RemoteConnectorController(RemoteConnectorOptionsViewModel optionsViewModel, IRemoteClient remoteClient, AddRemoteSourceController addRemoteSourceController)
        {
            _optionsViewModel = optionsViewModel;
            _remoteClient = remoteClient;
            _addRemoteSourceController = addRemoteSourceController;
        }

        public void OpenRemoteConnectorOptions()
        {
            if (_remoteConnectorWindow?.IsVisible == true)
            {
                _remoteConnectorWindow.Focus();
                return;
            }

            _remoteConnectorWindow = new RemoteConnectorWindow
            {
                DataContext = _optionsViewModel,
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            _remoteConnectorWindow.Show();
        }

        public void StartController()
        {
            _optionsViewModel.SwitchActivePeer ??= peerEndpoint => _remoteClient.SwitchToPeer(peerEndpoint);

            _optionsViewModel.AddNewServer ??= new RelayCommand(ShowAddRemoteSourceDialog);
        }

        public void StopController()
        {
            _optionsViewModel.SwitchActivePeer = null;
        }

        private void ShowAddRemoteSourceDialog()
        {
            _addRemoteSourceController.ShowDialog(_remoteConnectorWindow);
        }
    }
}