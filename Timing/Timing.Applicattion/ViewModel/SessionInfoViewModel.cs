namespace SecondMonitor.Timing.Application.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap;

    using SessionTiming.ViewModel;
    using ViewModels;
    using ViewModels.CarStatus;

    public class SessionInfoViewModel : AbstractViewModel, ISimulatorDataSetViewModel, IPaceProvider
    {
        public const string ViewModelLayoutName = "Session Best and Time Remaining";

        private static readonly TimeSpan SessionRemainingFrequency = TimeSpan.FromSeconds(1);
        private readonly SessionRemainingCalculator _sessionRemainingCalculator;
        private readonly Stopwatch _refreshWatch;

        private string _sessionRemaining;
        private BestTimesSetViewModel _selectedBestTimes;

        public SessionInfoViewModel()
        {
            SessionTiming = null;
            _refreshWatch = Stopwatch.StartNew();
            SessionRemaining = "L1/14";
            _sessionRemainingCalculator = new SessionRemainingCalculator(this);
        }

        public TimeSpan? PlayersPace => SessionTiming?.Player?.Pace;

        public TimeSpan? LeadersPace => SessionTiming?.Leader?.Pace;

        public string SessionRemaining
        {
            get => _sessionRemaining;
            set => SetProperty(ref _sessionRemaining, value);
        }

        public BestTimesSetViewModel SelectedBestTimes
        {
            get => _selectedBestTimes;
            set => SetProperty(ref _selectedBestTimes, value);
        }

        public SessionTimingController SessionTiming { get; set; }

        public Dictionary<string, TimeSpan> GetPaceForDriversMap()
        {
            return SessionTiming == null ? new Dictionary<string, TimeSpan>() : SessionTiming.Drivers.ToDictionary(x => x.Value.DriverId, x => x.Value.Pace);
        }

        public void ApplyDateSet(SimulatorDataSet dataSet)
        {
            if (_refreshWatch.Elapsed < SessionRemainingFrequency)
            {
                return;
            }

            SessionRemaining = GetSessionRemaining(dataSet);
            _refreshWatch.Restart();
        }

        public void Reset()
        {
            _refreshWatch.Restart();
            _sessionRemainingCalculator.Reset();
        }

        private string GetSessionRemaining(SimulatorDataSet dataSet)
        {
            switch (dataSet.SessionInfo.SessionLengthType)
            {
                case SessionLengthType.Na:
                    return "NA";
                case SessionLengthType.Time:
                case SessionLengthType.TimeWithExtraLap:
                {
                    string prefix = dataSet.SessionInfo.SessionLengthType == SessionLengthType.Time ? "Time: " : "Time (+1 Lap): ";
                    string timeRemaining = SessionTiming?.Leader?.DriverInfo.FinishStatus == DriverFinishStatus.Finished ? "Leader Finished" :
                        prefix + _sessionRemainingCalculator.GetTimeRemaining(dataSet).FormatToHoursMinutesSeconds();

                    if (SessionTiming?.Leader != null && dataSet.SessionInfo?.SessionType == SessionType.Race && SessionTiming?.Leader?.Pace != TimeSpan.Zero)
                    {
                        double lapsRemaining = _sessionRemainingCalculator.GetLapsRemaining(dataSet);
                        double totalLaps = Math.Ceiling(lapsRemaining + dataSet.PlayerInfo.CompletedLaps);
                        timeRemaining += $"\nEst. Laps: {(Math.Floor(lapsRemaining * 10) / 10.0):N1} / {totalLaps:N0}";
                    }

                    return timeRemaining;
                }

                case SessionLengthType.Laps:
                {
                    int lapsToGo = dataSet.SessionInfo.TotalNumberOfLaps - dataSet.SessionInfo.LeaderCurrentLap + 1;
                    switch (lapsToGo)
                    {
                        case < 1:
                            return "Leader Finished";
                        case 1:
                            return "Leader on Final Lap";
                    }

                    string lapsToDisplay = lapsToGo < 2000
                        ? lapsToGo.ToString()
                        : "Infinite";
                    if (SessionTiming?.Leader != null && dataSet.SessionInfo?.SessionType == SessionType.Race && SessionTiming?.Leader?.Pace != TimeSpan.Zero)
                    {
                        lapsToDisplay += "\nEst. Time: " + _sessionRemainingCalculator.GetTimeRemaining(dataSet).FormatToHoursMinutesSeconds();
                    }

                    return "Leader laps to go: " + lapsToDisplay;
                }

                default:
                    return "NA";
            }
        }
    }
}