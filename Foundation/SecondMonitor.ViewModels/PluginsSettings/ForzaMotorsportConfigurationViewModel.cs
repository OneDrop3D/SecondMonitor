namespace SecondMonitor.ViewModels.PluginsSettings
{
    using PluginsConfiguration.Common.DataModel;

    public class ForzaMotorsportConfigurationViewModel : AbstractViewModel<ForzaMotorsportConfiguration>
    {
        private int _port;

        private string _driverName;

        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        public string DriverName
        {
            get => _driverName;
            set => SetProperty(ref _driverName, value);
        }

        protected override void ApplyModel(ForzaMotorsportConfiguration model)
        {
            Port = model.Port;
            DriverName = model.DriverName;
        }

        public override ForzaMotorsportConfiguration SaveToNewModel()
        {
            return new ForzaMotorsportConfiguration() { Port = Port, DriverName = DriverName };
        }
    }
}