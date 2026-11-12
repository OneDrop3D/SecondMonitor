namespace SecondMonitor.RFactorConnector
{
    using Contracts.NInject;
    using Contracts.SimSettings;
    using DataModel;
    using Ninject.Modules;

    public class RFactorConnectorModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ISimSettings>().To<RFactorSimSettings>().WithMetadata(BindingMetadataIds.SimulatorNameBinding, SimulatorsNameMap.AmsSimName);
            Bind<ISimSettings>().To<RFactorSimSettings>().WithMetadata(BindingMetadataIds.SimulatorNameBinding, SimulatorsNameMap.Gtr2SimName);
        }
    }
}
