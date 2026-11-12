namespace SecondMonitor.RF2Connector
{
    using Contracts.NInject;
    using Contracts.SimSettings;
    using DataModel;
    using Ninject.Modules;

    public class Rf2ConnectorModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ISimSettings>().To<Rf2SimSettings>().WithMetadata(BindingMetadataIds.SimulatorNameBinding, SimulatorsNameMap.Rf2SimName);
        }
    }
}
