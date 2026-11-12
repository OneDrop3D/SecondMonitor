namespace SecondMonitor.Timing.Application.SessionTiming.Drivers.ViewModel
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using Common.SessionTiming.Drivers.Lap;
    using ViewModels;

    public class LapPortionTimesComparatorViewModel : AbstractViewModel, IDisposable
    {
        private readonly Stopwatch _refreshWatch;
        private TimeSpan _timeDifference;
        private bool _isVisible;
        private string _title;
        private bool _animateDeltaTimes;
        private ILapInfo _referenceLap;
        private ILapInfo _comparedLap;

        public LapPortionTimesComparatorViewModel()
        {
            _refreshWatch = Stopwatch.StartNew();
            Title = "Δ to Lap";
            TimeDifference = TimeSpan.Zero;
            IsVisible = true;
            SubscribeToComparedLap();
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public ILapInfo ReferenceLap
        {
            get => _referenceLap;
            private set
            {
                _referenceLap = value;
                NotifyPropertyChanged(nameof(HasValidReferenceLap));
            }
        }

        public ILapInfo ComparedLap
        {
            get => _comparedLap;
            private set
            {
                SetProperty(ref _comparedLap, value);
                NotifyPropertyChanged(nameof(HasValidReferenceLap));
            }
        }

        public bool IsPaused { get; set; }

        public TimeSpan TimeDifference
        {
            get => _timeDifference;
            set => SetProperty(ref _timeDifference, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public bool AnimateDeltaTimes
        {
            get => _animateDeltaTimes;
            set => SetProperty(ref _animateDeltaTimes, value);
        }

        public bool HasValidReferenceLap => ComparedLap != null && ReferenceLap != null;

        public void ChangeReferencedLaps(ILapInfo referencedLap, ILapInfo comparedLap)
        {
            if (referencedLap == ReferenceLap && comparedLap == ComparedLap)
            {
                return;
            }

            UnSubscribeToComparedLap();
            ReferenceLap = referencedLap;
            ComparedLap = comparedLap;
            SubscribeToComparedLap();
        }

        private void SubscribeToComparedLap()
        {
            if (ComparedLap?.LapTelemetryInfo?.PortionTimes == null)
            {
                return;
            }

            TimeDifference = TimeSpan.Zero;
            ComparedLap.LapTelemetryInfo.PortionTimes.PropertyChanged += PortionTimes_PropertyChanged;
        }

        private void UnSubscribeToComparedLap()
        {
            if (ComparedLap?.LapTelemetryInfo?.PortionTimes == null)
            {
                return;
            }

            TimeDifference = TimeSpan.Zero;
            ComparedLap.LapTelemetryInfo.PortionTimes.PropertyChanged -= PortionTimes_PropertyChanged;
        }

        private void PortionTimes_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_refreshWatch.Elapsed < TimeSpan.FromMilliseconds(100) || IsPaused)
            {
                return;
            }

            if (ReferenceLap?.LapTelemetryInfo?.PortionTimes == null || ReferenceLap.LapTelemetryInfo.IsPortionTimesPurged)
            {
                TimeDifference = TimeSpan.Zero;
                return;
            }

            if (ReferenceLap.LapTelemetryInfo.PortionTimes.GetTimeAtDistance(ComparedLap.CompletedDistance) != TimeSpan.Zero)
            {
                TimeDifference = ComparedLap.LapTelemetryInfo.PortionTimes.GetTimeAtDistance(ComparedLap.CompletedDistance)
                                 - ReferenceLap.LapTelemetryInfo.PortionTimes.GetTimeAtDistance(ComparedLap.CompletedDistance);
            }
            else
            {
                TimeDifference = TimeSpan.Zero;
            }

            _refreshWatch.Restart();
        }

        public void Dispose()
        {
            UnSubscribeToComparedLap();
        }

        public void ClearComparedLap()
        {
            UnSubscribeToComparedLap();
            ComparedLap = null;
            TimeDifference = TimeSpan.Zero;
        }
    }
}