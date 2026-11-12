namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using Model;

    public class TrackRecordsSettingsViewModel : AbstractViewModel<TrackRecordsSettings>
    {
        private bool _isEnabled;
        private bool _displayRecordForCurrentSessionType;
        private bool _isShowAverageSpeedEnabled;

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public bool DisplayRecordForCurrentSessionType
        {
            get => _displayRecordForCurrentSessionType;
            set => SetProperty(ref _displayRecordForCurrentSessionType, value);
        }

        public bool IsShowAverageSpeedEnabled
        {
            get => _isShowAverageSpeedEnabled;
            set => SetProperty(ref _isShowAverageSpeedEnabled, value);
        }

        protected override void ApplyModel(TrackRecordsSettings model)
        {
            IsEnabled = model.IsEnabled;
            DisplayRecordForCurrentSessionType = model.DisplayRecordForCurrentSessionType;
            IsShowAverageSpeedEnabled = model.IsShowAverageSpeedEnabled;
        }

        public override TrackRecordsSettings SaveToNewModel()
        {
            return new TrackRecordsSettings()
            {
                DisplayRecordForCurrentSessionType = DisplayRecordForCurrentSessionType,
                IsEnabled = IsEnabled,
                IsShowAverageSpeedEnabled = IsShowAverageSpeedEnabled,
            };
        }
    }
}
