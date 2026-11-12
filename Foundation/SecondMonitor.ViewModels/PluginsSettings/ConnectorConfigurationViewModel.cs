namespace SecondMonitor.ViewModels.PluginsSettings
{
    using PluginsConfiguration.Common.DataModel;
    public class ConnectorConfigurationViewModel : AbstractViewModel<ConnectorConfiguration>, IConnectorConfigurationViewModel
    {
        private string _connectorName;
        private bool _isEnabled;

        public string ConnectorName
        {
            get => _connectorName;
            set => this.SetProperty(ref _connectorName, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => this.SetProperty(ref _isEnabled, value);
        }

        protected override void ApplyModel(ConnectorConfiguration model)
        {
            ConnectorName = model.ConnectorName;
            IsEnabled = model.IsEnabled;
        }

        public override ConnectorConfiguration SaveToNewModel()
        {
            return new ConnectorConfiguration()
            {
                ConnectorName = ConnectorName,
                IsEnabled = IsEnabled
            };
        }
    }
}