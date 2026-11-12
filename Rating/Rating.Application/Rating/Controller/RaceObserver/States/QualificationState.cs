namespace SecondMonitor.Rating.Application.Rating.Controller.RaceObserver.States
{
    using System.Threading.Tasks;

    using Context;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;

    public class QualificationState : AbstractSessionTypeState
    {
        public QualificationState(SharedContext sharedContext) : base(sharedContext)
        {
        }

        public override SessionKind SessionKind { get; protected set; } = SessionKind.Qualification;
        public override SessionPhaseKind SessionPhaseKind { get; protected set; }
        public override bool CanUserSelectClass => false;
        protected override SessionType SessionType => SessionType.Qualification;
        public override bool ShowRatingChange => false;

        public override Task DoDataLoaded(SimulatorDataSet simulatorDataSet)
        {
            if (SharedContext.QualificationContext != null && SharedContext.UserSelectedDifficulty != SharedContext.QualificationContext.QualificationDifficulty)
            {
                SharedContext.QualificationContext.QualificationDifficulty = SharedContext.UserSelectedDifficulty;
                SessionDescription = SharedContext.QualificationContext.QualificationDifficulty.ToString();
            }

            return base.DoDataLoaded(simulatorDataSet);
        }

        protected override void Initialize(SimulatorDataSet simulatorDataSet)
        {
            if (SharedContext.QualificationContext == null)
            {
                SharedContext.QualificationContext = new QualificationContext() { QualificationDifficulty = SharedContext.UserSelectedDifficulty };
            }
            else
            {
                SharedContext.QualificationContext.QualificationDifficulty = SharedContext.UserSelectedDifficulty;
            }

            SessionDescription = SharedContext.QualificationContext.QualificationDifficulty.ToString();
        }
    }
}