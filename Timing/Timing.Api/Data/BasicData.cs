namespace SecondMonitor.Timing.Api.Data
{
    using SecondMonitor.Timing.Common.SessionTiming;

    public class SessionIdentification
    {
        public SessionIdentification(ISessionInfo? sessionInfo)
        {
            if (sessionInfo?.LastSet == null)
            {
                SessionGlobalKey = Guid.Empty;
                return;
            }

            SessionGlobalKey = sessionInfo.SessionGlobalKey;
            SessionTimeInSeconds = sessionInfo.LastSet.SessionInfo.SessionTime.TotalSeconds;
        }

        public SessionIdentification(Guid sessionGlobalKey, double sessionTimeInSeconds)
        {
            SessionGlobalKey = sessionGlobalKey;
            SessionTimeInSeconds = sessionTimeInSeconds;
        }

        public static SessionIdentification Empty => new(Guid.Empty, 0);

        public Guid SessionGlobalKey { get; }

        public double SessionTimeInSeconds { get; }
    }
}
