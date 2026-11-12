namespace SecondMonitor.Timing.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    using SecondMonitor.Timing.Api.Data;
    using SecondMonitor.Timing.Common.SessionTiming;
    using SecondMonitor.Timing.Common.SessionTiming.Drivers;

    using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;

    [ApiController]
    [Route("api/[controller]")]
    public class DriversInfoController : ControllerBase
    {
        private readonly ISessionInfoProvider? _sessionInfoProvider = TimingApplicationProxy.SessionInfoProvider;

        [HttpGet]
        public IList<DriverData> Get()
        {
            ISessionInfo? sessionInfo = _sessionInfoProvider?.SessionInfo;
            if (sessionInfo?.LastSet == null)
            {
                return new List<DriverData>();
            }

            SessionIdentification sessionIdentification = new(sessionInfo.SessionGlobalKey, sessionInfo.LastSet.SessionInfo.SessionTime.TotalSeconds);
            return sessionInfo.DriversByPosition.Select(x => new DriverData(sessionIdentification, x)).ToList();
        }

        [HttpGet]
        [Route(nameof(GetWithLapInfo))]
        public IList<DriverWithLapsData> GetWithLapInfo()
        {
            ISessionInfo? sessionInfo = _sessionInfoProvider?.SessionInfo;
            if (sessionInfo?.LastSet == null)
            {
                return new List<DriverWithLapsData>();
            }

            SessionIdentification sessionIdentification = new(sessionInfo.SessionGlobalKey, sessionInfo.LastSet.SessionInfo.SessionTime.TotalSeconds);
            return sessionInfo.DriversByPosition.Select(x => new DriverWithLapsData(sessionIdentification, x)).ToList();
        }

        [HttpGet]
        [Route(nameof(GetWithByDriverId))]
        [ProducesResponseType<DriverWithLapsData>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetWithByDriverId(string driverId)
        {
            ISessionInfo? sessionInfo = _sessionInfoProvider?.SessionInfo;
            DriverTiming? driver = sessionInfo?.DriversByPosition?.SingleOrDefault(x => x.DriverId == driverId);
            if (driver == null || sessionInfo?.LastSet == null)
            {
                return NotFound($"Driver: {driverId} not found");
            }

            return Ok(new DriverWithLapsData(new SessionIdentification(sessionInfo), driver));
        }
    }
}
