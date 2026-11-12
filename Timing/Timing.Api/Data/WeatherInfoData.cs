namespace SecondMonitor.Timing.Api.Data
{
    using SecondMonitor.DataModel.Snapshot;

    public class WeatherInfoData
    {
        public WeatherInfoData(WeatherInfo weatherInfo)
        {
            AirTemperatureCelsius = weatherInfo.AirTemperature.InCelsius;
            TrackTemperatureCelsius = weatherInfo.TrackTemperature.InCelsius;
            RainIntensity = weatherInfo.RainIntensity;
            TrackWetness = weatherInfo.TrackWetness;
            WindDirectionFrom = weatherInfo.WindDirectionFrom;
            WindSpeedMeterPerSecond = weatherInfo.WindSpeed.InMs;
            HasWindInformation = weatherInfo.HasWindInformation;
        }

        private WeatherInfoData()
        {
        }

        public static WeatherInfoData Empty => new WeatherInfoData();

        public double AirTemperatureCelsius { get; set; }

        public double TrackTemperatureCelsius { get; set; }

        public int RainIntensity { get; set; }

        public int TrackWetness { get; set; }

        public double WindDirectionFrom { get; set; }

        public double WindSpeedMeterPerSecond { get; set; }

        public bool HasWindInformation { get; set; }
    }
}
