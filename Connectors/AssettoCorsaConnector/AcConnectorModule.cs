namespace SecondMonitor.AssettoCorsaConnector
{
    using Contracts.NInject;
    using Contracts.SimSettings;
    using DataModel;
    using Ninject.Modules;

    public class AcConnectorModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ISimSettings>().To<AcSimSettings>().WithMetadata(BindingMetadataIds.SimulatorNameBinding, SimulatorsNameMap.AcSimName);
        }
    }
}