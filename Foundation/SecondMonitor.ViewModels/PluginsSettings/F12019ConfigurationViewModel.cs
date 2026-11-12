namespace SecondMonitor.ViewModels.PluginsSettings
{
    using PluginsConfiguration.Common.DataModel;

    public class F12019ConfigurationViewModel : AbstractViewModel<F12019Configuration>
    {
        private int _port;
        private string _myTeamName;

        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        public string MyTeamName
        {
            get => _myTeamName;
            set => SetProperty(ref _myTeamName, value);
        }

        protected override void ApplyModel(F12019Configuration model)
        {
            Port = model.Port;
            MyTeamName = model.MyTeamName;
        }

        public override F12019Configuration SaveToNewModel()
        {
            return new F12019Configuration()
            {
                Port = Port,
                MyTeamName = MyTeamName,
            };
        }
    }
}