 namespace SecondMonitor.Timing.Common.SessionTiming.Drivers
{
    using Contracts.NInject;
    using DataModel.Snapshot.Drivers;
    using Ninject;
    using Ninject.Syntax;

    public class DriverTimingFactory
    {
        private readonly IResolutionRoot _resolutionRoot;

        public DriverTimingFactory(IResolutionRoot resolutionRoot)
        {
            _resolutionRoot = resolutionRoot;
        }

        public DriverTiming Create(DriverInfo modelDriverInfo, ISessionInfo session, bool invalidateFirstLap)
        {
            TypeMatchingConstructorArgument<DriverInfo> driverInfoArgument = new((_, __) => modelDriverInfo);
            TypeMatchingConstructorArgument<ISessionInfo> sessionInfoArgument = new((_, __) => session);
            var driver = _resolutionRoot.Get<DriverTiming>(driverInfoArgument, sessionInfoArgument);
            driver.InvalidateFirstLap = invalidateFirstLap;
            return driver;
        }
    }
}