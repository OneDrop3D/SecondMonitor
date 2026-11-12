namespace SecondMonitor.Plugins.Remote
{
    using Controllers;
    using Ninject.Modules;
    using ViewModels;

    public class RemotePluginModule : NinjectModule
    {
        public override void Load()
        {
            this.Bind<IServerOverviewViewModel>().To<ServerOverviewViewModel>().InSingletonScope();
            this.Bind<IClientViewModel>().To<ClientViewModel>();
            this.Bind<IBroadcastServerConfiguration>().To<BroadcastServerPluginSettingsAdaptor>();

            this.Bind<IBroadCastServerController>().To<BroadCastServerController>();
        }
    }
}