namespace SecondMonitor.Contracts.Session
{
    using System;
    using DataModel.BasicProperties;

    public class PitReturnPrediction
    {
        public PitReturnPrediction(ReturnPosition classReturnPosition, ReturnPosition overallReturnPosition, Point3D returnWorldPos, TimeSpan pitTime, double returnLapDistance)
        {
            ClassReturnPosition = classReturnPosition;
            OverallReturnPosition = overallReturnPosition;
            ReturnWorldPos = returnWorldPos;
            PitTime = pitTime;
            ReturnLapDistance = returnLapDistance;
        }

        public ReturnPosition ClassReturnPosition { get; set; }

        public ReturnPosition OverallReturnPosition { get; set; }

        public Point3D ReturnWorldPos { get; }

        public TimeSpan PitTime { get; }
        public double ReturnLapDistance { get; set; }

        public static PitReturnPrediction Empty => new PitReturnPrediction(ReturnPosition.Empty, ReturnPosition.Empty,
            new Point3D(Distance.ZeroDistance, Distance.ZeroDistance, Distance.ZeroDistance), TimeSpan.Zero, 0);
    }
}
