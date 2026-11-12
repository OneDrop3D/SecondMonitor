namespace SecondMonitor.Timing.Application.LapTimings.ViewModel
{
    using DataModel.Snapshot.Drivers;
    using ViewModels;

    public class RatingViewModel : AbstractViewModel<RatingInfo>
    {
        private int _racesCompleted;
        private bool _isVisible;
        private double _reputation;
        private int _rating;

        public int RacesCompleted
        {
            get => _racesCompleted;
            set => SetProperty(ref _racesCompleted, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public double Reputation
        {
            get => _reputation;
            set => SetProperty(ref _reputation, value);
        }

        public int Rating
        {
            get => _rating;
            set => SetProperty(ref _rating, value);
        }

        protected override void ApplyModel(RatingInfo model)
        {
            RacesCompleted = model.RacesCompleted;
            IsVisible = model.IsFilled;
            Reputation = model.Reputation;
            Rating = model.Rating;
        }

        public override RatingInfo SaveToNewModel()
        {
            throw new System.NotSupportedException();
        }
    }
}