namespace SecondMonitor.Rating.Application.Rating.Controller.SimulatorRating
{
    using System.Linq;
    using Common.Repository;
    using Contracts.Rating;

    public class SimulatorRatingProvider : ISimulatorRatingProvider
    {
        private readonly IRatingRepository _ratingRepository;

        public SimulatorRatingProvider(IRatingRepository ratingRepository)
        {
            _ratingRepository = ratingRepository;
        }

        public bool TryGetSuggestedDifficulty(string simulatorName, string className, out int difficulty)
        {
            difficulty = 0;
            var ratings = _ratingRepository.SimulatorsRatings;
            var simulatorRatings = ratings.SimulatorsRatings.FirstOrDefault(x => x.SimulatorName == simulatorName);

            var classRating = simulatorRatings?.ClassRatings.FirstOrDefault(x => x.ClassName == className);
            if (classRating == null)
            {
                return false;
            }

            difficulty = classRating.DifficultySettings.SelectedDifficulty;
            return true;
        }
    }
}