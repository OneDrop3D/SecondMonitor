namespace SecondMonitor.ViewModels.TrackRecords
{
    using System;
    using DataModel.TrackRecords;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    public class RecordViewModel : AbstractViewModel<RecordEntryDto>, IRecordViewModel
    {
        private readonly DisplaySettingsViewModel _displaySettingsViewModel;
        private TimeSpan _lapTime;
        private bool _isHighlighted;
        private bool _isVisible;
        private DateTime _recordDatetime;
        private string _sessionType;
        private string _carName;
        private bool _isAverageSpeedVisible;
        private string _averageSpeed;

        public RecordViewModel(ISettingsProvider settings)
        {
            _displaySettingsViewModel = settings.DisplaySettingsViewModel;
        }

        public TimeSpan LapTime
        {
            get => _lapTime;
            set => SetProperty(ref _lapTime, value);
        }

        public bool IsHighlighted
        {
            get => _isHighlighted;
            set => SetProperty(ref _isHighlighted, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public DateTime RecordDatetime
        {
            get => _recordDatetime;
            private set => SetProperty(ref _recordDatetime, value);
        }

        public string SessionType
        {
            get => _sessionType;
            private set => SetProperty(ref _sessionType, value);
        }

        public string CarName
        {
            get => _carName;
            private set => SetProperty(ref _carName, value);
        }

        public bool IsAverageSpeedVisible
        {
            get => _isAverageSpeedVisible;
            set => SetProperty(ref _isAverageSpeedVisible, value);
        }

        public string AverageSpeed
        {
            get => _averageSpeed;
            set => SetProperty(ref _averageSpeed, value);
        }

        protected override void ApplyModel(RecordEntryDto model)
        {
            if (model == null)
            {
                IsVisible = false;
                return;
            }

            LapTime = model.LapTime;
            IsVisible = LapTime != TimeSpan.Zero && LapTime != TimeSpan.MinValue;
            CarName = model.CarDisplayName;
            RecordDatetime = model.RecordDate;
            SessionType = model.SessionType.ToString();
            AverageSpeed = $"{Velocity.FromKph(model.AverageSpeedInKph).GetValueInUnits(_displaySettingsViewModel.VelocityUnits):F2}{Velocity.GetUnitSymbol(_displaySettingsViewModel.VelocityUnits)}";
            IsAverageSpeedVisible = model.AverageSpeedInKph > 0 && _displaySettingsViewModel.TrackRecordsSettingsViewModel.IsShowAverageSpeedEnabled;
        }

        public override RecordEntryDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}