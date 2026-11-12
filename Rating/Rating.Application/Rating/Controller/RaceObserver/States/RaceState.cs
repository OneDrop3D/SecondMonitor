namespace SecondMonitor.Rating.Application.Rating.Controller.RaceObserver.States
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Common.DataModel.Player;
    using Common.Factories;
    using Context;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using DataModel.Summary;
    using RatingProvider.FieldRatingProvider;
    using SecondMonitor.ViewModels.Settings;
    using SimulatorRating.RatingUpdater;

    public class RaceState : AbstractSessionTypeState
    {
        private readonly IQualificationResultRatingProvider _qualificationResultRatingProvider;
        private readonly IRatingUpdater _ratingUpdater;
        private readonly ISessionFinishStateFactory _finishStateFactory;
        private readonly int _graceLaps;
        private readonly Stopwatch _flashStopwatch;
        private bool _isFlashing;
        private bool _ratingComputed;
        private bool _wasMoving;

        public RaceState(IQualificationResultRatingProvider qualificationResultRatingProvider, IRatingUpdater ratingUpdater, ISessionFinishStateFactory finishStateFactory, SharedContext sharedContext,
            ISettingsProvider settingsProvider) : base(sharedContext)
        {
            _qualificationResultRatingProvider = qualificationResultRatingProvider;
            _ratingUpdater = ratingUpdater;
            _finishStateFactory = finishStateFactory;
            _graceLaps = settingsProvider.DisplaySettingsViewModel.RatingSettingsViewModel.GraceLapsCount;
            _flashStopwatch = new Stopwatch();
            _ratingComputed = false;
            _wasMoving = false;
        }

        public override SessionKind SessionKind { get; protected set; } = SessionKind.RaceWithoutQualification;
        public override SessionPhaseKind SessionPhaseKind { get; protected set; }
        public override bool CanUserSelectClass => false;
        public override bool ShowRatingChange => _ratingComputed || SharedContext?.RaceContext?.WasQuit == true;
        protected override SessionType SessionType => SessionType.Race;

        protected override void Initialize(SimulatorDataSet simulatorDataSet)
        {
            _isFlashing = false;
            _ratingComputed = false;
            _wasMoving = false;
            DriverInfo[] eligibleDrivers = FilterEligibleDrivers(simulatorDataSet);
            if (CanUserPreviousRaceContext(eligibleDrivers))
            {
                Logger.Info("Using Previous Race Context");
                SessionKind = SharedContext.RaceContext.IsRatingBasedOnQualification ? SessionKind.RaceWithQualification : SessionKind.RaceWithoutQualification;
                SessionDescription = SharedContext.RaceContext.UsedDifficulty.ToString();
                return;
            }

            Logger.Info("Cannot use Race Context");
            int difficultyToUse = SharedContext.QualificationContext?.QualificationDifficulty ?? SharedContext.UserSelectedDifficulty;

            SharedContext.RaceContext = new RaceContext()
            {
                UsedDifficulty = difficultyToUse,
            };
            SessionDescription = difficultyToUse.ToString();

            if (CanUseQualification(eligibleDrivers) && SharedContext.QualificationContext != null)
            {
                Logger.Info("Can use qualification result");
                SessionKind = SessionKind.RaceWithQualification;
                SharedContext.RaceContext.IsRatingBasedOnQualification = true;
                SharedContext.RaceContext.FieldRating = _qualificationResultRatingProvider.CreateFieldRatingFromQualificationResult(SharedContext.QualificationContext.LastQualificationResult, difficultyToUse);
            }
            else
            {
                Logger.Info("Cannot use qualification result");
                SharedContext.RaceContext.FieldRating = _qualificationResultRatingProvider.CreateFieldRating(eligibleDrivers, difficultyToUse);
            }
        }

        private DriverInfo[] FilterEligibleDrivers(SimulatorDataSet simulatorDataSetDataSet)
        {
            var eligibleDrivers = simulatorDataSetDataSet.SessionInfo.IsMultiClass ? simulatorDataSetDataSet.DriversInfo.Where(x => x.CarClassId == simulatorDataSetDataSet.PlayerInfo.CarClassId) : simulatorDataSetDataSet.DriversInfo;
            return eligibleDrivers.ToArray();
        }

        public override Task DoDataLoaded(SimulatorDataSet simulatorDataSet)
        {
            if (!IsStateInitialized && simulatorDataSet.DriversInfo.Any(x => string.IsNullOrWhiteSpace(x.DriverSessionId)))
            {
                return Task.CompletedTask;
            }

            if (simulatorDataSet.PlayerInfo == null)
            {
                return Task.CompletedTask;
            }

            if (IsStateInitialized && simulatorDataSet.PlayerInfo != null && simulatorDataSet.SessionInfo.SessionType == SessionType.Race && simulatorDataSet.PlayerInfo.FinishStatus == DriverFinishStatus.None && simulatorDataSet.GetDriversInfoByMultiClass().Count != SharedContext.RaceContext.FieldRating.Count)
            {
                Initialize(simulatorDataSet);
            }

            // Second Race - Session didn't change, but player state has
            if (_ratingComputed && simulatorDataSet.PlayerInfo.FinishStatus == DriverFinishStatus.None)
            {
                Initialize(simulatorDataSet);
            }

            if (_isFlashing)
            {
                Flash();
                return base.DoDataLoaded(simulatorDataSet);
            }

            SessionKind = SharedContext.RaceContext?.IsRatingBasedOnQualification == true ? SessionKind.RaceWithQualification : SessionKind.RaceWithoutQualification;

            if (simulatorDataSet.SessionInfo.SessionPhase == SessionPhase.Countdown)
            {
                SessionPhaseKind = SessionPhaseKind.NotStarted;
            }
            else if (!_wasMoving)
            {
                _wasMoving = simulatorDataSet.PlayerInfo.Speed.InMs > 5;
                SessionPhaseKind = SessionPhaseKind.NotStarted;
            }
            else if (simulatorDataSet.PlayerInfo.CompletedLaps < _graceLaps)
            {
                SessionPhaseKind = SessionPhaseKind.FreeRestartPeriod;
            }
            else
            {
                SessionPhaseKind = SessionPhaseKind.InProgress;
            }

            /*if (!_isFlashing && simulatorDataSet.PlayerInfo.FinishStatus == DriverFinishStatus.Finished && (simulatorDataSet.SessionInfo.SessionPhase == SessionPhase.Green || simulatorDataSet.SessionInfo.SessionPhase == SessionPhase.Checkered))
            {
                StartFlashing();
                await ComputeRatingFromResults(simulatorDataSet);
                _ratingComputed = true;
            }*/

            return base.DoDataLoaded(simulatorDataSet);
        }

        private void Flash()
        {
            if (_flashStopwatch.ElapsedMilliseconds <= 800)
            {
                return;
            }

            _flashStopwatch.Restart();
            SessionKind = SessionKind == SessionKind.RaceWithQualification ? SessionKind.Idle : SessionKind.RaceWithQualification;
            SessionPhaseKind = SessionPhaseKind == SessionPhaseKind.None ? SessionPhaseKind.InProgress : SessionPhaseKind.None;
        }

        private void StartFlashing()
        {
            _isFlashing = true;
            _flashStopwatch.Start();
            SessionKind = SessionKind.RaceWithQualification;
            SessionPhaseKind = SessionPhaseKind.None;
        }

        public override async Task DoSessionCompletion(SessionSummary sessionSummary)
        {
            Logger.Info($"Session Completed, status = {SessionPhaseKind}");
            if (sessionSummary.SessionType != SessionType.Race)
            {
                await base.DoSessionCompletion(sessionSummary);
                IsStateInitialized = false;
                return;
            }

            if (sessionSummary.SessionRunDuration.TotalMinutes < 1)
            {
                return;
            }

            Driver player = sessionSummary.Drivers.FirstOrDefault(x => x.IsPlayer);
            if (_ratingComputed || player == null)
            {
                await base.DoSessionCompletion(sessionSummary);
                return;
            }

            _ratingComputed = true;

            if (player.FinishStatus == DriverFinishStatus.Finished)
            {
                await ComputeRatingFromResults(sessionSummary);
                _ratingComputed = true;
                StartFlashing();
                return;
            }

            if (SessionPhaseKind == SessionPhaseKind.InProgress)
            {
                await ComputeRatingAsLast(sessionSummary);
                SharedContext.RaceContext.WasQuit = true;
            }
        }

        private async Task ComputeRatingAsLast(SessionSummary sessionSummary)
        {
            Driver player = sessionSummary.Drivers.FirstOrDefault(x => x.IsPlayer);
            if (player == null)
            {
                return;
            }

            var newRatings = await _ratingUpdater.UpdateRatingsAsLoss(SharedContext.RaceContext.FieldRating, SharedContext.ClassRating, SharedContext.DifficultyRating,
                SharedContext.SimulatorRating, SharedContext.RaceContext.UsedDifficulty, player, sessionSummary.TrackInfo.TrackFullName);
        }

        private async Task ComputeRatingFromResults(SessionSummary sessionSummary)
        {
            var newRatings = await _ratingUpdater.UpdateRatingsByResults(SharedContext.RaceContext.FieldRating, SharedContext.ClassRating, SharedContext.DifficultyRating,
                SharedContext.SimulatorRating, _finishStateFactory.Create(sessionSummary), SharedContext.RaceContext.UsedDifficulty);
        }

        private bool CanUseQualification(DriverInfo[] eligibleDrivers)
        {
            if (SharedContext.QualificationContext?.LastQualificationResult == null)
            {
                Logger.Info("No previous qualification result");
                return false;
            }

            if (SharedContext.QualificationContext.LastQualificationResult.All(x => !x.IsPlayer))
            {
                Logger.Info("No player results found");
                return false;
            }

            List<string> missingDrivers = eligibleDrivers.Select(x => x.DriverSessionId).Except(SharedContext.QualificationContext.LastQualificationResult.Select(y => y.DriverId)).ToList();
            missingDrivers.ForEach(x => Logger.Info($"Missing rating qualification result for: {x}"));
            return missingDrivers.Count == 0;
        }

        private bool CanUserPreviousRaceContext(DriverInfo[] eligibleDrivers)
        {
            if (SharedContext.RaceContext?.FieldRating == null)
            {
                Logger.Info("No Previous Race Context");
                return false;
            }

            if (SharedContext.RaceContext.UsedDifficulty != SharedContext.UserSelectedDifficulty)
            {
                Logger.Info("Difficulty Differs");
                return false;
            }

            return eligibleDrivers.Length == SharedContext.RaceContext.FieldRating.Count && eligibleDrivers.All(x => SharedContext.RaceContext.FieldRating.ContainsKey(x.DriverSessionId));
        }

        public override bool TryGetDriverRating(string driverId, out DriversRating driversRating)
        {
            if (SharedContext?.RaceContext?.FieldRating != null)
            {
                return SharedContext.RaceContext.FieldRating.TryGetValue(driverId, out driversRating);
            }

            driversRating = new DriversRating();
            return false;
        }
    }
}