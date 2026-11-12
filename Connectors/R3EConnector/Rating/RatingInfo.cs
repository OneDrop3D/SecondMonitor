namespace SecondMonitor.R3EConnector.Rating
{
    public class RatingInfo
    {
        public RatingInfo(double rating, string fullName, int raceCompleted, double reputation)
        {
            Rating = rating;
            FullName = fullName;
            RaceCompleted = raceCompleted;
            Reputation = reputation;
        }

        public double Rating { get; }

        public string FullName { get; }

        public int RaceCompleted { get; }

        public double Reputation { get; }
    }
}