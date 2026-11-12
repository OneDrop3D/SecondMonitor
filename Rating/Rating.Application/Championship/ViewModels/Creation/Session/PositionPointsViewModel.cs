namespace SecondMonitor.Rating.Application.Championship.ViewModels.Creation.Session
{
    using SecondMonitor.ViewModels;

    public class PositionPointsViewModel : AbstractViewModel
    {
        private int _position;
        private int _points;

        public PositionPointsViewModel(int position, int points)
        {
            _position = position;
            _points = points;
        }

        public int Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        public int Points
        {
            get => _points;
            set => SetProperty(ref _points, value);
        }
    }
}
