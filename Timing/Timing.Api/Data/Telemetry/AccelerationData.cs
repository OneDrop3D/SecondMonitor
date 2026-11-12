namespace SecondMonitor.Timing.Api.Data.Telemetry
{
    using SecondMonitor.DataModel.BasicProperties;

    public class AccelerationData
    {
        public AccelerationData(Acceleration acceleration)
        {
            XinG = acceleration.XinG;
            YinG = acceleration.YinG;
            ZinG = acceleration.ZinG;
        }

        public double XinG { get; }

        public double YinG { get; }

        public double ZinG { get; }
    }
}
