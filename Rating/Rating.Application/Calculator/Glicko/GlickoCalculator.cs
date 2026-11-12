namespace SecondMonitor.Rating.Application.Calculator.Glicko
{
    using System;
    using System.Collections.Generic;

    using Common.Calculator;
    using Common.Configuration;
    using Common.DataModel.Player;
    using Common.DataModel.Results;

    public class GlickoCalculator : IRatingCalculator
    {
        public DriversRating CalculateNewRating(DriversRating oldRating, IEnumerable<MatchResult> results, SimulatorRatingConfiguration simulatorRatingConfiguration)
        {
            throw new NotSupportedException();
            /*GlickoPlayer glickoPlayer = oldRating.ToGlicko();
            List<GlickoOpponent> opponents = results.Select(x => new GlickoOpponent(x.OpponentRating.ToGlicko(), x.MatchOutcome == MatchOutcome.PlayerWon ? 1 : 0)).ToList();
            GlickoPlayer newRatingGlicko = CalculateNewRating(glickoPlayer, opponents);
            return newRatingGlicko.FromGlicko();*/
        }

        /*private GlickoPlayer CalculateNewRating(GlickoPlayer player, List<GlickoOpponent> opponents)
        {
            GlickoPlayer newRating = Glicko2.GlickoCalculator.CalculateRanking(player, opponents);
            return newRating;
        }*/
    }
}