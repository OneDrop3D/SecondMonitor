namespace SecondMonitor.Telemetry.TelemetryManagement.Migrations
{
    using System.Linq;
    using DataModel;
    using DTO;

    public class V001DownforceSourceInfoMigration : AbstractTelemetryMigration
    {
        private static readonly string[] SimsContainingTyreLoad = new[] { SimulatorsNameMap.AcSimName, SimulatorsNameMap.AccSimName, SimulatorsNameMap.R3ESimName, SimulatorsNameMap.Rf2SimName, SimulatorsNameMap.LmUSimName };

        private static readonly string[] SimsContainingOverallDownforce = new[] { SimulatorsNameMap.R3ESimName };

        public override int TargetVersion => 1;
        protected override void MigrateUpInternal(LapTelemetryDto lapTelemetryDto)
        {
            if (SimsContainingTyreLoad.Contains(lapTelemetryDto.LapSummary.Simulator))
            {
                lapTelemetryDto.DataPoints.ForEach(x => x.SimulatorSourceInfo.TelemetryInfo.ContainsTyreLoad = true);
            }

            if (SimsContainingOverallDownforce.Contains(lapTelemetryDto.LapSummary.Simulator))
            {
                lapTelemetryDto.DataPoints.ForEach(x => x.SimulatorSourceInfo.TelemetryInfo.ContainsOverallDownforce = true);
            }
        }
    }
}