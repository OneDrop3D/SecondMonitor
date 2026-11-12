namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;

    using Contracts.Commands;

    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;

    using Factory;

    using NLog;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.FuelConsumption.FuelStrategy;

    using Settings;
    using Settings.ViewModel;

    public class FuelOverviewViewModel : AbstractViewModel, IFuelOverviewViewModel
    {
        public const string ViewModelLayoutName = "Fuel Overview";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Stopwatch _refreshWatch;
        private readonly Stopwatch _informationRefreshWatch;
        private readonly SessionRemainingCalculator _sessionRemainingCalculator;
        private readonly IViewModelFactory _viewModelFactory;

        private TimeSpan _timeLeft;
        private double _lapsLeft;
        private string _avgPerLap = string.Empty;
        private string _avgPerMinute = string.Empty;
        private string _currentPerLap = string.Empty;
        private string _currentPerMinute = string.Empty;
        private string _consumptionRemark = string.Empty;
        private double _fuelPercentage;
        private FuelLevelStatus _fuelLevelState;
        private Volume _maximumFuel = Volume.FromLiters(0);
        private TimeSpan _timeDelta;
        private double _lapsDelta;
        private Volume _fuelDelta = Volume.FromLiters(0);
        private bool _isVisible;
        private bool _isFuelCalculatorButtonEnabled;

        private ICommand _showFuelCalculatorCommand;
        private ICommand _hideFuelCalculatorCommand;
        private string _fuelRemainingLabel;
        private bool _isDeltaInformationVisible;
        private IPitStopsViewModel _pitStopsViewModel;
        private bool _isWetSession;
        private int _fuelMultiplier;

        public FuelOverviewViewModel(SessionRemainingCalculator sessionRemainingCalculator, ISettingsProvider settingsProvider, IViewModelFactory viewModelFactory)
        {
            IsVisible = true;
            FuelDeltaLabels = new ObservableCollection<FuelDeltaViewModel>();
            HybridOverviewViewModel = viewModelFactory.Create<HybridOverviewViewModel>();
            FuelPlannerViewModel = viewModelFactory.Create<FuelPlannerViewModel>();
            FuelStrategiesViewModel = viewModelFactory.Create<FuelStrategiesViewModel>();
            _refreshWatch = Stopwatch.StartNew();
            _informationRefreshWatch = Stopwatch.StartNew();
            _sessionRemainingCalculator = sessionRemainingCalculator;
            _viewModelFactory = viewModelFactory;
            ResetCommand = new RelayCommand(Reset);
            DisplaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
            FuelMarkerViewModels = viewModelFactory.CreateAll<IFuelMarkerViewModel>().ToList();
            FuelVisualization = FuelVisualization.New;
            FuelMultiplier = 1;
            IncreaseFuelMultiplierCommand = new RelayCommand(() => FuelMultiplier++);
            DecreaseFuelMultiplierCommand = new RelayCommand(() => FuelMultiplier = Math.Max(FuelMultiplier - 1, 1));
        }

        public FuelPlannerViewModel FuelPlannerViewModel { get; }

        public FuelStrategiesViewModel FuelStrategiesViewModel { get; }
        public IPitStopsViewModel PitStopsViewModel
        {
            get => _pitStopsViewModel;
            set => SetProperty(ref _pitStopsViewModel, value);
        }

        public HybridOverviewViewModel HybridOverviewViewModel { get; }

        public IList<IFuelMarkerViewModel> FuelMarkerViewModels { get; }

        public FuelVisualization FuelVisualization
        {
            get;
        }

        public DisplaySettingsViewModel DisplaySettingsViewModel { get; }

        public bool IsWetSession
        {
            get => _isWetSession;
            private set => SetProperty(ref _isWetSession, value);
        }

        public bool IsFuelCalculatorButtonEnabled
        {
            get => _isFuelCalculatorButtonEnabled;
            set => SetProperty(ref _isFuelCalculatorButtonEnabled, value);
        }

        public ICommand ResetCommand { get; }

        public ICommand ShowFuelCalculatorCommand
        {
            get => _showFuelCalculatorCommand;
            set => SetProperty(ref _showFuelCalculatorCommand, value);
        }

        public ICommand HideFuelCalculatorCommand
        {
            get => _hideFuelCalculatorCommand;
            set => SetProperty(ref _hideFuelCalculatorCommand, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public TimeSpan TimeDelta
        {
            get => _timeDelta;
            private set => SetProperty(ref _timeDelta, value);
        }

        public double LapsDelta
        {
            get => _lapsDelta;
            private set => SetProperty(ref _lapsDelta, value);
        }

        public Volume FuelDelta
        {
            get => _fuelDelta;
            private set => SetProperty(ref _fuelDelta, value);
        }

        public ObservableCollection<FuelDeltaViewModel> FuelDeltaLabels
        {
            get;
        }

        public bool IsDeltaInformationVisible
        {
            get => _isDeltaInformationVisible;
            set => SetProperty(ref _isDeltaInformationVisible, value);
        }

        public TimeSpan TimeLeft
        {
            get => _timeLeft;
            private set => SetProperty(ref _timeLeft, value);
        }

        public double LapsLeft
        {
            get => _lapsLeft;
            private set => SetProperty(ref _lapsLeft, value);
        }

        public string AvgPerLap
        {
            get => _avgPerLap;
            private set => SetProperty(ref _avgPerLap, value);
        }

        public string AvgPerMinute
        {
            get => _avgPerMinute;
            private set => SetProperty(ref _avgPerMinute, value);
        }

        public string CurrentPerLap
        {
            get => _currentPerLap;
            private set => SetProperty(ref _currentPerLap, value);
        }

        public string CurrentPerMinute
        {
            get => _currentPerMinute;
            private set => SetProperty(ref _currentPerMinute, value);
        }

        public double FuelPercentage
        {
            get => _fuelPercentage;
            private set => SetProperty(ref _fuelPercentage, value);
        }

        public FuelLevelStatus FuelState
        {
            get => _fuelLevelState;
            private set => SetProperty(ref _fuelLevelState, value);
        }

        public Volume MaximumFuel
        {
            get => _maximumFuel;
            private set => SetProperty(ref _maximumFuel, value);
        }

        public string FuelRemainingLabel
        {
            get => _fuelRemainingLabel;
            set => SetProperty(ref _fuelRemainingLabel, value);
        }

        public string ConsumptionRemark
        {
            get => _consumptionRemark;
            set => SetProperty(ref _consumptionRemark, value);
        }

        public int FuelMultiplier
        {
            get => FuelPlannerViewModel.CalculatorForSelectedSession.FuelMultiplier;
            set
            {
                FuelPlannerViewModel.CalculatorForSelectedSession.FuelMultiplier = value;
                NotifyPropertyChanged();
            }
        }

        public ICommand IncreaseFuelMultiplierCommand { get; }

        public ICommand DecreaseFuelMultiplierCommand { get; }

        public void ApplyDataSet(SimulatorDataSet dataSet, ConsumptionMonitors consumptionMonitors)
        {
            try
            {
                if (!Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(() => ApplyDataSet(dataSet, consumptionMonitors));
                    return;
                }

                if (_refreshWatch.ElapsedMilliseconds < 500 || dataSet.SessionInfo.SessionType == SessionType.Na)
                {
                    return;
                }

                IsVisible = dataSet.PlayerInfo.CarInfo.HybridSystem.IsElectricOnly == false;

                if (!IsVisible)
                {
                    return;
                }

                FuelRemainingLabel = consumptionMonitors.GetAllRemainingFuelLabel(dataSet);
                ReApplyFuelLevels(dataSet.PlayerInfo.CarInfo.FuelSystemInfo);

                if (dataSet.SessionInfo.SessionPhase == SessionPhase.Countdown || dataSet.PlayerInfo == null)
                {
                    return;
                }

                _refreshWatch.Restart();
                if (!IsWetSession && dataSet.IsWetSession())
                {
                    IsWetSession = true;
                }

                UpdateActualData(dataSet, consumptionMonitors);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public void Reset()
        {
            PitStopsViewModel?.Reset();
            IsWetSession = false;
            _sessionRemainingCalculator.Reset();
            CurrentPerLap = string.Empty;
            CurrentPerMinute = string.Empty;
            AvgPerLap = string.Empty;
            AvgPerMinute = string.Empty;
            FuelState = FuelLevelStatus.Unknown;
            TimeDelta = TimeSpan.Zero;
            LapsDelta = 0;
            FuelDelta = Volume.FromLiters(0);
            IsDeltaInformationVisible = false;
            Logger.Info("Fuel Overview Reset");
        }

        private void ReApplyFuelLevels(FuelInfo fuel)
        {
            if (MaximumFuel != fuel.FuelCapacity)
            {
                MaximumFuel = fuel.FuelCapacity;
            }

            double fuelPercentage = Math.Round((fuel.FuelRemaining.InLiters / MaximumFuel.InLiters) * 100, 1);
            FuelPercentage = double.IsNaN(fuelPercentage) || double.IsInfinity(fuelPercentage) ? 0 : fuelPercentage;
        }

        private void UpdateActualData(SimulatorDataSet dataSet, ConsumptionMonitors consumptionMonitors)
        {
            if (_informationRefreshWatch.ElapsedMilliseconds < 1000)
            {
                return;
            }

            CurrentPerLap = consumptionMonitors.CreateActPerLapInfo();
            CurrentPerMinute = consumptionMonitors.CreateActPerMinuteInfo();
            AvgPerLap = consumptionMonitors.CreateTotalPerLapInfo();
            AvgPerMinute = consumptionMonitors.CreateTotalPerMinuteInfo();
            ConsumptionRemark = consumptionMonitors.GenerateRemark();

            UpdateLapAndTimeLeft(dataSet, consumptionMonitors);

            FuelMarkerViewModels.ForEach(x => x.Update(this));

            _informationRefreshWatch.Restart();
        }

        private void UpdateLapAndTimeLeft(SimulatorDataSet dataSet, ConsumptionMonitors consumptionMonitors)
        {
            double? lapsLeft = consumptionMonitors.GetLowestLapsLeft(dataSet);
            TimeSpan? timeLeft = consumptionMonitors.GetLowestTimeLeft(dataSet);
            if (!lapsLeft.HasValue || !timeLeft.HasValue)
            {
                IsDeltaInformationVisible = false;
                return;
            }

            LapsLeft = lapsLeft.Value;
            TimeLeft = timeLeft.Value;
            UpdateFuelState(dataSet, consumptionMonitors);
        }

        private void UpdateFuelState(SimulatorDataSet dataSet, ConsumptionMonitors consumptionMonitors)
        {
            switch (dataSet.SessionInfo.SessionType)
            {
                case SessionType.Qualification:
                    FuelState = FuelLevelStatus.Unknown;
                    break;
                case SessionType.Race:
                    UpdateFuelStateByLapsSessionLength(dataSet, consumptionMonitors);
                    break;
                default:
                    UpdateFuelStateByLapsLeft();
                    break;
            }
        }

        private void UpdateFuelStateByLapsSessionLength(SimulatorDataSet dataSet, ConsumptionMonitors consumptionMonitors)
        {
            if (dataSet.LeaderInfo == null)
            {
                IsDeltaInformationVisible = false;
                return;
            }

            double lapsToGo = _sessionRemainingCalculator.GetLapsRemaining(dataSet);
            if (double.IsNaN(lapsToGo) || double.IsInfinity(lapsToGo))
            {
                IsDeltaInformationVisible = false;
                return;
            }

            IsDeltaInformationVisible = true;
            FuelConsumptionMonitor fuelConsumptionMonitor = consumptionMonitors.FuelConsumptionMonitor;
            LapsDelta = LapsLeft - lapsToGo;
            FuelDelta = Volume.FromLiters(fuelConsumptionMonitor.TotalPerLap.InLiters * LapsDelta);
            TimeDelta = TimeSpan.FromMinutes(FuelDelta.InLiters / fuelConsumptionMonitor.TotalPerMinute.InLiters);
            List<FuelDeltaModel> labelModels = consumptionMonitors.GetFuelDeltaLabel(dataSet, lapsToGo).ToList();
            FuelDeltaLabels.MatchLength(labelModels.Count, () => _viewModelFactory.Create<FuelDeltaViewModel>());
            for (int i = 0; i < labelModels.Count; i++)
            {
                FuelDeltaLabels[i].FromModel(labelModels[i]);
            }

            switch (LapsDelta)
            {
                case > 1.5:
                    FuelState = FuelLevelStatus.IsEnoughForSession;
                    return;
                case > 0:
                    FuelState = FuelLevelStatus.PossiblyEnoughForSession;
                    return;
            }

            FuelState = LapsLeft < 2 ? FuelLevelStatus.Critical : FuelLevelStatus.NotEnoughForSession;
        }

        private void UpdateFuelStateByLapsLeft()
        {
            switch (LapsLeft)
            {
                case < 2:
                    FuelState = FuelLevelStatus.Critical;
                    return;
                case < 4:
                    FuelState = FuelLevelStatus.NotEnoughForSession;
                    return;
                case < 8:
                    FuelState = FuelLevelStatus.PossiblyEnoughForSession;
                    return;
                default:
                    FuelState = FuelLevelStatus.IsEnoughForSession;
                    break;
            }
        }
    }
}
