namespace SecondMonitor.PCars2Connector
{
    using Contracts.NInject;
    using Contracts.SimSettings;
    using DataModel;
    using Ninject.Modules;

    public class PCars2ConnectorModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ISimSettings>().To<PCars2SimSetting>().WithMetadata(BindingMetadataIds.SimulatorNameBinding, SimulatorsNameMap.PCars2SimName);
        }
    }
}