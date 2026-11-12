namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.ViewModels.Settings.Model;

    public class ChampionshipSettingViewModel : AbstractViewModel<ChampionshipSettings>
    {
        private bool _isOutlineLeaderEnabled = true;
        private ColorDto _leaderOutLine = ColorDto.GreenColor;
        private bool _isOutLineDriverMorePointsEnabled = true;
        private int _maxOutlinedDriversMorePoints = 3;
        private bool _isOutLineDriveLessPointsEnabled = true;
        private int _maxOutlinedDriveLessPoints = 3;
        private int _maxPointsDifferenceToOutline = 20;
        private ColorDto _driverMorePointsOutlineFrom;
        private ColorDto _driverMorePointsOutlineTo;
        private ColorDto _driverLessPointsOutlineFrom;
        private ColorDto _driverLessPointsOutlineTo;
        private bool _isMultiplayerEnabled;
        public bool IsOutlineLeaderEnabled
        {
            get => _isOutlineLeaderEnabled;
            set => SetProperty(ref _isOutlineLeaderEnabled, value);
        }

        public ColorDto LeaderOutLine
        {
            get => _leaderOutLine;
            set => SetProperty(ref _leaderOutLine, value);
        }

        public bool IsOutLineDriverMorePointsEnabled
        {
            get => _isOutLineDriverMorePointsEnabled;
            set => SetProperty(ref _isOutLineDriverMorePointsEnabled, value);
        }

        public int MaxOutlinedDriversMorePoints
        {
            get => _maxOutlinedDriversMorePoints;
            set => SetProperty(ref _maxOutlinedDriversMorePoints, value);
        }

        public bool IsOutLineDriveLessPointsEnabled
        {
            get => _isOutLineDriveLessPointsEnabled;
            set => SetProperty(ref _isOutLineDriveLessPointsEnabled, value);
        }

        public int MaxOutlinedDriveLessPoints
        {
            get => _maxOutlinedDriveLessPoints;
            set => SetProperty(ref _maxOutlinedDriveLessPoints, value);
        }

        public int MaxPointsDifferenceToOutline
        {
            get => _maxPointsDifferenceToOutline;
            set => SetProperty(ref _maxPointsDifferenceToOutline, value);
        }

        public ColorDto DriverMorePointsOutlineFrom
        {
            get => _driverMorePointsOutlineFrom;
            set => SetProperty(ref _driverMorePointsOutlineFrom, value);
        }

        public ColorDto DriverMorePointsOutlineTo
        {
            get => _driverMorePointsOutlineTo;
            set => SetProperty(ref _driverMorePointsOutlineTo, value);
        }

        public ColorDto DriverLessPointsOutlineFrom
        {
            get => _driverLessPointsOutlineFrom;
            set => SetProperty(ref _driverLessPointsOutlineFrom, value);
        }

        public ColorDto DriverLessPointsOutlineTo
        {
            get => _driverLessPointsOutlineTo;
            set => SetProperty(ref _driverLessPointsOutlineTo, value);
        }

        public bool IsMultiplayerEnabled
        {
            get => _isMultiplayerEnabled;
            set => SetProperty(ref _isMultiplayerEnabled, value);
        }
        
        public ChampionshipOptions LastChampionshipOptions { get; set; }

        protected override void ApplyModel(ChampionshipSettings model)
        {
            IsOutlineLeaderEnabled = model.IsOutlineLeaderEnabled;
            LeaderOutLine = model.LeaderOutLine;
            IsOutLineDriverMorePointsEnabled = model.IsOutLineDriverMorePointsEnabled;
            MaxOutlinedDriversMorePoints = model.MaxOutlinedDriversMorePoints;
            IsOutLineDriveLessPointsEnabled = model.IsOutLineDriveLessPointsEnabled;
            MaxOutlinedDriveLessPoints = model.MaxOutlinedDriveLessPoints;
            DriverMorePointsOutlineFrom = model.DriverMorePointsOutlineFrom;
            DriverMorePointsOutlineTo = model.DriverMorePointsOutlineTo;
            MaxPointsDifferenceToOutline = model.MaxPointsDifferenceToOutline;
            DriverLessPointsOutlineFrom = model.DriverLessPointsOutlineFrom;
            DriverLessPointsOutlineTo = model.DriverLessPointsOutlineTo;
            IsMultiplayerEnabled = model.IsMultiplayerEnabled;
            LastChampionshipOptions = model.LastChampionshipOptions;
        }

        public override ChampionshipSettings SaveToNewModel()
        {
            return new ChampionshipSettings()
            {
                IsOutlineLeaderEnabled = IsOutlineLeaderEnabled,
                IsOutLineDriverMorePointsEnabled = IsOutLineDriverMorePointsEnabled,
                MaxPointsDifferenceToOutline = MaxPointsDifferenceToOutline,
                IsOutLineDriveLessPointsEnabled = IsOutLineDriverMorePointsEnabled,
                LeaderOutLine = LeaderOutLine,
                MaxOutlinedDriversMorePoints = MaxOutlinedDriversMorePoints,
                MaxOutlinedDriveLessPoints = MaxOutlinedDriveLessPoints,
                DriverLessPointsOutlineTo = DriverLessPointsOutlineTo,
                DriverMorePointsOutlineFrom = DriverMorePointsOutlineFrom,
                DriverLessPointsOutlineFrom = DriverLessPointsOutlineFrom,
                DriverMorePointsOutlineTo = DriverMorePointsOutlineTo,
                IsMultiplayerEnabled = IsMultiplayerEnabled,
                LastChampionshipOptions = LastChampionshipOptions,
            };
        }
    }
}
