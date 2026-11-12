namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.TelemetryLoad
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using SecondMonitor.ViewModels.Controllers;
    using TelemetryManagement.DTO;

    public interface ITelemetryLoadController : IController
    {
        Task<IReadOnlyCollection<SessionInfoDto>> GetAllRecentSessionInfoAsync();
        Task<IReadOnlyCollection<SessionInfoDto>> GetAllArchivedSessionInfoAsync();

        Task<SessionInfoDto> LoadRecentSessionAsync(string sessionIdentifier);
        Task<SessionInfoDto> LoadRecentSessionAsync(SessionInfoDto sessionInfoDto);
        Task<SessionInfoDto> AddRecentSessionAsync(SessionInfoDto sessionInfoDto);
        Task<SessionInfoDto> LoadLastSessionAsync();

        Task<LapTelemetryDto> LoadLap(LapSummaryDto lapSummaryDto);
        Task<LapTelemetryDto> LoadLap(FileInfo file, string customDisplayName);

        Task<IEnumerable<LapTelemetryDto>> LoadLaps(IEnumerable<LapSummaryDto> lapSummaries);

        Task UnloadLap(LapSummaryDto lapSummaryDto);
        Task UnloadLaps(IEnumerable<LapSummaryDto> lapSummaryDtos);

        //Task RemoveLapFromSession(LapSummaryDto lapSummaryDto);

        Task ArchiveSession(SessionInfoDto sessionInfoDto);
        Task OpenSessionFolder(SessionInfoDto sessionInfoDto);

        Task SaveMainSession();

        void MarkLapAsModified(LapSummaryDto lapSummaryDto);

        void DeleteSession(SessionInfoDto sessionInfoDto);
        Task RemoveLap(LapSummaryDto lapSummary);
        Task ExportTelemetry(SessionInfoDto sessionInfoDto, string exportFileName);
        Task<SessionInfoDto> ImportTelemetry(string fileName);
        Task SaveModifiedLap(LapSummaryDto modifiedLap);
    }
}