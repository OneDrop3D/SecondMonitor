namespace SecondMonitor.Timing.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.Timing.Api.Data;
    using SecondMonitor.Timing.Common.SessionTiming;

    using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;

    [ApiController]
    [Route("api/[controller]")]
    public class SessionInfoController : ControllerBase
    {
        private readonly ISessionInfoProvider _sessionInfoProvider = TimingApplicationProxy.SessionInfoProvider!;

        [HttpGet]
        public SessionData Get()
        {
            ISessionInfo? sessionInfo = _sessionInfoProvider?.SessionInfo;
            SimulatorDataSet? dataSet = sessionInfo?.LastSet;
            if (sessionInfo == null || dataSet == null)
            {
                return new SessionData();
            }

            return new SessionData(sessionInfo.SessionGlobalKey, dataSet.SessionInfo, dataSet.Source);
        }
    }
}
