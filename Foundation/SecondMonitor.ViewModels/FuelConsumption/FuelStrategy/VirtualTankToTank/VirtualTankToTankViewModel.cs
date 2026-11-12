namespace SecondMonitor.ViewModels.FuelConsumption.FuelStrategy.VirtualTankToTank
{
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.Contracts;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Summary.FuelStrategy;
    using SecondMonitor.DataModel.Summary.FuelStrategy.VirtualEnergyStrategy;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    public class VirtualTankToTankViewModel : AbstractViewModel<VirtualTankToTankStrategySettings>, IFuelStrategyViewModel
    {
        private bool _isStrategyValid = false;
        private string _strategyRemark = string.Empty;
        private HumanReadableItem<FuelLoadKind> _startFuel;
        private double _customVirtualEnergy;
        private HumanReadableItem<StintLengthKind> _stintLength;
        private Volume _automaticFuelVolume;
        private int _startFuelLaps;
        private int _pitStopLapCount;
        private string _pitStopVolumeLabel;
        private string _stopDescription;
        private int _pitStopCount;
        private bool _isPitStopRequired;
        private int _automaticVirtualEnergyPercentage;
        private Volume _customStartingFuel;
        private HumanReadableItem<VirtualEnergyPitStopInfo> _selectedPitStopInfo;

        public VirtualTankToTankViewModel(ISettingsProvider settingsProvider)
        {
            AutomaticFuelVolume = Volume.FromLiters(0);
            DisplaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
            AllStartFuelOptions = new StartingFuelMap().GetAllItems().ToList();
            AllStintLengthOptions = new StintLengthMap().GetAllItems().ToList();
            AllPitStopInfos = new VirtualEnergyPitStopInfoMap().GetAllItems().ToList();
            CustomStartingFuel = Volume.FromLiters(0);
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

        public List<HumanReadableItem<VirtualEnergyPitStopInfo>> AllPitStopInfos { get;  }

        public HumanReadableItem<VirtualEnergyPitStopInfo> SelectedPitStopInfo
        {
            get => _selectedPitStopInfo;
            set => SetProperty(ref _selectedPitStopInfo, value);
        }

        public bool IsCustomFuelVisible => StartFuel.Value == FuelLoadKind.Custom;

        public double CustomVirtualEnergy
        {
            get => _customVirtualEnergy;
            set => SetProperty(ref _customVirtualEnergy, value);
        }

        public Volume CustomStartingFuel
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

        public int AutomaticVirtualEnergyPercentage
        {
            get => _automaticVirtualEnergyPercentage;
            set => SetProperty(ref _automaticVirtualEnergyPercentage, value);
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

        public string PitStopVolumeLabel
        {
            get => _pitStopVolumeLabel;
            set => SetProperty(ref _pitStopVolumeLabel, value);
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

        public Volume CustomStartingFuelAsVolume => Volume.FromUnits(CustomVirtualEnergy, DisplaySettingsViewModel.VolumeUnits);
        public bool IsPitStopRequired
        {
            get => _isPitStopRequired;
            set => SetProperty(ref _isPitStopRequired, value);
        }

        protected override void ApplyModel(VirtualTankToTankStrategySettings model)
        {
            _startFuel = AllStartFuelOptions.Single(x => x.Value == model.StartFuel);
            _customVirtualEnergy = model.CustomStartingFuel.GetValueInUnits(DisplaySettingsViewModel.VolumeUnits);
            _stintLength = AllStintLengthOptions.Single(x => x.Value == model.StintLength);
            _selectedPitStopInfo = AllPitStopInfos.Single(x => x.Value == model.PitStopInfo);
            NotifyPropertyChanged(string.Empty);
        }

        public override VirtualTankToTankStrategySettings SaveToNewModel()
        {
            return new VirtualTankToTankStrategySettings()
            {
                StartFuel = StartFuel.Value,
                CustomStartingFuel = CustomStartingFuelAsVolume,
                StintLength = StintLength.Value,
                PitStopInfo = SelectedPitStopInfo.Value,
            };
        }
    }
}
