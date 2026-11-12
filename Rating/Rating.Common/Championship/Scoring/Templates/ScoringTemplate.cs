namespace SecondMonitor.Rating.Common.Championship.Scoring.Templates
{
    public class ScoringTemplate
    {
        public string Name { get; set; }
        public int[] Scoring { get; set; }
        public int PointsForFastestLap { get; set; }
        public int MaxPositionForFastestLap { get; set; }
    }
}