namespace SecondMonitor.Rating.Common
{
    using Configuration;
    using Factories;
    using Ninject.Modules;
    using Repository;

    public class RatingCommonModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IRatingRepository>().To<RatingRepository>().InSingletonScope();
            Bind<ISimulatorRatingConfigurationProvider>().To<SimulatorRatingConfigurationProvider>();
            Bind<ISessionFinishStateFactory>().To<SessionFinishStateFactory>();

            Bind<IDriverAveragePositionAdapter>().To<SimpleAveragePositionAdapter>().WhenInjectedInto<PitCompensateAveragePositionAdapter>();
            Bind<IDriverAveragePositionAdapter>().To<PitCompensateAveragePositionAdapter>().WhenInjectedInto<ISessionFinishStateFactory>();
        }
    }
}