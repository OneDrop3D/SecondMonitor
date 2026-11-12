namespace SecondMonitor.Rating.Application.Rating.Controller.RaceObserver
{
    using States;
    using States.Context;

    public interface IRaceStateFactory
    {
        IRaceState CreateInitialState(SharedContext sharedContext);
        T Create<T>(SharedContext sharedContext) where T : AbstractSessionTypeState;
    }
}