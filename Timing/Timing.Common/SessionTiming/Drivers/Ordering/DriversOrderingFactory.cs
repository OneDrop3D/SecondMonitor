namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Ordering
{
    using Contracts.NInject;
    using Ninject.Syntax;

    public class DriversOrderingFactory : AbstractBindingMetadataFactory<IDriversOrdering>
    {
        public DriversOrderingFactory(IResolutionRoot resolutionRoot) : base(resolutionRoot)
        {
        }

        protected override string MetadataName => BindingMetadataIds.DriversOrdering;
        protected override IDriversOrdering CreateEmpty()
        {
            return new AbsoluteDriversOrdering();
        }
    }
}