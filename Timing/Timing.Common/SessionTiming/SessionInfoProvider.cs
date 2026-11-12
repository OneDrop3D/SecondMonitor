namespace SecondMonitor.Timing.Common.SessionTiming
{
    public class SessionInfoProvider : ISessionInfoProvider
    {
        public ISessionInfo SessionInfo { get; private set; }

        public void SetSessionInfo(ISessionInfo sessionInfo)
        {
            SessionInfo = sessionInfo;
        }
    }
}