namespace SecondMonitor.ViewModels.FuelConsumption.FuelStrategy.Stint
{
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.Contracts;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Summary.FuelStrategy;
    using SecondMonitor.DataModel.Summary.FuelStrategy.TimedStintStrategy;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    public class TimedStintStrategyViewModel : AbstractViewModel<TimedStintStrategySettings>, IFuelStrategyViewModel
    {
        private HumanReadableItem<StintLengthKind> _selectedStingLength;
        private bool _isStrategyValid;
        private string _strategyRemark;
        private int _stintDurationMinutes;
        private int _regularStintMinutes;
        private int _regularStintLaps;
        private Volume _regularStintRequiresFuel;
        private bool _showLastStintInfo;
        private int _lastStintMinutes;
        private int _lastStintLaps;
        private Volume _lastStintFuel;
        private int _regularStintCount;
        private HumanReadableItem<StintPitStopInfo> _selectedStingPitStopInfo;

        public TimedStintStrategyViewModel(ISettingsProvider settingsProvider)
        {
            DisplaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
        }

        public DisplaySettingsViewModel DisplaySettingsViewModel { get; }

        public List<HumanReadableItem<StintLengthKind>> AllStintLengthOptions { get; } = new StintLengthMap().GetAllItems().ToList();

        public HumanReadableItem<StintLengthKind> SelectedStingLength
        {
            get => _selectedStingLength;
            set => SetProperty(ref _selectedStingLength, value);
        }

        public List<HumanReadableItem<StintPitStopInfo>> AllStintPitStopInfo { get; } = new StintPitStopInfoMap().GetAllItems().ToList();

        public HumanReadableItem<StintPitStopInfo> SelectedStingPitStopInfo
        {
            get => _selectedStingPitStopInfo;
            set => SetProperty(ref _selectedStingPitStopInfo, value);
        }

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

        public int StintDurationMinutes
        {
            get => _stintDurationMinutes;
            set => SetProperty(ref _stintDurationMinutes, value);
        }

        public int RegularStintMinutes
        {
            get => _regularStintMinutes;
            set => SetProperty(ref _regularStintMinutes, value);
        }

        public int RegularStintLaps
        {
            get => _regularStintLaps;
            set => SetProperty(ref _regularStintLaps, value);
        }

        public Volume RegularStintRequiresFuel
        {
            get => _regularStintRequiresFuel;
            set => SetProperty(ref _regularStintRequiresFuel, value);
        }

        public bool ShowLastStintInfo
        {
            get => _showLastStintInfo;
            set => SetProperty(ref _showLastStintInfo, value);
        }

        public int LastStintMinutes
        {
            get => _lastStintMinutes;
            set => SetProperty(ref _lastStintMinutes, value);
        }

        public int LastStintLaps
        {
            get => _lastStintLaps;
            set => SetProperty(ref _lastStintLaps, value);
        }

        public Volume LastStintFuel
        {
            get => _lastStintFuel;
            set => SetProperty(ref _lastStintFuel, value);
        }

        public int RegularStintCount
        {
            get => _regularStintCount;
            set => SetProperty(ref _regularStintCount, value);
        }

        protected override void ApplyModel(TimedStintStrategySettings model)
        {
            _stintDurationMinutes = model.StintDurationMinutes;
            _selectedStingLength = AllStintLengthOptions.Single(x => x.Value == model.StintLengthKind);
            _selectedStingPitStopInfo = AllStintPitStopInfo.Single(x => x.Value == model.StintPitStopInfo);
            NotifyPropertyChanged(string.Empty);
        }

        public override TimedStintStrategySettings SaveToNewModel()
        {
            return new TimedStintStrategySettings()
            {
                StintLengthKind = SelectedStingLength.Value,
                StintDurationMinutes = StintDurationMinutes,
                StintPitStopInfo = SelectedStingPitStopInfo.Value,
            };
        }
    }
}
