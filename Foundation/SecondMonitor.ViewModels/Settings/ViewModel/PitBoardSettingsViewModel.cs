namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using Model;

    public class PitBoardSettingsViewModel : AbstractViewModel<PitBoardSettings>
    {
        private bool _isEnabled;
        private int _displaySeconds;
        private bool _isYellowBoardEnabled;
        private HorizontalAlignment _horizontalAlignment;
        private VerticalAlignment _verticalAlignment;
        private bool _onlyPlayerClass;
        private bool _isPitEntrySpeedBoardEnabled;
        private bool _isPitDurationBoardEnabled;

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public bool OnlyPlayerClass
        {
            get => _onlyPlayerClass;
            set => SetProperty(ref _onlyPlayerClass, value);
        }

        public int DisplaySeconds
        {
            get => _displaySeconds;
            set => SetProperty(ref _displaySeconds, value);
        }

        public bool IsYellowBoardEnabled
        {
            get => _isYellowBoardEnabled;
            set => SetProperty(ref _isYellowBoardEnabled, value);
        }

        public bool IsPitEntrySpeedBoardEnabled
        {
            get => _isPitEntrySpeedBoardEnabled;
            set => SetProperty(ref _isPitEntrySpeedBoardEnabled, value);
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get => _horizontalAlignment;
            set => SetProperty(ref _horizontalAlignment, value);
        }

        public VerticalAlignment VerticalAlignment
        {
            get => _verticalAlignment;
            set => SetProperty(ref _verticalAlignment, value);
        }

        public bool IsPitDurationBoardEnabled
        {
            get => _isPitDurationBoardEnabled;
            set => SetProperty(ref _isPitDurationBoardEnabled, value);
        }

        protected override void ApplyModel(PitBoardSettings model)
        {
            IsEnabled = model.IsEnabled;
            OnlyPlayerClass = model.OnlyPlayerClass;
            DisplaySeconds = model.DisplaySeconds;
            IsYellowBoardEnabled = model.IsYellowBoardEnabled;
            HorizontalAlignment = model.HorizontalAlignment;
            VerticalAlignment = model.VerticalAlignment;
            IsPitEntrySpeedBoardEnabled = model.IsPitEntrySpeedBoardEnabled;
            IsPitDurationBoardEnabled = model.IsPitDurationBoardEnabled;
        }

        public override PitBoardSettings SaveToNewModel()
        {
            return new PitBoardSettings()
            {
                DisplaySeconds = DisplaySeconds,
                OnlyPlayerClass = OnlyPlayerClass,
                IsEnabled = IsEnabled,
                IsYellowBoardEnabled = IsYellowBoardEnabled,
                VerticalAlignment = VerticalAlignment,
                HorizontalAlignment = HorizontalAlignment,
                IsPitEntrySpeedBoardEnabled = IsPitEntrySpeedBoardEnabled,
                IsPitDurationBoardEnabled = IsPitDurationBoardEnabled,
            };
        }
    }
}