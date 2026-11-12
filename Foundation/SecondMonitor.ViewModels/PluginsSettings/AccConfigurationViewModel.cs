namespace SecondMonitor.ViewModels.PluginsSettings
{
    using PluginsConfiguration.Common.DataModel;

    public class AccConfigurationViewModel : AbstractViewModel<AccConfiguration>
    {
        private string _connectionPassword;
        private string _connectionCommandPassword;
        private int _port;
        private int _poolInterval;
        private string _customName;

        public string ConnectionPassword
        {
            get => _connectionPassword;
            set => SetProperty(ref _connectionPassword, value);
        }

        public string ConnectionCommandPassword
        {
            get => _connectionCommandPassword;
            set => SetProperty(ref _connectionCommandPassword, value);
        }

        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        public int PoolInterval
        {
            get => _poolInterval;
            set => SetProperty(ref _poolInterval, value);
        }

        public string CustomName
        {
            get => _customName;
            set => SetProperty(ref _customName, value);
        }

        protected override void ApplyModel(AccConfiguration model)
        {
            ConnectionPassword = model.ConnectionPassword;
            ConnectionCommandPassword = model.ConnectionCommandPassword;
            Port = model.Port;
            PoolInterval = model.PoolInterval;
            CustomName = model.CustomName;
        }

        public override AccConfiguration SaveToNewModel()
        {
            return new AccConfiguration()
            {
                ConnectionCommandPassword = ConnectionCommandPassword,
                CustomName = CustomName,
                PoolInterval = PoolInterval,
                Port = Port,
                ConnectionPassword = ConnectionPassword
            };
        }
    }
}