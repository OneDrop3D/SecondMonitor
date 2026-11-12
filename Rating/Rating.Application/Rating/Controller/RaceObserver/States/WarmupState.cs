namespace SecondMonitor.Rating.Application.Rating.Controller.RaceObserver.States
{
    using Context;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;

    public class WarmupState : AbstractSessionTypeState
    {
        public WarmupState(SharedContext sharedContext) : base(sharedContext)
        {
        }

        public override SessionKind SessionKind { get; protected set; } = SessionKind.OtherSession;

        public override SessionPhaseKind SessionPhaseKind { get; protected set; } = SessionPhaseKind.None;

        public override bool ShowRatingChange => true;

        public override bool CanUserSelectClass => false;

        protected override SessionType SessionType => SessionType.WarmUp;

        protected override void Initialize(SimulatorDataSet simulatorDataSet)
        {
        }
    }
}