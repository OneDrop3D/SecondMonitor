namespace SecondMonitor.Telemetry.TelemetryManagement
{
    using Migrations;
    using Ninject.Modules;

    public class TelemetryManagementModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ITelemetryMigration>().To<V000WheelKindMigration>();
            Bind<ITelemetryMigration>().To<V001DownforceSourceInfoMigration>();
        }
    }
}