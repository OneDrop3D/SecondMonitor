namespace SecondMonitor.Connectors.Remote
{
    using System;
    using System.Threading.Tasks;
    using Controllers;
    using DataModel.Snapshot;

    using NLog;

    using SecondMonitor.Foundation.Connectors;

    internal class RemoteConnector : IRawTelemetryConnector
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly IRemoteClient _remoteClient;
        private readonly RemoteConnectorController _remoteConnectorController;
        private bool _wasSessionStarted;

        public RemoteConnector(IRemoteClient remoteClient, RemoteConnectorController remoteConnectorController)
        {
            _remoteClient = remoteClient;
            _remoteConnectorController = remoteConnectorController;
            _remoteClient.Disconnected += ClientDisconnected;
            _remoteClient.SessionStarted += ClientSessionStarted;
            _remoteClient.DataLoaded += ClientDataLoaded;
        }

        public event EventHandler<DataEventArgs> DataLoaded;
        public event EventHandler<EventArgs> ConnectedEvent;
        public event EventHandler<EventArgs> Disconnected;
        public event EventHandler<DataEventArgs> SessionStarted;
        public event EventHandler<MessageArgs> DisplayMessage;

        public string Name => "Remote";
        public bool IsConnected { get; private set; }
        public bool IsEnabledByDefault => false;
        public Action ConnectorOptionsDelegate => ShowConnectorOptions;

        public bool TryConnect()
        {
            _remoteConnectorController.StartController();
            return _remoteClient.TryConnect();
        }

        public Task FinnishConnectorAsync()
        {
            _remoteConnectorController.StopController();
            return Task.CompletedTask;
        }

        public void StartConnectorLoop()
        {
            IsConnected = true;
            ConnectedEvent?.Invoke(this, EventArgs.Empty);
            _remoteClient.StartClientLoop();
        }

        private void ShowConnectorOptions()
        {
            _remoteConnectorController.OpenRemoteConnectorOptions();
        }

        private void ClientDisconnected(object sender, EventArgs eventArgs)
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        private void ClientSessionStarted(object sender, SimulatorDataSet simulatorDataSet)
        {
            _wasSessionStarted = true;
            SessionStarted?.Invoke(this, new DataEventArgs(simulatorDataSet));
        }

        private void ClientDataLoaded(object sender, SimulatorDataSet simulatorDataSet)
        {
            if (!_wasSessionStarted)
            {
                logger.Info("Forcing Client Session Start");
                ClientSessionStarted(sender, simulatorDataSet);
            }
            else
            {
                DataLoaded?.Invoke(this, new DataEventArgs(simulatorDataSet));
            }
        }
    }
}