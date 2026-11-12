namespace SecondMonitor.Connectors.Remote
{
    using Controllers;
    using Factories;
    using Models;
    using Ninject.Extensions.Factory;
    using Ninject.Modules;
    using ViewModels;

    public class RemoteConnectorModule : NinjectModule
    {
        public override void Load()
        {
            this.Bind<IDatagramPayloadUnPackerFactory>().ToFactory().InSingletonScope();
            this.Bind<RemoteConnectorOptionsViewModel>().ToSelf().InSingletonScope();
            this.Bind<IRemoteConnectorConfiguration>().To<RemotePluginSettingsAdaptor>();
            this.Bind<IRemoteClient>().To<RemoteClient>().InSingletonScope();
            this.Bind<RemoteConnectorController>().ToSelf();
            this.Bind<SessionManager>().ToSelf();
        }
    }
}
