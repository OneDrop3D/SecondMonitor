namespace SecondMonitor.F12024Connector.DataModels
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
        // 3 = F1 Generic, 4 = Beta, 6 = Esports
        // 8 = F1 World, 9 = F1 Elimination
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

        // 0 = no safety car, 1 = full
        // 2 = virtual, 3 = formation lap
        public byte SafetyCarStatus;

        public byte NetworkGame; // 0 = offline, 1 = online
        public byte NumWeatherForecastSamples; // Number of weather samples to follow

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
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
        public byte GameMode;
        public byte RuleSet;
        public uint TimeOfDay;
        public byte SessionLength; // 0 = None, 2 = Very Short, 3 = Short, 4 = Medium
        // 5 = Medium Long, 6 = Long, 7 = Full
        public byte SpeedUnitsLeadPlayer; // 0 = MPH, 1 = KPH
        public byte TemperatureUnitsLeadPlayer; // 0 = Celsius, 1 = Fahrenheit
        public byte SpeedUnitsSecondaryPlayer; // 0 = MPH, 1 = KPH
        public byte TemperatureUnitsSecondaryPlayer; // 0 = Celsius, 1 = Fahrenheit
        public byte NumSafetyCarPeriods; // Number of safety cars called during session
        public byte NumVirtualSafetyCarPeriods; // Number of virtual safety cars called
        public byte NumRedFlagPeriods; // Number of red flags called during session
        public byte EqualCarPerformance; // 0 = Off, 1 = On
        public byte RecoveryMode; // 0 = None, 1 = Flashbacks, 2 = Auto-recovery
        public byte FlashbackLimit; // 0 = Low, 1 = Medium, 2 = High, 3 = Unlimited
        public byte SurfaceType; // 0 = Simplified, 1 = Realistic
        public byte LowFuelMode; // 0 = Easy, 1 = Hard
        public byte RaceStarts; // 0 = Manual, 1 = Assisted
        public byte TyreTemperature; // 0 = Surface only, 1 = Surface & Carcass
        public byte PitLaneTyreSim; // 0 = On, 1 = Off
        public byte CarDamage; // 0 = Off, 1 = Reduced, 2 = Standard, 3 = Simulation
        public byte CarDamageRate; // 0 = Reduced, 1 = Standard, 2 = Simulation
        public byte Collisions; // 0 = Off, 1 = Player-to-Player Off, 2 = On
        public byte CollisionsOffForFirstLapOnly; // 0 = Disabled, 1 = Enabled
        public byte MpUnsafePitRelease; // 0 = On, 1 = Off (Multiplayer)
        public byte MpOffForGriefing; // 0 = Disabled, 1 = Enabled (Multiplayer)
        public byte CornerCuttingStringency; // 0 = Regular, 1 = Strict
        public byte ParcFermeRules; // 0 = Off, 1 = On
        public byte PitStopExperience; // 0 = Automatic, 1 = Broadcast, 2 = Immersive
        public byte SafetyCar; // 0 = Off, 1 = Reduced, 2 = Standard, 3 = Increased
        public byte SafetyCarExperience; // 0 = Broadcast, 1 = Immersive
        public byte FormationLap; // 0 = Off, 1 = On
        public byte FormationLapExperience; // 0 = Broadcast, 1 = Immersive
        public byte RedFlags; // 0 = Off, 1 = Reduced, 2 = Standard, 3 = Increased
        public byte AffectsLicenceLevelSolo; // 0 = Off, 1 = On
        public byte AffectsLicenceLevelMP; // 0 = Off, 1 = On
        public byte NumSessionsInWeekend; // Number of session in following array
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] WeekendStructure; // List of session types to show weekend
        // structure - see appendix for types
        public float Sector2LapDistanceStart; // Distance in m around track where sector 2 starts
        public float Sector3LapDistanceStart; // Distance in m around track where sector 3 starts
    }
}
