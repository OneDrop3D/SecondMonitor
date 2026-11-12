namespace SecondMonitor.ViewModels.PitBoard
{
    using ViewModels;

    public class DriverDistanceViewModel : AbstractViewModel
    {
        private string _distance;
        private string _driverName;
        private bool _isVisible;

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public string DriverName
        {
            get => _driverName;
            set => SetProperty(ref _driverName, value);
        }

        public string Distance
        {
            get => _distance;
            set => SetProperty(ref _distance, value);
        }
    }
}