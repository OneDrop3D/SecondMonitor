namespace SecondMonitor.Timing.Application.PitBoard.DataProviders
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.SessionTiming.Drivers;

    using DataModel.BasicProperties;
    using DataModel.Snapshot;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.PitBoard;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    using ViewModels.Settings;

    public class GapPitBoardProvider : AbstractPitBoardDataProvider
    {
        private readonly PitBoardSettingsViewModel _pitBoardSettingsViewModel;
        private readonly Dictionary<string, DriverGapInformation> _lastGapToPlayer;

        public GapPitBoardProvider(ISettingsProvider settingsProvider)
        {
            _pitBoardSettingsViewModel = settingsProvider.DisplaySettingsViewModel.PitBoardSettingsViewModel;
            PitBoard = new RacePitBoardViewModel();
            _lastGapToPlayer = new Dictionary<string, DriverGapInformation>();
        }

        private RacePitBoardViewModel PitBoard { get; }

        public override void OnPlayerCompletedLap(SimulatorDataSet dataSet, IReadOnlyCollection<DriverTiming> driverTimingsModels)
        {
            if (dataSet.SessionInfo.SessionType == SessionType.Race)
            {
                UpdateAndShowBoard(driverTimingsModels);
            }
        }

        private void UpdateAndShowBoard(IReadOnlyCollection<DriverTiming> driverTimingsModels)
        {
            if (!IsStarted)
            {
                return;
            }

            DriverTiming[] orderedDriverTimings = driverTimingsModels.Where(x => x.IsActive).OrderBy(x => x.Position).ToArray();
            DriverTiming player = orderedDriverTimings.FirstOrDefault(x => x.IsPlayer);
            if (player == null)
            {
                return;
            }

            PitBoard.Lap = "L" + (player.CompletedLaps + 1);
            PitBoard.Position = "P" + player.PositionInClass;

            int playerIndex = Array.IndexOf(orderedDriverTimings, player);
            int driverBeforeIndex = playerIndex - 1;
            if (player.PositionInClass == 1 && _pitBoardSettingsViewModel.OnlyPlayerClass)
            {
                driverBeforeIndex = -1;
            }
            else
            {
                while (driverBeforeIndex >= 0 && orderedDriverTimings[driverBeforeIndex].InPits)
                {
                    driverBeforeIndex--;
                }
            }

            if (driverBeforeIndex >= 0)
            {
                DriverTiming driverBefore = orderedDriverTimings[driverBeforeIndex];
                PitBoard.GapAhead = GetGapInformation(driverBefore);
                PitBoard.GapAheadChange = GetGapChange(driverBefore, true);
            }
            else
            {
                PitBoard.GapAhead = string.Empty;
                PitBoard.GapAheadChange = string.Empty;
            }

            int driverAfterIndex = playerIndex + 1;
            if (_pitBoardSettingsViewModel.OnlyPlayerClass &&
                orderedDriverTimings.Where(x => x.CarClassId == player.CarClassId).All(x => x.PositionInClass <= player.PositionInClass))
            {
                driverAfterIndex = int.MaxValue;
            }
            else
            {
                while (driverAfterIndex < orderedDriverTimings.Length && orderedDriverTimings[driverAfterIndex].InPits)
                {
                    driverAfterIndex++;
                }
            }

            if (driverAfterIndex < orderedDriverTimings.Length)
            {
                var driverAfter = orderedDriverTimings[driverAfterIndex];
                PitBoard.GapBehind = GetGapInformation(driverAfter);
                PitBoard.GapBehindChange = GetGapChange(driverAfter, false);
            }
            else
            {
                PitBoard.GapBehind = string.Empty;
                PitBoard.GapBehindChange = string.Empty;
            }

            UpdateGapToPlayer(orderedDriverTimings);
            if (_pitBoardSettingsViewModel.IsEnabled)
            {
                PitBoardController.RequestToShowPitBoard(PitBoard, 1, TimeSpan.FromSeconds(_pitBoardSettingsViewModel.DisplaySeconds));
            }
        }

        private string GetGapChange(DriverTiming driver, bool inverseGap)
        {
            int lapChange;
            TimeSpan gapChange;
            if (_lastGapToPlayer.TryGetValue(driver.DriverId, out DriverGapInformation driverGapInformation))
            {
                lapChange = driver.LapToPlayerDifference - driverGapInformation.LapDifference;
                gapChange = driver.GapToPlayerAbsolute - driverGapInformation.Gap;
            }
            else
            {
                lapChange = driver.LapToPlayerDifference;
                gapChange = driver.GapToPlayerAbsolute.Duration();
            }

            if (inverseGap)
            {
                gapChange = -gapChange;
            }

            return lapChange == 0 ? gapChange.FormatTimeSpanOnlySecondNoMiliseconds(true)
                : $"{lapChange:+#;-#;0}Lap";
        }

        private static string GetGapInformation(DriverTiming driverInfo)
        {
            return driverInfo.LapToPlayerDifference == 0 ? driverInfo.GapToPlayerAbsolute.Duration().FormatTimeSpanOnlySecondNoMiliseconds(false)
                : $"{driverInfo.LapToPlayerDifference}L  {driverInfo.GapToPlayerAbsolute.Duration().FormatTimeSpanOnlySecondNoMiliseconds(false)}";
        }

        private void UpdateGapToPlayer(DriverTiming[] driverTimingViewModels)
        {
            foreach (DriverTiming driverTimingViewModel in driverTimingViewModels)
            {
                if (driverTimingViewModel.IsPlayer)
                {
                    continue;
                }

                if (!_lastGapToPlayer.TryGetValue(driverTimingViewModel.DriverId, out DriverGapInformation gapInformation))
                {
                    gapInformation = new DriverGapInformation(driverTimingViewModel.GapToPlayerAbsolute, driverTimingViewModel.LapToPlayerDifference);
                    _lastGapToPlayer[driverTimingViewModel.DriverId] = gapInformation;
                    continue;
                }

                gapInformation.Gap = driverTimingViewModel.GapToPlayerAbsolute;
                gapInformation.LapDifference = driverTimingViewModel.LapToPlayerDifference;
            }
        }

        public override void Reset()
        {
            _lastGapToPlayer.Clear();
        }
    }
}