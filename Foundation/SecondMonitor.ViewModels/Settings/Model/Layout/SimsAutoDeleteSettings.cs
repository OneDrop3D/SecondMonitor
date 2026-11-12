namespace SecondMonitor.ViewModels.Settings.Model.Layout
{
    public class SimsAutoDeleteSettings
    {
        public AutoDeleteSettings R3EReplayAutoDeleteSettings { get; set; } = new AutoDeleteSettings(false, false, 2000, 30);
        public AutoDeleteSettings AccSaveGameAutoDeleteSettings { get; set; } = new AutoDeleteSettings(false, false, 2000, 30);
        public AutoDeleteSettings LmUReplayAutoDeleteSettings { get; set; } = new AutoDeleteSettings(false, false, 2000, 30);
    }
}
