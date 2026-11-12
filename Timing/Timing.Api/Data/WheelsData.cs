namespace SecondMonitor.Timing.Api.Data
{
    using SecondMonitor.DataModel.Snapshot.Systems;

    public class WheelsData
    {
        public WheelsData(Wheels wheels)
        {
            FrontLeft = new WheelData(wheels.FrontLeft);
            FrontRight = new WheelData(wheels.FrontRight);
            RearLeft = new WheelData(wheels.RearLeft);
            RearRight = new WheelData(wheels.RearRight);
            IsRideHeightFilled = wheels.IsRideHeightFilled;
        }

        public WheelData FrontLeft { get; }

        public WheelData FrontRight { get; }

        public WheelData RearLeft { get; }

        public WheelData RearRight { get; }

        public bool IsRideHeightFilled { get; }
    }
}
