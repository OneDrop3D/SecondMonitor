namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators.Statistics
{
    public class QuantityWithLimitsStatistics : QuantityStatistics
    {
        public QuantityWithLimitsStatistics(double lowLimit, double highLimit, double idealValue)
        {
            IdealValue = idealValue;

            HighLimit = highLimit;
            LowLimit = lowLimit;
        }

        public double IdealValue { get; }

        public int PointsOverLimit { get; private set; }

        public int PointsBelowLimit { get; private set; }

        public double HighLimit { get; }

        public double LowLimit { get; }

        public override void ProcessDataPoint(double value)
        {
            base.ProcessDataPoint(value);

            if (value > HighLimit)
            {
                PointsOverLimit++;
            }

            if (value < LowLimit)
            {
                PointsBelowLimit++;
            }
        }
    }
}