namespace SecondMonitor.ViewModels.PluginsSettings
{
    using PluginsConfiguration.Common.DataModel;

    public class PCars2ConfigurationViewModel : AbstractViewModel<PCars2Configuration>
    {
        private int _port;

        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        protected override void ApplyModel(PCars2Configuration model)
        {
            Port = model.Port;
        }

        public override PCars2Configuration SaveToNewModel()
        {
            return new PCars2Configuration() { Port = Port };
        }
    }
}