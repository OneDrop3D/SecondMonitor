namespace SecondMonitor.Telemetry.TelemetryManagement.DTO
{
    using System;
    using System.Collections.Generic;

    public class SessionTransientData
    {
        public SessionTransientData()
        {
            RemovedLaps = new List<LapSummaryDto>();
            ModifiedLaps = new List<LapSummaryDto>();
            AddedLaps = new List<(LapSummaryDto LapSummary, string FullPathName)>();
        }

        public string SessionSummaryFilePath { get; set; }

        public bool IsModified { get; set; }

        public DateTime SessionSummaryFileLastModified { get; set; }

        public List<LapSummaryDto> RemovedLaps { get; }

        public List<LapSummaryDto> ModifiedLaps { get; }

        public List<(LapSummaryDto LapSummary, string FullPathName)> AddedLaps { get; }

        public void AddRemovedLap(LapSummaryDto lapSummaryDto)
        {
            RemovedLaps.Add(lapSummaryDto);
            AddedLaps.RemoveAll(x => x.LapSummary.Id == lapSummaryDto.Id);
        }

        public void AddLap(LapSummaryDto lapSummaryDto, string filePath)
        {
            AddedLaps.Add((lapSummaryDto, filePath));
        }

        public void PurgeAfterSave()
        {
            RemovedLaps.Clear();
            AddedLaps.Clear();
            ModifiedLaps.Clear();
        }
    }
}