namespace SecondMonitor.AccConnector
{
    using Contracts.NInject;
    using Contracts.SimSettings;
    using DataModel;
    using Ninject.Modules;
    using Repository;

    public class AccConnectorModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ISimSettings>().To<AccSimSettings>().WithMetadata(BindingMetadataIds.SimulatorNameBinding, SimulatorsNameMap.AccSimName);
            Bind<AccSettingsRepository>().To<AccSettingsRepository>();
        }
    }
}