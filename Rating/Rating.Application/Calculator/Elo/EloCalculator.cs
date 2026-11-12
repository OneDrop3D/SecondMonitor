namespace SecondMonitor.Rating.Application.Calculator.Elo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.Calculator;
    using Common.Configuration;
    using Common.DataModel.Player;
    using Common.DataModel.Results;
    using ViewModels.Settings;

    public class EloCalculator : IRatingCalculator
    {
        private readonly ISettingsProvider _settingsProvider;
        /*private const int EloK = 10;
        private const double LevelChangeCoef = 2.2;*/

        public EloCalculator(ISettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider;
        }

        public DriversRating CalculateNewRating(DriversRating oldRating, IEnumerable<MatchResult> results, SimulatorRatingConfiguration simulatorRatingConfiguration)
        {
            int playerRating = oldRating.Rating;
            double expectationRange = simulatorRatingConfiguration.RatingPerLevel * _settingsProvider.DisplaySettingsViewModel.RatingSettingsViewModel.AiCompetitiveRange;
            int ratingDelta = results.Sum(matchResult => CalculateEloChange(playerRating, matchResult.OpponentRating.Rating, matchResult.MatchOutcome, expectationRange));

            return new DriversRating(playerRating + ratingDelta, oldRating.Deviation, oldRating.Volatility)
            {
                Difficulty = oldRating.Difficulty,
            };
        }

        private int CalculateEloChange(int playerRating, int opponentRating, MatchOutcome outcome, double expectationRange)
        {
            int eloK = _settingsProvider.DisplaySettingsViewModel.RatingSettingsViewModel.EloK;
            int outcomeCoef = outcome == MatchOutcome.PlayerWon ? 1 : 0;
            int delta = (int)(eloK * ((int)outcomeCoef - ExpectationToWin(playerRating, opponentRating, expectationRange)));
            return delta;
        }

        internal static double ExpectationToWin(int playerOneRating, int playerTwoRating, double expectationRange)
        {
            double changeToWin = 1 / (1 + Math.Pow(10, (playerTwoRating - playerOneRating) / expectationRange));
            return changeToWin;
        }
    }
}