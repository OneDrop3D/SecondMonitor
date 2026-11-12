namespace SecondMonitor.Rating.Application.Rating.Controller.RaceObserver.States
{
    using Context;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;

    public class IdleState : AbstractSessionTypeState
    {
        public IdleState(SharedContext sharedContext) : base(sharedContext)
        {
        }

        public override SessionKind SessionKind { get; protected set; } = SessionKind.Idle;
        public override SessionPhaseKind SessionPhaseKind { get; protected set; } = SessionPhaseKind.None;

        protected override SessionType SessionType => SessionType.Na;

        public override bool ShowRatingChange => true;

        public override bool CanUserSelectClass => true;

        protected override void Initialize(SimulatorDataSet simulatorDataSet)
        {
        }
    }
}