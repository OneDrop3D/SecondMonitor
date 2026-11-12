namespace SecondMonitor.LmUConnector
{
    using Ninject.Modules;

    using SecondMonitor.Contracts.NInject;
    using SecondMonitor.Contracts.SimSettings;
    using SecondMonitor.DataModel;

    public class LmUConnectorModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ISimSettings>().To<LmUSimSettings>().WithMetadata(BindingMetadataIds.SimulatorNameBinding, SimulatorsNameMap.LmUSimName);
        }
    }
}
