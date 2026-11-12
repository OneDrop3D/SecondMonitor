namespace SecondMonitor.Timing.Application.SessionTiming.Drivers.Presentation.ViewModel
{
    using System;
    using Common.SessionTiming.Drivers;
    using Common.SessionTiming.Drivers.Lap;

    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using NLog;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.Timing.Application.SessionTiming.Drivers.Presentation.ViewModel.PitStops;
    using SecondMonitor.Timing.Application.SessionTiming.Drivers.Presentation.ViewModel.Tyres;

    using ViewModels;
    using ViewModels.CarStatus;
    using ViewModels.Factory;

    public class DriverTimingViewModel : AbstractViewModel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly PitStopInfoViewModelFactory _pitStopInfoViewModelFactory;
        private readonly SessionRemainingCalculator _sessionRemainingCalculator;
        private readonly TyresViewModelFactory _tyresViewModelFactory;
        private DriverTiming _driverTiming;
        private ColorDto _outLineColor;
        private ColorDto _classIndicationBrush;
        private IPitStopInfoViewModel _pitStopInfo;
        private bool _colorLapsColumns;
        private bool _isClassIndicationEnabled;
        private bool _isLastPLayerLapBetter;
        private bool _isPlayersPaceBetter;
        private string _position;
        private string _positionInClass;
        private string _carName;
        private double _gapHeight;
        private double _gapToNextDriver;
        private string _driverLongName;
        private string _driverId;
        private string _completedLaps;
        private string _lastLapTime;
        private string _currentLapProgressTime;
        private string _pace;
        private string _bestLap;
        private string _remark;
        private string _gapColumnText;
        private TimeSpan _gapToPlayer;
        private string _topSpeed;
        private bool _isPlayer;
        private string _driverShortName;
        private string _gapToPrevious;
        private bool _isLapping;
        private bool _inPits;
        private bool _inPitsMoving;
        private bool _isLastLapBestLap;
        private bool _isLastLapTrackRecord;
        private bool _isLastLapBestSessionLap;
        private bool _isLastLapBestClassSessionLap;
        private string _carClassName;
        private string _sector1;
        private string _sector2;
        private string _sector3;
        private string _bestSector1;
        private string _bestSector2;
        private string _bestSector3;
        private bool _isLastSector1PersonalBest;
        private bool _isLastSector2PersonalBest;
        private bool _isLastSector3PersonalBest;
        private bool _isLastSector1ClassSessionBest;
        private bool _isLastSector2ClassSessionBest;
        private bool _isLastSector1SessionBest;
        private bool _isLastSector3ClassSessionBest;
        private bool _isLastSector3SessionBest;
        private bool _isLastSector2SessionBest;
        private string _championshipPoints;
        private string _ratingFormatted;
        private int _reputation;
        private bool _isCausingYellow;
        private bool _isPlayersRatingBetter;
        private bool _isRatingSet;
        private ITyresViewModel _tyresViewModel;
        private int _pushToPass;

        private bool _isLapped;
        private string _teamName;
        private int _carRaceNumber;
        private bool _isCarRaceNumberFilled;
        private bool _isFinished;
        private double _virtualEnergy;

        public DriverTimingViewModel(DriverTiming driverTiming, IViewModelFactory viewModelFactory, PitStopInfoViewModelFactory pitStopInfoViewModelFactory,
            SessionRemainingCalculator sessionRemainingCalculator, TyresViewModelFactory tyresViewModelFactory)
        {
            SectorRelativeViewModel = viewModelFactory.Create<SectorRelativeViewModel>();
            LapTimeDeltaViewModel = viewModelFactory.Create<LapTimeDeltaViewModel>();
            DriverTimingAdditionalVisualizationViewModel = viewModelFactory.Create<DriverTimingAdditionalVisualizationViewModel>();
            _pitStopInfoViewModelFactory = pitStopInfoViewModelFactory;
            _sessionRemainingCalculator = sessionRemainingCalculator;
            _tyresViewModelFactory = tyresViewModelFactory;
            _pitStopInfo = _pitStopInfoViewModelFactory.CreateDefault();
            _tyresViewModel = _tyresViewModelFactory.CreateDefault();
            DriverTiming = driverTiming;
        }

        public LapTimeDeltaViewModel LapTimeDeltaViewModel { get; }

        public SectorRelativeViewModel SectorRelativeViewModel { get; }

        public ColorDto OutLineColor
        {
            get => _outLineColor;
            set
            {
                SetProperty(ref _outLineColor, value);
                NotifyPropertyChanged(nameof(HasCustomOutline));
            }
        }

        public bool HasCustomOutline => _outLineColor != null;

        public ColorDto ClassIndicationBrush
        {
            get => _classIndicationBrush;
            set => SetProperty(ref _classIndicationBrush, value);
        }

        public IPitStopInfoViewModel PitStopInfo
        {
            get => _pitStopInfo;
            set => SetProperty(ref _pitStopInfo, value);
        }

        public bool ColorLapsColumns
        {
            get => _colorLapsColumns;
            private set => SetProperty(ref _colorLapsColumns, value);
        }

        public bool IsClassIndicationEnabled
        {
            get => _isClassIndicationEnabled;
            private set => SetProperty(ref _isClassIndicationEnabled, value);
        }

        public bool IsLastPlayerLapBetter
        {
            get => _isLastPLayerLapBetter;
            private set => SetProperty(ref _isLastPLayerLapBetter, value);
        }

        public bool IsPlayersPaceBetter
        {
            get => _isPlayersPaceBetter;
            private set => SetProperty(ref _isPlayersPaceBetter, value);
        }

        public DriverTiming DriverTiming
        {
            get => _driverTiming;
            set
            {
                if (_driverTiming == value)
                {
                    return;
                }

                _driverTiming = value;
                InitializeOneTimeValues();
            }
        }

        public string Position
        {
            get => _position;
            private set => SetProperty(ref _position, value);
        }

        public string PositionInClass
        {
            get => _positionInClass;
            private set => SetProperty(ref _positionInClass, value);
        }

        public string CarName
        {
            get => _carName;
            private set => SetProperty(ref _carName, value);
        }

        public double GapHeight
        {
            get => _gapHeight;
            set => SetProperty(ref _gapHeight, value);
        }

        public double GapToNextDriver
        {
            get => _gapToNextDriver;
            set => SetProperty(ref _gapToNextDriver, value);
        }

        public string DriverShortName
        {
            get => _driverShortName;
            private set => SetProperty(ref _driverShortName, value);
        }

        public string DriverLongName
        {
            get => _driverLongName;
            private set => SetProperty(ref _driverLongName, value);
        }

        public string DriverId
        {
            get => _driverId;
            private set => SetProperty(ref _driverId, value);
        }

        public string CompletedLaps
        {
            get => _completedLaps;
            private set => SetProperty(ref _completedLaps, value);
        }

        public string LastLapTime
        {
            get => _lastLapTime;
            private set => SetProperty(ref _lastLapTime, value);
        }

        public string CurrentLapProgressTime
        {
            get => _currentLapProgressTime;
            private set => SetProperty(ref _currentLapProgressTime, value);
        }

        public string Pace
        {
            get => _pace;
            private set => SetProperty(ref _pace, value);
        }

        public string BestLap
        {
            get => _bestLap;
            private set => SetProperty(ref _bestLap, value);
        }

        public string Remark
        {
            get => _remark;
            private set => SetProperty(ref _remark, value);
        }

        public string GapToColumnText
        {
            get => _gapColumnText;
            private set => SetProperty(ref _gapColumnText, value);
        }

        public TimeSpan GapToPlayer
        {
            get => _gapToPlayer;
            private set => SetProperty(ref _gapToPlayer, value);
        }

        public string GapToPrevious
        {
            get => _gapToPrevious;
            private set => SetProperty(ref _gapToPrevious, value);
        }

        public string TopSpeed
        {
            get => _topSpeed;
            private set => SetProperty(ref _topSpeed, value);
        }

        public bool IsPlayer
        {
            get => _isPlayer;
            private set => SetProperty(ref _isPlayer, value);
        }

        public bool IsLapped
        {
            get => _isLapped;
            private set => SetProperty(ref _isLapped, value);
        }

        public bool IsLapping
        {
            get => _isLapping;
            private set => SetProperty(ref _isLapping, value);
        }

        public bool InPits
        {
            get => _inPits;
            private set => SetProperty(ref _inPits, value);
        }

        public bool InPitsMoving
        {
            get => _inPitsMoving;
            private set => SetProperty(ref _inPitsMoving, value);
        }

        public bool IsLastLapBestLap
        {
            get => _isLastLapBestLap;
            private set => SetProperty(ref _isLastLapBestLap, value);
        }

        public bool IsLastLapTrackRecord
        {
            get => _isLastLapTrackRecord;
            private set => SetProperty(ref _isLastLapTrackRecord, value);
        }

        public bool IsLastLapBestSessionLap
        {
            get => _isLastLapBestSessionLap;
            private set => SetProperty(ref _isLastLapBestSessionLap, value);
        }

        public bool IsLastLapBestClassSessionLap
        {
            get => _isLastLapBestClassSessionLap;
            private set => SetProperty(ref _isLastLapBestClassSessionLap, value);
        }

        public string CarClassName
        {
            get => _carClassName;
            private set => SetProperty(ref _carClassName, value);
        }

        public string BestSector1
        {
            get => _bestSector1;
            set => SetProperty(ref _bestSector1, value);
        }

        public string BestSector2
        {
            get => _bestSector2;
            set => SetProperty(ref _bestSector2, value);
        }

        public string BestSector3
        {
            get => _bestSector3;
            set => SetProperty(ref _bestSector3, value);
        }

        public string Sector1
        {
            get => _sector1;
            private set => SetProperty(ref _sector1, value);
        }

        public string Sector2
        {
            get => _sector2;
            private set => SetProperty(ref _sector2, value);
        }

        public string Sector3
        {
            get => _sector3;
            private set => SetProperty(ref _sector3, value);
        }

        public bool IsLastSector1PersonalBest
        {
            get => _isLastSector1PersonalBest;
            private set => SetProperty(ref _isLastSector1PersonalBest, value);
        }

        public bool IsLastSector2PersonalBest
        {
            get => _isLastSector2PersonalBest;
            private set => SetProperty(ref _isLastSector2PersonalBest, value);
        }

        public bool IsLastSector3PersonalBest
        {
            get => _isLastSector3PersonalBest;
            private set => SetProperty(ref _isLastSector3PersonalBest, value);
        }

        public bool IsLastSector1ClassSessionBest
        {
            get => _isLastSector1ClassSessionBest;
            private set => SetProperty(ref _isLastSector1ClassSessionBest, value);
        }

        public bool IsLastSector2ClassSessionBest
        {
            get => _isLastSector2ClassSessionBest;
            private set => SetProperty(ref _isLastSector2ClassSessionBest, value);
        }

        public bool IsLastSector3ClassSessionBest
        {
            get => _isLastSector3ClassSessionBest;
            private set => SetProperty(ref _isLastSector3ClassSessionBest, value);
        }

        public bool IsLastSector1SessionBest
        {
            get => _isLastSector1SessionBest;
            private set => SetProperty(ref _isLastSector1SessionBest, value);
        }

        public bool IsLastSector2SessionBest
        {
            get => _isLastSector2SessionBest;
            private set => SetProperty(ref _isLastSector2SessionBest, value);
        }

        public bool IsLastSector3SessionBest
        {
            get => _isLastSector3SessionBest;
            private set => SetProperty(ref _isLastSector3SessionBest, value);
        }

        public int Rating { get; private set; }

        public string ChampionshipPoints
        {
            get => _championshipPoints;
            set => SetProperty(ref _championshipPoints, value);
        }

        public string RatingFormatted
        {
            get => _ratingFormatted;
            private set => SetProperty(ref _ratingFormatted, value);
        }

        public bool IsCausingYellow
        {
            get => _isCausingYellow;
            private set => SetProperty(ref _isCausingYellow, value);
        }

        public bool IsPlayersRatingBetter
        {
            get => _isPlayersRatingBetter;
            set => SetProperty(ref _isPlayersRatingBetter, value);
        }

        public bool IsRatingSet
        {
            get => _isRatingSet;
            set => SetProperty(ref _isRatingSet, value);
        }

        public int PushToPass
        {
            get => _pushToPass;
            set => SetProperty(ref _pushToPass, value);
        }

        public int Reputation
        {
            get => _reputation;
            set => SetProperty(ref _reputation, value);
        }

        public string TeamName
        {
            get => _teamName;
            set => SetProperty(ref _teamName, value);
        }

        public int CarRaceNumber
        {
            get => _carRaceNumber;
            set => SetProperty(ref _carRaceNumber, value);
        }

        public bool IsCarRaceNumberFilled
        {
            get => _isCarRaceNumberFilled;
            set => SetProperty(ref _isCarRaceNumberFilled, value);
        }

        public ITyresViewModel TyresViewModel
        {
            get => _tyresViewModel;
            set => SetProperty(ref _tyresViewModel, value);
        }

        public bool IsFinished
        {
            get => _isFinished;
            set => SetProperty(ref _isFinished, value);
        }

        public double VirtualEnergy
        {
            get => _virtualEnergy;
            set => SetProperty(ref _virtualEnergy, value);
        }

        public DriverTimingAdditionalVisualizationViewModel DriverTimingAdditionalVisualizationViewModel { get; }

        public void RefreshProperties(SimulatorDataSet dataSet, DriverTiming previousDriver)
        {
            try
            {
                if (dataSet == null)
                {
                    return;
                }

                CarName = DriverTiming.CarDisplayName;
                DriverTimingAdditionalVisualizationViewModel.FromModel(DriverTiming.DriverTimingVisualization);
                DriverShortName = DriverTiming.DriverShortName;
                DriverLongName = DriverTiming.DriverLongName;
                Remark = DriverTiming.Remark;
                SetRating();
                Position = DriverTiming.Position.ToString();
                PositionInClass = FormatPositionInClass();
                CompletedLaps = DriverTiming.CompletedLaps.ToString();
                ChampionshipPoints = $"{DriverTiming.ChampionshipPosition}./{DriverTiming.ChampionshipPoints}pt";
                IsCausingYellow = DriverTiming.DriverInfo.IsCausingYellow;
                BestLap = GetBestLap();
                LastLapTime = GetLastLapTime();
                CurrentLapProgressTime = GetCurrentLapProgressTime();
                Pace = GetGapClosingInfo() + GetPace();
                InPits = DriverTiming.InPits;
                InPitsMoving = InPits && DriverTiming.DriverInfo.Speed.InKph > 10;
                TopSpeed = GetTopSpeed().GetValueInUnits(DriverTiming.Session.DisplaySettingsViewModel.VelocityUnits).ToString("N0");
                SetTimeToPlayerProperties(previousDriver);
                IsPlayer = DriverTiming.IsPlayer;
                IsLapped = DriverTiming.IsLappedByPlayer;
                IsLapping = DriverTiming.IsLappingPlayer;
                IsLastLapBestSessionLap = DriverTiming.IsLastLapBestSessionLap;
                IsLastLapBestClassSessionLap = DriverTiming.IsLastLapBestClassSessionLap;
                IsLastLapBestLap = DriverTiming.IsLastLapBestLap;
                IsLastLapTrackRecord = DriverTiming.IsLastLapTrackRecord;
                IsClassIndicationEnabled = DriverTiming.Session?.LastSet?.SessionInfo?.IsMultiClass == true;
                IsFinished = DriverTiming.DriverInfo.FinishStatus == DriverFinishStatus.Finished;
                VirtualEnergy = DriverTiming.DriverInfo.CarInfo.FuelSystemInfo.HasVirtualTank
                    ? DriverTiming.DriverInfo.CarInfo.FuelSystemInfo.VirtualTank.InPercentage : -1;

                Sector1 = GetSector1();
                Sector2 = GetSector2();
                Sector3 = GetSector3();

                FillBestSectors();

                IsLastSector1SessionBest = DriverTiming.IsLastSector1SessionBest;
                IsLastSector2SessionBest = DriverTiming.IsLastSector2SessionBest;
                IsLastSector3SessionBest = DriverTiming.IsLastSector3SessionBest;
                IsLastSector1ClassSessionBest = DriverTiming.IsLastSector1ClassSessionBest;
                IsLastSector2ClassSessionBest = DriverTiming.IsLastSector2ClassSessionBest;
                IsLastSector3ClassSessionBest = DriverTiming.IsLastSector3ClassSessionBest;
                IsLastSector1PersonalBest = DriverTiming.IsLastSector1PersonalBest;
                IsLastSector2PersonalBest = DriverTiming.IsLastSector2PersonalBest;
                IsLastSector3PersonalBest = DriverTiming.IsLastSector3PersonalBest;
                ColorLapsColumns = GetColorLapsColumns();
                IsLastPlayerLapBetter = GetIsLastPlayerLapBetter();
                IsPlayersPaceBetter = GetIsPlayersPaceBetter();
                Reputation = (int)DriverTiming.DriverInfo.RatingInfo.Reputation;
                TeamName = DriverTiming.DriverInfo.TeamName;
                CarRaceNumber = DriverTiming.DriverInfo.CarRaceNumber;
                IsCarRaceNumberFilled = DriverTiming.DriverInfo.IsCarRaceNumberFilled;

                if (!_tyresViewModel.CanBeUSed(DriverTiming))
                {
                    TyresViewModel = _tyresViewModelFactory.Create(DriverTiming);
                }

                TyresViewModel.FromModel(DriverTiming);

                PushToPass = _driverTiming.DriverInfo.CarInfo?.BoostSystem.ActivationsRemaining ?? -1;

                if (_pitStopInfo.CanBeUsed(dataSet))
                {
                    _pitStopInfo.UpdatePitInformation(DriverTiming, dataSet);
                }
                else
                {
                    PitStopInfo = _pitStopInfoViewModelFactory.Create(dataSet);
                    PitStopInfo.UpdatePitInformation(DriverTiming, dataSet);
                }

                SectorRelativeViewModel.Update(this);
                LapTimeDeltaViewModel.Update(this);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private string GetGapClosingInfo()
        {
            if (DriverTiming.GapClosureInformationProvider.LapsToClose == null)
            {
                return string.Empty;
            }

            int lapsToClose = DriverTiming.GapClosureInformationProvider.LapsToClose.Value;
            return lapsToClose - DriverTiming.Session.DisplaySettingsViewModel.GapClosingExtraLaps > Math.Ceiling(_sessionRemainingCalculator.GetLapsRemaining(DriverTiming.Session.LastSet)) ? string.Empty : $"[{lapsToClose}]  ";
        }

        private void SetRating()
        {
            IsRatingSet = DriverTiming.Rating > 0;
            if (IsPlayer || DriverTiming.Rating == 0 || DriverTiming?.Session?.Player == null)
            {
                IsPlayersRatingBetter = true;
                Rating = DriverTiming.Rating;
                RatingFormatted = Rating == 0 ? string.Empty : Rating.ToString();
                return;
            }

            IsPlayersRatingBetter = DriverTiming.Session.Player.Rating >= DriverTiming.Rating;
            Rating = DriverTiming.Rating;
            RatingFormatted = $"{Rating - DriverTiming.Session.Player.Rating:+#;-#;0} / {Rating}";
        }

        private string FormatPositionInClass()
        {
            if (DriverTiming.Session.LastSet?.SessionInfo?.IsMultiClass == false || DriverTiming.PositionInClass <= 0)
            {
                return Position;
            }

            switch (DriverTiming.Session.DisplaySettingsViewModel.MultiClassDisplayKind)
            {
                case MultiClassDisplayKind.OnlyOverall:
                    return Position;
                case MultiClassDisplayKind.OnlyClass:
                    return DriverTiming.PositionInClass.ToString();
                case MultiClassDisplayKind.ClassFirst:
                    return $"{DriverTiming.PositionInClass.ToString()} ({Position})";
                case MultiClassDisplayKind.OverallFirst:
                    return $"{Position} ({DriverTiming.PositionInClass.ToString()})";
                default:
                    return Position;
            }
        }

        private bool GetColorLapsColumns()
        {
            return DriverTiming?.Session.SessionType == SessionType.Race && !DriverTiming.IsPlayer && DriverTiming?.Session?.Player?.CompletedLaps > 0;
        }

        private bool GetIsPlayersPaceBetter()
        {
            if (!ColorLapsColumns)
            {
                return false;
            }

            return DriverTiming.Pace > DriverTiming.Session.Player.Pace;
        }

        private bool GetIsLastPlayerLapBetter()
        {
            if (!ColorLapsColumns || DriverTiming.LastCompletedLap is null || DriverTiming.Session.Player.LastCompletedLap is null)
            {
                return false;
            }

            return DriverTiming.LastCompletedLap.LapTime > DriverTiming.Session.Player.LastCompletedLap.LapTime;
        }

        private void InitializeOneTimeValues()
        {
            CarClassName = DriverTiming.CarClassName;
            DriverShortName = DriverTiming.DriverShortName;
            DriverLongName = DriverTiming.DriverLongName;
            DriverId = DriverTiming.DriverId;
        }

        private string FormatSectorTiming(SectorTiming sectorTiming)
        {
            if (sectorTiming == null)
            {
                return "N/A";
            }

            if (sectorTiming.Lap.Valid)
            {
                return TimeSpanFormatHelper.FormatTimeSpanOnlySeconds(sectorTiming.Duration, false);
            }

            if (!sectorTiming.Lap.Driver.Session.RetrieveAlsoInvalidLaps)
            {
                return "N/A";
            }

            if (sectorTiming.Lap.Driver.Session.SessionType == SessionType.Race || !sectorTiming.Lap.IsPitLap)
            {
                return TimeSpanFormatHelper.FormatTimeSpanOnlySeconds(sectorTiming.Duration, false);
            }

            return "N/A";
        }

        private string GetSector1()
        {
            if (DriverTiming.CurrentLap == null || DriverTiming.DriverInfo.FinishStatus == DriverFinishStatus.Dnf || DriverTiming.DriverInfo.FinishStatus == DriverFinishStatus.Dq)
            {
                return "N/A";
            }

            if (DriverTiming.Session.SessionType != SessionType.Race && (DriverTiming.InPits || DriverTiming.Session.Player?.InPits == true))
            {
                return GetBestSector1();
            }

            var sector = DriverTiming.GetSector1Timing();
            return FormatSectorTiming(sector);
        }

        private string GetSector2()
        {
            if (DriverTiming.CurrentLap == null || DriverTiming.DriverInfo.FinishStatus == DriverFinishStatus.Dnf || DriverTiming.DriverInfo.FinishStatus == DriverFinishStatus.Dq)
            {
                return "N/A";
            }

            if (DriverTiming.Session.SessionType != SessionType.Race && (DriverTiming.InPits || DriverTiming.Session.Player?.InPits == true))
            {
                return GetBestSector2();
            }

            SectorTiming sector = DriverTiming.GetSector2Timing();
            return FormatSectorTiming(sector);
        }

        private string GetSector3()
        {
            if (DriverTiming.CurrentLap == null || DriverTiming.DriverInfo.FinishStatus == DriverFinishStatus.Dnf || DriverTiming.DriverInfo.FinishStatus == DriverFinishStatus.Dq)
            {
                return "N/A";
            }

            if (DriverTiming.Session.SessionType != SessionType.Race && (DriverTiming.InPits || DriverTiming.Session.Player?.InPits == true))
            {
                return GetBestSector3();
            }

            SectorTiming sector = DriverTiming.GetSector3Timing();
            return FormatSectorTiming(sector);
        }

        private void FillBestSectors()
        {
            BestSector1 = GetBestSector1();
            BestSector2 = GetBestSector2();
            BestSector3 = GetBestSector3();
        }

        private string GetBestSector1()
        {
            var sector = DriverTiming.BestSector1;
            return FormatSectorTiming(sector);
        }

        private string GetBestSector2()
        {
            var sector = DriverTiming.BestSector2;
            return FormatSectorTiming(sector);
        }

        private string GetBestSector3()
        {
            var sector = DriverTiming.BestSector3;
            return FormatSectorTiming(sector);
        }

        private string GetLastLapTime()
        {
            DriverInfo driverInfo = DriverTiming.DriverInfo;
            ILapInfo lastCompletedLap = DriverTiming.LastCompletedLap;
            if (lastCompletedLap == null || DriverTiming.DriverInfo.FinishStatus == DriverFinishStatus.Dnf || DriverTiming.DriverInfo.FinishStatus == DriverFinishStatus.Dq)
            {
                return "N/A";
            }

            string toDisplay;
            /*if (!lastCompletedLap.Valid && DriverTiming.Session.SessionType != SessionType.Race)
            {
                return "Lap Invalid";
            }*/

            if (lastCompletedLap.IsPitLap && DriverTiming.Session.SessionType != SessionType.Race)
            {
                return "Out Lap";
            }

            if (driverInfo.IsPlayer || !DriverTiming.Session.DisplayBindTimeRelative || DriverTiming.Session.Player?.LastCompletedLap == null)
            {
                toDisplay = TimeSpanFormatHelper.FormatTimeSpan(lastCompletedLap.LapTime);
            }
            else
            {
                toDisplay = TimeSpanFormatHelper.FormatTimeSpanOnlySeconds(lastCompletedLap.LapTime.Subtract(DriverTiming.Session.Player.LastCompletedLap.LapTime), true);
            }

            return lastCompletedLap.Valid || DriverTiming.Session.SessionType != SessionType.Race ? toDisplay : "(I) " + toDisplay;
        }

        private string GetCurrentLapProgressTime()
        {
            if (DriverTiming.CurrentLap == null || DriverTiming.DriverInfo.FinishStatus == DriverFinishStatus.Dnf || DriverTiming.DriverInfo.FinishStatus == DriverFinishStatus.Dq)
            {
                return string.Empty;
            }

            TimeSpan progress = DriverTiming.CurrentLap.CurrentlyValidProgressTime;
            if (DriverTiming.Session.SessionType != SessionType.Race && (DriverTiming.CurrentLap.IsPitLap || DriverTiming.InPits))
            {
                return DriverTiming.InPits ? "In Pits" : "Out Lap";
            }

            if (!DriverTiming.CurrentLap.Valid && !DriverTiming.InPits && DriverTiming.Session.RetrieveAlsoInvalidLaps)
            {
                return "(I) " + TimeSpanFormatHelper.FormatTimeSpan(progress);
            }

            if (!DriverTiming.CurrentLap.Valid && !DriverTiming.Session.RetrieveAlsoInvalidLaps)
            {
                return DriverTiming.Session.SessionType == SessionType.Race ? "Lap Invalid" : DriverTiming.InPits ? "In Pits" : "Out Lap";
            }

            return DriverTiming.CurrentLap.Valid
                ? TimeSpanFormatHelper.FormatTimeSpan(progress)
                : "(I) " + TimeSpanFormatHelper.FormatTimeSpan(progress);
        }

        private string GetPace()
        {
            if (DriverTiming.Session.Player == null)
            {
                return string.Empty;
            }

            if (DriverTiming.DriverInfo.IsPlayer || !DriverTiming.Session.DisplayBindTimeRelative || DriverTiming.Session.Player.Pace == TimeSpan.Zero)
            {
                return TimeSpanFormatHelper.FormatTimeSpan(DriverTiming.Pace);
            }
            else
            {
                return TimeSpanFormatHelper.FormatTimeSpanOnlySeconds(DriverTiming.Pace.Subtract(DriverTiming.Session.Player.Pace), true);
            }
        }

        private string GetBestLap()
        {
            if (DriverTiming.BestLap == null)
            {
                return "N/A";
            }

            DriverTiming driverToUse = DriverTiming?.Session?.Player ?? DriverTiming?.Session?.Leader;

            if (driverToUse?.BestLap == null)
            {
                return "L" + DriverTiming.BestLap.LapNumber + "/" + TimeSpanFormatHelper.FormatTimeSpan(DriverTiming.BestLap.LapTime);
            }

            if (DriverTiming.DriverInfo.IsPlayer || !DriverTiming.Session.DisplayBindTimeRelative || driverToUse.BestLap == null)
            {
                return "L" + DriverTiming.BestLap.LapNumber + "/" + TimeSpanFormatHelper.FormatTimeSpan(DriverTiming.BestLap.LapTime);
            }
            else
            {
                return "L" + DriverTiming.BestLap.LapNumber + "/" + GetRelativeBestTime(driverToUse);
            }
        }

        private string GetRelativeBestTime(DriverTiming driverToUse)
        {
            if (DriverTiming?.BestLap == null || driverToUse.BestLap == null)
            {
                return string.Empty;
            }

            return TimeSpanFormatHelper.FormatTimeSpanOnlySeconds(DriverTiming.BestLap.LapTime.Subtract(driverToUse.BestLap.LapTime), true);
        }

        private Velocity GetTopSpeed()
        {
            return DriverTiming.TopSpeed;
        }

        private bool SetTimeToPlayerProperties(DriverTiming driverToUse, Action<string, TimeSpan> setAction)
        {
            if (driverToUse == null || DriverTiming.Session.LastSet == null)
            {
                setAction(string.Empty, TimeSpan.Zero);
                return true;
            }

            if (DriverTiming.DriverInfo.FinishStatus != DriverFinishStatus.None && DriverTiming.DriverInfo.FinishStatus != DriverFinishStatus.Na && DriverTiming.DriverInfo.FinishStatus != DriverFinishStatus.Finished
                                                                                && (DriverTiming.DriverInfo.FinishStatus != DriverFinishStatus.Dns || DriverTiming.DriverInfo.Speed.InKph < 1))
            {
                setAction(DriverTiming.DriverInfo.FinishStatus.ToString(), TimeSpan.Zero);
                return true;
            }

            if (DriverTiming.DriverInfo.DriverSessionId == driverToUse.DriverInfo.DriverSessionId)
            {
                setAction(string.Empty, TimeSpan.Zero);
                return true;
            }

            if (DriverTiming.Session.LastSet.SessionInfo.SessionType != SessionType.Race && !DriverTimingAdditionalVisualizationViewModel.ForceRelativeGapTime)
            {
                setAction(GetRelativeBestTime(driverToUse), TimeSpan.Zero);
                return true;
            }

            double distanceToUse = driverToUse.TotalDistanceTraveled - DriverTiming.TotalDistanceTraveled;

            if (!DriverTimingAdditionalVisualizationViewModel.ForceRelativeGapTime && !DriverTiming.Session.DisplayGapToPlayerRelative
                                                                                   && Math.Abs(distanceToUse) > DriverTiming.Session.LastSet.SessionInfo.TrackInfo.LayoutLength.InMeters
                                                                                   && DriverTiming.Session.LastSet.SessionInfo.TrackInfo.LayoutLength.InMeters > 0)
            {
                string gapToColumnText = ((int)distanceToUse / (int)DriverTiming.Session.LastSet.SessionInfo.TrackInfo.LayoutLength.InMeters) + "LAP";
                setAction(gapToColumnText, TimeSpan.Zero);
                return true;
            }

            if (driverToUse.IsPlayer && DriverTiming.DriverInfo.Timing.GapToPlayer != TimeSpan.Zero && !DriverTiming.IsLappedByPlayer && !DriverTiming.IsLappingPlayer)
            {
                TimeSpan gapToPlayer = DriverTiming.DriverInfo.Timing.GapToPlayer;
                string gapToColumnText = GapToPlayer.FormatTimeSpanOnlySecondNoMiliseconds(true);
                setAction(gapToColumnText, gapToPlayer);
                return true;
            }

            return false;
        }

        private void SetTimeToPlayerProperties(DriverTiming previousDriver)
        {
            if (DriverTiming.Session == null)
            {
                return;
            }

            var driverToUse = DriverTiming.Session?.Player ?? DriverTiming.Session?.Leader;

            if (!SetTimeToPlayerProperties(driverToUse, (s, span) =>
                {
                    GapToColumnText = s;
                    GapToPlayer = span;
                }) && driverToUse != null)
            {
                GapToPlayer = driverToUse.IsPlayer ? DriverTiming.GapToPlayerRelative : DriverTiming.GapToLeaderRelative;
                GapToColumnText = GapToPlayer.FormatTimeSpanOnlySecondNoMiliseconds(true);
            }

            if (SetTimeToPlayerProperties(previousDriver, (s, _) => { GapToPrevious = s; }) || previousDriver == null)
            {
                return;
            }

            if (IsPlayer && previousDriver.DriverInfo.Timing.GapToPlayer != TimeSpan.Zero && !previousDriver.IsLappingPlayer && !previousDriver.IsLappedByPlayer)
            {
                GapToPrevious = (-previousDriver.DriverInfo.Timing.GapToPlayer).FormatTimeSpanOnlySecondNoMiliseconds(true);
            }
            else if (driverToUse?.IsPlayer == true && DriverTiming.DriverInfo.Timing.GapToPlayer != TimeSpan.Zero && !DriverTiming.IsBeingLappedBy(previousDriver) && !DriverTiming.IsLapping(previousDriver))
            {
                GapToPrevious = (DriverTiming.DriverInfo.Timing.GapToPlayer - previousDriver.DriverInfo.Timing.GapToPlayer).FormatTimeSpanOnlySecondNoMiliseconds(true);
            }
            else
            {
                GapToPrevious = (DriverTiming.Session.DisplayGapToPlayerRelative ? DriverTiming.DriverLapSectorsTracker.GetGapToDriverRelative(previousDriver) : DriverTiming.DriverLapSectorsTracker.GetGapToDriverAbsolute(previousDriver))
                    .FormatTimeSpanOnlySecondNoMiliseconds(true);
            }
        }
    }
}