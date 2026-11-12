namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators.LapSummary
{
    public class LapSummaryViewModel : AbstractEvaluatorViewModel
    {
        private string _lapTime;
        private string _totalFuel;
        private string _totalFuelConsumed;
        private string _airTemperature;
        private string _trackTemperature;
        private string _rainLevel;
        private string _topSpeed;
        private string _averageSpeed;
        private string _frontLeftTyreWear;
        private string _frontRightTyreWear;
        private string _rearLeftTyreWear;
        private string _rearRightTyreWear;

        public LapSummaryViewModel() : base("Lap Summary")
        {
        }

        public string FrontLeftTyreWear
        {
            get => _frontLeftTyreWear;
            set => SetProperty(ref _frontLeftTyreWear, value);
        }

        public string FrontRightTyreWear
        {
            get => _frontRightTyreWear;
            set => SetProperty(ref _frontRightTyreWear, value);
        }

        public string RearLeftTyreWear
        {
            get => _rearLeftTyreWear;
            set => SetProperty(ref _rearLeftTyreWear, value);
        }

        public string RearRightTyreWear
        {
            get => _rearRightTyreWear;
            set => SetProperty(ref _rearRightTyreWear, value);
        }

        public string LapTime
        {
            get => _lapTime;
            set => SetProperty(ref _lapTime, value);
        }

        public string TotalFuel
        {
            get => _totalFuel;
            set => SetProperty(ref _totalFuel, value);
        }

        public string TotalFuelConsumed
        {
            get => _totalFuelConsumed;
            set => SetProperty(ref _totalFuelConsumed, value);
        }

        public string AirTemperature
        {
            get => _airTemperature;
            set => SetProperty(ref _airTemperature, value);
        }

        public string TrackTemperature
        {
            get => _trackTemperature;
            set => SetProperty(ref _trackTemperature, value);
        }

        public string RainLevel
        {
            get => _rainLevel;
            set => SetProperty(ref _rainLevel, value);
        }

        public string TopSpeed
        {
            get => _topSpeed;
            set => SetProperty(ref _topSpeed, value);
        }

        public string AverageSpeed
        {
            get => _averageSpeed;
            set => SetProperty(ref _averageSpeed, value);
        }

        public string TimeSpentOnThrottle { get; set; }

        public string TimeSpentOnFullThrottle { get; set; }

        public string TimeSpentOnBrake { get; set; }

        public string TimeSpentOnFullBrake { get; set; }

        public EvaluationResultKind BrakeEvaluationResultKind { get; set; }
        public string FullBrakePercentage { get; set; }
    }
}