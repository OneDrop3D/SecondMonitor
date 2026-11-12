namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Synchronization.Graphs
{
    using System;
    using TelemetryManagement.DTO;

    public class SessionModifiedArgs : EventArgs
    {
        public SessionModifiedArgs(SessionInfoDto sessionInfoDto)
        {
            SessionInfoDto = sessionInfoDto;
        }

        public SessionInfoDto SessionInfoDto { get; }
    }
}