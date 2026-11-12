namespace SecondMonitor.Rating.Application.Rating.Controller.SimulatorRating.RatingUpdater
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Common.DataModel;
    using Common.DataModel.Player;
    using DataModel.Summary;

    public interface IRatingUpdater
    {
        Task<(DriversRating NewSimulatorRating, DriversRating NewClassRating, DriversRating NewDifficultyRating)> UpdateRatingsByResults(Dictionary<string, DriversRating> ratings,
            DriversRating classRatting, DriversRating difficultyRating, DriversRating simulatorRating, SessionFinishState sessionFinishState, int difficulty);

        Task<(DriversRating NewSimulatorRating, DriversRating NewClassRating, DriversRating NewDifficultyRating)> UpdateRatingsAsLoss(Dictionary<string, DriversRating> ratingMap,
            DriversRating classRatting, DriversRating difficultyRating, DriversRating simulatorRating, int difficulty, Driver player, string trackName);
    }
}