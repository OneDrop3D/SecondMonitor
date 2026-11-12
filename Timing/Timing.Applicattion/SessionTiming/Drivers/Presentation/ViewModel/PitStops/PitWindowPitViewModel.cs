namespace SecondMonitor.Timing.Application.SessionTiming.Drivers.Presentation.ViewModel.PitStops
{
    using System;
    using System.Linq;

    using SecondMonitor.Contracts.PitStatistics;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.Timing.Common.SessionTiming.Drivers;
    using SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap.PitStop;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    public class PitWindowPitViewModel : AbstractViewModel<DriverTiming>, IPitStopInfoViewModel
    {
        private readonly DisplaySettingsViewModel _displaySettingsViewModel;
        private string _tyreCompound;
        private bool _isPitStopCountShown;
        private PitInfoBriefDescriptionKind _pitInfoBriefDescription;
        private PitStopCountGeneralizationKind _pitStopCountGeneralization;
        private string _lastPitInfo;
        private string _pitStopCount;
        private bool _hasValidInformation;
        private bool _isPlayer;

        public PitWindowPitViewModel(ISettingsProvider settingsProvider)
        {
            _displaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
        }

        public bool IsPlayer
        {
            get => _isPlayer;
            set => SetProperty(ref _isPlayer, value);
        }

        public string TyreCompound
        {
            get => _tyreCompound;
            set => SetProperty(ref _tyreCompound, value);
        }

        public bool IsPitStopCountShown
        {
            get => _isPitStopCountShown;
            set => SetProperty(ref _isPitStopCountShown, value);
        }

        public string PitStopCount
        {
            get => _pitStopCount;
            set => SetProperty(ref _pitStopCount, value);
        }

        public PitStopCountGeneralizationKind PitStopCountGeneralization
        {
            get => _pitStopCountGeneralization;
            set => SetProperty(ref _pitStopCountGeneralization, value);
        }

        public PitInfoBriefDescriptionKind PitInfoBriefDescription
        {
            get => _pitInfoBriefDescription;
            set => SetProperty(ref _pitInfoBriefDescription, value);
        }

        public string LastPitInfo
        {
            get => _lastPitInfo;
            private set => SetProperty(ref _lastPitInfo, value);
        }

        public bool HasValidInformation
        {
            get => _hasValidInformation;
            set => SetProperty(ref _hasValidInformation, value);
        }

        public override DriverTiming SaveToNewModel()
        {
            throw new NotSupportedException();
        }

        public bool CanBeUsed(SimulatorDataSet dataSet)
        {
            return dataSet.SessionInfo.SessionType == SessionType.Race && dataSet.SessionInfo.PitWindow.PitWindowState != PitWindowState.None;
        }

        public void UpdatePitInformation(DriverTiming driverTiming, SimulatorDataSet dataSet)
        {
            IsPlayer = driverTiming.IsPlayer;
            TyreCompound = driverTiming.DriverInfo.CarInfo?.WheelsInfo?.FrontLeft?.TyreVisualType ?? string.Empty;
            UpdatePitInfo(driverTiming);
            UpdatePitStopCount(driverTiming, dataSet);
            HasValidInformation = dataSet.SessionInfo.SessionType != SessionType.Race || dataSet.SessionInfo.PitWindow.PitWindowState == PitWindowState.InPitWindow || !driverTiming.PitStops.All(x => x.IsPitWindowStop);
        }

        protected override void ApplyModel(DriverTiming driverTiming)
        {
            IsPlayer = driverTiming.IsPlayer;
            TyreCompound = driverTiming.DriverInfo.CarInfo?.WheelsInfo?.FrontLeft?.TyreVisualType ?? string.Empty;
            UpdatePitInfo(driverTiming);
        }

        private void UpdatePitStopCount(DriverTiming driverTiming, SimulatorDataSet dataSet)
        {
            if (driverTiming == null || dataSet == null || (driverTiming.DriverInfo.FinishStatus != DriverFinishStatus.None && driverTiming.DriverInfo.FinishStatus != DriverFinishStatus.Finished))
            {
                return;
            }

            bool isInPitWindow = dataSet.SessionInfo.PitWindow.PitWindowState == PitWindowState.InPitWindow;
            int pitStopCount = driverTiming.PitCount;
            int windowPitStops = driverTiming.PitStops.Count(x => x.IsPitWindowStop && x.Completed);
            int outOfWindowPitStopCount = pitStopCount - windowPitStops;
            PitStopCount = isInPitWindow ? windowPitStops.ToString() : outOfWindowPitStopCount.ToString();

            if (driverTiming.Session.Player == null)
            {
                PitStopCountGeneralization = PitStopCountGeneralizationKind.SameAsPlayer;
                return;
            }

            int playerPitStopCount = isInPitWindow ? driverTiming.Session.Player.PitStops.Count(x => x.IsPitWindowStop) : driverTiming.Session.Player.PitCount;
            PitStopCount = isInPitWindow ? windowPitStops.ToString() : outOfWindowPitStopCount.ToString();
            int pitStopPlayerDifference = pitStopCount - playerPitStopCount;

            IsPitStopCountShown = isInPitWindow || pitStopCount != windowPitStops || (dataSet.SessionInfo.PitWindow.PitWindowState == PitWindowState.AfterPitWindow && windowPitStops == 0);

            if (isInPitWindow)
            {
                PitStopCount = windowPitStops > 0 ? "✔" : "X";
                PitStopCountGeneralization = windowPitStops == 0 ? PitStopCountGeneralizationKind.MoreThanPLayer : PitStopCountGeneralizationKind.SameAsPlayer;
            }
            else
            {
                if (pitStopPlayerDifference == 0)
                {
                    PitStopCountGeneralization = PitStopCountGeneralizationKind.SameAsPlayer;
                }
                else if (pitStopPlayerDifference < 0 || windowPitStops == 0)
                {
                    PitStopCountGeneralization = PitStopCountGeneralizationKind.LessThanPlayer;
                }
                else
                {
                    PitStopCountGeneralization = PitStopCountGeneralizationKind.MoreThanPLayer;
                }
            }
        }

        private void UpdatePitInfo(DriverTiming driverTiming)
        {
            if (driverTiming.DriverInfo.FinishStatus is DriverFinishStatus.Dnf or DriverFinishStatus.Dq)
            {
                PitInfoBriefDescription = PitInfoBriefDescriptionKind.None;
                LastPitInfo = string.Empty;
                return;
            }

            if (driverTiming.Session.SessionType != SessionType.Race)
            {
                IsPitStopCountShown = false;
                PitInfoBriefDescription = PitInfoBriefDescriptionKind.None;
                LastPitInfo = driverTiming.InPits ? "In Pits" : string.Empty;
                return;
            }

            UpdatePitInfoRace(driverTiming);
        }

        private void UpdatePitInfoRace(DriverTiming driverTiming)
        {
            if (driverTiming.InPits && driverTiming.LastPitStop != null)
            {
                UpdatePitInfoInPits(driverTiming);
                return;
            }

            if (driverTiming.LastPitStop?.EntryLap == null || driverTiming.CurrentLap.LapNumber - driverTiming.LastPitStop.EntryLap.LapNumber > 4)
            {
                CalculateTyreAgeDifference(driverTiming);
                return;
            }

            if (driverTiming.IsPlayer || driverTiming.Session?.Player?.LastPitStop == null)
            {
                PitInfoBriefDescription = PitInfoBriefDescriptionKind.None;
                LastPitInfo = FormatLatPitStop(driverTiming);
                return;
            }

            TimeSpan pitStopDifference = driverTiming.LastPitStop.PitStopDuration - driverTiming.Session.Player.LastPitStop.PitStopDuration;
            PitInfoBriefDescription = pitStopDifference.TotalSeconds switch
            {
                > 1 => PitInfoBriefDescriptionKind.PitStopSlowerThanPlayer,
                < -1 => PitInfoBriefDescriptionKind.PitStopFasterThanPlayer,
                _ => PitInfoBriefDescriptionKind.None
            };

            LastPitInfo = _displaySettingsViewModel.ShowPitStopTimeRelative ?
                $"{pitStopDifference.FormatTimeSpanOnlySecondNoMiliseconds(true)}s"
                : FormatLatPitStop(driverTiming);
        }

        private void CalculateTyreAgeDifference(DriverTiming driverTiming)
        {
            int tyreAge = driverTiming.TyresAge;
            int playersTyreAge = (driverTiming.Session?.Player?.TyresAge).GetValueOrDefault();
            int tyreAgeDifference = tyreAge - playersTyreAge;
            PitInfoBriefDescription = tyreAgeDifference switch
            {
                > 8 => PitInfoBriefDescriptionKind.TyresOlderThanPlayer,
                < -8 => PitInfoBriefDescriptionKind.TyresYoungerThanPlayers,
                _ => PitInfoBriefDescriptionKind.None
            };

            LastPitInfo = $"{tyreAge}L";
        }

        private void UpdatePitInfoInPits(DriverTiming driverTiming)
        {
            PitInfoBriefDescription = driverTiming.LastPitStop.Phase switch
            {
                PitPhase.Entry => PitInfoBriefDescriptionKind.PitEntry,
                PitPhase.InPits => PitInfoBriefDescriptionKind.InPits,
                _ => PitInfoBriefDescriptionKind.PitExit
            };

            LastPitInfo = driverTiming.LastPitStop.PitInfoFormatted;
        }

        private string FormatLatPitStop(DriverTiming driverTiming)
        {
            var lastPitStop = driverTiming.LastPitStop;
            if (lastPitStop == null)
            {
                return string.Empty;
            }

            switch (_displaySettingsViewModel.PitStopTimeDisplayKind)
            {
                case PitStopTimeDisplayKind.FullStop:
                    return $"{lastPitStop.PitStopDuration.FormatTimeSpanOnlySecondNoMiliseconds(false)}s";
                case PitStopTimeDisplayKind.PitStall:
                    return $"{lastPitStop.PitStopStallDuration.FormatTimeSpanOnlySecondNoMiliseconds(false)}s";
                case PitStopTimeDisplayKind.Both:
                    return $"{lastPitStop.PitStopDuration.FormatTimeSpanOnlySecondNoMiliseconds(false)} / {lastPitStop.PitStopStallDuration.FormatTimeSpanOnlySecondNoMiliseconds(false)}s";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
