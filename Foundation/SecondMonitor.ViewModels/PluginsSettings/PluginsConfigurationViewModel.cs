namespace SecondMonitor.ViewModels.PluginsSettings
{
    using System.Collections.Generic;
    using System.Linq;
    using Factory;
    using PluginsConfiguration.Common.DataModel;

    public class PluginsConfigurationViewModel : AbstractViewModel<PluginsConfiguration>, IPluginsConfigurationViewModel
    {
        private readonly IViewModelFactory _viewModelFactory;
        private IRemoteConfigurationViewModel _remoteConfigurationViewModel;
        private IReadOnlyCollection<IPluginConfigurationViewModel> _pluginConfigurations;
        private IReadOnlyCollection<IConnectorConfigurationViewModel> _connectorConfigurations;
        private F12019ConfigurationViewModel _f12019ConfigurationViewModel;
        private ForzaMotorsportConfigurationViewModel _forzaMotorsportConfigurationViewModel;
        private PCars2ConfigurationViewModel _pCars2ConfigurationViewModel;
        private AccConfigurationViewModel _accConfigurationViewModel;
        private Ams2ConfigurationViewModel _ams2ConfigurationViewModel;

        public PluginsConfigurationViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }

        public AccConfigurationViewModel AccConfigurationViewModel
        {
            get => _accConfigurationViewModel;
            set => SetProperty(ref _accConfigurationViewModel, value);
        }

        public IRemoteConfigurationViewModel RemoteConfigurationViewModel
        {
            get => _remoteConfigurationViewModel;
            private set => SetProperty(ref _remoteConfigurationViewModel, value);
        }

        public IReadOnlyCollection<IPluginConfigurationViewModel> PluginConfigurations
        {
            get => _pluginConfigurations;
            private set => SetProperty(ref _pluginConfigurations, value);
        }

        public IReadOnlyCollection<IConnectorConfigurationViewModel> ConnectorConfigurations
        {
            get => _connectorConfigurations;
            private set => SetProperty(ref _connectorConfigurations, value);
        }

        public F12019ConfigurationViewModel F12019ConfigurationViewModel
        {
            get => _f12019ConfigurationViewModel;
            set => SetProperty(ref _f12019ConfigurationViewModel, value);
        }

        public ForzaMotorsportConfigurationViewModel ForzaMotorsportConfigurationViewModel
        {
            get => _forzaMotorsportConfigurationViewModel;
            set => SetProperty(ref _forzaMotorsportConfigurationViewModel, value);
        }

        public PCars2ConfigurationViewModel PCars2ConfigurationViewModel
        {
            get => _pCars2ConfigurationViewModel;
            set => SetProperty(ref _pCars2ConfigurationViewModel, value);
        }

        public Ams2ConfigurationViewModel Ams2ConfigurationViewModel
        {
            get => _ams2ConfigurationViewModel;
            set => SetProperty(ref _ams2ConfigurationViewModel, value);
        }

        protected override void ApplyModel(PluginsConfiguration model)
        {
            IRemoteConfigurationViewModel newViewModel = _viewModelFactory.Create<IRemoteConfigurationViewModel>();
            newViewModel.FromModel(model.RemoteConfiguration);
            RemoteConfigurationViewModel = newViewModel;

            List<IPluginConfigurationViewModel> newPluginsViewModel = new List<IPluginConfigurationViewModel>(model.PluginsConfigurations.Count);
            foreach (PluginConfiguration modelPluginsConfiguration in model.PluginsConfigurations)
            {
                IPluginConfigurationViewModel newPluginConfigurationViewModel = _viewModelFactory.Create<IPluginConfigurationViewModel>();
                newPluginConfigurationViewModel.FromModel(modelPluginsConfiguration);
                newPluginsViewModel.Add(newPluginConfigurationViewModel);
            }

            PluginConfigurations = newPluginsViewModel;

            List<IConnectorConfigurationViewModel> newConnectorsViewModel = new List<IConnectorConfigurationViewModel>(model.ConnectorConfigurations.Count);
            foreach (ConnectorConfiguration modelConnectorConfiguration in model.ConnectorConfigurations)
            {
                IConnectorConfigurationViewModel newConnectorConfigurationViewModel = _viewModelFactory.Create<IConnectorConfigurationViewModel>();
                newConnectorConfigurationViewModel.FromModel(modelConnectorConfiguration);
                newConnectorsViewModel.Add(newConnectorConfigurationViewModel);
            }

            ConnectorConfigurations = newConnectorsViewModel;

            F12019ConfigurationViewModel f12019ConfigurationViewModel = _viewModelFactory.Create<F12019ConfigurationViewModel>();
            f12019ConfigurationViewModel.FromModel(model.F12019Configuration);
            F12019ConfigurationViewModel = f12019ConfigurationViewModel;

            ForzaMotorsportConfigurationViewModel forzaMotorsportConfigurationViewModel = _viewModelFactory.Create<ForzaMotorsportConfigurationViewModel>();
            forzaMotorsportConfigurationViewModel.FromModel(model.ForzaMotorsportConfiguration);
            ForzaMotorsportConfigurationViewModel = forzaMotorsportConfigurationViewModel;

            PCars2ConfigurationViewModel pCarsConfigurationViewModel = _viewModelFactory.Create<PCars2ConfigurationViewModel>();
            pCarsConfigurationViewModel.FromModel(model.PCars2Configurations);
            PCars2ConfigurationViewModel = pCarsConfigurationViewModel;

            AccConfigurationViewModel accConfigurationViewModel = _viewModelFactory.Create<AccConfigurationViewModel>();
            accConfigurationViewModel.FromModel(model.AccConfiguration);
            AccConfigurationViewModel = accConfigurationViewModel;

            Ams2ConfigurationViewModel ams2ConfigurationViewModel = _viewModelFactory.Create<Ams2ConfigurationViewModel>();
            ams2ConfigurationViewModel.FromModel(model.Ams2Configuration);
            Ams2ConfigurationViewModel = ams2ConfigurationViewModel;
        }

        public override PluginsConfiguration SaveToNewModel()
        {
            return new PluginsConfiguration()
            {
                RemoteConfiguration = RemoteConfigurationViewModel.SaveToNewModel(),
                F12019Configuration = F12019ConfigurationViewModel.SaveToNewModel(),
                ForzaMotorsportConfiguration = ForzaMotorsportConfigurationViewModel.SaveToNewModel(),
                PCars2Configurations = PCars2ConfigurationViewModel.SaveToNewModel(),
                AccConfiguration = AccConfigurationViewModel.SaveToNewModel(),
                Ams2Configuration = Ams2ConfigurationViewModel.SaveToNewModel(),
                PluginsConfigurations = PluginConfigurations.Select(x => x.SaveToNewModel()).ToList(),
                ConnectorConfigurations = ConnectorConfigurations.Select(x => x.SaveToNewModel()).ToList()
            };
        }
    }
}