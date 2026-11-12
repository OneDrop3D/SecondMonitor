namespace SecondMonitor.Timing.Api.Data
{
    using SecondMonitor.DataModel.Snapshot;

    public class SessionData
    {
        public SessionData()
        {
            SessionIdentification = SessionIdentification.Empty;
            TrackInfo = TrackInfoData.Empty;
            WeatherInfo = WeatherInfoData.Empty;
            SessionType = string.Empty;
            SessionPhase = string.Empty;
            SessionLengthType = string.Empty;
            SimulatorName = string.Empty;
        }

        public SessionData(Guid sessionGlobalKey, SessionInfo sessionInfo, string simulatorName)
        {
            SessionIdentification = new SessionIdentification(sessionGlobalKey, sessionInfo.SessionTime.TotalSeconds);
            TrackInfo = new TrackInfoData(sessionInfo.TrackInfo);
            WeatherInfo = new WeatherInfoData(sessionInfo.WeatherInfo);
            SessionType = sessionInfo.SessionType.ToString();
            SessionPhase = sessionInfo.SessionPhase.ToString();
            SessionLengthType = sessionInfo.SessionLengthType.ToString();
            SessionTimeRemainingSeconds = sessionInfo.SessionTimeRemaining;
            TotalNumberOfLaps = sessionInfo.TotalNumberOfLaps;
            LeaderCurrentLap = sessionInfo.LeaderCurrentLap;
            SimulatorName = simulatorName;
        }
        
        public SessionIdentification SessionIdentification { get; }

        public string SimulatorName { get; }

        public TrackInfoData TrackInfo { get; }

        public WeatherInfoData WeatherInfo { get; }

        public string SessionType { get; }

        public string SessionPhase { get; }

        public string SessionLengthType { get; }

        public double SessionTimeRemainingSeconds { get; }

        public int TotalNumberOfLaps { get; }

        public int LeaderCurrentLap { get; }
    }
}
