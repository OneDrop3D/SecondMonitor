namespace SecondMonitor.ViewModels.FuelConsumption.FuelStrategy.TankToTank
{
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.Contracts;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Summary.FuelStrategy;
    using SecondMonitor.DataModel.Summary.FuelStrategy.TankToTankStrategy;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    public class TankToTankViewModel : AbstractViewModel<TankToTankStrategySettings>, IFuelStrategyViewModel
    {
        private bool _isStrategyValid = false;
        private string _strategyRemark = string.Empty;
        private HumanReadableItem<FuelLoadKind> _startFuel;
        private double _customStartingFuel;
        private HumanReadableItem<StintLengthKind> _stintLength;
        private Volume _automaticFuelVolume;
        private int _startFuelLaps;
        private int _pitStopLapCount;
        private Volume _pitStopVolume;
        private string _stopDescription;
        private int _pitStopCount;
        private bool _isPitStopRequired;

        public TankToTankViewModel(ISettingsProvider settingsProvider)
        {
            AutomaticFuelVolume = Volume.FromLiters(0);
            DisplaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
            AllStartFuelOptions = new StartingFuelMap().GetAllItems().ToList();
            AllStintLengthOptions = new StintLengthMap().GetAllItems().ToList();
        }

        public DisplaySettingsViewModel DisplaySettingsViewModel { get; }

        public bool IsStrategyValid
        {
            get => _isStrategyValid;
            set => SetProperty(ref _isStrategyValid, value);
        }

        public string StrategyRemark
        {
            get => _strategyRemark;
            set => SetProperty(ref _strategyRemark, value);
        }

        public IReadOnlyCollection<HumanReadableItem<FuelLoadKind>> AllStartFuelOptions { get; }

        public HumanReadableItem<FuelLoadKind> StartFuel
        {
            get => _startFuel;
            set
            {
                SetProperty(ref _startFuel, value);
                NotifyPropertyChanged(nameof(IsCustomFuelVisible));
            }
        }

        public bool IsCustomFuelVisible => StartFuel.Value == FuelLoadKind.Custom;

        public double CustomStartingFuel
        {
            get => _customStartingFuel;
            set => SetProperty(ref _customStartingFuel, value);
        }

        public IReadOnlyCollection<HumanReadableItem<StintLengthKind>> AllStintLengthOptions { get; }

        public HumanReadableItem<StintLengthKind> StintLength
        {
            get => _stintLength;
            set => SetProperty(ref _stintLength, value);
        }

        public Volume AutomaticFuelVolume
        {
            get => _automaticFuelVolume;
            set => SetProperty(ref _automaticFuelVolume, value);
        }

        public int StartFuelLaps
        {
            get => _startFuelLaps;
            set => SetProperty(ref _startFuelLaps, value);
        }

        public int PitStopLapCount
        {
            get => _pitStopLapCount;
            set => SetProperty(ref _pitStopLapCount, value);
        }

        public Volume PitStopVolume
        {
            get => _pitStopVolume;
            set => SetProperty(ref _pitStopVolume, value);
        }

        public int PitStopCount
        {
            get => _pitStopCount;
            set => SetProperty(ref _pitStopCount, value);
        }

        public string StopDescription
        {
            get => _stopDescription;
            set => SetProperty(ref _stopDescription, value);
        }

        public Volume CustomStartingFuelAsVolume => Volume.FromUnits(CustomStartingFuel, DisplaySettingsViewModel.VolumeUnits);
        public bool IsPitStopRequired
        {
            get => _isPitStopRequired;
            set => SetProperty(ref _isPitStopRequired, value);
        }

        protected override void ApplyModel(TankToTankStrategySettings model)
        {
            _startFuel = AllStartFuelOptions.Single(x => x.Value == model.StartFuel);
            _customStartingFuel = model.CustomStartingFuel.GetValueInUnits(DisplaySettingsViewModel.VolumeUnits);
            _stintLength = AllStintLengthOptions.Single(x => x.Value == model.StintLength);
            NotifyPropertyChanged(string.Empty);
        }

        public override TankToTankStrategySettings SaveToNewModel()
        {
            return new TankToTankStrategySettings()
            {
                StartFuel = StartFuel.Value,
                CustomStartingFuel = CustomStartingFuelAsVolume,
                StintLength = StintLength.Value,
            };
        }
    }
}
