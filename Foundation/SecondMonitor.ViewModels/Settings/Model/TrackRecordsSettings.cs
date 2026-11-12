namespace SecondMonitor.ViewModels.Settings.Model
{
    public class TrackRecordsSettings
    {
        public bool IsEnabled { get; set; } = true;

        public bool DisplayRecordForCurrentSessionType { get; set; } = false;

        public bool IsShowAverageSpeedEnabled { get; set; } = true;
    }
}