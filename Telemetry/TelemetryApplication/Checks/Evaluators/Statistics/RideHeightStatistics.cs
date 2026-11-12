namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators.Statistics
{
    using System.Collections.Generic;

    public class RideHeightStatistics : QuantityStatistics
    {
        public const double BottomingThreshold = 0.001;
        private readonly int _requiredDataPointsWithoutBottoming = 10;
        private readonly List<int> _bottomingStatistics = new List<int>();
        private bool _currentlyBottoming = false;
        private int _currentlyBottomingDataPoints = 0;
        private int _currentlyDataPointsWithoutBottoming;

        public RideHeightStatistics(int requiredDataPointsWithoutBottoming)
        {
            _requiredDataPointsWithoutBottoming = requiredDataPointsWithoutBottoming;
        }

        public IReadOnlyList<int> BottomingStatistics => _bottomingStatistics.AsReadOnly();

        public override void ProcessDataPoint(double value)
        {
            base.ProcessDataPoint(value);

            if (value <= BottomingThreshold)
            {
                if (!_currentlyBottoming)
                {
                    _currentlyBottoming = true;
                    _currentlyBottomingDataPoints = 0;
                }

                _currentlyBottomingDataPoints += _currentlyDataPointsWithoutBottoming;
                _currentlyDataPointsWithoutBottoming = 0;
                _currentlyBottomingDataPoints++;
            }
            else
            {
                switch (_currentlyBottoming)
                {
                    case true when _currentlyDataPointsWithoutBottoming < _requiredDataPointsWithoutBottoming:
                        _currentlyDataPointsWithoutBottoming++;
                        break;
                    case true:
                        _currentlyBottoming = false;
                        _bottomingStatistics.Add(_currentlyBottomingDataPoints);
                        _currentlyDataPointsWithoutBottoming = 0;
                        break;
                }
            }
        }
    }
}