namespace SecondMonitor.Timing.Common
{
    using Contracts.NInject;
    using Ninject.Modules;
    using PitStatistics;

    using SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap.FinishCondition;

    using SessionTiming;
    using SessionTiming.Drivers;
    using SessionTiming.Drivers.Lap.SectorTracker;
    using SessionTiming.Drivers.Ordering;
    using ViewModels.Settings.Model;

    public class TimingCommonModule : NinjectModule
    {
        public override void Load()
        {
            Bind<DriverLapSectorsTrackerFactory>().ToSelf();
            Bind<IDriverLapSectorsTracker>().To<DriverLapSectorsTracker>();
            Bind<DriversOrderingFactory>().ToSelf();
            Bind<DriverTimingFactory>().ToSelf();
            Bind<DriverTiming>().ToSelf();
            Bind<IDriversOrdering>().To<AbsoluteDriversOrdering>().WithMetadata(BindingMetadataIds.DriversOrdering, nameof(DriverOrderKind.Absolute));
            Bind<IDriversOrdering>().To<RelativeDriversOrdering>().WithMetadata(BindingMetadataIds.DriversOrdering, nameof(DriverOrderKind.Relative));
            Bind<IDriversOrdering>().To<HybridByClassOrdering>().WithMetadata(BindingMetadataIds.DriversOrdering, nameof(DriverOrderKind.HybridByClass));
            Bind<IDriversOrdering>().To<AbsoluteByClassOrdering>().WithMetadata(BindingMetadataIds.DriversOrdering, nameof(DriverOrderKind.AbsoluteByClass));
            Bind<IDriversOrdering>().To<HybridOrdering>().WithMetadata(BindingMetadataIds.DriversOrdering, nameof(DriverOrderKind.Hybrid));
            Bind<ISessionInfoProvider, SessionInfoProvider>().To<SessionInfoProvider>().InSingletonScope();

            Bind<IPitStopEvenProvider>().To<PitStopEvenProvider>().InSingletonScope();
            Bind<ISpecificLapTracker>().To<PitTimeLostTracker>();

            Bind<ILapFinishCondition>().To<Ams2TimeTrialFirstLapCondition>();
        }
    }
}