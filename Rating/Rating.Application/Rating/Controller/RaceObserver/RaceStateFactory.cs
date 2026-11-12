namespace SecondMonitor.Rating.Application.Rating.Controller.RaceObserver
{
    using Contracts.NInject;
    using Ninject;
    using Ninject.Syntax;
    using States;
    using States.Context;

    public class RaceStateFactory : IRaceStateFactory
    {
        private readonly IResolutionRoot _resolutionRoot;

        public RaceStateFactory(IResolutionRoot resolutionRoot)
        {
            _resolutionRoot = resolutionRoot;
        }

        public IRaceState CreateInitialState(SharedContext sharedContext)
        {
            return Create<IdleState>(sharedContext);
        }

        public T Create<T>(SharedContext sharedContext) where T : AbstractSessionTypeState
        {
            TypeMatchingConstructorArgument<IRaceStateFactory> raceStateFactoryArgument = new TypeMatchingConstructorArgument<IRaceStateFactory>((context, target) => this);
            TypeMatchingConstructorArgument<SharedContext> sharedContextArgument = new TypeMatchingConstructorArgument<SharedContext>((context, target) => sharedContext);
            return _resolutionRoot.Get<T>(sharedContextArgument, raceStateFactoryArgument);
        }
    }
}