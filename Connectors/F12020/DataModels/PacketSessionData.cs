namespace SecondMonitor.F12020Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketSessionData
    {
        public PacketHeader Header; // Header

        // Weather - 0 = clear, 1 = light cloud, 2 = overcast
        // 3 = light rain, 4 = heavy rain, 5 = storm
        public byte Weather;
        public sbyte TrackTemperature; // Track temp. in degrees celsius
        public sbyte AirTemperature; // Air temp. in degrees celsius
        public byte TotalLaps; // Total number of laps in this race
        public ushort TrackLength; // Track length in metres

        // 0 = unknown, 1 = P1, 2 = P2, 3 = P3, 4 = Short P
        // 5 = Q1, 6 = Q2, 7 = Q3, 8 = Short Q, 9 = OSQ
        // 10 = R, 11 = R2, 12 = Time Trial
        public byte SessionType;

        public sbyte TrackId;

        // Formula, 0 = F1 Modern, 1 = F1 Classic, 2 = F2,
        // 3 = F1 Generic
        public byte Formula;

        public ushort SessionTimeLeft; // Time left in session in seconds
        public ushort SessionDuration; // Session duration in seconds
        public byte PitSpeedLimit; // Pit speed limit in kilometres per hour
        public byte GamePaused; // Whether the game is paused
        public byte IsSpectating; // Whether the player is spectating
        public byte SpectatorCarIndex; // Index of the car being spectated
        public byte SliProNativeSupport; // SLI Pro support, 0 = inactive, 1 = active
        public byte NumMarshalZones; // Number of marshal zones to follow

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 21)]
        public MarshalZone[] MarshalZones; // List of marshal zones – max 21

        public byte SafetyCarStatus; // 0 = no safety car, 1 = full safety car

        // 2 = virtual safety car
        public byte NetworkGame; // 0 = offline, 1 = online
        public byte NumWeatherForecastSamples; // Number of weather samples to follow

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public WeatherForecastSample[] WeatherForecastSamples; // Array of weather forecast samples
    }
}