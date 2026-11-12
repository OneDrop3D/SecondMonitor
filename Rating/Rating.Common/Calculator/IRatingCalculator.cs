namespace SecondMonitor.Rating.Common.Calculator
{
    using System.Collections.Generic;
    using Configuration;
    using DataModel.Player;
    using DataModel.Results;

    public interface IRatingCalculator
    {
        DriversRating CalculateNewRating(DriversRating oldRating, IEnumerable<MatchResult> results, SimulatorRatingConfiguration simulatorRatingConfiguration);
    }
}