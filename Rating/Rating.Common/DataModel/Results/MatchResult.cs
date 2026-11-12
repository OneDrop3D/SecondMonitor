namespace SecondMonitor.Rating.Common.DataModel.Results
{
    using Player;

    public class MatchResult
    {
        public MatchResult(DriversRating opponentRating, MatchOutcome matchOutcome) : this(string.Empty, opponentRating, matchOutcome)
        {
        }

        public MatchResult(string driverId, DriversRating opponentRating, MatchOutcome matchOutcome)
        {
            DriverId = driverId;
            OpponentRating = opponentRating;
            MatchOutcome = matchOutcome;
        }

        public string DriverId { get; }
        public DriversRating OpponentRating { get; }
        public MatchOutcome MatchOutcome { get; }
    }
}