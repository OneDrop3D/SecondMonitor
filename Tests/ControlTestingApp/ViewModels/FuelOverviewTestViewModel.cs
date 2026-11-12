namespace ControlTestingApp.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Input;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot.Systems;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.CarStatus.FuelStatus;
    using SecondMonitor.ViewModels.FuelConsumption.FuelStrategy;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    public class FuelOverviewTestViewModel : AbstractViewModel, IFuelOverviewViewModel
    {
        private DisplaySettingsViewModel _displaySettingsViewModel;
        private FuelConsumptionMonitor _fuelConsumptionMonitor;
        private bool _isWetSession;
        private bool _isFuelCalculatorButtonEnabled;
        private ICommand _resetCommand;
        private ICommand _showFuelCalculatorCommand;
        private ICommand _hideFuelCalculatorCommand;
        private bool _isVisible;
        private TimeSpan _timeDelta = TimeSpan.FromMinutes(2.5);
        private double _lapsDelta = 2.5;
        private Volume _fuelDelta = Volume.FromLiters(10);
        private bool _isRaceDeltaInfoShown;
        private TimeSpan _timeLeft = TimeSpan.FromMinutes(11.252);
        private double _lapsLeft = 2.2;
        private string _avgPerLap = "1.7";
        private string _avgPerMinute = "2.7";
        private string _currentPerLap = "1.7";
        private string _currentPerMinute = "2.7";
        private double _fuelPercentage;
        private FuelLevelStatus _fuelState;
        private Volume _maximumFuel;
        private RefuelingWindowState _refuelingWindowState;
        private FuelPlannerViewModel _fuelPlannerViewModel;
        private string _fuelRemainingLabel;
        private IPitStopsViewModel _pitStopsViewModel;

        public FuelPlannerViewModel FuelPlannerViewModel
        {
            get => _fuelPlannerViewModel;
            set => SetProperty(ref _fuelPlannerViewModel, value);
        }

        public List<IFuelMarkerViewModel> FuelMarkerViewModels { get; set; }

        public HybridOverviewViewModel HybridOverviewViewModel { get; set; }

        public FuelStrategiesViewModel FuelStrategiesViewModel { get; set; }
        public IPitStopsViewModel PitStopsViewModel
        {
            get => _pitStopsViewModel;
            set => SetProperty(ref _pitStopsViewModel, value);
        }

        public string FuelRemainingLabel
        {
            get => _fuelRemainingLabel;
            set => SetProperty(ref _fuelRemainingLabel, value);
        }

        public int FuelMultiplier { get; set; } = 1;

        public DisplaySettingsViewModel DisplaySettingsViewModel
        {
            get => _displaySettingsViewModel;
            set => SetProperty(ref _displaySettingsViewModel, value);
        }

        public FuelConsumptionMonitor FuelConsumptionMonitor
        {
            get => _fuelConsumptionMonitor;
            set => SetProperty(ref _fuelConsumptionMonitor, value);
        }

        public bool IsWetSession
        {
            get => _isWetSession;
            set => SetProperty(ref _isWetSession, value);
        }

        public bool IsFuelCalculatorButtonEnabled
        {
            get => _isFuelCalculatorButtonEnabled;
            set => SetProperty(ref _isFuelCalculatorButtonEnabled, value);
        }

        public ICommand ResetCommand
        {
            get => _resetCommand;
            set => SetProperty(ref _resetCommand, value);
        }

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
            set => SetProperty(ref _timeDelta, value);
        }

        public double LapsDelta
        {
            get => _lapsDelta;
            set => SetProperty(ref _lapsDelta, value);
        }

        public Volume FuelDelta
        {
            get => _fuelDelta;
            set => SetProperty(ref _fuelDelta, value);
        }

        public bool IsRaceDeltaInfoShown
        {
            get => _isRaceDeltaInfoShown;
            set => SetProperty(ref _isRaceDeltaInfoShown, value);
        }

        public TimeSpan TimeLeft
        {
            get => _timeLeft;
            set => SetProperty(ref _timeLeft, value);
        }

        public double LapsLeft
        {
            get => _lapsLeft;
            set => SetProperty(ref _lapsLeft, value);
        }

        public string AvgPerLap
        {
            get => _avgPerLap;
            set => SetProperty(ref _avgPerLap, value);
        }

        public string AvgPerMinute
        {
            get => _avgPerMinute;
            set => SetProperty(ref _avgPerMinute, value);
        }

        public string CurrentPerLap
        {
            get => _currentPerLap;
            set => SetProperty(ref _currentPerLap, value);
        }

        public string CurrentPerMinute
        {
            get => _currentPerMinute;
            set => SetProperty(ref _currentPerMinute, value);
        }

        public double FuelPercentage
        {
            get => _fuelPercentage;
            set
            {
                SetProperty(ref _fuelPercentage, value);
                FuelMarkerViewModels.ForEach(x => x.Update(this));
            }
        }

        public FuelLevelStatus FuelState
        {
            get => _fuelState;
            set => SetProperty(ref _fuelState, value);
        }

        public Volume MaximumFuel
        {
            get => _maximumFuel;
            set => SetProperty(ref _maximumFuel, value);
        }

        public RefuelingWindowState RefuelingWindowState
        {
            get => _refuelingWindowState;
            set => SetProperty(ref _refuelingWindowState, value);
        }

        public double FuelStatusDouble
        {
            get => (double)_fuelState;
            set
            {
                int fuelState = (int)value;
                FuelState = (FuelLevelStatus)fuelState;
                NotifyPropertyChanged(nameof(FuelLevelStatus));
            }
        }

        public double HybridStateDouble
        {
            get => (double)HybridOverviewViewModel.HybridSystemMode;
            set
            {
                int hybridSystemStatus = (int)value;
                HybridOverviewViewModel.HybridSystemMode = (HybridSystemMode)hybridSystemStatus;
            }
        }

        public double HybridDischargeDouble
        {
            get => (double)HybridOverviewViewModel.HybridChargeChange;
            set
            {
                int hybridSystemStatus = (int)value;
                HybridOverviewViewModel.HybridChargeChange = (HybridChargeChange)hybridSystemStatus;
            }
        }
    }
}
