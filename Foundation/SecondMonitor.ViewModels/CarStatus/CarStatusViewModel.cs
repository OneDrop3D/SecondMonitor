namespace SecondMonitor.ViewModels.CarStatus
{
    using System;
    using Contracts.SimSettings;
    using DataModel.BasicProperties;
    using DataModel.OperationalRange;
    using DataModel.Snapshot;
    using FuelStatus;
    using SessionEvents;
    using Settings;

    public class CarStatusViewModel : AbstractViewModel, ISimulatorDataSetViewModel
    {
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly ICarSpecificationProvider _carSpecificationProvider;
        private readonly SimulatorDSViewModels _viewModels;
        private CarModelProperties _currentCarProperties;
        private CarWheelsViewModel _playersWheelsViewModel;
        private CarSystemsViewModel _carSystemsViewModel;
        private DashboardViewModel _dashboardViewModel;

        private PedalsAndGearViewModel _pedalAndGearViewModel;

        public CarStatusViewModel(SessionRemainingCalculator sessionRemainingCalculator, IPaceProvider paceProvider,  ISettingsProvider settingsProvider, ISimSettingsFactory settingsFactory, ISessionEventProvider sessionEventProvider, ICarSpecificationProvider carSpecificationProvider, IFuelPredictionProvider fuelPredictionProvider)
        {
            _sessionEventProvider = sessionEventProvider;
            _carSpecificationProvider = carSpecificationProvider;
            _viewModels = new SimulatorDSViewModels 
            {
                new CarWheelsViewModel(sessionRemainingCalculator,
                paceProvider,
                new WheelStatusViewModelFactory(settingsProvider),
                fuelPredictionProvider), new PedalsAndGearViewModel(settingsProvider),
                new CarSystemsViewModel(settingsProvider, settingsFactory, sessionEventProvider),
                new DashboardViewModel(settingsFactory, sessionEventProvider, settingsProvider) 
            };
            sessionEventProvider.PlayerPropertiesChanged += SessionEventProviderOnPlayerPropertiesChanged;
            carSpecificationProvider.DataSourcePropertiesUpdated += CarSpecificationProviderOnDataSourcePropertiesUpdated;
            RefreshProperties();
        }

        public PedalsAndGearViewModel PedalsAndGearViewModel
        {
            get => _pedalAndGearViewModel;
            set => SetProperty(ref _pedalAndGearViewModel, value);
        }

        public CarWheelsViewModel PlayersWheelsViewModel
        {
            get => _playersWheelsViewModel;
            private set => SetProperty(ref _playersWheelsViewModel, value);
        }

        public CarSystemsViewModel CarSystemsViewModel
        {
            get => _carSystemsViewModel;
            private set => SetProperty(ref _carSystemsViewModel, value);
        }

        public DashboardViewModel DashboardViewModel
        {
            get => _dashboardViewModel;
            set => SetProperty(ref _dashboardViewModel, value);
        }

        public void ApplyDateSet(SimulatorDataSet dataSet)
        {
            if (dataSet.SessionInfo.SessionType == SessionType.Na)
            {
                return;
            }

            PlayersWheelsViewModel.ApplyDateSet(dataSet);
            CarSystemsViewModel.ApplyDateSet(dataSet);
            DashboardViewModel.ApplyDateSet(dataSet);
        }

        public void HighFrequencyInformation(SimulatorDataSet dataSet)
        {
            if (dataSet == null)
            {
                return;
            }

            DashboardViewModel.UpdateHighFrequencyInformation(dataSet);
            PlayersWheelsViewModel.UpdateHighFrequencyInformation(dataSet);
        }

        public void Reset()
        {
            _viewModels.Reset();
        }

        private void RefreshProperties()
        {
            PlayersWheelsViewModel = _viewModels.GetFirst<CarWheelsViewModel>();
            PedalsAndGearViewModel = _viewModels.GetFirst<PedalsAndGearViewModel>();
            CarSystemsViewModel = _viewModels.GetFirst<CarSystemsViewModel>();
            DashboardViewModel = _viewModels.GetFirst<DashboardViewModel>();
        }

        private void CarSpecificationProviderOnDataSourcePropertiesUpdated(object sender, EventArgs e)
        {
            UpdateWheelDiameters(_sessionEventProvider.LastDataSet);
        }

        private void SessionEventProviderOnPlayerPropertiesChanged(object sender, DataSetArgs e)
        {
            SimulatorDataSet dataSet = e.DataSet;
            UpdateWheelDiameters(dataSet);
        }

        private void UpdateWheelDiameters(SimulatorDataSet dataSet)
        {
            _currentCarProperties = _carSpecificationProvider.GetCarModelProperties(dataSet.Source, dataSet.PlayerInfo.CarName);
            if (_currentCarProperties == null)
            {
                return;
            }

            _playersWheelsViewModel.FrontWheelRadiusInMeters = _currentCarProperties.WasWheelDiameterSet ? _currentCarProperties.FrontWheelDiameter.InMeters : CarWheelsViewModel.DefaultWheelRadius;
            _playersWheelsViewModel.RearWheelRadiusInMeters = _currentCarProperties.WasWheelDiameterSet ? _currentCarProperties.RearWheelDiameter.InMeters : CarWheelsViewModel.DefaultWheelRadius;
        }
    }
}