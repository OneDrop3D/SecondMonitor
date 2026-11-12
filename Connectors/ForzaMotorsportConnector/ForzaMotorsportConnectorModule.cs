namespace SecondMonitor.ForzaMotorsportConnector
{
    using Contracts.NInject;
    using Contracts.SimSettings;
    using DataModel;
    using Ninject.Modules;

    public class ForzaMotorsportConnectorModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ISimSettings>().To<ForzaMotorsportSimSetting>().WithMetadata(BindingMetadataIds.SimulatorNameBinding, SimulatorsNameMap.ForzaMotorsportSimName);
        }
    }
}