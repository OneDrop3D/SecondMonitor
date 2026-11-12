namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap.SectorTracker
{
    using Ninject;
    using Ninject.Parameters;
    using Ninject.Syntax;

    public class DriverLapSectorsTrackerFactory
    {
        private readonly IResolutionRoot _resolutionRoot;

        public DriverLapSectorsTrackerFactory(IResolutionRoot resolutionRoot)
        {
            _resolutionRoot = resolutionRoot;
        }

        public IDriverLapSectorsTracker Build(DriverTiming driverTiming)
        {
            TypeMatchingConstructorArgument driverTimingArgument = new TypeMatchingConstructorArgument(typeof(DriverTiming), (_, __) => driverTiming);
            return _resolutionRoot.Get<IDriverLapSectorsTracker>(driverTimingArgument);
        }
    }
}