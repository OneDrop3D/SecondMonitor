namespace SecondMonitor.ViewModels.Settings.Model
{
    public class RaceSessionFuelCalculationSettings
    {
        public RaceSessionFuelCalculationSettings()
        {
        }

        public RaceSessionFuelCalculationSettings(int extraLaps, int extraMinutes)
        {
            ExtraLaps = extraLaps;
            ExtraMinutes = extraMinutes;
        }

        public int ExtraLaps { get; set; }

        public int ExtraMinutes { get; set; }
    }
}