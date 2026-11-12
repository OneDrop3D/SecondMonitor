namespace SecondMonitor.ViewModels.Settings.Model
{
    public class SessionFuelCalculationSettings
    {
        public SessionFuelCalculationSettings()
        {
        }

        public SessionFuelCalculationSettings(bool autoOpenOnSessionStart, bool autoOpenWhenInPits, int laps, int minutes)
        {
            AutoOpenOnSessionStart = autoOpenOnSessionStart;
            AutoOpenWhenInPits = autoOpenWhenInPits;
            Laps = laps;
            Minutes = minutes;
        }

        public bool AutoOpenOnSessionStart { get; set; }

        public bool AutoOpenWhenInPits { get; set; }

        public int Laps { get; set; }

        public int Minutes { get; set; }
    }
}