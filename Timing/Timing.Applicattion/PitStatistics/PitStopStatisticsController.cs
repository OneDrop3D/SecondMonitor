namespace SecondMonitor.Timing.Application.PitStatistics
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Common.PitStatistics;
    using Common.SessionTiming.Drivers.Lap.PitStop;
    using Common.SessionTiming.Drivers.Lap.SectorTracker;

    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using NLog;

    using SecondMonitor.Contracts.PitStatistics;
    using SecondMonitor.DataModel.Extensions;

    using ViewModels.Controllers;
    using ViewModels.SessionEvents;

    public class PitStopStatisticsController : IPitStopStatisticProvider, IController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IPitStopEvenProvider _pitStopEvenProvider;
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly IPitOutlierFilter _pitOutlierFilter;
        private readonly object _lockObject;
        private readonly List<PitStopInfo> _allPitStop;

        private PitStatistics _currentStatistics;

        public PitStopStatisticsController(IPitStopEvenProvider pitStopEvenProvider, ISessionEventProvider sessionEventProvider, IPitOutlierFilter pitOutlierFilter)
        {
            _allPitStop = new List<PitStopInfo>();
            _pitStopEvenProvider = pitStopEvenProvider;
            _sessionEventProvider = sessionEventProvider;
            _pitOutlierFilter = pitOutlierFilter;
            _lockObject = new object();
            Reset();
        }

        public PitStatistics GetCurrentStatisticsPlayerClass()
        {
            return _currentStatistics;
        }

        public void Reset()
        {
            _allPitStop.Clear();
            _currentStatistics = new PitStatistics(TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, 0);
            //_currentStatistics = new PitStatistics(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(8), 1);
        }

        public Task StartControllerAsync()
        {
            _pitStopEvenProvider.PitStopCompleted += PitStopEvenProviderOnPitStopCompleted;
            return Task.CompletedTask;
        }

        public Task StopControllerAsync()
        {
            _pitStopEvenProvider.PitStopCompleted -= PitStopEvenProviderOnPitStopCompleted;
            return Task.CompletedTask;
        }

        private async void PitStopEvenProviderOnPitStopCompleted(object sender, PitStopArgs e)
        {
            try
            {
                SimulatorDataSet simulatorDataSet = _sessionEventProvider.LastDataSet;
                DriverInfo player = simulatorDataSet.PlayerInfo;
                if (player == null || simulatorDataSet.SessionInfo.SessionType != SessionType.Race
                                   || simulatorDataSet.SessionInfo.SessionPhase != SessionPhase.Green
                                   || string.IsNullOrEmpty(player.CarClassId) || e.PitStop.Driver.CarClassId != player.CarClassId
                                   || e.PitStop.WasUnderFCY)
                {
                    return;
                }

                if (simulatorDataSet.SessionInfo.PitWindow.PitWindowState != PitWindowState.None && !e.PitStop.IsPitWindowStop && e.PitStop.WasDriveThrough)
                {
                    return;
                }

                await AddToStatistics(e.PitStop);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while creating pit statistics");
            }
        }

        private async Task AddToStatistics(PitStopInfo pitStopInfo)
        {
            if (!pitStopInfo.Driver.DriverLapSectorsTracker.TryGetSpecificTracker(out PitTimeLostTracker timeLostTracker))
            {
                Logger.Warn("Cannot find time lost tracker");
                return;
            }

            // some track, like AMS 2 ovals have weird pit exit, when sector tracking will not kick in until the car is back on the fast part of the oval
            // this can take +20seconds
            for (int i = 0; i < 20 && !timeLostTracker.IsLastPitStopTimeLostFilled; i++)
            {
                Logger.Info($"Last Pitstop time lost, for driver {pitStopInfo.Driver.DriverId}, not filled yet, wating");
                await Task.Delay(2000);
            }

            if (!timeLostTracker.IsLastPitStopTimeLostFilled)
            {
                Logger.Warn($"Last Pitstop time lost, for driver {pitStopInfo.Driver.DriverId}, not filled.");
                return;
            }

            TimeSpan timeLost = timeLostTracker.LastPitStopTimeLost;

            Logger.Info($"Pit Stop Statistics : Driver: {pitStopInfo.Driver.DriverId} Total {pitStopInfo.PitStopDuration.FormatTimeSpanOnlySecondNoMiliseconds(false)}, Pit Stall {pitStopInfo.PitStopStallDuration.FormatTimeSpanOnlySecondNoMiliseconds(false)}, Pit Lane {pitStopInfo.PitStopLaneDuration.FormatTimeSpanOnlySecondNoMiliseconds(false)}, Time Lost: {timeLost.FormatTimeSpanOnlySecondNoMiliseconds(false)}");
            if (timeLost < pitStopInfo.PitStopStallDuration)
            {
                Logger.Warn("Last Pitstop time lost was less than pit stall duration");
                return;
            }

            pitStopInfo.RaceTimeLost = timeLost;

            lock (_lockObject)
            {
                _allPitStop.Add(pitStopInfo);
                while (_allPitStop.Count > 100)
                {
                    _allPitStop.RemoveAt(0);
                }

                RecalculatePitStatistics();
            }
        }

        private void RecalculatePitStatistics()
        {
            IList<PitStopInfo> eligiblePitStops = _pitOutlierFilter.FilterOutOutliers(_allPitStop);
            double total = 0;
            double stall = 0;
            double lane = 0;
            double timeLost = 0;
            foreach (PitStopInfo pitStopInfo in eligiblePitStops)
            {
                total += pitStopInfo.PitStopDuration.TotalSeconds;
                stall += pitStopInfo.PitStopStallDuration.TotalSeconds;
                lane += pitStopInfo.PitStopLaneDuration.TotalSeconds;
                timeLost += pitStopInfo.RaceTimeLost.TotalSeconds;
            }

            TimeSpan newTotalAverage = TimeSpan.FromSeconds(total / eligiblePitStops.Count);
            TimeSpan newPitStopStallAverage = TimeSpan.FromSeconds(stall / eligiblePitStops.Count);
            TimeSpan newPitStopLaneAverage = TimeSpan.FromSeconds(lane / eligiblePitStops.Count);
            TimeSpan newTimeLost = TimeSpan.FromSeconds(timeLost / eligiblePitStops.Count);

            _currentStatistics = new PitStatistics(newTotalAverage, newPitStopStallAverage, newPitStopLaneAverage, newTimeLost, eligiblePitStops.Count);
            Logger.Info(
                $"New Pit Stop Statistics : Total Average {newTotalAverage.FormatTimeSpanOnlySecondNoMiliseconds(false)}, Pit Stall Average {newPitStopStallAverage.FormatTimeSpanOnlySecondNoMiliseconds(false)}, Pit Lane Average {newPitStopLaneAverage.FormatTimeSpanOnlySecondNoMiliseconds(false)}, Total Time Lost: {newTimeLost.FormatTimeSpanOnlySecondNoMiliseconds(false)} Total Entries: {eligiblePitStops.Count}");
        }
    }
}