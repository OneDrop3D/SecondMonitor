namespace SecondMonitor.ViewModels.CarStatus
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using Contracts.SimSettings;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using SessionEvents;
    using Settings;
    using Settings.ViewModel;

    public class CarSystemsViewModel : AbstractViewModel, ISimulatorDataSetViewModel, IDisposable
    {
        public const string ViewModelLayoutName = "Car Temperatures and Pressures";
        private readonly ISimSettingsFactory _simSettingsFactory;
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly Stopwatch _temperatureRefreshStopwatch;

        private OptimalQuantity<Temperature> _waterTemperature;
        private OptimalQuantity<Temperature> _oilTemperature;
        private Pressure _turboPressure;
        private Pressure _oilPressure;
        private Pressure _waterPressure;
        private Pressure _fuelPressure;
        private TemperatureUnits _temperatureUnits;
        private PressureUnits _pressureUnits;

        private bool _isTurboPressureVisible;
        private bool _isOilPressureVisible;
        private bool _isWaterPressureVisible;
        private bool _isFuelPressureVisible;
        private DisplaySettingsViewModel _displaySettingViewModel;

        public CarSystemsViewModel(ISettingsProvider settingsProvider, ISimSettingsFactory simSettingsFactory, ISessionEventProvider sessionEventProvider)
        {
            _temperatureRefreshStopwatch = Stopwatch.StartNew();
            _simSettingsFactory = simSettingsFactory;
            _sessionEventProvider = sessionEventProvider;
            TurboPressure = Pressure.Zero;
            sessionEventProvider.SimulatorChanged += SessionEventProviderOnSimulatorChanged;
            IsTurboPressureVisible = true;
            IsOilPressureVisible = true;
            IsWaterPressureVisible = true;
            IsFuelPressureVisible = true;
            DisplaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
            InitializeUnits();
            DisplaySettingsViewModel.PropertyChanged += DisplaySettingsViewModelOnPropertyChanged;
        }

        public bool IsTurboPressureVisible
        {
            get => _isTurboPressureVisible;
            set => SetProperty(ref _isTurboPressureVisible, value);
        }

        public bool IsOilPressureVisible
        {
            get => _isOilPressureVisible;
            set => SetProperty(ref _isOilPressureVisible, value);
        }

        public bool IsWaterPressureVisible
        {
            get => _isWaterPressureVisible;
            set => SetProperty(ref _isWaterPressureVisible, value);
        }

        public bool IsFuelPressureVisible
        {
            get => _isFuelPressureVisible;
            set => SetProperty(ref _isFuelPressureVisible, value);
        }

        public DisplaySettingsViewModel DisplaySettingsViewModel
        {
            get => _displaySettingViewModel;
            set => SetProperty(ref _displaySettingViewModel, value);
        }

        public Pressure OilPressure
        {
            get => _oilPressure;
            set => SetProperty(ref _oilPressure, value);
        }

        public Pressure WaterPressure
        {
            get => _waterPressure;
            set => SetProperty(ref _waterPressure, value);
        }

        public Pressure FuelPressure
        {
            get => _fuelPressure;
            set => SetProperty(ref _fuelPressure, value);
        }

        public Pressure TurboPressure
        {
            get => _turboPressure;
            set => SetProperty(ref _turboPressure, value);
        }

        public OptimalQuantity<Temperature> WaterTemperature
        {
            get => _waterTemperature;
            set => SetProperty(ref _waterTemperature, value);
        }

        public OptimalQuantity<Temperature> OilTemperature
        {
            get => _oilTemperature;
            set => SetProperty(ref _oilTemperature, value);
        }

        public TemperatureUnits TemperatureUnits
        {
            get => _temperatureUnits;
            set => SetProperty(ref _temperatureUnits, value);
        }

        public PressureUnits PressureUnits
        {
            get => _pressureUnits;
            set => SetProperty(ref _pressureUnits, value);
        }

        public void ApplyDateSet(SimulatorDataSet dataSet)
        {
            if (dataSet?.PlayerInfo == null)
            {
                return;
            }

            if (DisplaySettingsViewModel.EnableTemperatureInformation && _temperatureRefreshStopwatch.Elapsed.TotalSeconds > 1)
            {
                if (WaterTemperature == null || Math.Abs(WaterTemperature.ActualQuantity.RawValue - dataSet.PlayerInfo.CarInfo.WaterSystemInfo.OptimalWaterTemperature.ActualQuantity.RawValue) > 0.5)
                {
                    WaterTemperature = dataSet.PlayerInfo.CarInfo.WaterSystemInfo.OptimalWaterTemperature;
                }

                if (OilTemperature == null || Math.Abs(OilTemperature.ActualQuantity.RawValue - dataSet.PlayerInfo.CarInfo.OilSystemInfo.OptimalOilTemperature.ActualQuantity.RawValue) > 0.5)
                {
                    OilTemperature = dataSet.PlayerInfo.CarInfo.OilSystemInfo.OptimalOilTemperature;
                }

                _temperatureRefreshStopwatch.Restart();
            }

            if (DisplaySettingsViewModel.EnableNonTemperatureInformation)
            {
                if (TurboPressure == null || Math.Abs(TurboPressure.RawValue - dataSet.PlayerInfo.CarInfo.TurboPressure.RawValue) > 0.5)
                {
                    TurboPressure = dataSet.PlayerInfo.CarInfo.TurboPressure;
                }

                if (OilPressure == null || Math.Abs(OilPressure.RawValue - dataSet.PlayerInfo.CarInfo.OilSystemInfo.OilPressure.RawValue) > 1)
                {
                    OilPressure = dataSet.PlayerInfo.CarInfo.OilSystemInfo.OilPressure;
                }

                if (WaterPressure == null || Math.Abs(WaterPressure.RawValue - dataSet.PlayerInfo.CarInfo.WaterSystemInfo.WaterPressure.RawValue) > 1)
                {
                    WaterPressure = dataSet.PlayerInfo.CarInfo.WaterSystemInfo.WaterPressure;
                }

                if (FuelPressure == null || Math.Abs(FuelPressure.RawValue - dataSet.PlayerInfo.CarInfo.FuelSystemInfo.FuelPressure.RawValue) > 1)
                {
                    FuelPressure = dataSet.PlayerInfo.CarInfo.FuelSystemInfo.FuelPressure;
                }
            }
        }

        public void Reset()
        {
        }

        private void SessionEventProviderOnSimulatorChanged(object sender, DataSetArgs e)
        {
            ISimSettings simSettings = _simSettingsFactory.Create(e.DataSet.Source);
            ApplySimSettings(simSettings);
        }

        private void ApplySimSettings(ISimSettings simSettings)
        {
            IsTurboPressureVisible = simSettings.IsTurboBoostPressureProvided;
            IsOilPressureVisible = simSettings.IsOilPressureProvided;
            IsWaterPressureVisible = simSettings.IsCoolantPressureProvided;
            IsFuelPressureVisible = simSettings.IsFuelPressureProvided;
        }

        private void InitializeUnits()
        {
            PressureUnits = DisplaySettingsViewModel.DisplayPressureUnits;
            TemperatureUnits = DisplaySettingsViewModel.TemperatureUnits;
        }

        private void DisplaySettingsViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            InitializeUnits();
        }

        public void Dispose()
        {
            _sessionEventProvider.SimulatorChanged -= SessionEventProviderOnSimulatorChanged;
        }
    }
}