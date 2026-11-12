namespace SecondMonitor.F12021Connector.DataModels
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
        // 10 = R, 11 = R2, 12 = R3, 13 = Time Trial
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

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 56)]
        public WeatherForecastSample[] WeatherForecastSamples; // Array of weather forecast samples

        public byte ForecastAccuracy; // 0 = Perfect, 1 = Approximate
        public byte AiDifficulty; // AI Difficulty rating – 0-110
        public uint SeasonLinkIdentifier; // Identifier for season - persists across saves
        public uint WeekendLinkIdentifier; // Identifier for weekend - persists across saves
        public uint SessionLinkIdentifier; // Identifier for session - persists across saves
        public byte PitStopWindowIdealLap; // Ideal lap to pit on for current strategy (player)
        public byte PitStopWindowLatestLap; // Latest lap to pit on for current strategy (player)
        public byte PitStopRejoinPosition; // Predicted position to rejoin at (player)
        public byte SteeringAssist; // 0 = off, 1 = on
        public byte BrakingAssist; // 0 = off, 1 = low, 2 = medium, 3 = high
        public byte GearboxAssist; // 1 = manual, 2 = manual & suggested gear, 3 = auto
        public byte PitAssist; // 0 = off, 1 = on
        public byte PitReleaseAssist; // 0 = off, 1 = on
        public byte ERSAssist; // 0 = off, 1 = on
        public byte DRSAssist; // 0 = off, 1 = on
        public byte DynamicRacingLine; // 0 = off, 1 = corners only, 2 = full
        public byte DynamicRacingLineType; // 0 = 2D, 1 = 3D
    }
}