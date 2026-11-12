namespace SecondMonitor.Rating.Application.Championship.Operations.PointsStrategy
{
    public class RepresentantFinishPosition
    {
        public RepresentantFinishPosition(string name, int finishPosition)
        {
            FinishPosition = finishPosition;
            Name = name;
        }

        public int FinishPosition { get; }

        public string Name { get; }
    }
}
