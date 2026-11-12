namespace SecondMonitor.AMS2Connector
{
    using Contracts.NInject;
    using Contracts.SimSettings;
    using DataModel;
    using Ninject.Modules;

    public class Ams2ConnectorModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ISimSettings>().To<Ams2SimSetting>().WithMetadata(BindingMetadataIds.SimulatorNameBinding, SimulatorsNameMap.Ams2SimName);
        }
    }
}