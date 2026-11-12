namespace SecondMonitor.PluginManager.SimulatorSettings
{
    using Contracts.NInject;
    using Contracts.SimSettings;
    using Ninject.Syntax;

    public class SimSettingsFactory : AbstractBindingMetadataFactory<ISimSettings>, ISimSettingsFactory
    {
        public SimSettingsFactory(IResolutionRoot resolutionRoot) : base(resolutionRoot)
        {
        }

        protected override string MetadataName => BindingMetadataIds.SimulatorNameBinding;
        protected override ISimSettings CreateEmpty()
        {
            return new DefaultSimulatorSettings();
        }
    }
}