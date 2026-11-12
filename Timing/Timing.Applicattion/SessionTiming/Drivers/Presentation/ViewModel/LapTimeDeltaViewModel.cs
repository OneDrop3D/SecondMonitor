namespace SecondMonitor.Timing.Application.SessionTiming.Drivers.Presentation.ViewModel
{
    using System;
    using System.Linq;
    using Common.SessionTiming.Drivers;
    using Common.SessionTiming.Drivers.Lap;

    using DataModel.BasicProperties;

    using SecondMonitor.DataModel.Extensions;

    using ViewModels;

    public class LapTimeDeltaViewModel : AbstractViewModel
    {
        private string _deltaToSessionBest;
        private string _deltaToPersonalBest;
        private string _deltaToClassBest;
        private bool _isCurrentlySessionBest;
        private bool _isCurrentlySessionClassBest;
        private bool _isCurrentlyPersonalBest;
        private string _deltaToCombined;
        private TimeSpan _deltaToPersonalBestSpan;
        private TimeSpan _deltaToSessionBestSpan;
        private string _predictedLapTime;
        private TimeSpan _predictedLapTimeSpan;

        public string DeltaToSessionBest
        {
            get => _deltaToSessionBest;
            set => SetProperty(ref _deltaToSessionBest, value);
        }

        public string DeltaToPersonalBest
        {
            get => _deltaToPersonalBest;
            set => SetProperty(ref _deltaToPersonalBest, value);
        }

        public bool IsCurrentlySessionBest
        {
            get => _isCurrentlySessionBest;
            set => SetProperty(ref _isCurrentlySessionBest, value);
        }

        public bool IsCurrentlySessionClassBest
        {
            get => _isCurrentlySessionClassBest;
            set => SetProperty(ref _isCurrentlySessionClassBest, value);
        }

        public bool IsCurrentlyPersonalBest
        {
            get => _isCurrentlyPersonalBest;
            set => SetProperty(ref _isCurrentlyPersonalBest, value);
        }

        public string DeltaToClassBest
        {
            get => _deltaToClassBest;
            set => SetProperty(ref _deltaToClassBest, value);
        }

        public string DeltaToCombined
        {
            get => _deltaToCombined;
            set => SetProperty(ref _deltaToCombined, value);
        }

        public string PredictedLapTime
        {
            get => _predictedLapTime;
            set => SetProperty(ref _predictedLapTime, value);
        }

        public void Update(DriverTimingViewModel driverTimingViewModel)
        {
            DriverTiming driverTiming = driverTimingViewModel.DriverTiming;
            if (driverTiming.CurrentLap == null || double.IsNaN(driverTiming.CurrentLap.CompletedDistance) || driverTimingViewModel.InPits || driverTiming.CurrentLap.LapTelemetryInfo?.PortionTimes == null)
            {
                IsCurrentlyPersonalBest = false;
                DeltaToPersonalBest = string.Empty;
                IsCurrentlySessionBest = false;
                DeltaToSessionBest = string.Empty;
                IsCurrentlySessionClassBest = false;
                DeltaToClassBest = string.Empty;
                DeltaToCombined = string.Empty;
                PredictedLapTime = string.Empty;
                return;
            }

            TimeSpan lastPortionTime = driverTiming.CurrentLap.LapTelemetryInfo.PortionTimes.GetLastRecordedTime();
            int currentPortionIndex = driverTiming.CurrentLap.LapTelemetryInfo.PortionTimes.LastTrackedPortion;

            if (lastPortionTime == TimeSpan.Zero)
            {
                return;
            }

            UpdateDeltaToPersonal(driverTiming, lastPortionTime, currentPortionIndex);
            int? predictedPosition = GetPredictedPosition(driverTiming, _predictedLapTimeSpan);
            if (_deltaToPersonalBestSpan != TimeSpan.Zero && driverTiming.BestLap != null)
            {
                _predictedLapTimeSpan = driverTiming.BestLap.LapTime + _deltaToPersonalBestSpan;
                string predictedLapTime = driverTiming.CurrentLap.Valid ? TimeSpanFormatHelper.FormatTimeSpan(_predictedLapTimeSpan) : $"(I) {TimeSpanFormatHelper.FormatTimeSpan(_predictedLapTimeSpan)}";
                if (predictedPosition != null)
                {
                    predictedLapTime = $"[P{predictedPosition.Value}] {predictedLapTime}";
                }

                PredictedLapTime = predictedLapTime;
                UpdateDeltaToSession(driverTiming);
                UpdateDeltaToClassBest(driverTiming);
            }

            if (IsCurrentlySessionBest)
            {
                DeltaToCombined = predictedPosition != null ? $"[P{predictedPosition.Value}] {_deltaToSessionBest}" : _deltaToSessionBest;
            }
            else if (IsCurrentlySessionClassBest)
            {
                DeltaToCombined = predictedPosition != null ? $"[P{predictedPosition.Value}] {_deltaToClassBest}" : _deltaToClassBest;
            }
            else
            {
                DeltaToCombined = predictedPosition != null ? $"[P{predictedPosition.Value}] {_deltaToPersonalBest}" : _deltaToPersonalBest;
            }
        }

        private static int? GetPredictedPosition(DriverTiming driverTiming, TimeSpan predictedLapTime)
        {
            if (driverTiming?.Session == null || driverTiming?.BestLap == null || predictedLapTime == TimeSpan.Zero || !driverTiming.CurrentLap.Valid || driverTiming.Session.SessionType == SessionType.Race || predictedLapTime > driverTiming.BestLap.LapTime)
            {
                return null;
            }

            if (driverTiming.Session.GetBestLap(driverTiming.CarClassId) == null)
            {
                return 1;
            }

            DriverTiming beatenDriver = driverTiming.Session.DriversByPosition.FirstOrDefault(x => x.BestLap != null && x.BestLap.LapTime > predictedLapTime);
            if (beatenDriver == null)
            {
                return driverTiming.Session.DriversByPosition.Count;
            }

            if (beatenDriver == driverTiming)
            {
                return driverTiming.PositionInClass;
            }

            return beatenDriver.PositionInClass > driverTiming.PositionInClass ? beatenDriver?.PositionInClass - 1 : beatenDriver.PositionInClass;
        }

        private void UpdateDeltaToPersonal(DriverTiming driverTiming, TimeSpan currentPortionTime, int currentPortionIndex)
        {
            ILapInfo lapToCompare = driverTiming.BestLap;
            TimeSpan bestLapAtDistance = GetLapTimeAtDistance(driverTiming, lapToCompare, currentPortionIndex);
            if (bestLapAtDistance == TimeSpan.Zero)
            {
                IsCurrentlyPersonalBest = false;
                DeltaToPersonalBest = string.Empty;
                return;
            }

            _deltaToPersonalBestSpan = currentPortionTime - bestLapAtDistance;

            DeltaToPersonalBest = TimeSpanFormatHelper.FormatTimeSpanOnlySeconds(_deltaToPersonalBestSpan, true);
            IsCurrentlyPersonalBest = bestLapAtDistance > currentPortionTime;
        }

        private void UpdateDeltaToSession(DriverTiming driverTiming)
        {
            ILapInfo lapToCompare = driverTiming.Session.SessionBestTimesViewModel.BestLap;
            if (lapToCompare == null || lapToCompare.LapTime == TimeSpan.Zero)
            {
                IsCurrentlySessionBest = false;
                DeltaToSessionBest = string.Empty;
                _deltaToSessionBestSpan = TimeSpan.Zero;
                return;
            }

            _deltaToSessionBestSpan = _predictedLapTimeSpan - lapToCompare.LapTime;
            DeltaToSessionBest = TimeSpanFormatHelper.FormatTimeSpanOnlySeconds(_deltaToSessionBestSpan, true);
            IsCurrentlySessionBest = _predictedLapTimeSpan < lapToCompare.LapTime;
        }

        private void UpdateDeltaToClassBest(DriverTiming driverTiming)
        {
            ILapInfo lapToCompare = driverTiming.Session.GetBestTimesForClass(driverTiming.CarClassId).BestLap;
            if (lapToCompare == null || lapToCompare.LapTime == TimeSpan.Zero)
            {
                IsCurrentlySessionBest = false;
                DeltaToSessionBest = string.Empty;
                _deltaToSessionBestSpan = TimeSpan.Zero;
                return;
            }

            DeltaToClassBest = TimeSpanFormatHelper.FormatTimeSpanOnlySeconds(_predictedLapTimeSpan - lapToCompare.LapTime, true);
            IsCurrentlySessionClassBest = _predictedLapTimeSpan < lapToCompare.LapTime;
        }

        private TimeSpan GetLapTimeAtDistance(DriverTiming driverTiming, ILapInfo lapToCompare, int currentPortionIndex)
        {
            if (lapToCompare == null || driverTiming.CurrentLap == null || lapToCompare.LapTelemetryInfo?.IsPortionTimesPurged != false)
            {
                return TimeSpan.Zero;
            }

            TimeSpan bestLapAtDistance = lapToCompare.LapTelemetryInfo.PortionTimes.GetTimeAtPortion(currentPortionIndex);
            return bestLapAtDistance;
        }
    }
}