namespace SecondMonitor.PluginManager
{
    using System;
    using Contracts.SimSettings;
    using DataModel.Snapshot;
    using DataModel.Visitors;
    using Ninject.Modules;
    using SimulatorSettings;

    public class PluginsManagerModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ISimulatorDataSetVisitor>().To<ComputeGapToPlayerVisitor>().WithConstructorArgument("informationValiditySpan", TimeSpan.FromMilliseconds(300));
            Bind<ISimulatorDataSetVisitor>().To<ComputeSuspensionVelocityVisitor>();
            Bind<ISimulatorDataSetVisitor>().To<YellowFlagPopulatorVisitor>();

            Bind<ISimSettings>().To<DefaultSimulatorSettings>();
            Bind<ISimSettingsFactory>().To<SimSettingsFactory>();
        }
    }
}