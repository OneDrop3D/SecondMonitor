namespace SecondMonitor.Timing.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    using SecondMonitor.DataModel.Telemetry;
    using SecondMonitor.Timing.Api.Data;
    using SecondMonitor.Timing.Api.Data.Telemetry;
    using SecondMonitor.Timing.Common.SessionTiming;
    using SecondMonitor.Timing.Common.SessionTiming.Drivers;
    using SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap;

    using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;

    [ApiController]
    [Route("api/[controller]")]
    public class TelemetryInfoController : ControllerBase
    {
        private readonly ISessionInfoProvider _sessionInfoProvider = TimingApplicationProxy.SessionInfoProvider!;

        [HttpGet]
        [Route("GetForPlayerAndLap")]
        [ProducesResponseType<TelemetryCollectionData>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetForDriverAndLap(int lapNumber)
        {
            ISessionInfo? sessionInfo = _sessionInfoProvider?.SessionInfo;
            DriverTiming? driver = sessionInfo?.DriversByPosition?.SingleOrDefault(x => x.IsPlayer);
            if (driver == null)
            {
                return NotFound($"Player was not found");
            }

            ILapInfo? lap = driver.Laps.FirstOrDefault(x => x.LapNumber == lapNumber);
            if (lap == null)
            {
                return NotFound($"Lap {lapNumber} was not found");
            }

            ITimedTelemetrySnapshots? lapTelemetry = lap.LapTelemetryInfo.TimedTelemetrySnapshots;
            if (lapTelemetry == null || lapTelemetry.Snapshots.Count == 0)
            {
                return NotFound($"Lap {lapNumber} does not have captured telemetry");
            }

            return Ok(new TelemetryCollectionData(new SessionIdentification(sessionInfo), driver.DriverId, lapTelemetry.Snapshots));
        }

        [HttpGet]
        [Route("GetForPlayerSinceTime")]
        [ProducesResponseType<TelemetryCollectionData>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetForDriverSinceTime(double sessionTimeSeconds)
        {
            ISessionInfo? sessionInfo = _sessionInfoProvider?.SessionInfo;
            DriverTiming? driver = sessionInfo?.DriversByPosition?.SingleOrDefault(x => x.IsPlayer);
            if (driver == null)
            {
                return NotFound($"Player was not Found");
            }

            TimeSpan sessionTimeToCheck = TimeSpan.FromSeconds(sessionTimeSeconds);
            List<TimedTelemetrySnapshot> telemetrySnapshotsToReturn = driver.Laps.Where(x => (x.LapEndSessionTime == TimeSpan.Zero || x.LapEndSessionTime >= sessionTimeToCheck) && x.LapTelemetryInfo.HasTelemetryData)
                .SelectMany(x => x.LapTelemetryInfo.TimedTelemetrySnapshots.Snapshots).Where(x => x.SessionTime > sessionTimeToCheck).ToList();

            return Ok(new TelemetryCollectionData(new SessionIdentification(sessionInfo), driver.DriverId, telemetrySnapshotsToReturn));
        }
    }
}
