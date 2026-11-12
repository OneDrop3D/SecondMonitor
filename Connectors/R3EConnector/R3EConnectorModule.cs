namespace SecondMonitor.R3EConnector
{
    using Contracts.NInject;
    using Contracts.SimSettings;
    using DataModel;
    using Ninject.Modules;

    public class R3EConnectorModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ISimSettings>().To<R3ESimSettings>().WithMetadata(BindingMetadataIds.SimulatorNameBinding, SimulatorsNameMap.R3ESimName);
        }
    }
}