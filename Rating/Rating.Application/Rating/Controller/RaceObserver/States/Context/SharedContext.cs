namespace SecondMonitor.Rating.Application.Rating.Controller.RaceObserver.States.Context
{
    using System;

    using Common.DataModel.Player;

    using DataModel.BasicProperties;

    using SimulatorRating;

    public class SharedContext
    {
        private readonly IRaceStateFactory _stateFactory;

        public SharedContext(IRaceStateFactory stateFactory)
        {
            _stateFactory = stateFactory;
            SwitchToInitialState();
        }

        public int UserSelectedDifficulty { get; set; }
        public QualificationContext QualificationContext { get; set; }
        public RaceContext RaceContext { get; set; }
        public ISimulatorRatingController SimulatorRatingController { get; set; }

        public DriversRating ClassRating { get; set; }

        public DriversRating DifficultyRating { get; set; }

        public DriversRating SimulatorRating { get; set; }

        public IRaceState CurrentState { get; private set; }

        public void SwitchToInitialState()
        {
            CurrentState = _stateFactory.CreateInitialState(this);
        }

        public void SwitchToStateBasedOnSessionType(SessionType sessionType)
        {
            switch (sessionType)
            {
                case SessionType.Na:
                    CurrentState = _stateFactory.Create<IdleState>(this);
                    break;
                case SessionType.WarmUp:
                    CurrentState = _stateFactory.Create<WarmupState>(this);
                    break;
                case SessionType.Practice:
                    CurrentState = _stateFactory.Create<PracticeState>(this);
                    break;
                case SessionType.Qualification:
                    CurrentState = _stateFactory.Create<QualificationState>(this);
                    break;
                case SessionType.Race:
                    CurrentState = _stateFactory.Create<RaceState>(this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
