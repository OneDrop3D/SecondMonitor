namespace SecondMonitor.ViewModels.CarStatus
{
    using System;
    using System.ComponentModel;

    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;

    using FuelStatus;

    using SecondMonitor.DataModel.Extensions;

    using Settings;

    public class WheelStatusViewModel : AbstractViewModel
    {
        private static readonly Temperature temperatureTolerance = Temperature.FromCelsius(0.5);
        private static readonly Pressure pressureTolerance = Pressure.FromKiloPascals(0.1);
        private readonly ISettingsProvider _settingsProvider;
        private readonly TyreLifeTimeMonitor _tyreLifeTimeMonitor;

        private double _wearAtStintEnd;
        private string _idealPressure;
        private double _wearAtRaceEnd;
        private bool _isLeftWheel;

        private double _wheelCamber;
        private bool _isCamberVisible;
        private double _brakeCondition;
        private int _lapsUntilHeavyWear;
        private string _tyreCompound;
        private IndicationStatus _slipStatus;
        private TimeSpan _slipEndSessionTime;
        private TimeSpan _bottomOutEndSessionTime;
        private bool _isTyreDetached;
        private TemperatureUnits _temperatureUnits;
        private PressureUnits _pressureUnits;

        private double _tyreCondition;
        private double _tyreNoWearWearLimit;
        private double _tyreMildWearLimit;
        private double _tyreHeavyWearLimit;

        private OptimalQuantity<Temperature> _tyreCoreTemperature;
        private OptimalQuantity<Temperature> _tyreLeftTemperature;
        private OptimalQuantity<Temperature> _tyreCenterTemperature;
        private OptimalQuantity<Temperature> _brakeTemperature;
        private OptimalQuantity<Pressure> _tyrePressure;
        private OptimalQuantity<Temperature> _tyreRightTemperature;
        private IndicationStatus _bottomOutStatus;
        private Distance _bottomOutHeight;
        private bool _isBottomOutIndicatorEnabled;
        private string _lapsTimeUntilHeavyWear;

        public WheelStatusViewModel(bool isLeft, ISettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider;
            IsLeftWheel = isLeft;
        }

        public WheelStatusViewModel(bool isLeft, SessionRemainingCalculator sessionRemainingCalculator, ISettingsProvider settingsProvider, IPaceProvider paceProvider,
            IFuelPredictionProvider fuelPredictionProvider) : this(isLeft, settingsProvider)
        {
            _tyreLifeTimeMonitor = new TyreLifeTimeMonitor(paceProvider, sessionRemainingCalculator, fuelPredictionProvider);
            BottomOutStatus = IndicationStatus.NoIndication;
            ApplyDisplaySettings();
            settingsProvider.DisplaySettingsViewModel.PropertyChanged += DisplaySettingsViewModelOnPropertyChanged;
        }

        public double WheelCamber
        {
            get => _wheelCamber;
            set => SetProperty(ref _wheelCamber, value);
        }

        public bool IsCamberVisible
        {
            get => _isCamberVisible;
            set => SetProperty(ref _isCamberVisible, value);
        }

        public double BrakeCondition
        {
            get => _brakeCondition;
            set => SetProperty(ref _brakeCondition, value);
        }

        public int LapsUntilHeavyWear
        {
            get => _lapsUntilHeavyWear;
            set => SetProperty(ref _lapsUntilHeavyWear, value);
        }
        
        public string LapsTimeUntilHeavyWear
        {
            get => _lapsTimeUntilHeavyWear;
            set => SetProperty(ref _lapsTimeUntilHeavyWear, value);
        }

        public string TyreCompound
        {
            get => _tyreCompound;
            set => SetProperty(ref _tyreCompound, value);
        }

        public IndicationStatus SlipStatus
        {
            get => _slipStatus;
            set => SetProperty(ref _slipStatus, value);
        }

        public IndicationStatus BottomOutStatus
        {
            get => _bottomOutStatus;
            set => SetProperty(ref _bottomOutStatus, value);
        }

        public bool IsTyreDetached
        {
            get => _isTyreDetached;
            set => SetProperty(ref _isTyreDetached, value);
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

        public double TyreCondition
        {
            get => _tyreCondition;
            set => SetProperty(ref _tyreCondition, value);
        }

        public double TyreNoWearWearLimit
        {
            get => _tyreNoWearWearLimit;
            set => SetProperty(ref _tyreNoWearWearLimit, value);
        }

        public double TyreMildWearLimit
        {
            get => _tyreMildWearLimit;
            set => SetProperty(ref _tyreMildWearLimit, value);
        }

        public double TyreHeavyWearLimit
        {
            get => _tyreHeavyWearLimit;
            set => SetProperty(ref _tyreHeavyWearLimit, value);
        }

        public OptimalQuantity<Temperature> TyreCoreTemperature
        {
            get => _tyreCoreTemperature;
            set => SetProperty(ref _tyreCoreTemperature, value);
        }

        public OptimalQuantity<Temperature> TyreLeftTemperature
        {
            get => _tyreLeftTemperature;
            set => SetProperty(ref _tyreLeftTemperature, value);
        }

        public OptimalQuantity<Temperature> TyreCenterTemperature
        {
            get => _tyreCenterTemperature;
            set => SetProperty(ref _tyreCenterTemperature, value);
        }

        public OptimalQuantity<Temperature> TyreRightTemperature
        {
            get => _tyreRightTemperature;
            set => SetProperty(ref _tyreRightTemperature, value);
        }

        public OptimalQuantity<Temperature> BrakeTemperature
        {
            get => _brakeTemperature;
            set => SetProperty(ref _brakeTemperature, value);
        }

        public OptimalQuantity<Pressure> TyrePressure
        {
            get => _tyrePressure;
            set => SetProperty(ref _tyrePressure, value);
        }

        public bool IsLeftWheel
        {
            get => _isLeftWheel;
            set => SetProperty(ref _isLeftWheel, value);
        }

        public double WearAtRaceEnd
        {
            get => _wearAtRaceEnd;
            set => SetProperty(ref _wearAtRaceEnd, value);
        }

        public double WearAtStintEnd
        {
            get => _wearAtStintEnd;
            set => SetProperty(ref _wearAtStintEnd, value);
        }

        public string IdealPressure
        {
            get => _idealPressure;
            set => SetProperty(ref _idealPressure, value);
        }

        public void ApplyWheelCondition(WheelInfo wheelInfo)
        {
            TyreCondition = wheelInfo.Detached ? 0.1 : Math.Round(100 * (1 - wheelInfo.TyreWear.ActualWear), 1);
            TyreNoWearWearLimit = 100 * (1 - wheelInfo.TyreWear.NoWearWearLimit);
            TyreMildWearLimit = 100 * (1 - wheelInfo.TyreWear.LightWearLimit);
            TyreHeavyWearLimit = 100 * (1 - wheelInfo.TyreWear.HeavyWearLimit);

            if (wheelInfo.TyreCoreTemperature.ActualQuantity.InCelsius > -200 && (TyreCoreTemperature?.Equals(wheelInfo.TyreCoreTemperature, temperatureTolerance) != true))
            {
                TyreCoreTemperature = wheelInfo.TyreCoreTemperature;
            }

            if (TyreLeftTemperature?.Equals(wheelInfo.LeftTyreTemp, temperatureTolerance) != true)
            {
                TyreLeftTemperature = wheelInfo.LeftTyreTemp;
            }

            if (wheelInfo.CenterTyreTemp.ActualQuantity.InCelsius > -200 && (TyreCenterTemperature?.Equals(wheelInfo.CenterTyreTemp, temperatureTolerance) != true))
            {
                TyreCenterTemperature = wheelInfo.CenterTyreTemp;
            }

            if (TyreRightTemperature?.Equals(wheelInfo.RightTyreTemp, temperatureTolerance) != true)
            {
                TyreRightTemperature = wheelInfo.RightTyreTemp;
            }

            if (TyrePressure?.Equals(wheelInfo.TyrePressure, pressureTolerance) != true)
            {
                TyrePressure = wheelInfo.TyrePressure;
            }

            if (BrakeTemperature?.Equals(wheelInfo.BrakeTemperature, temperatureTolerance) != true)
            {
                BrakeTemperature = wheelInfo.BrakeTemperature;
            }

            UpdateCompound(wheelInfo);
            IdealPressure = "Ideal: " + wheelInfo.TyrePressure.IdealQuantity.GetValueInUnits(PressureUnits).ToStringScalableDecimals();
            IsTyreDetached = wheelInfo.Detached;
            BrakeCondition = Math.Round(100 * (1 - wheelInfo.BrakesDamage.Damage), 0);
            IsCamberVisible = _settingsProvider.DisplaySettingsViewModel.EnableCamberVisualization;

            if (IsCamberVisible)
            {
                WheelCamber = Math.Round(wheelInfo.Camber.GetValueInUnits(AngleUnits.Degrees), 1);
            }
            else
            {
                WheelCamber = 0;
            }
        }

        private void UpdateCompound(WheelInfo wheelInfo)
        {
            TyreCompound = wheelInfo.TyreCompoundWithSet;
        }

        public void UpdateSlippingInformation(SimulatorDataSet dataSet, WheelInfo wheelInfo, double radiusInMeters)
        {
            if (dataSet?.PlayerInfo == null || wheelInfo == null)
            {
                SlipStatus = IndicationStatus.NoIndication;
                return;
            }

            if (!dataSet.SimulatorSourceInfo.TelemetryInfo.ContainsWheelRps || dataSet.PlayerInfo.Speed.InKph < 30)
            {
                SlipStatus = IndicationStatus.NoIndication;
                return;
            }

            double wheelVelocity = wheelInfo.Rps * radiusInMeters;
            bool isSLipping = wheelVelocity < dataSet.PlayerInfo.Speed.InMs * 0.7;
            if (isSLipping)
            {
                SlipStatus = IndicationStatus.CurrentlyIndicating;
                return;
            }

            switch (SlipStatus)
            {
                case IndicationStatus.CurrentlyIndicating:
                    SlipStatus = IndicationStatus.WasIndicating;
                    _slipEndSessionTime = dataSet.SessionInfo.SessionTime;
                    break;
                case IndicationStatus.WasIndicating when (dataSet.SessionInfo.SessionTime - _slipEndSessionTime).TotalSeconds > 10:
                    SlipStatus = IndicationStatus.NoIndication;
                    break;
            }
        }

        public void UpdateBottomOutInformation(SimulatorDataSet dataSet, WheelInfo wheelInfo)
        {
            if (dataSet?.PlayerInfo == null || wheelInfo == null || !dataSet.PlayerInfo.CarInfo.WheelsInfo.IsRideHeightFilled || dataSet.PlayerInfo.Speed.InKph < 10)
            {
                SlipStatus = IndicationStatus.NoIndication;
                return;
            }

            if (_isBottomOutIndicatorEnabled && wheelInfo.RideHeight.InCentimeters <= _bottomOutHeight.InCentimeters)
            {
                BottomOutStatus = IndicationStatus.CurrentlyIndicating;
                return;
            }

            switch (BottomOutStatus)
            {
                case IndicationStatus.CurrentlyIndicating:
                    BottomOutStatus = IndicationStatus.WasIndicating;
                    _bottomOutEndSessionTime = dataSet.SessionInfo.SessionTime;
                    break;
                case IndicationStatus.WasIndicating when (dataSet.SessionInfo.SessionTime - _bottomOutEndSessionTime).TotalSeconds > 10:
                    BottomOutStatus = IndicationStatus.NoIndication;
                    break;
            }
        }

        public void ApplyWheelCondition(SimulatorDataSet dataSet, WheelInfo wheelInfo)
        {
            if (dataSet.PlayerInfo == null)
            {
                return;
            }

            if (dataSet.SessionInfo.SessionType != SessionType.Race && dataSet.PlayerInfo.InPits)
            {
                UpdateCompound(wheelInfo);
                return;
            }

            ApplyWheelCondition(wheelInfo);

            if (_tyreLifeTimeMonitor == null)
            {
                return;
            }

            _tyreLifeTimeMonitor.ApplyWheelInfo(dataSet, wheelInfo);
            WearAtRaceEnd = Math.Round(_tyreLifeTimeMonitor.WearAtRaceEnd);
            WearAtStintEnd = Math.Round(_tyreLifeTimeMonitor.WearAtStintEnd);
            LapsUntilHeavyWear = _tyreLifeTimeMonitor.LapsUntilHeavyWear;
            string lapUntilHeavyWearInfo = _tyreLifeTimeMonitor.LapsUntilHeavyWear > 99 ? "99+" : _tyreLifeTimeMonitor.LapsUntilHeavyWear.ToString();
            string minutesUntilHeavyWearInfo = _tyreLifeTimeMonitor.MinutesUntilHeavyWear > 60 ? "1+h" : $"{_tyreLifeTimeMonitor.MinutesUntilHeavyWear}min"; 
            LapsTimeUntilHeavyWear = $"{lapUntilHeavyWearInfo}L\n{minutesUntilHeavyWearInfo}";
        }

        public void Reset()
        {
            _tyreLifeTimeMonitor?.Reset();
            SlipStatus = IndicationStatus.NoIndication;
            BottomOutStatus = IndicationStatus.NoIndication;
            _slipEndSessionTime = TimeSpan.Zero;
            _bottomOutEndSessionTime = TimeSpan.Zero;
        }

        private void DisplaySettingsViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ApplyDisplaySettings();
        }

        private void ApplyDisplaySettings()
        {
            PressureUnits = _settingsProvider.DisplaySettingsViewModel.DisplayPressureUnits;
            TemperatureUnits = _settingsProvider.DisplaySettingsViewModel.TemperatureUnits;
            _bottomOutHeight = _settingsProvider.DisplaySettingsViewModel.BottomingOutHeight;
            _isBottomOutIndicatorEnabled = _settingsProvider.DisplaySettingsViewModel.IsBottomingOutIndicationEnabled;
        }
    }
}
