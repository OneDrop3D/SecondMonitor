namespace SecondMonitor.ViewModels.FuelConsumption.FuelStrategy
{
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.ViewModels.CarStatus.FuelStatus;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;
    using SecondMonitor.ViewModels.Text;

    public class StandardPitStopViewModel : AbstractViewModel, IPitStopsViewModel
    {
        private FontStyle _nextPitStopFontStyle;
        private bool _isNextPitStopInfoVisible;
        private string _nextPitStopInfo;
        private RefuelingWindowState _nextPitStopState;
        private bool _isFuelToAddVisible;
        private bool _isLastStopFuelToAddVisible;
        private string _fuelForNextPitStopLabel;
        private string _fuelToAddLastStopLabel;
        private Volume _fuelForNextPitStop;
        private Volume _fuelToAddLastStop;
        private bool _isPitStopCountVisible;
        private int _pitStopCount;
        private RefuelingWindowState _refuelingWindowState;

        public StandardPitStopViewModel(ISettingsProvider settingsProvider)
        {
            FuelToAddLastStopLabel = "Last Stop: ";
            FuelForNextPitStop = Volume.FromLiters(0);
            DisplaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
        }

        public DisplaySettingsViewModel DisplaySettingsViewModel { get; }
        public FontStyle NextPitStopFontStyle
        {
            get => _nextPitStopFontStyle;
            set => SetProperty(ref _nextPitStopFontStyle, value);
        }

        public bool IsNextPitStopInfoVisible
        {
            get => _isNextPitStopInfoVisible;
            set => SetProperty(ref _isNextPitStopInfoVisible, value);
        }

        public string NextPitStopInfo
        {
            get => _nextPitStopInfo;
            set => SetProperty(ref _nextPitStopInfo, value);
        }

        public RefuelingWindowState NextPitStopState
        {
            get => _nextPitStopState;
            set => SetProperty(ref _nextPitStopState, value);
        }

        public bool IsFuelToAddVisible
        {
            get => _isFuelToAddVisible;
            set => SetProperty(ref _isFuelToAddVisible, value);
        }

        public bool IsLastStopFuelToAddVisible
        {
            get => _isLastStopFuelToAddVisible;
            set => SetProperty(ref _isLastStopFuelToAddVisible, value);
        }

        public string FuelForNextPitStopLabel
        {
            get => _fuelForNextPitStopLabel;
            set => SetProperty(ref _fuelForNextPitStopLabel, value);
        }

        public string FuelToAddLastStopLabel
        {
            get => _fuelToAddLastStopLabel;
            set => SetProperty(ref _fuelToAddLastStopLabel, value);
        }

        public Volume FuelForNextPitStop
        {
            get => _fuelForNextPitStop;
            set => SetProperty(ref _fuelForNextPitStop, value);
        }

        public Volume FuelToAddLastStop
        {
            get => _fuelToAddLastStop;
            set => SetProperty(ref _fuelToAddLastStop, value);
        }

        public bool IsPitStopCountVisible
        {
            get => _isPitStopCountVisible;
            set => SetProperty(ref _isPitStopCountVisible, value);
        }

        public int PitStopCount
        {
            get => _pitStopCount;
            set => SetProperty(ref _pitStopCount, value);
        }

        public RefuelingWindowState RefuelingWindowState
        {
            get => _refuelingWindowState;
            set => SetProperty(ref _refuelingWindowState, value);
        }

        public void Reset()
        {
            RefuelingWindowState = RefuelingWindowState.Closed;
            IsPitStopCountVisible = false;
            PitStopCount = 0;
            IsFuelToAddVisible = false;
            IsLastStopFuelToAddVisible = false;
            FuelForNextPitStop = Volume.FromLiters(0);
            FuelToAddLastStop = Volume.FromLiters(0);
            FuelForNextPitStopLabel = string.Empty;
            FuelToAddLastStopLabel = "Last Stop: ";
        }

        public virtual Volume GetFuelToAddNow(IFuelOverviewViewModel fuelOverviewViewModel)
        {
            return FuelForNextPitStop;
        }
    }
}
