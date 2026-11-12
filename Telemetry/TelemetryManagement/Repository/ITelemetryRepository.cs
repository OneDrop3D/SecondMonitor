namespace SecondMonitor.Telemetry.TelemetryManagement.Repository
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using DTO;

    public interface ITelemetryRepository
    {
        IReadOnlyCollection<SessionInfoDto> GetAllRecentSessions();
        IReadOnlyCollection<SessionInfoDto> GetAllArchivedSessions();
        IEnumerable<SessionInfoDto> LoadPreviouslyLoadedSessions(List<SessionInfoDto> sessions);

        void SaveRecentSessionInformation(SessionInfoDto sessionInfoDto, string sessionIdentifier);
        void SaveRecentSessionInformationDirectly(SessionInfoDto sessionInfoDto, string fullFilePath);
        string SaveRecentSessionLap(LapTelemetryDto lapTelemetry, string sessionIdentifier, string fileName, bool compress);

        string SaveRecentSessionLapAsXml(LapTelemetryDto lapTelemetry, string sessionIdentifier, string fileName, bool compress);
        
        string SaveRecentSessionLapAsJson(LapTelemetryDto lapTelemetry, string sessionIdentifier, string fileName, bool compress);

        void Save(LapTelemetryDto lapTelemetryDto, string path, bool compress);

        SessionInfoDto OpenRecentSession(string sessionIdentifier);
        void CloseSession(string sessionIdentifier);
        LapTelemetryDto LoadLapTelemetryDtoFromAnySession(LapSummaryDto lapSummaryDto);
        LapTelemetryDto LoadLapTelemetryDto(FileInfo file);
        string GetLastRecentSessionIdentifier();

        Task ArchiveSessions(SessionInfoDto sessionInfoDto);

        Task OpenSessionFolder(SessionInfoDto sessionInfoDto);
        void DeleteSession(SessionInfoDto sessionInfoDto);

        Task<SessionInfoDto> ImportTelemetry(string fileName);
        bool DeleteSessionLap(string sessionIdentifier, string fileName);
        bool DeleteSessionLap(string fileNameWithoutExtension);
    }
}