namespace SecondMonitor.Telemetry.TelemetryManagement.Migrations
{
    using System.Linq;
    using DataModel.Snapshot.Systems;
    using DataModel.Telemetry;
    using DTO;

    public class V000WheelKindMigration : AbstractTelemetryMigration
    {
        public override int TargetVersion => 0;
        protected override void MigrateUpInternal(LapTelemetryDto lapTelemetryDto)
        {
            foreach (TimedTelemetrySnapshot timedTelemetrySnapshot in lapTelemetryDto.DataPoints.Where(timedTelemetrySnapshot => timedTelemetrySnapshot.PlayerData?.CarInfo?.WheelsInfo != null))
            {
                timedTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo.FrontLeft.WheelKind = WheelKind.FrontLeft;
                timedTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo.FrontRight.WheelKind = WheelKind.FrontRight;
                timedTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo.RearLeft.WheelKind = WheelKind.RearLeft;
                timedTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo.RearRight.WheelKind = WheelKind.RearRight;
            }
        }
    }
}