namespace SecondMonitor.ViewModels.Settings.Model
{
    using System;

    [Serializable]
    public class TelemetrySettings
    {
        public bool IsTelemetryLoggingEnabled { get; set; } = true;
        public int LoggingInterval { get; set; } = 16;
        public int MaxSessionsKept { get; set; } = 10;
        public bool LogInvalidLaps { get; set; } = true;
        public bool LogOpponentsBest { get; set; } = true;
        public bool LogBestForEachDriver { get; set; } = true;
        public bool LogOnlyPlayerClass { get; set; } = true;
        public bool LogOpponentsAll { get; set; } = false;

        public int LogOnlyTop { get; set; } = 10;
        public bool LogInPractice { get; set; } = true;
        public bool LogInQualification { get; set; } = true;
        public bool LogInRace { get; set; } = true;

        public bool CompressFiles { get; set; } = true;

        public bool IsSaveAlsoAsXMlEnabled { get; set; } = false;

        public bool IsSaveAlsoAsJsonEnabled { get; set; } = false;
    }
}