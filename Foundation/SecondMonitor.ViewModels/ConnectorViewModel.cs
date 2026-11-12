namespace SecondMonitor.ViewModels
{
    using System.Windows.Input;
    using Contracts.Commands;
    using Foundation.Connectors;
    public class ConnectorViewModel : AbstractViewModel
    {
        private ICommand _connectorOptions;
        private bool _hasConnectorOptions;

        public ConnectorViewModel()
        {
        }

        public ICommand ConnectorOptions
        {
            get => _connectorOptions;
            set
            {
                this.SetProperty(ref _connectorOptions, value);
                HasConnectorOptions = value != null;
            }
        }

        public bool HasConnectorOptions
        {
            get => _hasConnectorOptions;
            private set => SetProperty(ref _hasConnectorOptions, value);
        }

        public void BindConnector(IRawTelemetryConnector connector)
        {
            var connectorOptionsDelegate = connector.ConnectorOptionsDelegate;

            if (connectorOptionsDelegate == null)
            {
                ConnectorOptions = null;
            }
            else
            {
                this.ConnectorOptions = new RelayCommand(connectorOptionsDelegate);
            }
        }

        public void UnBindPreviousConnector()
        {
            this.ConnectorOptions = null;
        }
    }
}