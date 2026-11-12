namespace SecondMonitor.Remote
{
    using Adapter;
    using Comparators;
    using LiteNetLib;
    using Models;
    using Ninject.Modules;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.PluginsSettings;
    using ViewModels;

    public class RemoteModule : NinjectModule
    {
        public override void Load()
        {
            this.Bind<IDatagramPayloadUnPacker>().To<DatagramPayloadUnPacker>();
            this.Bind<IDatagramPayloadPacker>().To<DatagramPayloadPacker>();
            this.Bind<ISimulatorSourceInfoComparator>().To<SimulatorSourceInfoComparator>();
            this.Bind<INetworkStatsViewModel, IViewModel<NetStatistics>>().To<NetworkStatsViewModel>();
            this.Rebind<IHostAddressValidator>().To<LiteNetLibHostAddressValidator>();
        }
    }
}