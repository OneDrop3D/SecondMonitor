namespace SecondMonitor.Timing.Api
{
    using Ninject.Modules;

    using SecondMonitor.Timing.Common.TimingPlugin;

    public class TimingApiModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ITimingPlugin>().To<TimingRestApiPlugin>();
        }
    }
}