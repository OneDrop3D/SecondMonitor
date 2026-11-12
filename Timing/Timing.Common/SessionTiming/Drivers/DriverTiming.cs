namespace SecondMonitor.Timing.Common.SessionTiming.Drivers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using Lap;
    using Lap.PitStop;
    using Lap.SectorTracker;
    using NLog;
    using PitStatistics;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap.FinishCondition;

    using ViewModels.Settings;

    public class DriverTiming
    {
        private const int MaxLapsWithTelemetry = 2;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ILapEventProvider _lapEventProvider;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IPitStopEvenProvider _pitStopEvenProvider;
        private readonly IList<ILapFinishCondition> _lapFinishConditions;
        private readonly object _lapsObjectLock;
        private readonly Stopwatch _inactiveTimer;

        private readonly Stopwatch _refreshBestSectorIndicationWatch;
        private readonly Velocity _maximumVelocity = Velocity.FromMs(185);
        private readonly List<ILapInfo> _lapsInfo;
        private readonly List<PitStopInfo> _pitStopInfo;
        private readonly Stopwatch _bestLapCheckTimer;
        private double _previousTickLapDistance;
        private bool _inLappedRange;
        private bool _inLappingRange;

        private ILapInfo _bestLap;

        public DriverTiming(DriverInfo driverInfo, ISessionInfo session, DriverLapSectorsTrackerFactory driverLapSectorsTrackerFactory,
            ILapEventProvider lapEventProvider, ISettingsProvider settingsProvider, IPitStopEvenProvider pitStopEvenProvider,
            IGapClosureInformationProvider gapClosureInformationProvider, IList<ILapFinishCondition> lapFinishConditions)
        {
            _bestLapCheckTimer = Stopwatch.StartNew();
            _lapEventProvider = lapEventProvider;
            _settingsProvider = settingsProvider;
            _pitStopEvenProvider = pitStopEvenProvider;
            _lapFinishConditions = lapFinishConditions;
            GapClosureInformationProvider = gapClosureInformationProvider;
            _inactiveTimer = new Stopwatch();
            _lapsObjectLock = new object();
            _refreshBestSectorIndicationWatch = Stopwatch.StartNew();
            _lapsInfo = new List<ILapInfo>();
            _pitStopInfo = new List<PitStopInfo>();
            DriverInfo = driverInfo;
            Pace = TimeSpan.Zero;
            LapPercentage = 0;
            _previousTickLapDistance = 0;
            Session = session;
            DriverLapSectorsTracker = driverLapSectorsTrackerFactory.Build(this);
            DriverTimingVisualization = new DriverTimingVisualization();
        }

        public event EventHandler<LapEventArgs> NewLapStarted;
        public event EventHandler<LapEventArgs> LapInvalidated;
        public event EventHandler<LapEventArgs> LapCompleted;
        public event EventHandler<LapEventArgs> LapTimeReevaluated;

        public event EventHandler<SectorCompletedArgs> SectorCompletedEvent;

        public IGapClosureInformationProvider GapClosureInformationProvider { get; }

        public bool InvalidateFirstLap { get; set; }

        public ISessionInfo Session { get; }

        public DriverInfo DriverInfo { get; private set; }

        public bool IsPlayer => DriverInfo.IsPlayer;

        public string DriverShortName => DriverInfo.DriverShortName;

        public string DriverLongName => DriverInfo.DriverLongName;

        public string DriverId => DriverInfo.DriverSessionId;

        public int Position => DriverInfo.Position;

        public int PositionInClass => DriverInfo.PositionInClass;

        public int CompletedLaps => DriverInfo.CompletedLaps;

        public bool InPits { get; private set; }

        public TimeSpan Pace { get; private set; }

        public double TotalDistanceTraveled => DriverInfo.TotalDistance;

        public bool IsLappedByPlayer { get; private set; }

        public bool IsLappingPlayer { get; private set; }

        public double DistanceToPlayer { get; private set; }

        public ILapInfo BestLap
        {
            get => _bestLap;
            private set
            {
                if (_bestLap == value)
                {
                    return;
                }

                _lapEventProvider.NotifyBestLapPersonalChanged(this, _bestLap, value);
                _bestLap = value;
                Logger.Info($"New Best Lap For Driver {DriverId}, Lap Num: {BestLap.LapNumber}, Lap Time:{BestLap.LapTime.FormatToDefault()}");
            }
        }

        public int PitCount => _pitStopInfo.Count(x => x.Completed);

        public PitStopInfo LastPitStop => _pitStopInfo.Count != 0 ? _pitStopInfo[^1] : null;

        public IReadOnlyCollection<PitStopInfo> PitStops => _pitStopInfo.AsReadOnly();

        public double LapPercentage { get; private set; }

        public string CarName => DriverInfo.CarName;

        public string CarDisplayName => DriverInfo.CarDisplayName;

        public string CarClassName => DriverInfo.CarClassName;

        public string CarClassId => DriverInfo.CarClassId;

        public int PaceLaps => Session.PaceLaps;

        public bool IsLastLapBestLap => BestLap != null && BestLap == LastLap;

        public bool IsLastLapBestClassSessionLap { get; private set; }

        public bool IsActive { get; private set; } = true;

        public TimeSpan InactiveTime => _inactiveTimer.Elapsed;

        public SectorTiming BestSector1 { get; private set; }

        public SectorTiming BestSector2 { get; private set; }

        public SectorTiming BestSector3 { get; private set; }

        public bool IsLastSector1PersonalBest { get; private set; }
        public bool IsLastSector2PersonalBest { get; private set; }
        public bool IsLastSector3PersonalBest { get; private set; }

        public bool IsLastSector1SessionBest { get; private set; }
        public bool IsLastSector2SessionBest { get; private set; }
        public bool IsLastSector3SessionBest { get; private set; }

        public bool IsLastSector1ClassSessionBest { get; private set; }
        public bool IsLastSector2ClassSessionBest { get; private set; }
        public bool IsLastSector3ClassSessionBest { get; private set; }

        public IDriverLapSectorsTracker DriverLapSectorsTracker { get; }

        public DriverTimingVisualization DriverTimingVisualization { get; }

        public IReadOnlyCollection<ILapInfo> Laps
        {
            get
            {
                lock (_lapsObjectLock)
                {
                    return _lapsInfo.AsReadOnly();
                }
            }
        }

        public int TyresAge
        {
            get
            {
                if (CurrentLap == null)
                {
                    return 0;
                }

                var lastValidPitStop = PitStops.LastOrDefault(x => x.Completed && !x.WasDriveThrough);

                return lastValidPitStop?.EntryLap == null ? CurrentLap.LapNumber - 1 : CurrentLap.LapNumber - lastValidPitStop.EntryLap.LapNumber;
            }
        }

        public ILapInfo CurrentLap
        {
            get
            {
                lock (_lapsObjectLock)
                {
                    return _lapsInfo.Count == 0 ? null : _lapsInfo.Last();
                }
            }
        }

        public ILapInfo LastCompletedLap
        {
            get
            {
                lock (_lapsObjectLock)
                {
                    return _lapsInfo.LastOrDefault(x => x.Completed && (x.Valid || Session.RetrieveAlsoInvalidLaps));
                }
            }
        }

        public ILapInfo LastLap
        {
            get
            {
                lock (_lapsObjectLock)
                {
                    return _lapsInfo.Count < 2 ? null : _lapsInfo[_lapsInfo.Count - 2];
                }
            }
        }

        public bool IsLastLapBestSessionLap
        {
            get
            {
                if (LastLap == null)
                {
                    return false;
                }

                return LastLap == Session.GetBestLap();
            }
        }

        public string Remark => DriverInfo.FinishStatus.ToString();

        public string Speed => DriverInfo.Speed.InKph.ToString("N0");

        public Velocity TopSpeed { get; private set; } = Velocity.Zero;

        public int Rating { get; set; }
        public int ChampionshipPoints { get; set; }
        public int ChampionshipPosition { get; set; }
        public bool IsLastLapTrackRecord { get; set; }
        public TimeSpan GapToPlayerRelative { get; private set; }

        public TimeSpan GapToPlayerAbsolute { get; private set; }

        public TimeSpan GapToLeaderRelative { get; private set; }
        public int LapToPlayerDifference { get; private set; }
        internal double PreviousLapDistance => _previousTickLapDistance;

        public bool UpdateLaps(SimulatorDataSet set)
        {
            SessionInfo sessionInfo = set.SessionInfo;
            if (!sessionInfo.IsActive)
            {
                return false;
            }

            if (sessionInfo.SessionPhase == SessionPhase.Countdown)
            {
                return false;
            }

            if (DriverInfo.FinishStatus != DriverFinishStatus.Na && DriverInfo.FinishStatus.IsFinishedRaceStatus() && CurrentLap?.Completed == true)
            {
                return false;
            }

            if (TopSpeed < DriverInfo.Speed && DriverInfo.Speed < _maximumVelocity)
            {
                TopSpeed = DriverInfo.Speed;
            }

            UpdateInPitsProperty(set);
            UpdateLappedProperties(set);
            GapClosureInformationProvider.GenerateClosureInformation(this);

            lock (_lapsObjectLock)
            {
                if (_lapsInfo.Count == 0)
                {
                    if (!DriverInfo.FinishStatus.IsFinishedRaceStatus())
                    {
                        CreateFirstLap(set);
                    }
                    else
                    {
                        return false;
                    }
                }

                if (LastLap?.IsPending == true && !LastLap.UpdatePendingState(set, DriverInfo))
                {
                    Logger.Info("Finishing pending lap");
                    FinishLap(LastLap, set);
                }

                ILapInfo currentLap = CurrentLap;
                if (!currentLap.Completed)
                {
                    UpdateCurrentLap(set);
                }

                if (_refreshBestSectorIndicationWatch.ElapsedMilliseconds > 2000)
                {
                    UpdateBestSectorProperties();
                    _refreshBestSectorIndicationWatch.Restart();
                }

                DriverLapSectorsTracker.Update();

                if (ShouldFinishLap(set, currentLap))
                {
                    if (!currentLap.SwitchToPendingIfNecessary(set, DriverInfo))
                    {
                        FinishLap(currentLap, set);
                    }
                    else
                    {
                        Logger.Info($"Lap {currentLap.LapNumber} Switched to Pending, Driver {DriverInfo.DriverSessionId}");
                    }

                    CreateNewLap(set, currentLap);
                    _previousTickLapDistance = DriverInfo.LapDistance;
                    return currentLap.Valid;
                }

                if (currentLap.Completed)
                {
                    CreateNewLap(set, currentLap);
                }

                if (set.SimulatorSourceInfo.OverrideBestLap)
                {
                    CheckAndOverrideBestLap(sessionInfo.TrackInfo.LayoutLength.InMeters);
                }

                _previousTickLapDistance = DriverInfo.LapDistance;
            }

            return false;
        }

        public void CalculateGapToPLayer()
        {
            if (DriverInfo.FinishStatus == DriverFinishStatus.Finished)
            {
                return;
            }

            GapToPlayerRelative = DriverLapSectorsTracker.GetRelativeGapToPlayer();
            GapToLeaderRelative = DriverLapSectorsTracker.GetRelativeGapToLeader();

            TimeSpan gapToPlayerAbsolute = DriverInfo.Timing.GapToPlayer != TimeSpan.Zero && !IsLappedByPlayer && !IsLappingPlayer ? DriverInfo.Timing.GapToPlayer : DriverLapSectorsTracker.GetGapToPlayerAbsolute();
            if (gapToPlayerAbsolute != TimeSpan.Zero)
            {
                GapToPlayerAbsolute = gapToPlayerAbsolute;
            }

            if (Session.Player == null)
            {
                return;
            }

            LapToPlayerDifference = (int)((TotalDistanceTraveled - Session.Player.TotalDistanceTraveled) / Session.LastSet.SessionInfo.TrackInfo.LayoutLength.InMeters);
        }

        private void CheckAndOverrideBestLap(double layoutLength)
        {
            if (DriverInfo.Timing.BestLapTime == TimeSpan.Zero || _bestLapCheckTimer.ElapsedMilliseconds < 1000)
            {
                return;
            }

            _bestLapCheckTimer.Restart();

            lock (_lapsObjectLock)
            {
                if (BestLap == null)
                {
                    int bestLapNumber = DriverInfo.Timing.BestLapTimeNumber != -1 ? DriverInfo.Timing.BestLapTimeNumber : Laps.Count + 1;
                    ILapInfo newBestLap = new StaticLapInfo(bestLapNumber, DriverInfo.Timing.BestLapTime, false, Laps.LastOrDefault(), layoutLength, this);
                    _lapsInfo.Insert(0, newBestLap);
                    BestLap = newBestLap;
                    LapCompleted?.Invoke(this, new LapEventArgs(newBestLap));
                    return;
                }

                if (BestLap.LapTime == DriverInfo.Timing.BestLapTime)
                {
                    return;
                }

                ILapInfo lapToOverride = Laps.FirstOrDefault(x => x.LapNumber == DriverInfo.Timing.BestLapTimeNumber) ?? BestLap;
                if (lapToOverride != BestLap)
                {
                    BestLap.InvalidateLap(LapInvalidationReasonKind.InvalidatedBySim);
                }

                lapToOverride.OverrideTime(DriverInfo.Timing.BestLapTime, true);
                BestLap = Laps.Where(x => x.Completed && x.Valid && x.LapTime != TimeSpan.Zero).OrderBy(x => x.LapTime).FirstOrDefault() ?? new StaticLapInfo(lapToOverride.LapNumber, TimeSpan.Zero, false, null, 0, this);
                LapTimeReevaluated?.Invoke(this, new LapEventArgs(lapToOverride));
            }
        }

        private void LapInvalidatedHandler(object sender, LapEventArgs e)
        {
            OnLapInvalidated(e);
        }

        private void CreateFirstLap(SimulatorDataSet set)
        {
            LapInfo firstLap = new LapInfo(set, DriverInfo.CompletedLaps + 1, this, true, null, _settingsProvider);
            if (InvalidateFirstLap)
            {
                firstLap.InvalidateLap(LapInvalidationReasonKind.InvalidatedFirstLap);
            }

            firstLap.SectorCompletedEvent += LapSectorCompletedEvent;
            firstLap.LapInvalidatedEvent += LapInvalidatedHandler;
            _lapsInfo.Add(firstLap);
            OnNewLapStarted(new LapEventArgs(firstLap));
        }

        private bool ShouldFinishLap(SimulatorDataSet dataSet, ILapInfo currentLap)
        {
            SessionInfo sessionInfo = dataSet.SessionInfo;

            if (currentLap is StaticLapInfo)
            {
                return false;
            }

            if (LastLap?.IsPending == true)
            {
                return false;
            }

            if (DriverInfo.FinishStatus == DriverFinishStatus.Finished && currentLap.IsPending)
            {
                return false;
            }

            if (currentLap.Completed)
            {
                currentLap.LapCompletionMethod = LapCompletionMethod.LapCompleted;
                return false;
            }

            // Use completed laps indication to end lap, when we use the sim provided lap times. This gives us the biggest assurance that lap time is already properly set. But wait for lap to be at least 5 seconds in
            if (dataSet.SimulatorSourceInfo.HasLapTimeInformation && (currentLap.LapNumber < DriverInfo.CompletedLaps + 1))
            {
                currentLap.LapCompletionMethod = LapCompletionMethod.ByLapNumber;
                return true;
            }

            // Crossed line at out lap
            if (dataSet.SessionInfo.SessionType != SessionType.Race && currentLap.IsPitLap && (DriverInfo.LapDistance - _previousTickLapDistance < sessionInfo.TrackInfo.LayoutLength.InMeters * -0.90))
            {
                currentLap.LapCompletionMethod = LapCompletionMethod.ByCrossingTheLine;
                return true;
            }

            if ((!dataSet.SimulatorSourceInfo.HasLapTimeInformation || dataSet.SimulatorSourceInfo.SimNotReportingEndOfOutLapCorrectly) && (DriverInfo.LapDistance - _previousTickLapDistance < sessionInfo.TrackInfo.LayoutLength.InMeters * -0.90))
            {
                currentLap.LapCompletionMethod = LapCompletionMethod.ByCrossingTheLine;
                return true;
            }

            if (dataSet.SimulatorSourceInfo.HasLapTimeInformation && !dataSet.SimulatorSourceInfo.OutLapIsValid && !currentLap.Valid
                && DriverInfo.CurrentLapValid && (currentLap.FirstLap || IsPlayer) && !InPits && !dataSet.SimulatorSourceInfo.InvalidateLapBySector)
            {
                currentLap.LapCompletionMethod = LapCompletionMethod.ByChangingValidity;
                return true;
            }

            if (dataSet.SimulatorSourceInfo.HasLapTimeInformation && !dataSet.SimulatorSourceInfo.OutLapIsValid &&
                !currentLap.Valid && DriverInfo.CurrentLapValid && DriverInfo.IsPlayer && _previousTickLapDistance < DriverInfo.LapDistance
                && sessionInfo.SessionType != SessionType.Race && !DriverInfo.InPits && !dataSet.SimulatorSourceInfo.InvalidateLapBySector)
            {
                currentLap.LapCompletionMethod = LapCompletionMethod.ByChangingValidity2;
                return true;
            }

            if (dataSet.SimulatorSourceInfo.HasLapTimeInformation && !currentLap.Valid && DriverInfo.CurrentLapValid
                && sessionInfo.SessionType == SessionType.Race && !DriverInfo.IsPlayer && (currentLap.FirstLap && !InvalidateFirstLap && !dataSet.SimulatorSourceInfo.InvalidateLapBySector))
            {
                currentLap.LapCompletionMethod = LapCompletionMethod.ByChangingValidity3;
                return true;
            }

            LapCompletionMethod completionMethod = LapCompletionMethod.None;
            if (_lapFinishConditions.FirstOrDefault(x => x.IsLapFinished(dataSet, currentLap, this, out completionMethod)) != null)
            {
                currentLap.LapCompletionMethod = completionMethod;
                return true;
            }

            switch (DriverInfo.FinishStatus)
            {
                case DriverFinishStatus.Finished:
                    currentLap.LapCompletionMethod = LapCompletionMethod.DriverFinished;
                    return true;
                // Driver is DNF/DQ -> finish timed lap, and set it to invalid
                case DriverFinishStatus.Dnf:
                case DriverFinishStatus.Dq:
                    currentLap.InvalidateLap(LapInvalidationReasonKind.DriverDnf);
                    return true;
                default:
                    return false;
            }
        }

        private void UpdateCurrentLap(SimulatorDataSet dataSet)
        {
            CurrentLap.Tick(dataSet, DriverInfo);
            CurrentLap.InvalidBySim = !DriverInfo.CurrentLapValid;
            LapPercentage = (DriverInfo.LapDistance / dataSet.SessionInfo.TrackInfo.LayoutLength.InMeters) * 100;
            if (InPits && ShouldPitLapBeInvalid(dataSet))
            {
                CurrentLap.InvalidateLap(LapInvalidationReasonKind.DriverInPits);
            }

            if (CurrentLap.Valid && !DriverInfo.CurrentLapValid && _lapsInfo.Count > 0 && (LastLap?.IsPending != true))
            {
                CurrentLap.InvalidateLap(LapInvalidationReasonKind.InvalidatedBySim);
            }
        }

        private void FinishLap(ILapInfo lapToFinish, SimulatorDataSet dataSet)
        {
            if (lapToFinish.Completed)
            {
                return;
            }

            lapToFinish.FinishLap(dataSet, DriverInfo);
            lapToFinish.SectorCompletedEvent -= LapSectorCompletedEvent;
            lapToFinish.LapInvalidatedEvent -= LapInvalidatedHandler;

            if (lapToFinish.LapTime == TimeSpan.Zero)
            {
                lapToFinish.InvalidateLap(LapInvalidationReasonKind.NoValidLapTime);
                RevertSectorChanges(lapToFinish);
            }

            if (ShouldLapBeDiscarded(lapToFinish, dataSet))
            {
                Logger.Info($"Lap {lapToFinish.LapNumber} discarded. Completed distance : {lapToFinish.CompletedDistance:N2}, Completion Method {lapToFinish.LapCompletionMethod}");
                LapInvalidated?.Invoke(this, new LapEventArgs(lapToFinish));
                lock (_lapsObjectLock)
                {
                    _lapsInfo.Remove(lapToFinish);
                }

                return;
            }

            if (lapToFinish.Valid && lapToFinish.LapTime != TimeSpan.Zero && (BestLap == null || BestLap.LapTime == TimeSpan.Zero || lapToFinish.LapTime < BestLap.LapTime))
            {
                BestLap = lapToFinish;
            }

            OnLapCompleted(new LapEventArgs(lapToFinish));
            Logger.Info($"Driver {DriverInfo.DriverSessionId}, Lap {lapToFinish.LapNumber} finnished. REASON: {lapToFinish.LapCompletionMethod}. Time {TimeSpanFormatHelper.FormatTimeSpan(lapToFinish.LapTime)}. Is Valid {lapToFinish.Valid}. Total Laps: {Laps.Count}.");

            ComputePace();
            PurgeLapsTelemetry();
            PurgeLapPortionTimes();
        }

        private void PurgeLapPortionTimes()
        {
            Laps.Where(x => x.Completed && x.LapTelemetryInfo?.IsPortionTimesPurged == false && x != BestLap && x.LapNumber != 1 && x != x.Driver.LastCompletedLap)
                .ForEach(x => x.LapTelemetryInfo.PurgeLapPortionTimes());
        }

        private void PurgeLapsTelemetry()
        {
            Laps.Where(x => x.Completed && x.LapTelemetryInfo?.IsTelemetryPurged == false && x != BestLap && ((x.LapNumber != 1 && x != x.Driver.LastCompletedLap) || !IsPlayer)).
                Skip(IsPlayer ? MaxLapsWithTelemetry : 0).ForEach(x => x.LapTelemetryInfo.PurgeTelemetry());
        }

        private bool ShouldLapBeDiscarded(ILapInfo lap, SimulatorDataSet dataSet)
        {
            return !lap.IsLapDataSane(dataSet);
        }

        private void CreateNewLap(SimulatorDataSet dataSet, ILapInfo lapToCreateFrom)
        {
            if ((DriverInfo.FinishStatus != DriverFinishStatus.Na && DriverInfo.FinishStatus != DriverFinishStatus.None))
            {
                return;
            }

            ILapInfo newLap = new LapInfo(dataSet, DriverInfo.CompletedLaps + 1, this, lapToCreateFrom, _settingsProvider);

            newLap.SectorCompletedEvent += LapSectorCompletedEvent;
            newLap.LapInvalidatedEvent += LapInvalidatedHandler;
            lock (_lapsObjectLock)
            {
                _lapsInfo.Add(newLap);
            }

            OnNewLapStarted(new LapEventArgs(newLap));
        }

        private void LapSectorCompletedEvent(object sender, SectorCompletedArgs e)
        {
            SectorTiming completedSector = e.SectorTiming;
            if (!e.SectorTiming.Lap.Valid)
            {
                return;
            }

            switch (completedSector.SectorNumber)
            {
                case 1:
                    if ((BestSector1 == null || BestSector1.Duration > completedSector.Duration) && completedSector.Duration > TimeSpan.Zero)
                    {
                        BestSector1 = completedSector;
                    }

                    break;
                case 2:
                    if ((BestSector2 == null || BestSector2.Duration > completedSector.Duration) && completedSector.Duration > TimeSpan.Zero)
                    {
                        BestSector2 = completedSector;
                    }

                    break;
                case 3:
                    if ((BestSector3 == null || BestSector3.Duration > completedSector.Duration) && completedSector.Duration > TimeSpan.Zero)
                    {
                        BestSector3 = completedSector;
                    }

                    break;
            }

            OnSectorCompletedEvent(e);
            UpdateBestSectorProperties();
        }

        private void UpdateBestSectorProperties()
        {
            IsLastSector1PersonalBest = GetIsSector1PersonalBest();
            IsLastSector2PersonalBest = GetIsSector2PersonalBest();
            IsLastSector3PersonalBest = GetIsSector3PersonalBest();

            IsLastSector1SessionBest = GetIsSector1SessionBest();
            IsLastSector2SessionBest = GetIsSector2SessionBest();
            IsLastSector3SessionBest = GetIsSector3SessionBest();

            BestTimesSetViewModel bestTimesForClass = Session.GetBestTimesForClass(CarClassId);
            IsLastSector1ClassSessionBest = GetIsSector1ClassSessionBest(bestTimesForClass);
            IsLastSector2ClassSessionBest = GetIsSector2ClassSessionBest(bestTimesForClass);
            IsLastSector3ClassSessionBest = GetIsSector3ClassSessionBest(bestTimesForClass);
            IsLastLapBestClassSessionLap = GetIsLastLapClassSessionBest(bestTimesForClass);
        }

        private void UpdateInPitsProperty(SimulatorDataSet set)
        {
            if (InPits && !LastPitStop.Completed)
            {
                LastPitStop.Tick(set);
                if (LastPitStop.Completed)
                {
                    CheckAndNotifyLastPitStop(set);
                }
            }

            if (!InPits && DriverInfo.InPits)
            {
                InPits = true;
                _pitStopInfo.Add(new PitStopInfo(set, this, CurrentLap));
            }

            if (InPits && !DriverInfo.InPits)
            {
                InPits = false;
            }
        }

        private void CheckAndNotifyLastPitStop(SimulatorDataSet set)
        {
            if (LastPitStop.IsValid() && set.SessionInfo.SessionType == SessionType.Race)
            {
                _pitStopEvenProvider.NotifyPitStopCompleted(this, LastPitStop);
            }
            else
            {
                _pitStopInfo.Remove(LastPitStop);
            }
        }

        private void UpdateLappedProperties(SimulatorDataSet set)
        {
            if (set.SessionInfo.SessionType != SessionType.Race)
            {
                IsLappingPlayer = false;
                DistanceToPlayer = DriverInfo.DistanceToPlayer;
                IsLappedByPlayer = CurrentLap?.Valid == false;
                return;
            }

            if (DriverInfo.IsPlayer)
            {
                return;
            }

            IsLappingPlayer = DriverInfo.IsLappingPlayer;
            IsLappedByPlayer = DriverInfo.IsBeingLappedByPlayer;

            if (UpdateLappedByPlayer(set) || UpdateLappingPlayer(set))
            {
                return;
            }

            DistanceToPlayer = DriverInfo.DistanceToPlayer;
        }

        private bool UpdateLappedByPlayer(SimulatorDataSet set)
        {
            if (!_inLappedRange && IsLappedByPlayer && DriverInfo.DistanceToPlayer < 0 && DriverInfo.DistanceToPlayer > set.SessionInfo.TrackInfo.LayoutLength.InMeters * -0.3)
            {
                _inLappedRange = true;
            }

            if (_inLappedRange && DriverInfo.DistanceToPlayer > 0 && DriverInfo.DistanceToPlayer > set.SessionInfo.TrackInfo.LayoutLength.InMeters * 0.3)
            {
                _inLappedRange = false;
            }

            if (DriverInfo.DistanceToPlayer < 0 && IsLappedByPlayer && !_inLappedRange)
            {
                DistanceToPlayer = set.SessionInfo.TrackInfo.LayoutLength.InMeters + DriverInfo.DistanceToPlayer;
                return true;
            }

            return false;
        }

        private bool UpdateLappingPlayer(SimulatorDataSet set)
        {
            if (!_inLappingRange && IsLappingPlayer && DriverInfo.DistanceToPlayer > 0 && DriverInfo.DistanceToPlayer < set.SessionInfo.TrackInfo.LayoutLength.InMeters * 0.3)
            {
                _inLappingRange = true;
            }

            if (_inLappingRange && DriverInfo.DistanceToPlayer < 0 && DriverInfo.DistanceToPlayer < set.SessionInfo.TrackInfo.LayoutLength.InMeters * -0.3)
            {
                _inLappingRange = false;
            }

            if (DriverInfo.DistanceToPlayer > 0 && IsLappingPlayer && !_inLappingRange)
            {
                DistanceToPlayer = DriverInfo.DistanceToPlayer - set.SessionInfo.TrackInfo.LayoutLength.InMeters;
                return true;
            }

            return false;
        }

        private void ComputePace()
        {
            if (LastCompletedLap == null)
            {
                Pace = TimeSpan.Zero;
                return;
            }

            double averageLapTimeSeconds = _lapsInfo
                .Where(x => x.Completed && !x.IsPitLap & !x.IsSafetyCarLap && (x.Valid || Session.RetrieveAlsoInvalidLaps))
                .Reverse()
                .Take(PaceLaps)
                .Select(x => x.LapTime.TotalSeconds)
                .DefaultIfEmpty()
                .Average(x => x);
            Pace = TimeSpan.FromSeconds(averageLapTimeSeconds);
        }

        protected virtual void OnSectorCompletedEvent(SectorCompletedArgs e)
        {
            SectorCompletedEvent?.Invoke(this, e);
        }

        protected virtual void OnNewLapStarted(LapEventArgs e)
        {
            NewLapStarted?.Invoke(this, e);
        }

        protected virtual void OnLapInvalidated(LapEventArgs e)
        {
            try
            {
                if (e.Lap == null)
                {
                    return;
                }

                RevertSectorChanges(e.Lap);
                LapInvalidated?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error handling lap invalidation for driver : {ex.Message}");
            }
        }

        protected virtual void OnLapCompleted(LapEventArgs e)
        {
            LapCompleted?.Invoke(this, e);
        }

        private void RevertSectorChanges(ILapInfo lap)
        {
            if (BestSector1 != null && BestSector1 == lap.Sector1)
            {
                BestSector1 = FindBestSector(LapInfo.Sector1SelFunc);
            }

            if (BestSector2 != null && BestSector2 == lap.Sector2)
            {
                BestSector2 = FindBestSector(LapInfo.Sector2SelFunc);
            }

            if (BestSector3 != null && BestSector3 == lap.Sector3)
            {
                BestSector3 = FindBestSector(LapInfo.Sector3SelFunc);
            }
        }

        private SectorTiming FindBestSector(Func<ILapInfo, SectorTiming> sectorPickerFunc)
        {
            return Laps.Where(l => l.Valid).Select(sectorPickerFunc).Where(s => s != null && s.Duration != TimeSpan.Zero)
                .OrderBy(s => s.Duration).FirstOrDefault();
        }

        private bool GetIsSector1SessionBest()
        {
            if (CurrentLap == null)
            {
                return false;
            }

            var sector = GetSector1Timing();
            return sector != null && sector == Session.SessionBestTimesViewModel.BestSector1;
        }

        private bool GetIsSector2SessionBest()
        {
            if (CurrentLap == null)
            {
                return false;
            }

            var sector = GetSector2Timing();
            return sector != null && sector == Session.SessionBestTimesViewModel.BestSector2;
        }

        private bool GetIsSector3SessionBest()
        {
            if (CurrentLap == null)
            {
                return false;
            }

            var sector = GetSector3Timing();
            return sector != null && sector == Session.SessionBestTimesViewModel.BestSector3;
        }

        private bool GetIsSector1ClassSessionBest(BestTimesSetViewModel classBestTimes)
        {
            if (CurrentLap == null)
            {
                return false;
            }

            var sector = GetSector1Timing();
            return sector != null && sector == classBestTimes.BestSector1;
        }

        private bool GetIsSector2ClassSessionBest(BestTimesSetViewModel classBestTimes)
        {
            if (CurrentLap == null)
            {
                return false;
            }

            var sector = GetSector2Timing();
            return sector != null && sector == classBestTimes.BestSector2;
        }

        private bool GetIsSector3ClassSessionBest(BestTimesSetViewModel classBestTimes)
        {
            if (CurrentLap == null)
            {
                return false;
            }

            var sector = GetSector3Timing();
            return sector != null && sector == classBestTimes.BestSector3;
        }

        private bool GetIsLastLapClassSessionBest(BestTimesSetViewModel classBestTimes)
        {
            if (LastLap == null)
            {
                return false;
            }

            return LastLap == classBestTimes.BestLap;
        }

        private bool GetIsSector1PersonalBest()
        {
            if (CurrentLap == null)
            {
                return false;
            }

            var sector = GetSector1Timing();
            return sector != null && sector == BestSector1;
        }

        private bool GetIsSector2PersonalBest()
        {
            if (CurrentLap == null)
            {
                return false;
            }

            var sector = GetSector2Timing();
            return sector != null && sector == BestSector2;
        }

        private bool GetIsSector3PersonalBest()
        {
            if (CurrentLap == null)
            {
                return false;
            }

            var sector = GetSector3Timing();
            return sector != null && sector == BestSector3;
        }

        public SectorTiming GetSector1Timing(bool onlyComplete = false)
        {
            if (CurrentLap == null)
            {
                return null;
            }

            SectorTiming sector = CurrentLap.Sector1;
            if ((sector == null || (!sector.IsFinished && onlyComplete)) && CurrentLap.PreviousLap != null)
            {
                sector = CurrentLap.PreviousLap.Sector1;
            }

            return sector?.IsFinished == false && onlyComplete ? null : sector;
        }

        public SectorTiming GetSector2Timing(bool onlyComplete = false)
        {
            if (CurrentLap == null)
            {
                return null;
            }

            SectorTiming sector = CurrentLap.Sector2;
            if ((sector == null || (!sector.IsFinished && onlyComplete)) && CurrentLap.PreviousLap != null)
            {
                sector = CurrentLap.PreviousLap.Sector2;
            }

            return sector?.IsFinished == false && onlyComplete ? null : sector;
        }

        public SectorTiming GetSector3Timing(bool onlyComplete = false)
        {
            if (CurrentLap == null)
            {
                return null;
            }

            SectorTiming sector = CurrentLap.Sector3;
            if ((sector == null || (!sector.IsFinished && onlyComplete)) && CurrentLap.PreviousLap != null)
            {
                sector = CurrentLap.PreviousLap.Sector3;
            }

            return sector?.IsFinished == false && onlyComplete ? null : sector;
        }

        public void DeActiveDriver()
        {
            IsActive = false;
            _inactiveTimer.Reset();
            _inactiveTimer.Start();
        }

        public void ReActivateDriver()
        {
            IsActive = true;
            _inactiveTimer.Stop();
            _inactiveTimer.Reset();
        }

        public void UpdateDriverInfo(DriverInfo modelInfo, SimulatorDataSet dataSet)
        {
            if (DriverInfo.CompletedLaps > modelInfo.CompletedLaps && !dataSet.SimulatorSourceInfo.HasRewindFunctionality)
            {
                DriverInfo = modelInfo;
                Logger.Info($"Completed laps disagree {modelInfo.DriverSessionId}");
                lock (_lapsObjectLock)
                {
                    _lapsInfo.Clear();
                    CreateFirstLap(dataSet);
                }

                return;
            }

            UpdateDriverPositionChange(modelInfo, dataSet);

            DriverInfo = modelInfo;
        }

        private void UpdateDriverPositionChange(IDriverInfo modelInfo, SimulatorDataSet dataSet)
        {
            if (modelInfo.PositionInClass == PositionInClass && DriverTimingVisualization.DriverPositionChange != DriverPositionChange.None &&
                DriverTimingVisualization.DriverPositionResetSessionTime < dataSet.SessionInfo.SessionTime)
            {
                DriverTimingVisualization.DriverPositionChange = DriverPositionChange.None;
            }

            if (modelInfo.PositionInClass == 0 || PositionInClass == 0 || modelInfo.PositionInClass == PositionInClass)
            {
                return;
            }

            DriverTimingVisualization.DriverPositionChange = modelInfo.PositionInClass < PositionInClass ? DriverPositionChange.Gained : DriverPositionChange.Lost;
            DriverTimingVisualization.DriverPositionResetSessionTime = dataSet.SessionInfo.SessionTime + TimeSpan.FromSeconds(4);
        }

        public bool IsBeingLappedBy(DriverTiming otherDriver)
        {
            return DriverInfo.IsBeingLappedBy(otherDriver.DriverInfo, Session.LastSet.SessionInfo.TrackInfo.LayoutLength.InMeters);
        }

        public bool IsLapping(DriverTiming otherDriver)
        {
            return DriverInfo.IsLapping(otherDriver.DriverInfo, Session.LastSet.SessionInfo.TrackInfo.LayoutLength.InMeters);
        }
        
        private bool ShouldPitLapBeInvalid(SimulatorDataSet dataSet)
        {
            if (!CurrentLap.Valid || dataSet.SessionInfo.SessionType == SessionType.Race)
            {
                return false;
            }
            
            return !dataSet.SimulatorSourceInfo.HasLapTimeInformation || _lapsInfo.Count <= 1;
        }
    }
}