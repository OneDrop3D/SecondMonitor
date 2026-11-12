// ReSharper disable InconsistentNaming
// ReSharper disable StyleCop.SA1307

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
#pragma warning disable SA1649
namespace SecondMonitor.AMS2Connector.SharedMemory
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    public struct UDPTelemetryData
    {
        // starts with packet base (0-12)
        public uint mPacketNumber; //0 counter reflecting all the packets that have been sent during the game run
        public uint mCategoryPacketNumber; //4 counter of the packet groups belonging to the given category
        public byte mPartialPacketIndex; //8 If the data from this class had to be sent in several packets, the index number
        public byte mPartialPacketNumber; //9 If the data from this class had to be sent in several packets, the total number
        public byte mPacketType; //10 what is the type of this packet (see EUDPStreamerPacketHanlderType for details)
        public byte mPacketVersion; //11 what is the version of protocol for this handler, to be bumped with data structure change

        // Participant info
        public sbyte sViewedParticipantIndex; // 12

        public byte sUnfilteredThrottle; // 13 1
        public byte sUnfilteredBrake; // 14 1
        public sbyte sUnfilteredSteering; // 15 1
        public byte sUnfilteredClutch; // 16 1

        public byte sCarFlags; // 17 1
        public short sOilTempCelsius; // 18 2
        public ushort sOilPressureKPa; // 20 2
        public short sWaterTempCelsius; // 22 2
        public ushort sWaterPressureKpa; // 24 2
        public ushort sFuelPressureKpa; // 26 2
        public byte sFuelCapacity; // 28 1
        public byte sBrake; // 29 1
        public byte sThrottle; // 30 1
        public byte sClutch; // 31 1
        public float sFuelLevel; // 32 4
        public float sSpeed; // 36 4
        public ushort sRpm; // 40 2
        public ushort sMaxRpm; // 42 2
        public sbyte sSteering; // 44 1
        public byte sGearNumGears; // 45 1
        public byte sBoostAmount; // 46 1
        public byte sCrashState; // 47 1
        public float sOdometerKM; // 48 4

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] sOrientation; // 52 12

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] sLocalVelocity;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] sWorldVelocity; // 76 12

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] sAngularVelocity; // 88 12

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] sLocalAcceleration; // 100 12

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] sWorldAcceleration; // 112 12

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] sExtentsCentre; // 124 12

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] sTyreFlags; // 136 4

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] sTerrain; // 140 4

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] sTyreY; // 144 16

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] sTyreRPS; // 160 16

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] sTyreTemp; // 176 4

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] sTyreHeightAboveGround; // 180 16

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] sTyreWear; // 196 4

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] sBrakeDamage; // 200 4

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] sSuspensionDamage; // 204 4

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public short[] sBrakeTempCelsius; // 208 8

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] sTyreTreadTemp; // 216 8

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] sTyreLayerTemp; // 224 8

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] sTyreCarcassTemp; // 232 8

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] sTyreRimTemp; // 240 8

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] sTyreInternalAirTemp; // 248 8

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] sTyreTempLeft; // 256 8

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] sTyreTempCenter; // 264 8

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] sTyreTempRight; // 272 8

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] sWheelLocalPositionY; // 280 16

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] sRideHeight; // 296 16

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] sSuspensionTravel; // 312 16

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] sSuspensionVelocity; // 328 16

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] sSuspensionRideHeight; // 344 8

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] sAirPressure; // 352 8

        public float sEngineSpeed; // 360 4
        public float sEngineTorque; // 364 4

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] sWings; // 368 2

        public byte sHandBrake; // 370 1

        // Car damage
        public byte sAeroDamage; // 371 1

        public byte sEngineDamage; // 372 1

        //  HW state
        public byte sJoyPad1; // 376 4
        public byte sJoyPad2; // 376 4
        public byte sDPad; // 377 1

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] lfTyreCompound;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] rfTyreCompound;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] lrTyreCompound;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] rrTyreCompound;

        public float sTurboBoostPressure; // 538 4

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] sFullPosition; // 542 12 -- position of the viewed participant with "full" precision (full? but it's a float[])

        public byte sBrakeBias; // 554 1 -- quantized brake bias
    }

    public enum UdpPacketType
    {
        eCarPhysics = 0,
        eRaceDefinition = 1,
        eParticipants = 2,
        eTimings = 3,
        eGameState = 4,
        eWeatherState = 5, // not sent at the moment, information can be found in the game state packet
        eVehicleNames = 6, //not sent at the moment
        eTimeStats = 7,
        eParticipantVehicleNames = 8
    };

    // WheelIndex
    public enum WheelIndex
    {
        TyreFrontLeft = 0,
        TyreFrontRight,
        TyreRearLeft,
        TyreRearRight,
        TyreMax
    };

    // Vector
    public enum VectorAxis
    {
        VecX = 0,
        VecY,
        VecZ,
        VecMax
    };

    // (Type#1) GameState (to be used with 'mSessionType')
    public enum GameState
    {
        GameExited = 0,

        GameFrontEnd,

        GameInGamePlaying,

        GameInGamePaused,

        GameInGameInMenuTimeTicking,

        GameInGameRestarting,

        GameInGameReplay,

        GameFrontEndReplay,

        //-------------
        GameMax
    };

    // (Type#2) Session state (to be used with 'mSessionState')
    public enum Ams2SessionType
    {
        SessionInvalid = 0,

        SessionPractice,

        SessionTest,

        SessionQualify,

        SessionFormationLap,

        SessionRace,

        SessionTimeAttack,

        //-------------
        SessionMax
    };

    // (Type#3) RaceState (to be used with 'mRaceState' and 'mRaceStates')
    public enum RaceState
    {
        RaceStateInvalid,

        RaceStateNotStarted,

        RaceStateRacing,

        RaceStateFinished,

        RaceStateDisqualified,

        RaceStateRetired,

        RaceStateDnf,

        //-------------
        RaceStateMax
    };

    // (Type#5) Flag Colours (to be used with 'mHighestFlagColour')
    public enum HighestFlagColor
    {
        FlagColourNone = 0, // Not used for actual flags, only for some query functions

        FlagColourGreen, // End of danger zone, or race started

        FlagColourBlue, // Faster car wants to overtake the participant

        FlagColourWhiteSlowCar, // Slow car in area

        FlagColourWhiteFinalLap, // Final Lap

        FlagColourRed, // Huge collisions where one or more cars become wrecked and block the track

        FlagColourYellow, // Danger on the racing surface itself

        FlagColourDoubleYellow, // Danger that wholly or partly blocks the racing surface

        FlagColourBlackAndWhite, // Unsportsmanlike conduct

        FlagColourBlackOrangeCircle, // Mechanical Failure

        FlagColourBlack, // Participant disqualified

        FlagColourChequered, // Chequered flag

        //-------------
        FlagColourMax
    };

    // (Type#6) Flag Reason (to be used with 'mHighestFlagReason')
    public enum Reason
    {
        FlagReasonNone = 0,

        FlagReasonSoloCrash,

        FlagReasonVehicleCrash,

        FlagReasonVehicleObstruction,

        //-------------
        FlagReasonMax
    };

    // (Type#7) Pit Mode (to be used with 'mPitMode')
    public enum PitMode
    {
        PitModeNone = 0,

        PitModeDrivingIntoPits,

        PitModeInPit,

        PitModeDrivingOutOfPits,

        PitModeInGarage,

        PitModeDrivingOutOfGarage,

        //-------------
        PitModeMax
    };

    // (Type#8) Pit Stop Schedule (to be used with 'mPitSchedule')
    public enum PitStopSchedule
    {
        PitScheduleNone = 0, // Nothing scheduled

        PitSchedulePlayerRequested, // Used for standard pit sequence - requested by player

        PitScheduleEngineerRequested, // Used for standard pit sequence - requested by engineer

        PitScheduleDamageRequested, // Used for standard pit sequence - requested by engineer for damage

        PitScheduleMandatory, // Used for standard pit sequence - requested by engineer from career enforced lap number

        PitScheduleDriveThrough, // Used for drive-through penalty

        PitScheduleStopGo, // Used for stop-go penalty

        PitSchedulePitspotOccupied, // Used for drive-through when pitspot is occupied

        //-------------
        PitScheduleMax
    };

    // (Type#9) Car Flags (to be used with 'mCarFlags')
    [Flags]
    public enum CarFlags
    {
        CarHeadlight = (1 << 0),

        CarEngineActive = (1 << 1),

        CarEngineWarning = (1 << 2),

        CarSpeedLimiter = (1 << 3),

        CarAbs = (1 << 4),

        CarHandbrake = (1 << 5),
        CarTcs = (1 << 6),
        CarScs = (1 << 7),
    };

    // (Type#10) Tyre Flags (to be used with 'mTyreFlags')
    public enum TyreFlags
    {
        TyreAttached = (1 << 0),

        TyreInflated = (1 << 1),

        TyreIsOnGround = (1 << 2),
    };

    // (Type#11) Terrain Materials (to be used with 'mTerrain')
    public enum TerrainType
    {
        TerrainRoad = 0,

        TerrainLowGripRoad,

        TerrainBumpyRoad1,

        TerrainBumpyRoad2,

        TerrainBumpyRoad3,

        TerrainMarbles,

        TerrainGrassyBerms,

        TerrainGrass,

        TerrainGravel,

        TerrainBumpyGravel,

        TerrainRumbleStrips,

        TerrainDrains,

        TerrainTyrewalls,

        TerrainCementwalls,

        TerrainGuardrails,

        TerrainSand,

        TerrainBumpySand,

        TerrainDirt,

        TerrainBumpyDirt,

        TerrainDirtRoad,

        TerrainBumpyDirtRoad,

        TerrainPavement,

        TerrainDirtBank,

        TerrainWood,

        TerrainDryVerge,

        TerrainExitRumbleStrips,

        TerrainGrasscrete,

        TerrainLongGrass,

        TerrainSlopeGrass,

        TerrainCobbles,

        TerrainSandRoad,

        TerrainBakedClay,

        TerrainAstroturf,

        TerrainSnowhalf,

        TerrainSnowfull,

        TerrainDamagedRoad1,

        TerrainTrainTrackRoad,

        TerrainBumpycobbles,

        TerrainAriesOnly,

        TerrainOrionOnly,

        TerrainB1Rumbles,

        TerrainB2Rumbles,

        TerrainRoughSandMedium,

        TerrainRoughSandHeavy,

        TerrainSnowwalls,

        TerrainIceRoad,

        TerrainRunoffRoad,

        TerrainIllegalStrip,

        //-------------
        TerrainMax
    };

    // (Type#12) Crash Damage State  (to be used with 'mCrashState')
    public enum DamageState
    {
        CrashDamageNone = 0,

        CrashDamageOfftrack,

        CrashDamageLargeProp,

        CrashDamageSpinning,

        CrashDamageRolling,

        //-------------
        CrashMax
    }

    // (Type#14) DrsState Flags (to be used with 'mDrsState')
    [Flags]
    public enum DrsState
    {
        NoDrs = 0, // Vehicle has DRS capability
        DrsInstalled = 1, // Vehicle has DRS capability
        DrsZoneRules = 2, // 1 if DRS uses F1 style rules
        DrsAvailableNext = 4, // detection zone was triggered (only applies to f1 style rules)
        DrsAvailableNow = 8, // detection zone was triggered and we are now in the zone (only applies to f1 style rules)
        DrsActive = 16, // Wing is in activated state
    };

    public enum ErsDeploymentMode
    {
        ERS_DEPLOYMENT_MODE_NONE = 0, // The vehicle does not support deployment modes
        ERS_DEPLOYMENT_MODE_OFF, // Regen only, no deployment
        ERS_DEPLOYMENT_MODE_BUILD, // Heavy emphasis towards regen
        ERS_DEPLOYMENT_MODE_BALANCED, // Deployment map automatically adjusted to try and maintain target SoC
        ERS_DEPLOYMENT_MODE_ATTACK,  // More aggressive deployment, no target SoC
        ERS_DEPLOYMENT_MODE_QUAL, // Maximum deployment, no target Soc
    };
    
    public enum YellowFlagState
    {
        YFS_INVALID = -1,
        YFS_NONE,           // No yellow flag pending on track
        YFS_PENDING,        // Flag has been thrown, but not yet taken by leader
        YFS_PITS_CLOSED,    // Flag taken by leader, pits not yet open
        YFS_PIT_LEAD_LAP,   // Those on the lead lap may pit
        YFS_PITS_OPEN,      // Everyone may pit
        YFS_PITS_OPEN2,     // Everyone may pit
        YFS_LAST_LAP,       // On the last caution lap
        YFS_RESUME,         // About to restart (pace car will duck out)
        YFS_RACE_HALT,      // Safety car will lead field into pits
        //-------------
        YFS_MAXIMUM,
    };
    
    // (Type#17) LaunchStage current launch control state
    public enum LaunchStage
    {
        LAUNCH_INVALID = -1,    // Launch control unavailable
        LAUNCH_OFF = 0,         // Launch control inactive
        LAUNCH_REV,             // Launch control revving to optimum engine speed
        LAUNCH_ON,              // Launch control actively accelerating vehicle
    };

    [Serializable]
    public struct ParticipantInfo
    {
        [MarshalAs(UnmanagedType.I1)]
        public bool mIsActive;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] mName; // [ string ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] mWorldPosition; // [ UNITS = World Space  X  Y  Z ]

        public float mCurrentLapDistance; // [ UNITS = Metres ]   [ RANGE = 0.0f->... ]    [ UNSET = 0.0f ]
        public uint mRacePosition; // [ RANGE = 1->... ]   [ UNSET = 0 ]
        public uint mLapsCompleted; // [ RANGE = 0->... ]   [ UNSET = 0 ]
        public uint mCurrentLap; // [ RANGE = 0->... ]   [ UNSET = 0 ]
        public int mCurrentSector; // [ enum (Type#4) Current Sector ]
    }

    [Serializable]
    public readonly struct Ams2SharedMemory
    {
        public const int SHARED_MEMORY_VERSION = 13;
        public const int STRING_LENGTH_MAX = 64;
        public const int STORED_PARTICIPANTS_MAX = 64;
        public const int TYRE_COMPOUND_NAME_LENGTH_MAX = 40;

        // Version Number
        public readonly uint mVersion; // [ RANGE = 0->... ]
        public readonly uint mBuildVersionNumber; // [ RANGE = 0->... ]   [ UNSET = 0 ]

        // Game States
        public readonly uint mGameState; // [ enum (Type#1) Game state ]
        public readonly uint mSessionState; // [ enum (Type#2) Session state ]
        public readonly uint mRaceState; // [ enum (Type#3) Race State ]

        // Participant Info
        public readonly int mViewedParticipantIndex; // [ RANGE = 0->STORED_PARTICIPANTS_MAX ]   [ UNSET = -1 ]
        public readonly int mNumParticipants; // [ RANGE = 0->STORED_PARTICIPANTS_MAX ]   [ UNSET = -1 ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly ParticipantInfo[] mParticipantData; // [ struct (Type#13) ParticipantInfo struct ]

        // Unfiltered Input
        public readonly float mUnfilteredThrottle; // [ RANGE = 0.0f->1.0f ]
        public readonly float mUnfilteredBrake; // [ RANGE = 0.0f->1.0f ]
        public readonly float mUnfilteredSteering; // [ RANGE = -1.0f->1.0f ]
        public readonly float mUnfilteredClutch; // [ RANGE = 0.0f->1.0f ]

        // Vehicle information
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly byte[] mCarName; // [ string ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly byte[] mCarClassName; // [ string ]

        // Event information
        public readonly uint mLapsInEvent; // [ RANGE = 0->... ]   [ UNSET = 0 ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly byte[] mTrackLocation; // [ string ] - untranslated shortened English name

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly byte[] mTrackVariation; // [ string ]- untranslated shortened English variation description

        public readonly float mTrackLength; // [ UNITS = Metres ]   [ RANGE = 0.0f->... ]    [ UNSET = 0.0f ]

        public readonly int mmfOnly_mNumSectors; // [ RANGE = 0->... ]   [ UNSET = -1 ]
        [MarshalAs(UnmanagedType.I1)]
        public readonly bool mLapInvalidated; // [ UNITS = boolean ]   [ RANGE = false->true ]   [ UNSET = false ]
        public readonly float mmfOnly_mBestLapTime; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public readonly float mLastLapTime; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public readonly float mmfOnly_mCurrentTime; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public readonly float mSplitTimeAhead; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public readonly float mSplitTimeBehind; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public readonly float mSplitTime; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public readonly float mEventTimeRemaining; // [ UNITS = milli-seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public readonly float mPersonalFastestLapTime; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public readonly float mWorldFastestLapTime; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public readonly float mmfOnly_mCurrentSector1Time; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public readonly float mmfOnly_mCurrentSector2Time; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public readonly float mmfOnly_mCurrentSector3Time; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public readonly float mmfOnly_mFastestSector1Time; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public readonly float mmfOnly_mFastestSector2Time; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public readonly float mmfOnly_mFastestSector3Time; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public readonly float mPersonalFastestSector1Time; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public readonly float mPersonalFastestSector2Time; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public readonly float mPersonalFastestSector3Time; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public readonly float mWorldFastestSector1Time; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public readonly float mWorldFastestSector2Time; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public readonly float mWorldFastestSector3Time; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]

        // Flags
        public readonly uint mHighestFlagColour; // [ enum (Type#5) Flag Colour ]
        public readonly uint mHighestFlagReason; // [ enum (Type#6) Flag Reason ]

        // Pit Info
        public readonly uint mPitMode; // [ enum (Type#7) Pit Mode ]
        public readonly uint mPitSchedule; // [ enum (Type#8) Pit Stop Schedule ]

        // Car State
        public readonly uint mCarFlags; // [ enum (Type#9) Car Flags ]
        public readonly float mOilTempCelsius; // [ UNITS = Celsius ]   [ UNSET = 0.0f ]
        public readonly float mOilPressureKPa; // [ UNITS = Kilopascal ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public readonly float mWaterTempCelsius; // [ UNITS = Celsius ]   [ UNSET = 0.0f ]
        public readonly float mWaterPressureKPa; // [ UNITS = Kilopascal ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public readonly float mFuelPressureKPa; // [ UNITS = Kilopascal ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public readonly float mFuelLevel; // [ RANGE = 0.0f->1.0f ]
        public readonly float mFuelCapacity; // [ UNITS = Liters ]   [ RANGE = 0.0f->1.0f ]   [ UNSET = 0.0f ]
        public readonly float mSpeed; // [ UNITS = Metres per-second ]   [ RANGE = 0.0f->... ]
        public readonly float mRpm; // [ UNITS = Revolutions per minute ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public readonly float mMaxRPM; // [ UNITS = Revolutions per minute ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public readonly float mBrake; // [ RANGE = 0.0f->1.0f ]
        public readonly float mThrottle; // [ RANGE = 0.0f->1.0f ]
        public readonly float mClutch; // [ RANGE = 0.0f->1.0f ]
        public readonly float mSteering; // [ RANGE = -1.0f->1.0f ]
        public readonly int mGear; // [ RANGE = -1 (Reverse)  0 (Neutral)  1 (Gear 1)  2 (Gear 2)  etc... ]   [ UNSET = 0 (Neutral) ]
        public readonly int mNumGears; // [ RANGE = 0->... ]   [ UNSET = -1 ]
        public readonly float mOdometerKM; // [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        [MarshalAs(UnmanagedType.I1)]
        public readonly bool mAntiLockActive; // [ UNITS = boolean ]   [ RANGE = false->true ]   [ UNSET = false ]
        public readonly int mLastOpponentCollisionIndex; // [ RANGE = 0->STORED_PARTICIPANTS_MAX ]   [ UNSET = -1 ]
        public readonly float mLastOpponentCollisionMagnitude; // [ RANGE = 0.0f->... ]
        [MarshalAs(UnmanagedType.I1)]
        public readonly bool mBoostActive; // [ UNITS = boolean ]   [ RANGE = false->true ]   [ UNSET = false ]
        public readonly float mBoostAmount; // [ RANGE = 0.0f->100.0f ]

        // Motion & Device Related
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly float[] mOrientation; // [ UNITS = Euler Angles ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly float[] mLocalVelocity; // [ UNITS = Metres per-second ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly float[] mWorldVelocity; // [ UNITS = Metres per-second ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly float[] mAngularVelocity; // [ UNITS = Radians per-second ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly float[] mLocalAcceleration; // [ UNITS = Metres per-second ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly float[] mWorldAcceleration; // [ UNITS = Metres per-second ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly float[] mExtentsCentre; // [ UNITS = Local Space  X  Y  Z ]

        // Wheels / Tyres
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly uint[] mTyreFlags; // [ enum (Type#10) Tyre Flags ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly uint[] mTerrain; // [ enum (Type#11) Terrain Materials ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mTyreY; // [ UNITS = Local Space  Y ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mTyreRPS; // [ UNITS = Revolutions per second ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mTyreSlipSpeed; // OBSOLETE, kept for backward compatibility only

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mTyreTemp; // [ UNITS = Celsius ]   [ UNSET = 0.0f ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mTyreGrip; // OBSOLETE, kept for backward compatibility only

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mTyreHeightAboveGround; // [ UNITS = Local Space  Y ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mTyreLateralStiffness; // OBSOLETE, kept for backward compatibility only

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mTyreWear; // [ RANGE = 0.0f->1.0f ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mBrakeDamage; // [ RANGE = 0.0f->1.0f ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mSuspensionDamage; // [ RANGE = 0.0f->1.0f ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mBrakeTempCelsius; // [ UNITS = Celsius ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mTyreTreadTemp; // [ UNITS = Kelvin ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mTyreLayerTemp; // [ UNITS = Kelvin ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mTyreCarcassTemp; // [ UNITS = Kelvin ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mTyreRimTemp; // [ UNITS = Kelvin ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mTyreInternalAirTemp; // [ UNITS = Kelvin ]

        // Car Damage
        public readonly uint mCrashState; // [ enum (Type#12) Crash Damage State ]
        public readonly float mAeroDamage; // [ RANGE = 0.0f->1.0f ]
        public readonly float mEngineDamage; // [ RANGE = 0.0f->1.0f ]

        // Weather
        public readonly float mAmbientTemperature; // [ UNITS = Celsius ]   [ UNSET = 25.0f ]
        public readonly float mTrackTemperature; // [ UNITS = Celsius ]   [ UNSET = 30.0f ]
        public readonly float mRainDensity; // [ UNITS = How much rain will fall ]   [ RANGE = 0.0f->1.0f ]
        public readonly float mWindSpeed; // [ RANGE = 0.0f->100.0f ]   [ UNSET = 2.0f ]
        public readonly float mWindDirectionX; // [ UNITS = Normalised Vector X ]
        public readonly float mWindDirectionY; // [ UNITS = Normalised Vector Y ]

        public readonly float mCloudBrightness; // [ RANGE = 0.0f->... ]

        //PCars2 additions start, version 8
        // Sequence Number to help slightly with data integrity reads
        public readonly uint mSequenceNumber; // 0 at the start, incremented at start and end of writing, so odd when Shared Memory is being filled, even when the memory is not being touched

        //Additional car variables
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mWheelLocalPositionY; // [ UNITS = Local Space  Y ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mSuspensionTravel; // [ UNITS = meters ] [ RANGE 0.f =>... ]  [ UNSET =  0.0f ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mSuspensionVelocity; // [ UNITS = Rate of change of pushrod deflection ] [ RANGE 0.f =>... ]  [ UNSET =  0.0f ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mAirPressure; // [ UNITS = PSI ]  [ RANGE 0.f =>... ]  [ UNSET =  0.0f ]

        public readonly float mEngineSpeed; // [ UNITS = Rad/s ] [UNSET = 0.f ]
        public readonly float mEngineTorque; // [ UNITS = Newton Meters] [UNSET = 0.f ] [ RANGE = 0.0f->... ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly float[] mWings; // [ RANGE = 0.0f->1.0f ] [UNSET = 0.f ]

        public readonly float mHandBrake; // [ RANGE = 0.0f->1.0f ] [UNSET = 0.f ]

        // additional race variables for each participant
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly float[] mCurrentSector1Times; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly float[] mCurrentSector2Times; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly float[] mCurrentSector3Times; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly float[] mFastestSector1Times; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly float[] mFastestSector2Times; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly float[] mFastestSector3Times; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly float[] mFastestLapTimes; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly float[] mLastLapTimes; // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly byte[] mLapsInvalidated; // [ UNITS = boolean for all participants ]   [ RANGE = false->true ]   [ UNSET = false ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly uint[] mRaceStates; // [ enum (Type#3) Race State ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly uint[] mPitModes; // [ enum (Type#7)  Pit Mode ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64 * 3)]
        public readonly float[] mOrientations; // [ UNITS = Euler Angles ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly float[] mSpeeds; // [ UNITS = Metres per-second ]   [ RANGE = 0.0f->... ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64 * 64)]
        public readonly byte[] mCarNames; // [ string ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64 * 64)]
        public readonly byte[] mCarClassNames;

        public readonly int mEnforcedPitStopLap; // [ UNITS = in which lap there will be a mandatory pitstop] [ RANGE = 0.0f->... ] [ UNSET = -1 ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly byte[] mTranslatedTrackLocation; // [ string ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly byte[] mTranslatedTrackVariation; // [ string ]]

        public readonly float mBrakeBias; // [ RANGE = 0.0f->1.0f... ]   [ UNSET = -1.0f ]
        public readonly float mTurboBoostPressure;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public readonly byte[] mLFTyreCompoundName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public readonly byte[] mRFTyreCompoundName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public readonly byte[] mLRTyreCompoundName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public readonly byte[] mRRTyreCompoundName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly uint[] mPitSchedules; // [ enum (Type#7)  Pit Mode ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly uint[] mHighestFlagColours; // [ enum (Type#5) Flag Colour ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly uint[] mHighestFlagReasons; // [ enum (Type#6) Flag Reason ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly uint[] mNationalities; // [ nationality table , SP AND UNSET = 0 ] See nationalities.txt file for details

        public readonly float mSnowDensity; // [ UNITS = How much snow will fall ]   [ RANGE = 0.0f->1.0f ], this will be non zero only in Snow season, in other seasons whatever is falling from the sky is reported as rain

        // AMS2 Additions (v10...)

        // Session info
        public readonly float mSessionDuration;           // [ UNITS = minutes ]   [ UNSET = 0.0f ]  The scheduled session Length (unset means laps race. See mLapsInEvent)
        public readonly int mSessionAdditionalLaps;     // The number of additional complete laps lead lap drivers must complete to finish a timed race after the session duration has elapsed.

        // Tyres
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mTyreTempLeft;    // [ UNITS = Celsius ]   [ UNSET = 0.0f ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mTyreTempCenter;  // [ UNITS = Celsius ]   [ UNSET = 0.0f ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mTyreTempRight;   // [ UNITS = Celsius ]   [ UNSET = 0.0f ]

        // DRS
        public readonly uint mDrsState;           // [ enum (Type#14) DrsState ]

        // Suspension
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly float[] mRideHeight;      // [ UNITS = cm ]

        // Input
        public readonly uint mJoyPad0;            // button mask
        public readonly uint mDPad;               // button mask

        public readonly int mAntiLockSetting;             // [ UNSET = -1 ] Current ABS garage setting. Valid under player control only.
        public readonly int mTractionControlSetting;      // [ UNSET = -1 ] Current ABS garage setting. Valid under player control only.

        // ERS
        public readonly int mErsDeploymentMode;           // [ enum (Type#15)  ErsDeploymentMode ]
        public readonly bool mErsAutoModeEnabled;         // true if the deployment mode was selected by auto system. Valid only when mErsDeploymentMode > ERS_DEPLOYMENT_MODE_NONE

        // Clutch State & Damage
        public readonly float mClutchTemp;                // [ UNITS = Kelvin ] [ UNSET = -273.16 ]
        public readonly float mClutchWear;                // [ RANGE = 0.0f->1.0f... ]
        public readonly bool  mClutchOverheated;          // true if clutch performance is degraded due to overheating
        //public bool  mClutchSlipping;            // true if clutch is slipping (can be induced by overheating or wear)

        public readonly YellowFlagState mYellowFlagState;             // [ enum (Type#16) YellowFlagState ]
        
        public readonly uint mLaunchStage;           // [ enum (Type#17) LaunchStage
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
#pragma warning restore SA1649
}