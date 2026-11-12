namespace SecondMonitor.Timing.Application.SessionTiming.Drivers.Presentation.ViewModel.PitStops
{
    using System;

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

    public class NoPitWindowPitViewModel : AbstractViewModel<DriverTiming>, IPitStopInfoViewModel
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

        public NoPitWindowPitViewModel(ISettingsProvider settingsProvider)
        {
            _displaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
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

        public bool IsPlayer
        {
            get => _isPlayer;
            set => SetProperty(ref _isPlayer, value);
        }

        public bool HasValidInformation
        {
            get => _hasValidInformation;
            private set => SetProperty(ref _hasValidInformation, value);
        }

        public override DriverTiming SaveToNewModel()
        {
            throw new NotSupportedException();
        }

        public bool CanBeUsed(SimulatorDataSet dataSet)
        {
            return dataSet.SessionInfo.SessionType != SessionType.Race || dataSet.SessionInfo.PitWindow.PitWindowState == PitWindowState.None;
        }

        public void UpdatePitInformation(DriverTiming driverTiming, SimulatorDataSet dataSet)
        {
            IsPlayer = driverTiming.IsPlayer;
            TyreCompound = driverTiming.DriverInfo.CarInfo?.WheelsInfo?.FrontLeft?.TyreVisualType ?? string.Empty;
            UpdatePitInfo(driverTiming);
        }

        protected override void ApplyModel(DriverTiming driverTiming)
        {
            IsPlayer = driverTiming.IsPlayer;
            TyreCompound = driverTiming.DriverInfo.CarInfo?.WheelsInfo?.FrontLeft?.TyreVisualType ?? string.Empty;
            UpdatePitInfo(driverTiming);
        }

        private void UpdatePitInfo(DriverTiming driverTiming)
        {
            if (driverTiming.DriverInfo.FinishStatus != DriverFinishStatus.None)
            {
                HasValidInformation = false;
                return;
            }

            if (driverTiming.Session.SessionType != SessionType.Race)
            {
                HasValidInformation = true;
                IsPitStopCountShown = false;
                PitInfoBriefDescription = PitInfoBriefDescriptionKind.None;
                LastPitInfo = driverTiming.InPits ? "In Pits" : string.Empty;
                return;
            }

            UpdatePitInfoRace(driverTiming);
        }

        private void UpdatePitInfoRace(DriverTiming driverTiming)
        {
            int pitStopCount = driverTiming.PitCount;
            int playerPitStopCount = driverTiming.Session.Player?.PitCount ?? 0;
            PitStopCount = pitStopCount.ToString();
            int pitStopPlayerDifference = pitStopCount - playerPitStopCount;

            IsPitStopCountShown = true;

            if (pitStopPlayerDifference == 0)
            {
                PitStopCountGeneralization = PitStopCountGeneralizationKind.SameAsPlayer;
            }
            else if (pitStopPlayerDifference < 0)
            {
                PitStopCountGeneralization = PitStopCountGeneralizationKind.LessThanPlayer;
            }
            else
            {
                PitStopCountGeneralization = PitStopCountGeneralizationKind.MoreThanPLayer;
            }

            if (driverTiming.InPits && driverTiming.LastPitStop != null)
            {
                if (driverTiming.LastPitStop.Phase == PitPhase.Entry)
                {
                    PitInfoBriefDescription = PitInfoBriefDescriptionKind.PitEntry;
                }
                else if (driverTiming.LastPitStop.Phase == PitPhase.InPits)
                {
                    PitInfoBriefDescription = PitInfoBriefDescriptionKind.InPits;
                }
                else
                {
                    PitInfoBriefDescription = PitInfoBriefDescriptionKind.PitExit;
                }

                LastPitInfo = driverTiming.LastPitStop.PitInfoFormatted;
                HasValidInformation = true;
                return;
            }

            if (driverTiming.LastPitStop?.EntryLap == null || driverTiming.CurrentLap.LapNumber - driverTiming.LastPitStop.EntryLap.LapNumber > 4)
            {
                int tyreAge = driverTiming.TyresAge;
                int playersTyreAge = (driverTiming.Session?.Player?.TyresAge).GetValueOrDefault();
                int tyreAgeDifference = tyreAge - playersTyreAge;
                if (tyreAgeDifference > 8)
                {
                    PitInfoBriefDescription = PitInfoBriefDescriptionKind.TyresOlderThanPlayer;
                }
                else if (tyreAgeDifference < -8)
                {
                    PitInfoBriefDescription = PitInfoBriefDescriptionKind.TyresYoungerThanPlayers;
                }
                else
                {
                    PitInfoBriefDescription = PitInfoBriefDescriptionKind.None;
                }

                LastPitInfo = $"{tyreAge}L";
                HasValidInformation = driverTiming.PitCount > 0;
                return;
            }

            if (driverTiming.IsPlayer || driverTiming.Session?.Player?.LastPitStop == null)
            {
                PitInfoBriefDescription = PitInfoBriefDescriptionKind.None;
                LastPitInfo = FormatLatPitStop(driverTiming);
                HasValidInformation = driverTiming.PitCount > 0;
                return;
            }

            TimeSpan pitStopDifference = driverTiming.LastPitStop.PitStopDuration - driverTiming.Session.Player.LastPitStop.PitStopDuration;
            if (pitStopDifference.TotalSeconds > 1)
            {
                PitInfoBriefDescription = PitInfoBriefDescriptionKind.PitStopSlowerThanPlayer;
            }
            else if (pitStopDifference.TotalSeconds < -1)
            {
                PitInfoBriefDescription = PitInfoBriefDescriptionKind.PitStopFasterThanPlayer;
            }
            else
            {
                PitInfoBriefDescription = PitInfoBriefDescriptionKind.None;
            }

            HasValidInformation = true;
            LastPitInfo = _displaySettingsViewModel.ShowPitStopTimeRelative ?
                $"{pitStopDifference.FormatTimeSpanOnlySecondNoMiliseconds(true)}s"
                : FormatLatPitStop(driverTiming);
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
