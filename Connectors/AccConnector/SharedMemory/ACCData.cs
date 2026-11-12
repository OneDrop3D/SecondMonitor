namespace SecondMonitor.AccConnector.SharedMemory
{
    using System;
    using System.Runtime.InteropServices;

    /**
     * Based on ACCData reader from CrewChief
     * https://gitlab.com/mr_belowski/CrewChiefV4
     */
    //This is a copied contract class, so disable all this stuff
#pragma warning disable SA1403
#pragma warning disable SA1201
#pragma warning disable SA1106
#pragma warning disable SA1400
#pragma warning disable SA1513
#pragma warning disable SA1306
#pragma warning disable SA1300
#pragma warning disable SA1310
#pragma warning disable SA1307
#pragma warning disable SA1120
#pragma warning disable SA1121
#pragma warning disable SA1002
#pragma warning disable SA1507
#pragma warning disable SA1505
#pragma warning disable SA1025
#pragma warning disable SX1309
#pragma warning disable RCS1013

    public enum AcStatus
    {
        AcOffSpectate = 0, //This is AcOff, but this also the case when in ACC spectating, as shared data is not set in that case
        AcReplay = 1,
        AcLive = 2,
        AcPause = 3
    }

    public enum AcSessionType
    {
        AcUnknown = -1,
        AcPractice = 0,
        AcQualify = 1,
        AcRace = 2,
        AcHotlap = 3,
        AcTimeAttack = 4,
        AcDrift = 5,
        AcDrag = 6,
        AccHotstint = 7,
        AccHotstintsuperpole = 8
    }

    public enum AcFlagType
    {
        AcNoFlag = 0,
        AcBlueFlag = 1,
        AcYellowFlag = 2,
        AcBlackFlag = 3,
        AcWhiteFlag = 4,
        AcCheckeredFlag = 5,
        AcPenaltyFlag = 6,
    }

    public enum AcWheels
    {
        FL = 0,
        FR = 1,
        RL = 2,
        RR = 3,
    }

    public enum AcPenaltyType
    {
        AccNone = 0,
        AccDriveThroughCutting = 1,
        AccStopAndGo10Cutting = 2,
        AccStopAndGo20Cutting = 3,
        AccStopAndGo30Cutting = 4,
        AccDisqualifiedCutting = 5,
        AccRemoveBestLaptimeCutting = 6,
        AccDriveThroughPitSpeeding = 7,
        AccStopAndGo10PitSpeeding = 8,
        AccStopAndGo20PitSpeeding = 9,
        AccStopAndGo30PitSpeeding = 10,
        AccDisqualifiedPitSpeeding = 11,
        AccRemoveBestLaptimePitSpeeding = 12,
        AccDisqualifiedIgnoredMandatoryPit = 13,
        AccPostRaceTime = 14,
        AccDisqualifiedTrolling = 15,
        AccDisqualifiedPitEntry = 16,
        AccDisqualifiedPitExit = 17,
        AccDisqualifiedWrongway = 18,
        AccDriveThroughIgnoredDriverStint = 19,
        AccDisqualifiedIgnoredDriverStint = 20,
        AccDisqualifiedExceededDriverStintLimit = 21
    }

    public enum AccTrackGripStatus
    {
        Green = 0,
        Fast = 1,
        Optimum = 2,
        Greasy = 3,
        Damp = 4,
        Wet = 5,
        Flooded = 6
    }

    public enum AccRainIntensity
    {
        NoRain = 0,
        Drizzle = 1,
        LightRain = 2,
        MediumRain = 3,
        HeavyRain = 4,
        Thunderstorm = 5
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    [Serializable]
    public struct SPageFilePhysics
    {
        public int packetId;
        public float gas;
        public float brake;
        public float fuel;
        public int gear;
        public int rpms;
        public float steerAngle;
        public float speedKmh;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] velocity;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] accG;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] wheelSlip;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] wheelLoad; // NOT USED

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] wheelsPressure;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] wheelAngularSpeed;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] tyreWear; // NOT USED

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] tyreDirtyLevel; // NOT USED

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] tyreCoreTemperature;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] camberRAD; // NOT USED

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] suspensionTravel;

        public float drs;
        public float tc;
        public float heading;
        public float pitch;
        public float roll;
        public float cgHeight; // NOT USED

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public float[] carDamage;

        public int numberOfTyresOut;
        public int pitLimiterOn;
        public float abs;
        public float kersCharge; // NOT USED
        public float kersInput; // NOT USED
        public int autoShifterOn;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public float[] rideHeight; // NOT USED

        public float turboBoost;
        public float ballast; // NOT USED
        public float airDensity; // NOT USED
        public float airTemp;
        public float roadTemp;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] localAngularVel;

        public float finalFF;
        public float performanceMeter; // NOT USED

        public int engineBrake; // NOT USED
        public int ersRecoveryLevel; // NOT USED
        public int ersPowerLevel; // NOT USED
        public int ersHeatCharging; // NOT USED
        public int ersIsCharging; // NOT USED
        public float kersCurrentKJ; // NOT USED

        public int drsAvailable; // NOT USED
        public int drsEnabled; // NOT USED

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] brakeTemp;

        public float clutch;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] tyreTempI; // NOT USED

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] tyreTempM; // NOT USED

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] tyreTempO; // NOT USED

        public int isAIControlled;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public AccVec3[] tyreContactPoint;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public AccVec3[] tyreContactNormal;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public AccVec3[] tyreContactHeading;

        float brakeBias;
        public AccVec3 localVelocity;

        public int P2PActivation; // Not used in ACC
        public int P2PStatus; // Not used in ACC
        public float currentMaxRpm; // NOT USED

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mz; // Not shown in ACC

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] fx; // Not shown in ACC

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] fy; // Not shown in ACC

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] slipRatio; // Tyre slip ratio[FL, FR, RL, RR]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] slipAngle; // Tyre slip angle[FL, FR, RL, RR]

        public int tcinAction;
        public int absInAction;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] suspensionDamage; // Suspensions damage levels[FL, FR, RL, RR]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] tyreTemp; // * Tyres core temperatures[FL, FR, RL, RR]

        public float waterTemp;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] brakePressure;

        public int frontBrakeCompound;

        public int rearBrakeCompound;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] padLife;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] discLifeLife;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    [Serializable]
    public struct SPageFileGraphic
    {
        public int packetId;
        public AcStatus status;
        public AcSessionType session;

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string currentTime;

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string lastTime;

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string bestTime;

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string split;

        public int completedLaps;
        public int position;
        public int iCurrentTime;
        public int iLastTime;
        public int iBestTime;
        public float sessionTimeLeft;
        public float distanceTraveled;
        public int isInPit;
        public int currentSectorIndex;
        public int lastSectorTime;
        public int numberOfLaps;

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string tyreCompound;

        public float replayTimeMultiplier; // NOT USED
        public float normalizedCarPosition;

        // note that carCount frequently disagrees with the UDP data - looks like UDP data is more correct
        public int carCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
        public AccVec3[] carCoordinates;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
        public int[] carIDs;

        public int playerCarID;

        public float penaltyTime;
        public AcFlagType flag;

        public AcPenaltyType penalty;

        public int idealLineOn;
        public int isInPitLane;

        public float surfaceGrip;
        public int MandatoryPitDone;
        public float windSpeed;
        public float windDirection;

        public int isSetupMenuVisible;

        public int mainDisplayIndex;
        public int secondaryDisplayIndex;
        public int TC;
        public int TCCut;
        public int EngineMap;
        public int ABS;
        public int fuelXLap;
        public int rainLights;
        public int flashingLights;
        public int lightsStage;
        public float exhaustTemperature;
        public int wiperLV;

        public int driverStintTotalTimeLeft; // Time is the driver is allowed to drive per race in milliseconds
        public int driverStintTimeLeft; // Time the driver is allowed to drive per stint in milliseconds
        public int rainTyres; // Are rain tyres equipped
        public int sessionIndex;
        public float usedFuel; // Used fuel since last time refueling

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string deltaLapTime; // Delta time in wide character

        public int iDeltaLapTime; //Delta time time in milliseconds

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string estimatedLapTime; //Estimated lap time in milliseconds

        public int iEstimatedLapTime; //Estimated lap time in wide character
        public int isDeltaPositive; //Delta positive(1) or negative(0)
        public int iSplit; // Last split time in milliseconds
        public int isValidLap; // Check if Lap is valid for timing

        public float fuelEstimatedLaps;

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string trackStatus;

        public int missingMandatoryPits;
        public float clock;

        public int directionLightsLeft;
        public int directionLightsRight;

        public int GlobalYellow;
        public int GlobalYellow1;
        public int GlobalYellow2;
        public int GlobalYellow3;

        public int GlobalWhite;
        public int GlobalGreen;
        public int GlobalChequered;
        public int GlobalRed;

        public int mfdTyreSet;
        public float mfdFuelToAdd;
        public float mfdTyrePressureLF;
        public float mfdTyrePressureRF;
        public float mfdTyrePressureLR;
        public float mfdTyrePressureRR;

        public AccTrackGripStatus trackGripStatus;
        public AccRainIntensity rainIntensity;
        public AccRainIntensity rainIntensityIn10min;
        public AccRainIntensity rainIntensityIn30min;

        public int currentTyreSet;
        public int strategyTyreSet;
        public int gapAhead;
        public int gapBehind;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    [Serializable]
    public struct SPageFileStatic
    {
        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string smVersion;

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string acVersion;

        // session static info
        public int numberOfSessions;
        public int numCars;

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string carModel;

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string track;

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string playerName;

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string playerSurname;

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string playerNick;

        public int sectorCount;

        // car static info
        public float maxTorque;// NOT USED
        public float maxPower;// NOT USED
        public int maxRpm;
        public float maxFuel;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] suspensionMaxTravel;// NOT USED

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] tyreRadius;// NOT USED

        public float maxTurboBoost;// NOT USED

        public float deprecated_1;// NOT USED
        public float deprecated_2;// NOT USED

        public int penaltiesEnabled;

        public float aidFuelRate;
        public float aidTireRate;
        public float aidMechanicalDamage;
        public int aidAllowTyreBlankets;
        public float aidStability;
        public int aidAutoClutch;
        public int aidAutoBlip;

        public int hasDRS; // NOT USED
        public int hasERS; // NOT USED
        public int hasKERS; // NOT USED
        public float kersMaxJ; // NOT USED
        public int engineBrakeSettingsCount; // NOT USED
        public int ersPowerControllerCount; // NOT USED

        public float trackSPlineLength; // NOT USED

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string trackConfiguration; // NOT USED

        public float ersMaxJ; // NOT USED
        public int isTimedRace; // NOT USED
        public int hasExtraLap; // NOT USED

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string carSkin; // NOT USED

        public int reversedGridPositions; // NOT USED
        public int PitWindowStart;
        public int PitWindowEnd;
        public int isOnline; // If is a multiplayer session
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
    [Serializable]
    public struct AccVec3
    {
        public float x;
        public float y;
        public float z;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
    [Serializable]
    public struct AccVehicleInfo
    {
        public int carId;
        public int isPlayerVehicle;
        public string driverName;
        public string carModel;
        public float speedMS;
        public int bestLapMS;
        public int lapCount;
        public int currentLapInvalid;
        public int currentLapTimeMS;
        public int lastLapTimeMS;
        public AccVec3 worldPosition;
        public int isCarInPitline;
        public int isCarInPit;
        public int carLeaderboardPosition;
        public int carRealTimeLeaderboardPosition;
        public float spLineLength;
        public int isConnected; // NOT USED, IS ALWAYS 1
        public float[] tyreInflation;
        public int raceNumber;
    }
#pragma warning restore SA1403
#pragma warning restore SA1201
#pragma warning restore SA1106
#pragma warning restore SA1400
#pragma warning restore SA1513
#pragma warning restore SA1306
#pragma warning restore SA1300
#pragma warning restore SA1310
#pragma warning restore SA1307
#pragma warning restore SA1120
#pragma warning restore SA1121
#pragma warning restore SA1002
#pragma warning restore SA1507
#pragma warning restore SA1505
#pragma warning restore SA1025
#pragma warning restore SX1309
#pragma warning restore RCS1013
}