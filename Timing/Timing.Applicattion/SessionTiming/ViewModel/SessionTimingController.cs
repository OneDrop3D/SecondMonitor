namespace SecondMonitor.Timing.Application.SessionTiming.ViewModel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Application.ViewModel;
    using Common.SessionTiming;
    using Common.SessionTiming.Drivers;
    using Common.SessionTiming.Drivers.Lap;
    using Controllers;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using DataModel.Snapshot.Systems;
    using Drivers;
    using NLog;
    using Rating.Application.Championship;
    using Rating.Application.Rating.RatingProvider;
    using Rating.Common.DataModel.Player;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.Colors;

    using Telemetry;
    using TrackRecords.Controller;
    using ViewModels.Controllers;
    using ViewModels.Factory;
    using ViewModels.SessionEvents;
    using ViewModels.Settings;
    using ViewModels.Settings.ViewModel;

    public class SessionTimingController : IController, ISessionInfo
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly ISessionRatingProvider _sessionRatingProvider;
        private readonly ITrackRecordsController _trackRecordsController;
        private readonly IChampionshipCurrentEventPointsProvider _championshipCurrentEventPointsProvider;
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly Stopwatch _gapRefreshStopwatch;
        private readonly MapManagementController _mapManagementController;
        private readonly IViewModelFactory _viewModelFactory;
        private readonly ISettingsProvider _settingsProvider;
        private readonly DriverTimingFactory _driverTimingFactory;
        private readonly ILapEventProvider _lapEventProvider;
        private readonly IClassColorProvider _classColorProvider;
        private readonly Stopwatch _ratingUpdateStopwatch;
        private readonly Dictionary<string, BestTimesSetViewModel> _bestTimesForClasses;

        private Task _bestInformationSwapTask;
        private CancellationTokenSource _bestInformationSwapCancellationToken;

        private SimulatorDataSet _beforeLastSet = new SimulatorDataSet("None");
        private SimulatorDataSet _lastSet = new SimulatorDataSet("None");

        public SessionTimingController(TimingApplicationViewModel timingApplicationViewModel, ISessionTelemetryController sessionTelemetryController,
            ISessionRatingProvider sessionRatingProvider, ITrackRecordsController trackRecordsController,
            IChampionshipCurrentEventPointsProvider championshipCurrentEventPointsProvider,
            ISessionEventProvider sessionEventProvider, SimulatorDataSet initialDataSet, MapManagementController mapManagementController, IViewModelFactory viewModelFactory,
            ISettingsProvider settingsProvider, DriverTimingFactory driverTimingFactory, ILapEventProvider lapEventProvider,
            IClassColorProvider classColorProvider, DriverTimingBySessionProvider driverTimingBySessionProvider)
        {
            SessionGlobalKey = Guid.NewGuid();
            InitializeOneTimeProperties(initialDataSet);
            SessionBestTimesViewModel = viewModelFactory.Set("setName").To(BestTimesSetViewModel.AllClassesName).Set("setLabel").To("All Classes").Create<BestTimesSetViewModel>();
            SessionBestTimesViewModel.IsSetNameShown = initialDataSet.SessionInfo.IsMultiClass;
            SessionBestTimesViewModel.ClassColor = ColorDto.FromHex("FF7F7F7F");
            _sessionRatingProvider = sessionRatingProvider;
            _trackRecordsController = trackRecordsController;
            _championshipCurrentEventPointsProvider = championshipCurrentEventPointsProvider;
            _sessionEventProvider = sessionEventProvider;
            _mapManagementController = mapManagementController;
            _viewModelFactory = viewModelFactory;
            _settingsProvider = settingsProvider;
            _driverTimingFactory = driverTimingFactory;
            _lapEventProvider = lapEventProvider;
            _classColorProvider = classColorProvider;
            TimingApplicationViewModel = timingApplicationViewModel;
            SessionTelemetryController = sessionTelemetryController;
            _ratingUpdateStopwatch = Stopwatch.StartNew();
            _gapRefreshStopwatch = Stopwatch.StartNew();
            GlobalKey = Guid.NewGuid();
            _bestTimesForClasses = new Dictionary<string, BestTimesSetViewModel>();
            _bestTimesForClasses.Add(SessionBestTimesViewModel.SetName, SessionBestTimesViewModel);
            driverTimingBySessionProvider.RegisterSessionTimingController(this);
            DriversByPosition = new List<DriverTiming>();
        }

        public event EventHandler<DriverListModificationEventArgs> DriverAdded;
        public event EventHandler<DriverListModificationEventArgs> DriverRemoved;

        public DriverTiming Player { get; private set; }

        public DriverTiming Leader { get; private set; }

        public TimeSpan SessionTime { get; private set; }

        public bool IsMultiClass { get; set; }

        public Guid GlobalKey { get; }

        public double TotalSessionLength { get; private set; }

        public TimeSpan SessionStarTime { get; private set; }

        public bool IsFinished { get; private set; }

        public SessionType SessionType { get; private set; }

        public bool DisplayBindTimeRelative
        {
            get;
            set;
        }

        public BestTimesSetViewModel SessionBestTimesViewModel { get; }

        public DisplaySettingsViewModel DisplaySettingsViewModel => _settingsProvider.DisplaySettingsViewModel;
        public IReadOnlyCollection<DriverTiming> DriversByPosition { get; private set; }

        public bool WasGreen { get; private set; }

        public TimingApplicationViewModel TimingApplicationViewModel { get; }
        public ISessionTelemetryController SessionTelemetryController { get; }

        public SimulatorDataSet LastSet
        {
            get => _lastSet;
            private set
            {
                _beforeLastSet = _lastSet;
                _lastSet = value;
            }
        }

        public Dictionary<string, DriverTiming> Drivers { get; private set; }

        public Guid SessionGlobalKey { get; }
        public int PaceLaps
        {
            get;
            set;
        }

        public bool RetrieveAlsoInvalidLaps { get; set; }

        public int SessionCompletedPerMiles => (int)(SessionCompletedPercentage * 1000);

        public double SessionCompletedPercentage
        {
            get
            {
                return LastSet?.SessionInfo.SessionLengthType switch
                {
                    SessionLengthType.Laps => ((LastSet.LeaderInfo.CompletedLaps + (LastSet.LeaderInfo.LapDistance / LastSet.SessionInfo.TrackInfo.LayoutLength.InMeters)) / LastSet.SessionInfo.TotalNumberOfLaps),
                    SessionLengthType.Time or SessionLengthType.TimeWithExtraLap => (1 - ((LastSet.SessionInfo.SessionTimeRemaining / TotalSessionLength))),
                    _ => 0
                };
            }
        }

        public bool DisplayGapToPlayerRelative { get; set; }

        public ILapInfo GetBestLap() => SessionBestTimesViewModel.BestLap;

        public ILapInfo GetBestLap(string classId) => GetBestTimesForClass(classId)?.BestLap;
        public ILapInfo GetBestLapExcludePlayer(string classId)
        {
            ILapInfo bestLapForClass = GetBestLap(classId);
            if (bestLapForClass?.Driver.IsPlayer != true)
            {
                return bestLapForClass;
            }

            var bestAi = Drivers.Values.FirstOrDefault(x => x.PositionInClass == 2 && x.CarClassId == Player.CarClassId);
            return bestAi?.BestLap;
        }

        public BestTimesSetViewModel GetBestTimesForClass(string classId)
        {
            if (_bestTimesForClasses.TryGetValue(classId, out BestTimesSetViewModel bestTimesSetViewModel))
            {
                return bestTimesSetViewModel;
            }

            string className = _lastSet.DriversInfo.FirstOrDefault(x => x.CarClassId == classId)?.CarClassName;

            if (string.IsNullOrWhiteSpace(className))
            {
                return null;
            }

            bestTimesSetViewModel = _viewModelFactory.Set("setName").To(classId)
                .Set("setLabel").To(className)
                .Create<BestTimesSetViewModel>();
            bestTimesSetViewModel.ClassColor = _classColorProvider.GetColorForClass(classId);
            bestTimesSetViewModel.IsSetNameShown = true;
            _bestTimesForClasses.Add(classId, bestTimesSetViewModel);
            return bestTimesSetViewModel;
        }

        public void Finish()
        {
            IsFinished = true;
        }

        private void DriverOnLapCompleted(object sender, LapEventArgs lapEventArgs)
        {
            _lapEventProvider.NotifyLapCompleted(this, lapEventArgs.Lap);

            if (lapEventArgs.Lap.Driver.IsPlayer)
            {
                _mapManagementController.OnLapCompleted(lapEventArgs);
            }

            if (lapEventArgs.Lap.Driver.IsPlayer && TimingApplicationViewModel.DisplaySettingsViewModel.TelemetrySettingsViewModel.IsTelemetryLoggingEnabled &&
                TimingApplicationViewModel.DisplaySettingsViewModel.TelemetrySettingsViewModel.IsTelemetryEnabledForSession(SessionType) &&
                (lapEventArgs.Lap.Valid || TimingApplicationViewModel.DisplaySettingsViewModel.TelemetrySettingsViewModel.LogInvalidLaps))
            {
                SessionTelemetryController.TrySaveLapTelemetry(lapEventArgs.Lap);
            }

            if (!lapEventArgs.Lap.Valid && lapEventArgs.Lap.Driver.IsPlayer)
            {
                Player.IsLastLapTrackRecord = false;
                return;
            }

            SessionBestTimesViewModel.CheckAndUpdateBestLap(lapEventArgs.Lap);
            GetBestTimesForClass(lapEventArgs.Lap.Driver.CarClassId).CheckAndUpdateBestLap(lapEventArgs.Lap);

            if (lapEventArgs.Lap.Driver.IsPlayer && Player != null)
            {
                Player.IsLastLapTrackRecord = _trackRecordsController.EvaluateFastestLapCandidate(lapEventArgs.Lap);
            }
        }

        private void OnSectorCompletedEvent(object sender, SectorCompletedArgs e)
        {
            SectorTiming completedSector = e.SectorTiming;
            if (!e.SectorTiming.Lap.Valid)
            {
                return;
            }

            SessionBestTimesViewModel.UpdateSectorBest(completedSector);
            GetBestTimesForClass(completedSector.Lap.Driver.CarClassId).UpdateSectorBest(completedSector);
        }

        private void AddNewDriver(DriverInfo newDriverInfo)
        {
            int position;
            if (Drivers.TryGetValue(newDriverInfo.DriverSessionId, out DriverTiming driverTiming))
            {
                if (driverTiming.IsActive)
                {
                    return;
                }

                driverTiming.ReActivateDriver();

                if (newDriverInfo.RatingInfo.IsFilled)
                {
                    driverTiming.Rating = newDriverInfo.RatingInfo.Rating;
                }
                else if (_sessionRatingProvider.TryGetRatingForDriverCurrentSession(newDriverInfo.DriverSessionId, out DriversRating driversRatingNew))
                {
                    driverTiming.Rating = driversRatingNew.Rating;
                }

                if (_championshipCurrentEventPointsProvider.TryGetPointsAndPositionForDriver(newDriverInfo.DriverLongName, out int pointsNew, out position))
                {
                    driverTiming.ChampionshipPoints = pointsNew;
                    driverTiming.ChampionshipPosition = position;
                }

                logger.Info($"Driver Reactivated, {driverTiming.DriverId}");
                RaiseDriverAddedEvent(driverTiming);
                return;
            }

            logger.Info($"Adding new driver: {newDriverInfo.DriverSessionId}");

            DriverTiming newDriver = _driverTimingFactory.Create(newDriverInfo, this, SessionType != SessionType.Race);
            newDriver.SectorCompletedEvent += OnSectorCompletedEvent;
            newDriver.LapInvalidated += LapInvalidatedHandler;
            newDriver.LapCompleted += DriverOnLapCompleted;
            newDriver.LapTimeReevaluated += DriverOnLapTimeReevaluated;
            Drivers.Add(newDriver.DriverId, newDriver);

            if (newDriverInfo.RatingInfo.IsFilled)
            {
                newDriver.Rating = newDriverInfo.RatingInfo.Rating;
            }
            else if (_sessionRatingProvider.TryGetRatingForDriverCurrentSession(newDriverInfo.DriverSessionId, out DriversRating driversRating))
            {
                newDriver.Rating = driversRating.Rating;
            }

            if (_championshipCurrentEventPointsProvider.TryGetPointsAndPositionForDriver(newDriverInfo.DriverLongName, out int points, out position))
            {
                newDriver.ChampionshipPoints = points;
                newDriver.ChampionshipPosition = position;
            }

            RaiseDriverAddedEvent(newDriver);
            logger.Info($"Added new driver");
        }

        private void DriverOnLapTimeReevaluated(object sender, LapEventArgs e)
        {
            SessionBestTimesViewModel.FindBestLap(Drivers.Values);
            GetBestTimesForClass(e.Lap.Driver.CarClassId).FindBestLap(Drivers.Values.Where(x => x.CarClassId == e.Lap.Driver.CarClassId).ToList());
        }

        private void LapInvalidatedHandler(object sender, LapEventArgs e)
        {
            if (e?.Lap?.Driver?.CarClassId == null)
            {
                return;
            }

            SessionBestTimesViewModel.InvalidateBestSectorForLap(e.Lap, Drivers.Values);
            SessionBestTimesViewModel.FindBestLap(Drivers.Values);
            GetBestTimesForClass(e.Lap.Driver.CarClassId).InvalidateBestSectorForLap(e.Lap, Drivers.Values.Where(x => x.CarClassId == e.Lap.Driver.CarClassId).ToList());
        }

        public void UpdateTiming(SimulatorDataSet dataSet)
        {
            if (dataSet.SessionInfo.SpectatingState == SpectatingState.Replay)
            {
                return;
            }

            LastSet = dataSet;
            SessionTime = dataSet.SessionInfo.SessionTime - SessionStarTime;
            SessionType = dataSet.SessionInfo.SessionType;
            WasGreen |= dataSet.SessionInfo.SessionPhase == SessionPhase.Green;
            IsMultiClass |= dataSet.SessionInfo.IsMultiClass;
            if (dataSet.SessionInfo.SessionLengthType == SessionLengthType.Time || dataSet.SessionInfo.SessionLengthType == SessionLengthType.TimeWithExtraLap)
            {
                TotalSessionLength = Math.Max(TotalSessionLength, dataSet.SessionInfo.SessionTimeRemaining);
            }

            UpdateDrivers();
        }

        private void UpdateDrivers()
        {
            try
            {
                bool updateRating = _ratingUpdateStopwatch.ElapsedMilliseconds > 1000;
                if (updateRating)
                {
                    _ratingUpdateStopwatch.Restart();
                }

                HashSet<string> updatedDrivers = UpdatedDrivers(updateRating);

                if (Leader != null && Leader.DriverInfo?.FinishStatus != DriverFinishStatus.Finished)
                {
                    RemoveDisconnectedDrivers(updatedDrivers, LastSet);
                }

                if (_gapRefreshStopwatch.ElapsedMilliseconds <= 1000)
                {
                    return;
                }

                _gapRefreshStopwatch.Restart();
                DriversByPosition = Drivers.Values.OrderBy(x => x.Position).ToList();
                Drivers.Values.ForEach(x => x.CalculateGapToPLayer());
            }
            catch (KeyNotFoundException ex)
            {
                throw new DriverNotFoundException("Driver not found", ex);
            }
        }

        private HashSet<string> UpdatedDrivers(bool updateRating)
        {
            HashSet<string> updatedDrivers = new HashSet<string>();
            foreach (DriverInfo driverInfo in LastSet.DriversInfo.Where(x => x.DriverSessionId != null))
            {
                updatedDrivers.Add(driverInfo.DriverSessionId);
                if (Drivers.TryGetValue(driverInfo.DriverSessionId, out DriverTiming driverToUpdate) && driverToUpdate.IsActive)
                {
                    driverToUpdate = Drivers[driverInfo.DriverSessionId];
                    UpdateDriver(driverInfo, driverToUpdate, LastSet, updateRating);

                    if (driverToUpdate.IsPlayer)
                    {
                        Player = driverToUpdate;
                    }
                }
                else
                {
                    AddNewDriver(driverInfo);
                }
            }

            return updatedDrivers;
        }

        private void RemoveDisconnectedDrivers(HashSet<string> updatedDrivers, SimulatorDataSet dataSet)
        {
            List<string> driversToRemove = Drivers.Keys
                .Where(s => !updatedDrivers.Contains(s) && Drivers[s].IsActive && Drivers[s].DriverInfo.FinishStatus != DriverFinishStatus.Finished).ToList();

            if (driversToRemove.Count == 0)
            {
                return;
            }

            if (dataSet?.SessionInfo.SessionType == SessionType.Race)
            {
                RemoveDisconnectedDriversRace(driversToRemove, dataSet);
                return;
            }

            foreach (string driver in driversToRemove)
            {
                logger.Info($"Removing driver {Drivers[driver].DriverId}");
                RaiseDriverRemovedEvent(Drivers[driver]);
                Drivers[driver].DeActiveDriver();
                logger.Info($"Driver Removed");
            }
        }

        private void RemoveDisconnectedDriversRace(List<string> driversToRemove, SimulatorDataSet dataSet)
        {
            foreach (DriverTiming driver in driversToRemove.Select(driverId => Drivers[driverId]).Where(x => x.DriverInfo.FinishStatus != DriverFinishStatus.Dnf))
            {
                logger.Info($"Settign Driver as DNF - disconnected {driver.DriverId}");
                driver.DriverInfo.FinishStatus = DriverFinishStatus.Dnf;
                UpdateDriver(driver.DriverInfo, driver, dataSet, false);
            }
        }

        public void InitializeDriversFrom(SimulatorDataSet dataSet, bool invalidateFirstLap)
        {
            Dictionary<string, DriverTiming> drivers = new Dictionary<string, DriverTiming>();
            foreach (DriverInfo driverInfo in dataSet.DriversInfo)
            {
                if (drivers.ContainsKey(driverInfo.DriverSessionId))
                {
                    continue;
                }

                drivers[driverInfo.DriverSessionId] = _driverTimingFactory.Create(driverInfo, this, invalidateFirstLap);
            }

            SetDrivers(dataSet, drivers.Values);
        }

        private void SetDrivers(SimulatorDataSet dataSet, IReadOnlyCollection<DriverTiming> drivers)
        {
            foreach (DriverTiming newDriver in drivers)
            {
                newDriver.SectorCompletedEvent += OnSectorCompletedEvent;
                newDriver.LapInvalidated += LapInvalidatedHandler;
                newDriver.LapCompleted += DriverOnLapCompleted;
                newDriver.LapTimeReevaluated += DriverOnLapTimeReevaluated;
                if (newDriver.IsPlayer)
                {
                    Player = newDriver;
                }
            }

            Drivers = drivers.ToDictionary(x => x.DriverId, x => x);
            _sessionEventProvider.NotifyDriversAdded(dataSet, LastSet, drivers.Select(x => x.DriverInfo));
        }

        private void RaiseDriverAddedEvent(DriverTiming driver)
        {
            _sessionEventProvider.NotifyDriversAdded(LastSet, _beforeLastSet, new[] { driver.DriverInfo });
            DriverAdded?.Invoke(this, new DriverListModificationEventArgs(driver));
        }

        private void RaiseDriverRemovedEvent(DriverTiming driver)
        {
            _sessionEventProvider.NotifyDriversRemoved(LastSet, _beforeLastSet, new[] { driver.DriverInfo });
            DriverRemoved?.Invoke(this, new DriverListModificationEventArgs(driver));
        }

        private void UpdateDriver(DriverInfo modelInfo, DriverTiming timingInfo, SimulatorDataSet set, bool updateRating)
        {
            timingInfo.UpdateDriverInfo(modelInfo, set);

            if (timingInfo.Position == 1)
            {
                Leader = timingInfo;
            }

            timingInfo.UpdateLaps(set);

            if (modelInfo.RatingInfo.IsFilled)
            {
                timingInfo.Rating = modelInfo.RatingInfo.Rating;
            }
            else if (updateRating && _sessionRatingProvider.TryGetRatingForDriverCurrentSession(modelInfo.DriverSessionId, out DriversRating driversRating))
            {
                timingInfo.Rating = driversRating.Rating;
            }

            if (updateRating && timingInfo.ChampionshipPoints == 0 && _championshipCurrentEventPointsProvider.TryGetPointsAndPositionForDriver(modelInfo.DriverLongName, out int points, out int position))
            {
                timingInfo.ChampionshipPoints = points;
                timingInfo.ChampionshipPosition = position;
            }
        }

        private void InitializeOneTimeProperties(SimulatorDataSet initialDataSet)
        {
            SessionStarTime = initialDataSet.SessionInfo.SessionTime;
            SessionType = initialDataSet.SessionInfo.SessionType;
            RetrieveAlsoInvalidLaps = true; // initialDataSet.SessionInfo.SessionType == SessionType.Race;
            if (initialDataSet.SessionInfo.SessionLengthType == SessionLengthType.Time || initialDataSet.SessionInfo.SessionLengthType == SessionLengthType.TimeWithExtraLap)
            {
                TotalSessionLength = initialDataSet.SessionInfo.SessionTimeRemaining;
            }

            PaceLaps = 4;
            DisplayBindTimeRelative = false;
        }

        public IEnumerator GetEnumerator()
        {
            return Drivers.Values.GetEnumerator();
        }

        public async Task StartControllerAsync()
        {
            TimingApplicationViewModel.SessionInfoViewModel.SelectedBestTimes = SessionBestTimesViewModel;
            await SessionTelemetryController.StartControllerAsync();
            _bestInformationSwapCancellationToken = new CancellationTokenSource();
            _bestInformationSwapTask = CycleBestInformation(_bestInformationSwapCancellationToken.Token);
        }

        public async Task StopControllerAsync()
        {
            foreach (DriverTiming driversValue in Drivers.Values)
            {
                driversValue.SectorCompletedEvent -= OnSectorCompletedEvent;
                driversValue.LapInvalidated -= LapInvalidatedHandler;
                driversValue.LapCompleted -= DriverOnLapCompleted;
                driversValue.LapTimeReevaluated -= DriverOnLapTimeReevaluated;
            }

            Drivers.Clear();

            await SessionTelemetryController.StopControllerAsync();
            _bestInformationSwapCancellationToken.Cancel();
            try
            {
                await _bestInformationSwapTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task CycleBestInformation(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(20000, cancellationToken);
                if (LastSet?.SessionInfo.IsMultiClass != true || string.IsNullOrWhiteSpace(LastSet.PlayerInfo?.CarClassId))
                {
                    SessionBestTimesViewModel.IsSetNameShown = false;
                    TimingApplicationViewModel.SessionInfoViewModel.SelectedBestTimes = SessionBestTimesViewModel;
                    continue;
                }

                SessionBestTimesViewModel.IsSetNameShown = true;
                switch (DisplaySettingsViewModel.SelectedSessionBestInformationKind.Value)
                {
                    case SessionBestInformationKind.BestOverall:
                        TimingApplicationViewModel.SessionInfoViewModel.SelectedBestTimes = SessionBestTimesViewModel;
                        break;
                    case SessionBestInformationKind.BestOfClass:
                        TimingApplicationViewModel.SessionInfoViewModel.SelectedBestTimes = GetBestTimesForClass(LastSet.PlayerInfo.CarClassId);
                        break;
                    case SessionBestInformationKind.BestOverallAndClass:
                        TimingApplicationViewModel.SessionInfoViewModel.SelectedBestTimes =
                            TimingApplicationViewModel.SessionInfoViewModel.SelectedBestTimes == SessionBestTimesViewModel ? GetBestTimesForClass(LastSet.PlayerInfo.CarClassId)
                                : SessionBestTimesViewModel;
                        break;
                    case SessionBestInformationKind.AllClasses:
                        List<BestTimesSetViewModel> allClassesInfo = _bestTimesForClasses.Values.ToList();
                        int currentIndex = allClassesInfo.IndexOf(TimingApplicationViewModel.SessionInfoViewModel.SelectedBestTimes);
                        int nextIndex = (currentIndex + 1) % allClassesInfo.Count;
                        TimingApplicationViewModel.SessionInfoViewModel.SelectedBestTimes = allClassesInfo[nextIndex];
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
