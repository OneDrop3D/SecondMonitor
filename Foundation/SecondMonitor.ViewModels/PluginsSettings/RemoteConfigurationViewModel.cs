namespace SecondMonitor.ViewModels.PluginsSettings
{
    using Factory;
    using PluginsConfiguration.Common.DataModel;

    public class RemoteConfigurationViewModel : AbstractViewModel<RemoteConfiguration>, IRemoteConfigurationViewModel
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly IHostAddressValidator _hostAddressValidator;
        private readonly IDialogService _dialogService;
        private string _hostAddress;
        private bool _isFindInLanEnabled;
        private int _port;
        private IBroadcastLimitSettingsViewModel _borBroadcastLimitSettingsViewModel;
        private bool _isMultiClientsEnabled;

        public RemoteConfigurationViewModel(IViewModelFactory viewModelFactory, IHostAddressValidator hostAddressValidator, IDialogService dialogService)
        {
            _viewModelFactory = viewModelFactory;
            _hostAddressValidator = hostAddressValidator;
            _dialogService = dialogService;
        }

        public string HostAddress
        {
            get => _hostAddress;
            set
            {
                if (_hostAddressValidator.IsValidHostAddress(value ?? string.Empty))
                {
                    SetProperty(ref _hostAddress, value ?? string.Empty);
                }
            }
        }

        public bool IsFindInLanEnabled
        {
            get => _isFindInLanEnabled;
            set => SetProperty(ref _isFindInLanEnabled, value);
        }

        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        public IBroadcastLimitSettingsViewModel BroadcastLimitSettingsViewModel
        {
            get => _borBroadcastLimitSettingsViewModel;
            set => SetProperty(ref _borBroadcastLimitSettingsViewModel, value);
        }

        public bool IsMultiClientEnabled
        {
            get => _isMultiClientsEnabled;
            set
            {
                if (value)
                {
                    if (!_dialogService.ShowYesNoDialog("Warning",
                            "This is an experimental setting that allows multiple broadcast server to connect to one client, who can then serve as a race engineer for them.\nTHIS IS EXPERIMENTAL, AND CAN CAUSE ISSUES. Enable only if you know you need this."))
                    {
                        NotifyPropertyChanged();
                        return;
                    }
                }

                SetProperty(ref _isMultiClientsEnabled, value);
            }
        }

        protected override void ApplyModel(RemoteConfiguration model)
        {
            if (string.IsNullOrEmpty(model.HostAddress))
            {
                HostAddress = model?.IpAddress ?? string.Empty;
            }
            else
            {
                _hostAddress = model.HostAddress;
            }

            IsFindInLanEnabled = model.IsFindInLanEnabled;
            Port = model.Port;
            _isMultiClientsEnabled = model.IsMultiClientsEnabled;

            IBroadcastLimitSettingsViewModel newBroadcastLimitSettingsViewModel =
                _viewModelFactory.CreateAndApply<IBroadcastLimitSettingsViewModel, BroadcastLimitSettings>(model.BroadcastLimitSettings);
            BroadcastLimitSettingsViewModel = newBroadcastLimitSettingsViewModel;
        }

        public override RemoteConfiguration SaveToNewModel()
        {
            return new RemoteConfiguration()
            {
                HostAddress = HostAddress,
                IsFindInLanEnabled = IsFindInLanEnabled,
                Port = Port,
                BroadcastLimitSettings = BroadcastLimitSettingsViewModel.SaveToNewModel(),
                IsMultiClientsEnabled = IsMultiClientEnabled,
            };
        }
    }
}