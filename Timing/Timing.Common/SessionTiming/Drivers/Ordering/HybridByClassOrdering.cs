namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Ordering
{
    using System.Collections.Generic;
    using System.Linq;
    using DataModel.BasicProperties;

    using NLog;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.DataModel.Snapshot.Drivers;

    using ViewModels.SessionEvents;
    using ViewModels.Settings;
    using ViewModels.Settings.Model;
    using ViewModels.Settings.ViewModel;

    public class HybridByClassOrdering : IDriversOrdering
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly DisplaySettingsViewModel _displaySettingsViewModel;
        private readonly ISessionEventProvider _sessionEventProvider;

        public HybridByClassOrdering(ISettingsProvider settingsProvider, ISessionEventProvider sessionEventProvider)
        {
            _displaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
            _sessionEventProvider = sessionEventProvider;
        }

        public string Name => nameof(DriverOrderKind.HybridByClass);

        public List<DriverTiming> Order(ICollection<DriverTiming> driverTimings)
        {
            SessionType sessionType = _sessionEventProvider.LastDataSet.SessionInfo.SessionType;
            driverTimings.ForEach(x =>
            {
                x.DriverTimingVisualization.ClearMarkers();
                x.DriverTimingVisualization.ForceRelativeGapTime = false;
            });

            if (_sessionEventProvider.LastDataSet.PlayerInfo?.InPits != false)
            {
                return driverTimings.GroupBy(x => x.CarClassId)
                    .Select(group => new { Name = group.Key, Drivers = group.OrderBy(x => x.Position) })
                    .OrderBy(x => x.Drivers.First().Position).SelectMany(x => x.Drivers).ToList();
            }

            List<DriverTiming> driverOrderedByGroup = driverTimings.Where(x => !IsRelativeTiming(x)).GroupBy(x => x.CarClassId)
                .Select(group => new { Name = group.Key, Drivers = group.OrderBy(x => x.Position) })
                .OrderBy(x => x.Drivers.First().Position).SelectMany(x => x.Drivers).ToList();

            List<DriverTiming> driversForRelative = driverTimings.Where(IsRelativeTiming).OrderBy(x => x.DistanceToPlayer).ToList();
            bool addMarker = sessionType != SessionType.Race || driversForRelative.Any(x => x.IsLappingPlayer || x.IsLappedByPlayer);
            driversForRelative.ForEach(x => x.DriverTimingVisualization.ForceRelativeGapTime = true);

            List<DriverTiming> combinedList = new();
            foreach (DriverTiming driverTiming in driverOrderedByGroup)
            {
                if (driverTiming.IsPlayer)
                {
                    if (addMarker && driversForRelative.Count > 0 && _displaySettingsViewModel.IsHybridModeGapEnabled)
                    {
                        DriverTiming driverToMark = driversForRelative[0].DistanceToPlayer < 0 ? driversForRelative[0] : driverTiming;
                        driverToMark.DriverTimingVisualization.SetMarkerBefore(_displaySettingsViewModel.HybridModeGapHeight);
                    }

                    combinedList.AddRange(driversForRelative.Where(x => x.DistanceToPlayer <= 0));
                }

                combinedList.Add(driverTiming);

                if (driverTiming.IsPlayer)
                {
                    combinedList.AddRange(driversForRelative.Where(x => x.DistanceToPlayer > 0));
                    if (addMarker && driversForRelative.Count > 0 && _displaySettingsViewModel.IsHybridModeGapEnabled)
                    {
                        combinedList[combinedList.Count - 1].DriverTimingVisualization.SetMarkerAfter(_displaySettingsViewModel.HybridModeGapHeight);
                    }
                }
            }

            //Fallback if something went wrong and some drivers are missing / duplicate, so the app doesn't crash
            if (combinedList.Count != driverTimings.Count)
            {
                Logger.Error($"Drivers count missmatc, total drivers: {driverTimings.Count}, Ordered drives: {combinedList.Count}");
                return driverTimings.GroupBy(x => x.CarClassId)
                    .Select(group => new { Name = group.Key, Drivers = group.OrderBy(x => x.Position) })
                    .OrderBy(x => x.Drivers.First().Position).SelectMany(x => x.Drivers).ToList();
            }

            return combinedList;
        }

        private bool IsRelativeTiming(DriverTiming driverTiming)
        {
            return !driverTiming.IsPlayer && !driverTiming.InPits && driverTiming.GapToPlayerRelative.TotalSeconds >= -_displaySettingsViewModel.HybridModeRelativeInFront &&
                   driverTiming.GapToPlayerRelative.TotalSeconds <= _displaySettingsViewModel.HybridModeRelativeInBehind &&
                   driverTiming.DriverInfo.FinishStatus != DriverFinishStatus.Dnf && driverTiming.DriverInfo.FinishStatus != DriverFinishStatus.Dnq;
            //return !driverTiming.IsPlayer && (driverTiming.IsLappingPlayer || driverTiming.IsLappedByPlayer) && Math.Abs(driverTiming.GapToPlayerRelative.TotalSeconds) <= 5;
        }
    }
}