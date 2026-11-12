namespace SecondMonitor.ViewModels.Settings.Model
{
    using System;

    [Serializable]
    public class SessionOptions
    {
        public string SessionName { get; set; } = "NA";

        public DisplayModeEnum TimesDisplayMode { get; set; } = DisplayModeEnum.Relative;

        public DriverOrderKind OrderingDisplayMode { get; set; } = DriverOrderKind.Absolute;

        public bool HighlightPlayer { get; set; } = true;

        public int HighlightFontSize { get; set; } = 22;

        public ColumnsSettings ColumnsSettings { get; set; } = new ColumnsSettings();
    }
}
