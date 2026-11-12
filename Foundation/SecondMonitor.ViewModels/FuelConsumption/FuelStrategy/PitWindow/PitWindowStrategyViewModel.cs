namespace SecondMonitor.ViewModels.FuelConsumption.FuelStrategy.PitWindow
{
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.Contracts;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Summary.FuelStrategy;
    using SecondMonitor.DataModel.Summary.FuelStrategy.PitWindowStrategy;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    public class PitWindowStrategyViewModel : AbstractViewModel<PitWindowStrategySettings>, IFuelStrategyViewModel
    {
        private bool _isStrategyValid = true;
        private string _currentIssue = string.Empty;
        private List<HumanReadableItem<FuelLoadKind>> _allStartFuelOptions;
        private bool _isRefuelingAllowed = true;
        private HumanReadableItem<FuelLoadKind> _startFuel;
        private double _startFuelCustom;
        private int _pitWindowPercentage;
        private HumanReadableItem<FuelLoadKind> _pitWindowFuel;
        private double _pitWindowFuelCustom;
        private int _startFuelMinutes;
        private int _pitFuelMinutes;
        private Volume _automaticStartFuelVolume;
        private Volume _automaticPitFuelVolume;

        public PitWindowStrategyViewModel(ISettingsProvider settingsProvider)
        {
            AutomaticStartFuelVolume = Volume.FromLiters(0);
            AutomaticPitFuelVolume = Volume.FromLiters(0);
            AllStartFuelOptions = new StartingFuelMap().GetAllItems().ToList();
            DisplaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
        }

        public DisplaySettingsViewModel DisplaySettingsViewModel { get; }

        public List<HumanReadableItem<FuelLoadKind>> AllStartFuelOptions
        {
            get => _allStartFuelOptions;
            set => SetProperty(ref _allStartFuelOptions, value);
        }

        public bool IsStrategyValid
        {
            get => _isStrategyValid;
            set => SetProperty(ref _isStrategyValid, value);
        }

        public string StrategyRemark
        {
            get => _currentIssue;
            set => SetProperty(ref _currentIssue, value);
        }

        public bool IsRefuelingAllowed
        {
            get => _isRefuelingAllowed;
            set => SetProperty(ref _isRefuelingAllowed, value);
        }

        public HumanReadableItem<FuelLoadKind> StartFuel
        {
            get => _startFuel;
            set
            {
                SetProperty(ref _startFuel, value);
                NotifyPropertyChanged(nameof(IsStartFuelFuelCustomVisible));
            }
        }

        public bool IsStartFuelFuelCustomVisible => StartFuel.Value == FuelLoadKind.Custom;

        public double StartFuelCustom
        {
            get => _startFuelCustom;
            set => SetProperty(ref _startFuelCustom, value);
        }

        public bool IsCustomFuelVisible => StartFuel.Value == FuelLoadKind.Custom;

        public int PitWindowPercentage
        {
            get => _pitWindowPercentage;
            set => SetProperty(ref _pitWindowPercentage, value);
        }

        public HumanReadableItem<FuelLoadKind> PitWindowFuel
        {
            get => _pitWindowFuel;
            set
            {
                SetProperty(ref _pitWindowFuel, value);
                NotifyPropertyChanged(nameof(IsPitWindowFuelCustomVisible));
            }
        }

        public bool IsPitWindowFuelCustomVisible => PitWindowFuel.Value == FuelLoadKind.Custom;

        public double PitWindowFuelCustom
        {
            get => _pitWindowFuelCustom;
            set => SetProperty(ref _pitWindowFuelCustom, value);
        }

        public int StartFuelMinutes
        {
            get => _startFuelMinutes;
            set => SetProperty(ref _startFuelMinutes, value);
        }

        public int PitFuelMinutes
        {
            get => _pitFuelMinutes;
            set => SetProperty(ref _pitFuelMinutes, value);
        }

        public Volume AutomaticStartFuelVolume
        {
            get => _automaticStartFuelVolume;
            set => SetProperty(ref _automaticStartFuelVolume, value);
        }

        public Volume AutomaticPitFuelVolume
        {
            get => _automaticPitFuelVolume;
            set => SetProperty(ref _automaticPitFuelVolume, value);
        }

        protected override void ApplyModel(PitWindowStrategySettings model)
        {
            _isRefuelingAllowed = model.IsRefuelingAllowed;
            _startFuel = AllStartFuelOptions.Single(x => x.Value == model.StartFuel);
            _startFuelCustom = model.StartFuelCustom.GetValueInUnits(DisplaySettingsViewModel.VolumeUnits);
            _pitWindowFuel = AllStartFuelOptions.Single(x => x.Value == model.PitWindowFuel);
            _pitWindowFuelCustom = model.PitWindowFuelCustom.GetValueInUnits(DisplaySettingsViewModel.VolumeUnits);
            _pitWindowPercentage = model.PitWindowPercentage;
            NotifyPropertyChanged(string.Empty);
        }

        public override PitWindowStrategySettings SaveToNewModel()
        {
            return new PitWindowStrategySettings()
            {
                IsRefuelingAllowed = IsRefuelingAllowed,
                StartFuel = StartFuel.Value,
                StartFuelCustom = Volume.FromUnits(StartFuelCustom, DisplaySettingsViewModel.VolumeUnits),
                PitWindowFuel = PitWindowFuel.Value,
                PitWindowPercentage = PitWindowPercentage,
                PitWindowFuelCustom = Volume.FromUnits(PitWindowFuelCustom, DisplaySettingsViewModel.VolumeUnits)
            };
        }
    }
}
