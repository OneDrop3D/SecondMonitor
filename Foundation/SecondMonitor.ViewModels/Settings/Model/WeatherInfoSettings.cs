namespace SecondMonitor.ViewModels.Settings.Model
{
    using System;

    [Serializable]
    public class WeatherInfoSettings
    {
        public TemperatureChangeKind PracticeFirstAirTempInfo { get; set; } = TemperatureChangeKind.Last5Minutes;

        public TemperatureChangeKind PracticeSecondAirTempInfo { get; set; } = TemperatureChangeKind.FromSessionStart;

        public TemperatureChangeKind PracticeFirstTrackTempInfo { get; set; } = TemperatureChangeKind.Last5Minutes;

        public TemperatureChangeKind PracticeSecondTrackTempInfo { get; set; } = TemperatureChangeKind.FromSessionStart;

        public TemperatureChangeKind RaceFirstAirTempInfo { get; set; } = TemperatureChangeKind.Last5Minutes;

        public TemperatureChangeKind RaceSecondAirTempInfo { get; set; } = TemperatureChangeKind.FromStintStart;

        public TemperatureChangeKind RaceFirstTrackTempInfo { get; set; } = TemperatureChangeKind.Last5Minutes;

        public TemperatureChangeKind RaceSecondTrackTempInfo { get; set; } = TemperatureChangeKind.FromStintStart;
    }
}
