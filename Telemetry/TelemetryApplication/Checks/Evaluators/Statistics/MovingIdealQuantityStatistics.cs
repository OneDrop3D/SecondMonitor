namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators.Statistics
{
    public class MovingIdealQuantityStatistics
    {
        private int _dataPointsCount;
        private double _sum;
        private double _idealSum;
        private double _distanceToIdealSum;
        private double _upperLimitSum;
        private double _lowerLimitSum;

        public MovingIdealQuantityStatistics()
        {
            MinValue = double.MaxValue;
            MaxValue = double.MinValue;
        }

        public double IdealValueAverage => _idealSum / _dataPointsCount;

        public int PointsOverLimit { get; private set; }

        public int PointsBelowLimit { get; private set; }

        public double MinValue { get; private set; }

        public double MaxValue { get; private set; }

        public double HighLimitAverage => _upperLimitSum / _dataPointsCount;

        public double LowLimitAverage => _lowerLimitSum / _dataPointsCount;

        public double DistanceToIdealAverage => _distanceToIdealSum / _dataPointsCount;

        public double Average => _sum / _dataPointsCount;

        public void ProcessDataPoint(double value, double lowLimit, double highLimit, double idealValue)
        {
            _dataPointsCount++;
            _sum += value;
            _idealSum += idealValue;
            _lowerLimitSum += lowLimit;
            _upperLimitSum += highLimit;
            double distanceToIdeal = value - idealValue;

            _distanceToIdealSum += distanceToIdeal;

            if (value > MaxValue)
            {
                MaxValue = value;
            }

            if (value < MinValue)
            {
                MinValue = value;
            }

            if (value > highLimit)
            {
                PointsOverLimit++;
            }

            if (value < lowLimit)
            {
                PointsBelowLimit++;
            }
        }
    }
}