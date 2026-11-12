namespace SecondMonitor.Rating.Application.Rating.Controller.RaceObserver.States
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Common.DataModel.Player;
    using Context;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Summary;
    using NLog;

    public abstract class AbstractSessionTypeState : IRaceState
    {
        private readonly Stopwatch _stateStopwatch;
        protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected AbstractSessionTypeState(SharedContext sharedContext)
        {
            SharedContext = sharedContext;
            SessionDescription = string.Empty;
            _stateStopwatch = Stopwatch.StartNew();
        }

        public abstract SessionKind SessionKind { get; protected set; }
        public abstract SessionPhaseKind SessionPhaseKind { get; protected set; }
        public IRaceState NextState { get; protected set; }
        public string SessionDescription { get; protected set; }

        public abstract bool ShowRatingChange { get; }

        public abstract bool CanUserSelectClass { get; }
        protected abstract SessionType SessionType { get; }

        public SharedContext SharedContext { get; }

        protected bool IsStateInitialized { get; set; }

        public virtual Task DoSessionCompletion(SessionSummary sessionSummary)
        {
            if (sessionSummary.SessionType == SessionType.Qualification)
            {
                CreateQualificationResults(sessionSummary);
            }

            return Task.CompletedTask;
        }

        protected void CreateQualificationResults(SessionSummary sessionSummary)
        {
            if (sessionSummary.SessionRunDuration.TotalSeconds < 20)
            {
                Logger.Info("Session time too short.");
                return;
            }

            if (sessionSummary.SessionType != SessionType.Qualification)
            {
                Logger.Info("Session is not qualification");
                return;
            }

            if (!IsSessionResultAcceptable(sessionSummary))
            {
                Logger.Info("Seesion is not acceptable to be used by rating calculations. Not enough drivers with laps times. Session Summary type was:" + sessionSummary.SessionType);
                return;
            }

            List<Driver> eligibleDrivers = FilterNotEligibleDriversAndOrder(sessionSummary);
            if (eligibleDrivers == null || eligibleDrivers.Count == 0)
            {
                Logger.Info("Seesion is not acceptable to be used by rating calculations - no drivers");
                return;
            }

            if (sessionSummary.SessionRunDuration.TotalMinutes > 2)
            {
                Logger.Info("Seesion is acceptable to be used by rating calculations.");
                eligibleDrivers.ForEach(x => Logger.Info($"Driver for qualification Context : {x.DriverId}"));
                SharedContext.QualificationContext = new QualificationContext { QualificationDifficulty = SharedContext.UserSelectedDifficulty, LastQualificationResult = eligibleDrivers };
            }
        }

        private bool IsSessionResultAcceptable(SessionSummary sessionSummary)
        {
            return sessionSummary.Drivers.Count(x => x.BestPersonalLap != null) > sessionSummary.Drivers.Count / 2;
        }

        private List<Driver> FilterNotEligibleDriversAndOrder(SessionSummary sessionSummary)
        {
            if (!sessionSummary.IsMultiClass)
            {
                return sessionSummary.Drivers.OrderBy(x => x.FinishingPosition).ToList();
            }

            Driver player = sessionSummary.Drivers.FirstOrDefault(x => x.IsPlayer);
            return player == null ? new List<Driver>() : sessionSummary.IsMultiClass ? sessionSummary.Drivers.Where(x => x.ClassId == player.ClassId).OrderBy(x => x.FinishingPosition).ToList() : sessionSummary.Drivers.OrderBy(x => x.FinishingPosition).ToList();
        }

        protected abstract void Initialize(SimulatorDataSet simulatorDataSet);

        public virtual Task DoDataLoaded(SimulatorDataSet simulatorDataSet)
        {
            if (!IsStateInitialized && simulatorDataSet.SessionInfo.SessionPhase != SessionPhase.Unavailable)
            {
                Initialize(simulatorDataSet);
                IsStateInitialized = true;
            }

            if (simulatorDataSet.SessionInfo.SessionType != SessionType.Race)
            {
                SessionPhaseKind = simulatorDataSet.SessionInfo.SessionPhase == SessionPhase.Countdown ? SessionPhaseKind.NotStarted : SessionPhaseKind.InProgress;
            }

            if (simulatorDataSet.SessionInfo.SessionType == SessionType)
            {
                return Task.CompletedTask;
            }

            SharedContext.SwitchToStateBasedOnSessionType(simulatorDataSet.SessionInfo.SessionType);
            return Task.CompletedTask;
        }

        public virtual bool TryGetDriverRating(string driverId, out DriversRating driversRating)
        {
            driversRating = new DriversRating();
            return false;
        }
    }
}