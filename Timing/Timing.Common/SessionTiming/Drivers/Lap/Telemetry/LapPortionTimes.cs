namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap.Telemetry
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class LapPortionTimes : INotifyPropertyChanged
    {
        private const int MaximumSectors = 400;
        private const int DesiredSectionLength = 5;
        private readonly int _trackPortionLength;

        private readonly TimeSpan[] _trackPortions;

        private int _lastTrackedPortion;

        private double _nextPositionUpdate;

        public LapPortionTimes(double trackLength, LapInfo lap)
        {
            _lastTrackedPortion = -1;
            Lap = lap;
            int sections = Math.Min(MaximumSectors, (int)(trackLength / DesiredSectionLength));
            _trackPortionLength = (int)Math.Ceiling(trackLength / sections);
            _trackPortions = new TimeSpan[((int)trackLength / _trackPortionLength) + 1];
            _nextPositionUpdate = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public LapInfo Lap { get; }

        public int LastTrackedPortion
        {
            get => _lastTrackedPortion;
            set
            {
                _lastTrackedPortion = value;
                OnPropertyChanged();
            }
        }

        public void UpdateLapPortions()
        {
            if (double.IsNaN(Lap.CompletedDistance))
            {
                return;
            }

            if (Lap.CompletedDistance < _nextPositionUpdate)
            {
                return;
            }

            int currentPortion = GetIndexByDistance(Lap.CompletedDistance);
            if (currentPortion >= _trackPortions.Length || currentPortion == _lastTrackedPortion)
            {
                return;
            }

            double lastPortionTimeInSeconds = _lastTrackedPortion > -1 ? _trackPortions[_lastTrackedPortion].TotalSeconds : 0;
            double timeDifferenceBetweenPortions = Lap.CurrentlyValidProgressTime.TotalSeconds - lastPortionTimeInSeconds;
            double distanceOverPortionStart = Lap.CompletedDistance % _trackPortionLength;
            int portionsToUpdate = currentPortion - _lastTrackedPortion;
            double timePortionToUse = (portionsToUpdate * _trackPortionLength) / ((portionsToUpdate * _trackPortionLength) + distanceOverPortionStart);

            double deltaPerPortion = (timeDifferenceBetweenPortions * timePortionToUse) / portionsToUpdate;

            for (int i = 1; i <= portionsToUpdate; i++)
            {
                _lastTrackedPortion++;
                _trackPortions[_lastTrackedPortion] = TimeSpan.FromSeconds(lastPortionTimeInSeconds + (deltaPerPortion * i));
            }

            _nextPositionUpdate = Lap.CompletedDistance + _trackPortionLength;
            LastTrackedPortion = currentPortion;
        }

        public TimeSpan GetTimeAtDistance(double distance)
        {
            int index = GetIndexByDistance(distance);
            return GetTimeAtPortion(index);
        }

        public TimeSpan GetTimeAtPortion(int index)
        {
            return index > _lastTrackedPortion ? TimeSpan.Zero : _trackPortions[index];
        }

        public TimeSpan GetLastRecordedTime()
        {
            return _lastTrackedPortion < 0 ? TimeSpan.Zero : _trackPortions[_lastTrackedPortion];
        }

        private int GetIndexByDistance(double trackDistance) => (int)trackDistance / _trackPortionLength;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}