namespace SecondMonitor.F12020Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WeatherForecastSample
    {
        public byte SessionType; // 0 = unknown, 1 = P1, 2 = P2, 3 = P3, 4 = Short P, 5 = Q1

        // 6 = Q2, 7 = Q3, 8 = Short Q, 9 = OSQ, 10 = R, 11 = R2
        // 12 = Time Trial
        public byte TimeOffset; // Time in minutes the forecast is for

        public byte Weather; // Weather - 0 = clear, 1 = light cloud, 2 = overcast

        // 3 = light rain, 4 = heavy rain, 5 = storm
        public sbyte TrackTemperature; // Track temp. in degrees celsius
        public sbyte AirTemperature; // Air temp. in degrees celsius
    }
}