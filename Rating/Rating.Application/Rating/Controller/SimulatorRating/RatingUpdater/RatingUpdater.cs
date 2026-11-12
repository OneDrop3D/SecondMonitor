namespace SecondMonitor.Rating.Application.Rating.Controller.SimulatorRating.RatingUpdater
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Common.Calculator;
    using Common.DataModel;
    using Common.DataModel.Player;
    using Common.DataModel.Results;
    using DataModel.Summary;
    using NLog;
    using SecondMonitor.ViewModels.Settings;

    public class RatingUpdater : IRatingUpdater
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ISimulatorRatingController _ratingController;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IRatingCalculator _ratingCalculator;

        public RatingUpdater(ISimulatorRatingController ratingController, ISettingsProvider settingsProvider, IRatingCalculator ratingCalculator)
        {
            _ratingController = ratingController;
            _settingsProvider = settingsProvider;
            _ratingCalculator = ratingCalculator;
        }

        public async Task<(DriversRating NewSimulatorRating, DriversRating NewClassRating, DriversRating NewDifficultyRating)> UpdateRatingsByResults(Dictionary<string, DriversRating> ratings, DriversRating classRatting, DriversRating difficultyRating, DriversRating simulatorRating, SessionFinishState sessionFinishState, int difficulty)
        {
            DriverFinishState player = sessionFinishState.DriverFinishStates.First(x => x.IsPlayer);
            Logger.Info("---- CALCULATING NEW CLASS RATINGS ---");
            DriversRating newClassRating = CalculateNewRatingByResult(ratings, classRatting, sessionFinishState, player, false);
            Logger.Info("---- CALCULATING NEW SIM RATINGS ---");
            DriversRating newSimRating = CalculateNewRatingByResult(ratings, simulatorRating, sessionFinishState, player, false);
            Logger.Info("---- CALCULATING NEW DIFFICULTY RATINGS ---");
            DriversRating newDifficultyRating = CalculateNewRatingByResult(ratings, difficultyRating, sessionFinishState, player, true);

            await ComputeNewRatingsAndNotify(newClassRating, newDifficultyRating, newSimRating, difficulty, player, sessionFinishState.TrackName);
            return (newSimRating, newClassRating, newDifficultyRating);
        }

        public async Task<(DriversRating NewSimulatorRating, DriversRating NewClassRating, DriversRating NewDifficultyRating)> UpdateRatingsAsLoss(Dictionary<string, DriversRating> ratings, DriversRating classRatting, DriversRating difficultyRating, DriversRating simulatorRating, int difficulty, Driver player, string trackName)
        {
            DriverFinishState playerFinishState = new DriverFinishState(player.DriverId, true, player.DriverLongName, player.CarName, player.ClassName, ratings.Count, player.FinishingPosition);
            DriversRating newClassRating = CalculateNewAsLoss(ratings, classRatting, player);
            DriversRating newSimRating = CalculateNewAsLoss(ratings, simulatorRating, player);

            await ComputeNewRatingsAndNotify(newClassRating, difficultyRating, newSimRating, difficulty, playerFinishState, trackName);
            return (newSimRating, newClassRating, difficultyRating);
        }

        private static void LogRatings(List<MatchResult> ratings)
        {
            ratings.ForEach(x => Logger.Info($"Rating for {x.DriverId} : {x.OpponentRating.Rating} - {x.OpponentRating.Deviation} - Result: {x.MatchOutcome}"));
        }

        private DriversRating CalculateNewAsLoss(Dictionary<string, DriversRating> ratings, DriversRating playerRating,  Driver player)
        {
            var opponents = ratings.Where(x => x.Key != player.DriverId).Select(x => new MatchResult(x.Key, x.Value, 0)).ToList();
            return CalculateNewRating(playerRating, opponents);
        }

        private DriversRating CalculateNewRatingByResult(Dictionary<string, DriversRating> ratings, DriversRating rating, SessionFinishState sessionFinishState, DriverFinishState player, bool useAveragePosition)
        {
            Logger.Info("---- CALCULATING NEW RATINGS ---");
            Logger.Info($"Players Rating: {rating.Rating}-{rating.Deviation}-{rating.Volatility}");

            Func<DriverFinishState, double> finishPositionFunc = useAveragePosition ? x => x.AveragePosition : new Func<DriverFinishState, double>(x => x.FinishPosition);

            IEnumerable<DriverFinishState> eligibleDrivers = sessionFinishState.DriverFinishStates.Where(x => !x.IsPlayer && ratings.ContainsKey(x.DriverId));
            var opponents = eligibleDrivers.Select(x => new MatchResult(x.DriverId, ratings[x.DriverId], finishPositionFunc(x) < finishPositionFunc(player) ? MatchOutcome.PlayerLost : MatchOutcome.PlayerWon)).ToList();
            LogRatings(opponents);
            return CalculateNewRating(rating, opponents);
        }

        private DriversRating CalculateNewRating(DriversRating player, List<MatchResult> opponents)
        {
            double ratingBeforeChange = player.Rating;
            DriversRating newRating = _ratingCalculator.CalculateNewRating(player, opponents, _ratingController.CurrentSimRatingConfiguration);
            if (_settingsProvider.DisplaySettingsViewModel.RatingSettingsViewModel.NormalizedFieldSize == 0)
            {
                return newRating;
            }

            double normalizationFactor = (double)_settingsProvider.DisplaySettingsViewModel.RatingSettingsViewModel.NormalizedFieldSize / opponents.Count;
            Logger.Info($"Rating Normalization Factor is {normalizationFactor:F2}");
            double ratingChange = newRating.Rating - ratingBeforeChange;
            double normalizedRatingChange = ratingChange * normalizationFactor;
            Logger.Info($"Rating Change: {ratingChange:F2}, Normalized Rating Change: {normalizedRatingChange:F2}");
            newRating.Rating = (int)(ratingBeforeChange + normalizedRatingChange);
            return newRating;
        }

        private async Task ComputeNewRatingsAndNotify(DriversRating newPlayerClassRating, DriversRating newDifficultyRating, DriversRating newPlayerSimRatingRating, int difficulty, DriverFinishState playerFinishState, string trackName)
        {
            await _ratingController.UpdateRating(newPlayerClassRating, newDifficultyRating, newPlayerSimRatingRating, difficulty, trackName, playerFinishState);
        }
    }
}