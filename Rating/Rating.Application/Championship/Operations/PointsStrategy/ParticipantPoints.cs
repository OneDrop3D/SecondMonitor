namespace SecondMonitor.Rating.Application.Championship.Operations.PointsStrategy
{
    public class ParticipantPoints
    {
        public ParticipantPoints(string name, int points)
        {
            Points = points;
            Name = name;
        }

        public int Points { get; }

        public string Name { get; }
    }
}
