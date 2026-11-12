namespace SecondMonitor.Timing.Api.Data
{
    using SecondMonitor.DataModel.BasicProperties;

    public class OrientationData
    {
        public OrientationData(Orientation orientation)
        {
            RollDegrees = orientation.Roll.InDegrees;
            YawDegrees = orientation.Yaw.InDegrees;
            PitchDegrees = orientation.Pitch.InDegrees;
        }

        public double RollDegrees { get; }

        public double YawDegrees { get; }

        public double PitchDegrees { get; }
    }
}
