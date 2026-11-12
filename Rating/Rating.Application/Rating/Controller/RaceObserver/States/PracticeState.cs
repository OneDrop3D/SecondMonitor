namespace SecondMonitor.Rating.Application.Rating.Controller.RaceObserver.States
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Context;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using Timing.Common.SessionTiming;
    using Timing.Common.SessionTiming.Drivers.Lap;

    public class PracticeState : AbstractSessionTypeState
    {
        private readonly ISessionInfoProvider _sessionInfoProvider;
        private readonly Stopwatch _refreshInfoWatch;

        public PracticeState(SharedContext sharedContext, ISessionInfoProvider sessionInfoProvider) : base(sharedContext)
        {
            _refreshInfoWatch = Stopwatch.StartNew();
            _sessionInfoProvider = sessionInfoProvider;
        }

        public override SessionKind SessionKind { get; protected set; } = SessionKind.OtherSession;

        public override SessionPhaseKind SessionPhaseKind { get; protected set; }
        public override bool CanUserSelectClass => false;

        public override bool ShowRatingChange => true;

        protected override SessionType SessionType => SessionType.Practice;

        public override Task DoDataLoaded(SimulatorDataSet simulatorDataSet)
        {
            if (_refreshInfoWatch.ElapsedMilliseconds <= 10000)
            {
                return base.DoDataLoaded(simulatorDataSet);
            }

            _refreshInfoWatch.Restart();
            RefreshBestLapInfo();
            return base.DoDataLoaded(simulatorDataSet);
        }

        private void RefreshBestLapInfo()
        {
            ISessionInfo sessionInfo = _sessionInfoProvider.SessionInfo;
            if (sessionInfo.Player == null)
            {
                return;
            }

            ILapInfo bestPlayerLap = sessionInfo.Player.BestLap;
            ILapInfo bestAiLap = sessionInfo.GetBestLapExcludePlayer(sessionInfo.Player.CarClassId);
            if (bestPlayerLap == null || bestAiLap == null || bestAiLap.LapTime == TimeSpan.Zero || bestPlayerLap.LapTime == TimeSpan.Zero)
            {
                SessionDescription = string.Empty;
                return;
            }

            double aiLapTime = bestAiLap.LapTime.TotalSeconds;
            double playerLapTime = bestPlayerLap.LapTime.TotalSeconds;

            double difference = (((playerLapTime - aiLapTime) / ((playerLapTime + aiLapTime) / 2)) * 100);
            SessionDescription = $"Best AI Diff: {difference:F1}%";
        }

        protected override void Initialize(SimulatorDataSet simulatorDataSet)
        {
        }
    }
}