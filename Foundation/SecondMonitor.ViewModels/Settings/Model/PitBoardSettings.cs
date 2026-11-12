namespace SecondMonitor.ViewModels.Settings.Model
{
    public class PitBoardSettings
    {
        public bool IsEnabled { get; set; } = true;
        public bool OnlyPlayerClass { get; set; } = true;
        public int DisplaySeconds { get; set; } = 10;
        public bool IsYellowBoardEnabled { get; set; } = true;
        public bool IsPitEntrySpeedBoardEnabled { get; set; } = true;
        public bool IsPitDurationBoardEnabled { get; set; } = true;

        public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Bottom;

        public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;
    }
}