namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators.Statistics
{
    public class QuantityStatistics
    {
        private int _dataPointsCount;
        private double _sum;

        public QuantityStatistics()
        {
            MinValue = double.MaxValue;
            MaxValue = double.MinValue;
        }

        public double MinValue { get; private set; }

        public double MaxValue { get; private set; }

        public double Average => _sum / _dataPointsCount;

        public virtual void ProcessDataPoint(double value)
        {
            _dataPointsCount++;
            _sum += value;

            if (value > MaxValue)
            {
                MaxValue = value;
            }

            if (value < MinValue)
            {
                MinValue = value;
            }
        }
    }
}