/*
lmU internal state mapping structures.  Allows access to native C++ structs from C#.
Must be kept in sync with Include\lmUState.h.

See: MainForm.MainUpdate for sample on how to marshall from native in memory struct.

Author: The Iron Wolf (vleonavicius@hotmail.com)
Website: thecrewchief.org
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
#pragma warning disable RCS1036
#pragma warning disable SA1132
#pragma warning disable SA1649

namespace SecondMonitor.LmUConnector.SharedMemory
{
    using System.Runtime.InteropServices;

    // Marshalled types:
    // C++                 C#
    // char          ->    byte
    // unsigned char ->    byte
    // signed char   ->    sbyte
    // bool          ->    byte
    // long          ->    int
    // short         ->    short
    // ULONGLONG     ->    Int64
    public class LmuConstants
    {
        public const string LmuTelemetryFilename = "LMU_Data";

        public const int MaxMappedVehicles = 104;
        public const int MaxMappedIds = 512;
        public const int MaxPathLength = 260;

        public const byte RowX = 0;
        public const byte RowY = 1;
        public const byte RowZ = 2;

        public enum LmuVehicleClass
        {
            Hypercar = 0x00,
            Lmp2Elms = 0x02,
            Lmp2,
            Lmp3,
            Gte,
            Gt3,
            PaceCar = 0x08,
            Unknown = 0xFF
        }

        public enum LmuVehicleChampionship
        {
            Wec2023 = 0x00, Wec2024, Wec2025, Wec2026,
            Elms2025 = 0X10, Elms2026,
            Unknown = 0xFF,
        }

        // 0 Before session has begun
        // 1 Reconnaissance laps (race only)
        // 2 Grid walk-through (race only)
        // 3 Formation lap (race only)
        // 4 Starting-light countdown has begun (race only)
        // 5 Green flag
        // 6 Full course yellow / safety car
        // 7 Session stopped
        // 8 Session over
        public enum LmUGamePhase
        {
            Garage = 0,
            WarmUp = 1,
            GridWalk = 2,
            Formation = 3,
            Countdown = 4,
            GreenFlag = 5,
            FullCourseYellow = 6,
            SessionStopped = 7,
            SessionOver = 8
        }

        // Yellow flag states (applies to full-course only)
        // -1 Invalid
        //  0 None
        //  1 Pending
        //  2 Pits closed
        //  3 Pit lead lap
        //  4 Pits open
        //  5 Last lap
        //  6 Resume
        //  7 Race halt (not currently used)
        public enum LmUYellowFlagState
        {
            Invalid = -1,
            NoFlag = 0,
            Pending = 1,
            PitClosed = 2,
            PitLeadLap = 3,
            PitOpen = 4,
            LastLap = 5,
            Resume = 6,
            RaceHalt = 7
        }

        // 0=dry, 1=wet, 2=grass, 3=dirt, 4=gravel, 5=rumblestrip, 6=special
        public enum LmUSurfaceType
        {
            Dry = 0,
            Wet = 1,
            Grass = 2,
            Dirt = 3,
            Gravel = 4,
            Kerb = 5,
            Special = 6
        }

        // 0=sector3, 1=sector1, 2=sector2 (don't ask why)
        public enum LmUSector
        {
            Sector3 = 0,
            Sector1 = 1,
            Sector2 = 2
        }

        // 0=none, 1=finished, 2=dnf, 3=dq
        public enum LmUFinishStatus
        {
            None = 0,
            Finished = 1,
            Dnf = 2,
            Dq = 3
        }

        // who's in control: -1=nobody (shouldn't get this), 0=local player, 1=local AI, 2=remote, 3=replay (shouldn't get this)
        public enum LmUControl
        {
            Nobody = -1,
            Player = 0,
            Ai = 1,
            Remote = 2,
            Replay = 3
        }

        // wheel info (front left, front right, rear left, rear right)
        public enum LmUWheelIndex
        {
            FrontLeft = 0,
            FrontRight = 1,
            RearLeft = 2,
            RearRight = 3
        }

        // 0=none, 1=request, 2=entering, 3=stopped, 4=exiting
        public enum LmUPitState
        {
            None = 0,
            Request = 1,
            Entering = 2,
            Stopped = 3,
            Exiting = 4
        }

        // primary flag being shown to vehicle (currently only 0=green or 6=blue)
        public enum LmUPrimaryFlag
        {
            Green = 0,
            Blue = 6
        }

        // 0 = do not count lap or time, 1 = count lap but not time, 2 = count lap and time
        public enum LmUCountLapFlag
        {
            DoNotCountLap = 0,
            CountLapButNotTime = 1,
            CountLapAndTime = 2,
        }

        // 0=disallowed, 1=criteria detected but not allowed quite yet, 2=allowed
        public enum LmURearFlapLegalStatus
        {
            Disallowed = 0,
            DetectedButNotAllowedYet = 1,
            Alllowed = 2
        }

        // 0=off 1=ignition 2=ignition+starter
        public enum LmUIgnitionStarterStatus
        {
            Off = 0,
            Ignition = 1,
            IgnitionAndStarter = 2
        }

        // 0=no change, 1=go active, 2=head for pits
        public enum LmUSafetyCarInstruction
        {
            NoChange = 0,
            GoActive = 1,
            HeadForPits = 2
        }
    }

    namespace lmuData
    {

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public readonly struct LmUVec3
        {
            public readonly double x;
            public readonly double y;
            public readonly double z;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUWheel
        {
            public readonly double mSuspensionDeflection; // meters
            public readonly double mRideHeight; // meters
            public readonly double mSuspForce; // pushrod load in Newtons
            public readonly double mBrakeTemp; // Celsius
            public readonly double mBrakePressure; // currently 0.0-1.0, depending on driver input and brake balance; will convert to true brake pressure (kPa) in future

            public readonly double mRotation; // radians/sec
            public readonly double mLateralPatchVel; // lateral velocity at contact patch
            public readonly double mLongitudinalPatchVel; // longitudinal velocity at contact patch
            public readonly double mLateralGroundVel; // lateral velocity at contact patch
            public readonly double mLongitudinalGroundVel; // longitudinal velocity at contact patch
            public readonly double mCamber; // radians (positive is left for left-side wheels, right for right-side wheels)
            public readonly double mLateralForce; // Newtons
            public readonly double mLongitudinalForce; // Newtons
            public readonly double mTireLoad; // Newtons

            public readonly double mGripFract; // an approximation of what fraction of the contact patch is sliding
            public readonly double mPressure; // kPa (tire pressure)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public readonly double[] mTemperature; // Kelvin (subtract 273.15 to get Celsius), left/center/right (not to be confused with inside/center/outside!)
            public readonly double mWear; // wear (0.0-1.0, fraction of maximum) ... this is not necessarily proportional with grip loss
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public readonly byte[] mTerrainName; // the material prefixes from the TDF file
            public readonly byte mSurfaceType; // 0=dry, 1=wet, 2=grass, 3=dirt, 4=gravel, 5=rumblestrip, 6=special
            public readonly byte mFlat; // whether tire is flat
            public readonly byte mDetached; // whether wheel is detached
            public readonly byte mStaticUndeflectedRadius; // tire radius in centimeters

            public readonly double mVerticalTireDeflection; // how much is tire deflected from its (speed-sensitive) radius
            public readonly double mWheelYLocation; // wheel's y location relative to vehicle y location
            public readonly double mToe; // current toe angle w.r.t. the vehicle

            public readonly double mTireCarcassTemperature; // rough average of temperature samples from carcass (Kelvin)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public readonly double[] mTireInnerLayerTemperature; // rough average of temperature samples from innermost layer of rubber (before carcass) (Kelvin)

            public readonly float mOptimalTemp;
            public readonly byte mCompoundIndex;
            public readonly byte mCompoundType;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
            readonly byte[] mExpansion; // for future use
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUVehicleTelemetry
        {
            // Time
            public readonly int mID; // slot ID (note that it can be re-used in multiplayer after someone leaves)
            public readonly double mDeltaTime; // time since last update (seconds)
            public readonly double mElapsedTime; // game session time
            public readonly int mLapNumber; // current lap number
            public readonly double mLapStartET; // time this lap was started
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public readonly byte[] mVehicleName; // current vehicle name
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public readonly byte[] mTrackName; // current track name

            // Position and derivatives
            public readonly LmUVec3 mPos; // world position in meters
            public readonly LmUVec3 mLocalVel; // velocity (meters/sec) in local vehicle coordinates
            public readonly LmUVec3 mLocalAccel; // acceleration (meters/sec^2) in local vehicle coordinates

            // Orientation and derivatives
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public readonly LmUVec3[] mOri; // rows of orientation matrix (use TelemQuat conversions if desired), also converts local
            // vehicle vectors into world X, Y, or Z using dot product of rows 0, 1, or 2 respectively
            public readonly LmUVec3 mLocalRot; // rotation (radians/sec) in local vehicle coordinates
            public readonly LmUVec3 mLocalRotAccel; // rotational acceleration (radians/sec^2) in local vehicle coordinates

            // Vehicle status
            public readonly int mGear; // -1=reverse, 0=neutral, 1+=forward gears
            public readonly double mEngineRPM; // engine RPM
            public readonly double mEngineWaterTemp; // Celsius
            public readonly double mEngineOilTemp; // Celsius
            public readonly double mClutchRPM; // clutch RPM

            // Driver input
            public readonly double mUnfilteredThrottle; // ranges  0.0-1.0
            public readonly double mUnfilteredBrake; // ranges  0.0-1.0
            public readonly double mUnfilteredSteering; // ranges -1.0-1.0 (left to right)
            public readonly double mUnfilteredClutch; // ranges  0.0-1.0

            // Filtered input (various adjustments for rev or speed limiting, TC, ABS?, speed sensitive steering, clutch work for semi-automatic shifting, etc.)
            public readonly double mFilteredThrottle; // ranges  0.0-1.0
            public readonly double mFilteredBrake; // ranges  0.0-1.0
            public readonly double mFilteredSteering; // ranges -1.0-1.0 (left to right)
            public readonly double mFilteredClutch; // ranges  0.0-1.0

            // Misc
            public readonly double mSteeringShaftTorque; // torque around steering shaft (used to be mSteeringArmForce, but that is not necessarily accurate for feedback purposes)
            public readonly double mFront3rdDeflection; // deflection at front 3rd spring
            public readonly double mRear3rdDeflection; // deflection at rear 3rd spring

            // Aerodynamics
            public readonly double mFrontWingHeight; // front wing height
            public readonly double mFrontRideHeight; // front ride height
            public readonly double mRearRideHeight; // rear ride height
            public readonly double mDrag; // drag
            public readonly double mFrontDownforce; // front downforce
            public readonly double mRearDownforce; // rear downforce

            // State/damage info
            public readonly double mFuel; // amount of fuel (liters)
            public readonly double mEngineMaxRPM; // rev limit
            public readonly byte mScheduledStops; // number of scheduled pitstops
            public readonly byte mOverheating; // whether overheating icon is shown
            public readonly byte mDetached; // whether any parts (besides wheels) have been detached
            public readonly byte mHeadlights; // whether headlights are on
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public readonly byte[] mDentSeverity; // dent severity at 8 locations around the car (0=none, 1=some, 2=more)
            public readonly double mLastImpactET; // time of last impact
            public readonly double mLastImpactMagnitude; // magnitude of last impact
            public readonly LmUVec3 mLastImpactPos; // location of last impact

            // Expanded
            public readonly double mEngineTorque; // current engine torque (including additive torque) (used to be mEngineTq, but there's little reason to abbreviate it)
            public readonly int
                mCurrentSector; // the current sector (zero-based) with the pitlane stored in the sign bit (example: entering pits from third sector gives 0x80000002)
            public readonly byte mSpeedLimiter; // whether speed limiter is on
            public readonly byte mMaxGears; // maximum forward gears
            public readonly byte mFrontTireCompoundIndex; // index within brand
            public readonly byte mRearTireCompoundIndex; // index within brand
            public readonly double mFuelCapacity; // capacity in liters
            public readonly byte mFrontFlapActivated; // whether front flap is activated
            public readonly byte mRearFlapActivated; // whether rear flap is activated
            public readonly byte mRearFlapLegalStatus; // 0=disallowed, 1=criteria detected but not allowed quite yet, 2=allowed
            public readonly byte mIgnitionStarter; // 0=off 1=ignition 2=ignition+starter

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
            public readonly byte[] mFrontTireCompoundName; // name of front tire compound
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
            public readonly byte[] mRearTireCompoundName; // name of rear tire compound

            public readonly byte mSpeedLimiterAvailable; // whether speed limiter is available
            public readonly byte mAntiStallActivated; // whether (hard) anti-stall is activated
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public readonly byte[] mUnused; //
            public readonly float mVisualSteeringWheelRange; // the *visual* steering wheel range

            public readonly double mRearBrakeBias; // fraction of brakes on rear
            public readonly double mTurboBoostPressure; // current turbo boost pressure if available
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public readonly float[] mPhysicsToGraphicsOffset; // offset from static CG to graphical center
            public readonly float mPhysicalSteeringWheelRange; // the *physical* steering wheel range

            public readonly double mDelta;
            public readonly double mBatteryChargeFraction; // Battery charge as fraction [0.0-1.0]

            // electric boost motor
            public readonly double mElectricBoostMotorTorque; // current torque of boost motor (can be negative when in regenerating mode)
            public readonly double mElectricBoostMotorRPM; // current rpm of boost motor
            public readonly double mElectricBoostMotorTemperature; // current temperature of boost motor
            public readonly double mElectricBoostWaterTemperature; // current water temperature of boost motor cooler if present (0 otherwise)
            public readonly byte mElectricBoostMotorState; // 0=unavailable 1=inactive, 2=propulsion, 3=regeneration

            public readonly byte mLapInvalidated;
            public readonly byte mABSActive;
            public readonly byte mTCActive;
            public readonly byte mSpeedLimiterActive;
            public readonly byte mWiperState;
            public readonly byte mTC;
            public readonly byte mTCMax;
            public readonly byte mTCSlip;
            public readonly byte mTCSlipMax;
            public readonly byte mTCCut;
            public readonly byte mTCCutMax;
            public readonly byte mABS;
            public readonly byte mABSMax;
            public readonly byte mMotorMap;
            public readonly byte mMotorMapMax;
            public readonly byte mMigration;
            public readonly byte mMigrationMax;
            public readonly byte mFrontAntiSway;
            public readonly byte mFrontAntiSwayMax;
            public readonly byte mRearAntiSway;
            public readonly byte mRearAntiSwayMax;
            public readonly byte mLiftAndCoastProgress;
            public readonly byte mTrackLimitsSteps; // Normalized track limits points (TrackLimitPoints * TrackLimitStepsPerPoint)
            public readonly float mRegen; //kW
            public readonly float mSoC;
            public readonly float mVirtualEnergy;
            public readonly float mTimeGapCarAhead;
            public readonly float mTimeGapCarBehind;
            public readonly float mTimeGapPlaceAhead;
            public readonly float mTimeGapPlaceBehind;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            public readonly byte[] mVehicleModel;

            //lmuConstants.lmuVehicleClass
            public readonly byte mVehicleClass;

            //lmuConstants.lmuVehicleChampionship
            public readonly byte mVehicleChampionship;

            // Future use
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public readonly byte[] mExpansion; // for future use (note that the slot ID has been moved to mID above)

            // keeping this at the end of the structure to make it easier to replace in future versions
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public readonly LmUWheel[] mWheels; // wheel info (front left, front right, rear left, rear right)
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUGraficsInfoV001
        {
            public readonly LmUVec3 mCamPos;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public readonly LmUVec3[] mCamOri;

            public readonly ulong mHWND;
            public readonly double mAmbientRed;
            public readonly double mAmbientGreen;
            public readonly double mAmbientBlue;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUGraficsInfoV002
        {
            public readonly LmUVec3 mCamPos;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public readonly LmUVec3[] mCamOri;

            public readonly ulong mHWND;
            public readonly double mAmbientRed;
            public readonly double mAmbientGreen;
            public readonly double mAmbientBlue;

            public readonly int mID; // slot ID being viewed (-1 if invalid)

            // Camera types (some of these may only be used for *setting* the camera type in WantsToViewVehicle())
            //    0  = TV cockpit
            //    1  = cockpit
            //    2  = nosecam
            //    3  = swingman
            //    4  = trackside (nearest)
            //    5  = onboard000
            //       :
            //       :
            // 1004  = onboard999
            // 1005+ = (currently unsupported, in the future may be able to set/get specific trackside camera)
            public readonly int mCameraType; // see above comments for possible values

            // Future use
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            public readonly byte[] mExpansion; // for future use (note that the slot ID has been moved to mID above)
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUCameraControlInfoV01
        {
            // Cameras
            public readonly int mID; // slot ID to view
            public readonly int mCameraType; // see GraphicsInfoV02 comments for values

            // Replays (note that these are asynchronous)
            public readonly byte mReplayActive; // This variable is an *input* filled with whether the replay is currently active (as opposed to realtime).
            public readonly byte mReplayUnused; //
            public readonly byte
                mReplayCommand; // 0=do nothing, 1=begin, 2=end, 3=rewind, 4=fast backwards, 5=backwards, 6=slow backwards, 7=stop, 8=slow play, 9=play, 10=fast play, 11=fast forward

            public readonly byte mReplaySetTime; // Whether to skip to the following replay time:
            public readonly float mReplaySeconds; // The replay time in seconds to skip to (note: the current replay maximum ET is passed into this variable in case you need it)

            // Future use
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 120)]
            public readonly byte[] mExpansion; // for future use (note that the slot ID has been moved to mID above)
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUMessageInfoV01
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            public readonly byte[] mText; // message to display

            public readonly byte mDestination; // 0 = message center, 1 = chat (can be used for multiplayer chat commands)
            public readonly byte mTranslate; // 0 = do not attempt to translate, 1 = attempt to translate

            // Future use
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 126)]
            public readonly byte[] mExpansion; // for future use (note that the slot ID has been moved to mID above)
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUVehicleScoring
        {
            public readonly int mID; // slot ID (note that it can be re-used in multiplayer after someone leaves)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public readonly byte[] mDriverName; // driver name
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public readonly byte[] mVehicleName; // vehicle name
            public readonly short mTotalLaps; // laps completed
            public readonly sbyte mSector; // 0=sector3, 1=sector1, 2=sector2 (don't ask why)
            public readonly sbyte mFinishStatus; // 0=none, 1=finished, 2=dnf, 3=dq
            public readonly double mLapDist; // current distance around track
            public readonly double mPathLateral; // lateral position with respect to *very approximate* "center" path
            public readonly double mTrackEdge; // track edge (w.r.t. "center" path) on same side of track as vehicle

            public readonly double mBestSector1; // best sector 1
            public readonly double mBestSector2; // best sector 2 (plus sector 1)
            public readonly double mBestLapTime; // best lap time
            public readonly double mLastSector1; // last sector 1
            public readonly double mLastSector2; // last sector 2 (plus sector 1)
            public readonly double mLastLapTime; // last lap time
            public readonly double mCurSector1; // current sector 1 if valid
            public readonly double mCurSector2; // current sector 2 (plus sector 1) if valid
            // no current laptime because it instantly becomes "last"

            public readonly short mNumPitstops; // number of pitstops made
            public readonly short mNumPenalties; // number of outstanding penalties
            public readonly byte mIsPlayer; // is this the player's vehicle

            public readonly sbyte mControl; // who's in control: -1=nobody (shouldn't get this), 0=local player, 1=local AI, 2=remote, 3=replay (shouldn't get this)
            public readonly byte mInPits; // between pit entrance and pit exit (not always accurate for remote vehicles)
            public readonly byte mPlace; // 1-based position
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public readonly byte[] mVehicleClass; // vehicle class

            // Dash Indicators
            public readonly double mTimeBehindNext; // time behind vehicle in next higher place
            public readonly int mLapsBehindNext; // laps behind vehicle in next higher place
            public readonly double mTimeBehindLeader; // time behind leader
            public readonly int mLapsBehindLeader; // laps behind leader
            public readonly double mLapStartET; // time this lap was started

            // Position and derivatives
            public readonly LmUVec3 mPos; // world position in meters
            public readonly LmUVec3 mLocalVel; // velocity (meters/sec) in local vehicle coordinates
            public readonly LmUVec3 mLocalAccel; // acceleration (meters/sec^2) in local vehicle coordinates

            // Orientation and derivatives
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public readonly LmUVec3[] mOri; // rows of orientation matrix (use TelemQuat conversions if desired), also converts local
            // vehicle vectors into world X, Y, or Z using dot product of rows 0, 1, or 2 respectively
            public readonly LmUVec3 mLocalRot; // rotation (radians/sec) in local vehicle coordinates
            public readonly LmUVec3 mLocalRotAccel; // rotational acceleration (radians/sec^2) in local vehicle coordinates

            // tag.2012.03.01 - stopped casting some of these so variables now have names and mExpansion has shrunk, overall size and old data locations should be same
            public readonly byte mHeadlights; // status of headlights
            public readonly byte mPitState; // 0=none, 1=request, 2=entering, 3=stopped, 4=exiting
            public readonly byte mServerScored; // whether this vehicle is being scored by server (could be off in qualifying or racing heats)
            public readonly byte mIndividualPhase; // game phases (described below) plus 9=after formation, 10=under yellow, 11=under blue (not used)

            public readonly int mQualification; // 1-based, can be -1 when invalid

            public readonly double mTimeIntoLap; // estimated time into lap
            public readonly double mEstimatedLapTime; // estimated laptime used for 'time behind' and 'time into lap' (note: this may changed based on vehicle and setup!?)

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
            public readonly byte[] mPitGroup; // pit group (same as team name unless pit is shared)
            public readonly byte mFlag; // primary flag being shown to vehicle (currently only 0=green or 6=blue)
            public readonly byte mUnderYellow; // whether this car has taken a full-course caution flag at the start/finish line
            public readonly byte mCountLapFlag; // 0 = do not count lap or time, 1 = count lap but not time, 2 = count lap and time
            public readonly byte mInGarageStall; // appears to be within the correct garage stall

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public readonly byte[] mUpgradePack; // Coded upgrades

            public readonly float mPitLapDist; // location of pit in terms of lap distance

            public readonly float mBestLapSector1; // sector 1 time from best lap (not necessarily the best sector 1 time)
            public readonly float mBestLapSector2; // sector 2 time from best lap (not necessarily the best sector 2 time)

            public readonly ulong mSteamID; // SteamID of the current driver (if any)

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public readonly byte[] mVehFilename; // filename of veh file used to identify this vehicle.

            public readonly sbyte mAttackMode;

            // 2020.11.12 - Took 1 byte from mExpansion to transmit fuel percentage
            public readonly byte mFuelFraction; // Percentage of fuel or battery left in vehicle. 0x00 = 0%; 0xFF = 100%

            // 2021.05.28 - Took 1 byte from mExpansion to transmit DRS (RearFlap) state - consider making this a bitfield if further bools are needed later on
            public readonly byte mDRSState;

            // Future use
            // tag.2012.04.06 - SEE ABOVE!
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public readonly byte[] mExpansion; // for future use
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUScoringInfo
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public readonly byte[] mTrackName; // current track name
            public readonly int mSession; // current session (0=testday 1-4=practice 5-8=qual 9=warmup 10-13=race)
            public readonly double mCurrentET; // current time
            public readonly double mEndET; // ending time
            public readonly int mMaxLaps; // maximum laps
            public readonly double mLapDist; // distance around track
            // MM_NOT_USED
            //char *mResultsStream;          // results stream additions since last update (newline-delimited and NULL-terminated)
            // MM_NEW
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public readonly byte[] pointer1;

            public readonly int mNumVehicles; // current number of vehicles

            // Game phases:
            // 0 Before session has begun
            // 1 Reconnaissance laps (race only)
            // 2 Grid walk-through (race only)
            // 3 Formation lap (race only)
            // 4 Starting-light countdown has begun (race only)
            // 5 Green flag
            // 6 Full course yellow / safety car
            // 7 Session stopped
            // 8 Session over
            // 9 Paused (tag.2015.09.14 - this is new, and indicates that this is a heartbeat call to the plugin)
            public readonly byte mGamePhase;

            // Yellow flag states (applies to full-course only)
            // -1 Invalid
            //  0 None
            //  1 Pending
            //  2 Pits closed
            //  3 Pit lead lap
            //  4 Pits open
            //  5 Last lap
            //  6 Resume
            //  7 Race halt (not currently used)
            public readonly sbyte mYellowFlagState;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public readonly sbyte[] mSectorFlag; // whether there are any local yellows at the moment in each sector (not sure if sector 0 is first or last, so test)
            public readonly byte mStartLight; // start light frame (number depends on track)
            public readonly byte mNumRedLights; // number of red lights in start sequence
            public readonly byte mInRealtime; // in realtime as opposed to at the monitor
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public readonly byte[] mPlayerName; // player name (including possible multiplayer override)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public readonly byte[] mPlrFileName; // may be encoded to be a legal filename

            // weather
            public readonly double mDarkCloud; // cloud darkness? 0.0-1.0
            public readonly double mRaining; // raining severity 0.0-1.0
            public readonly double mAmbientTemp; // temperature (Celsius)
            public readonly double mTrackTemp; // temperature (Celsius)
            public readonly LmUVec3 mWind; // wind speed
            public readonly double mMinPathWetness; // minimum wetness on main path 0.0-1.0
            public readonly double mMaxPathWetness; // maximum wetness on main path 0.0-1.0

            // multiplayer
            public readonly byte mGameMode; // 1 = server, 2 = client, 3 = server and client
            public readonly byte mIsPasswordProtected; // is the server password protected
            public readonly byte mServerPort; // the port of the server (if on a server)
            public readonly uint mServerPublicIP; // the public IP address of the server (if on a server)
            public readonly int mMaxPlayers; // maximum number of vehicles that can be in the session

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public readonly byte[] mServerName; // name of the server
            public readonly float mStartET; // start time (seconds since midnight) of the event

            //
            public readonly double mAvgPathWetness; // average wetness on main path 0.0-1.0
            public readonly float mSessionTimeRemaining;
            public readonly float mTimeOfDay;
            public readonly byte mIsFixedSetup;
            public readonly byte mTrackGripLevel;
            public readonly byte mCloudCoverage;
            public readonly byte mTrackLimitsStepsPerPenalty;
            public readonly byte mTrackLimitsStepsPerPoint;

            // Future use
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 187)]
            public readonly byte[] mExpansion;

            // MM_NOT_USED
            // keeping this at the end of the structure to make it easier to replace in future versions
            // VehicleScoringInfoV01 *mVehicle; // array of vehicle scoring info's
            // MM_NEW
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public readonly byte[] pointer2;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUCommentaryRequestInfoV01
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public readonly byte[] mName; // one of the event names in the commentary INI file
            public readonly double mInput1; // first value to pass in (if any)
            public readonly double mInput2; // first value to pass in (if any)
            public readonly double mInput3; // first value to pass in (if any)
            public readonly byte mSkipChecks; // ignores commentary detail and random probability of event
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUPhysicsOptions
        {
            public readonly byte mTractionControl; // 0 (off) - 3 (high)
            public readonly byte mAntiLockBrakes; // 0 (off) - 2 (high)
            public readonly byte mStabilityControl; // 0 (off) - 2 (high)
            public readonly byte mAutoShift; // 0 (off), 1 (upshifts), 2 (downshifts), 3 (all)
            public readonly byte mAutoClutch; // 0 (off), 1 (on)
            public readonly byte mInvulnerable; // 0 (off), 1 (on)
            public readonly byte mOppositeLock; // 0 (off), 1 (on)
            public readonly byte mSteeringHelp; // 0 (off) - 3 (high)
            public readonly byte mBrakingHelp; // 0 (off) - 2 (high)
            public readonly byte mSpinRecovery; // 0 (off), 1 (on)
            public readonly byte mAutoPit; // 0 (off), 1 (on)
            public readonly byte mAutoLift; // 0 (off), 1 (on)
            public readonly byte mAutoBlip; // 0 (off), 1 (on)

            public readonly byte mFuelMult; // fuel multiplier (0x-7x)
            public readonly byte mTireMult; // tire wear multiplier (0x-7x)
            public readonly byte mMechFail; // mechanical failure setting; 0 (off), 1 (normal), 2 (timescaled)
            public readonly byte mAllowPitcrewPush; // 0 (off), 1 (on)
            public readonly byte mRepeatShifts; // accidental repeat shift prevention (0-5; see PLR file)
            public readonly byte mHoldClutch; // for auto-shifters at start of race: 0 (off), 1 (on)
            public readonly byte mAutoReverse; // 0 (off), 1 (on)
            public readonly byte mAlternateNeutral; // Whether shifting up and down simultaneously equals neutral

            // tag.2014.06.09 - yes these are new, but no they don't change the size of the structure nor the address of the other variables in it (because we're just using the existing padding)
            public readonly byte mAIControl; // Whether player vehicle is currently under AI control
            public readonly byte mUnused1; //
            public readonly byte mUnused2; //

            public readonly float mManualShiftOverrideTime; // time before auto-shifting can resume after recent manual shift
            public readonly float mAutoShiftOverrideTime; // time before manual shifting can resume after recent auto shift
            public readonly float mSpeedSensitiveSteering; // 0.0 (off) - 1.0
        }


        //////////////////////////////////////////////////////////////////////////////////////////
        // Identical to TrackRulesCommandV01, except where noted by MM_NEW/MM_NOT_USED comments.  Renamed to match plugin convention.
        //////////////////////////////////////////////////////////////////////////////////////////
        public enum LmUTrackRulesCommand
        {
            AddFromTrack = 0, // crossed s/f line for first time after full-course yellow was called
            AddFromPit, // exited pit during full-course yellow
            AddFromUndq, // during a full-course yellow, the admin reversed a disqualification
            RemoveToPit, // entered pit during full-course yellow
            RemoveToDnf, // vehicle DNF'd during full-course yellow
            RemoveToDq, // vehicle DQ'd during full-course yellow
            RemoveToUnloaded, // vehicle unloaded (possibly kicked out or banned) during full-course yellow
            MoveToBack, // misbehavior during full-course yellow, resulting in the penalty of being moved to the back of their current line
            LongestTime, // misbehavior during full-course yellow, resulting in the penalty of being moved to the back of the longest line
            //------------------
            Maximum // should be last
        }


        //////////////////////////////////////////////////////////////////////////////////////////
        // Identical to TrackRulesActionV01, except where noted by MM_NEW/MM_NOT_USED comments.
        //////////////////////////////////////////////////////////////////////////////////////////
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public readonly struct LmUTrackRulesAction
        {
            // input only
            public readonly LmUTrackRulesCommand mCommand; // recommended action
            public readonly int mID; // slot ID if applicable
            public readonly double mET; // elapsed time that event occurred, if applicable
        }


        //////////////////////////////////////////////////////////////////////////////////////////
        // Identical to TrackRulesColumnV01, except where noted by MM_NEW/MM_NOT_USED comments.  Renamed to match plugin convention.
        //////////////////////////////////////////////////////////////////////////////////////////
        public enum LmUTrackRulesColumn
        {
            LeftLane = 0, // left (inside)
            MidLefLane, // mid-left
            MiddleLane, // middle
            MidrRghtLane, // mid-right
            RightLane, // right (outside)
            //------------------
            MaxLanes, // should be after the valid static lane choices
            //------------------
            Invalid = MaxLanes, // currently invalid (hasn't crossed line or in pits/garage)
            FreeChoice, // free choice (dynamically chosen by driver)
            Pending, // depends on another participant's free choice (dynamically set after another driver chooses)
            //------------------
            Maximum // should be last
        }

        // replaces the ScreenInfoV01 structure that was deprecated since V8
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUApplicationState
        {
            public readonly ulong mAppWindow; // application window handle
            public readonly uint mWidth; // screen width
            public readonly uint mHeight; // screen height
            public readonly uint mRefreshRate; // refresh rate
            public readonly uint mWindowed; // really just a boolean whether we are in windowed mode
            public readonly byte mOptionsLocation; // 0=main UI, 1=track loading, 2=monitor, 3=on track

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 31)]
            public readonly byte[] mOptionsPage; // the name of the options page

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 204)]
            public readonly byte[] mExpansion; // future use
        }


        //////////////////////////////////////////////////////////////////////////////////////////
        // Identical to TrackRulesParticipantV01, except where noted by MM_NEW/MM_NOT_USED comments.
        //////////////////////////////////////////////////////////////////////////////////////////
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUTrackRulesParticipant
        {
            // input only
            public readonly int mID; // slot ID
            public readonly short mFrozenOrder; // 0-based place when caution came out (not valid for formation laps)
            public readonly short mPlace; // 1-based place (typically used for the initialization of the formation lap track order)
            public readonly float
                mYellowSeverity; // a rating of how much this vehicle is contributing to a yellow flag (the sum of all vehicles is compared to TrackRulesV01::mSafetyCarThreshold)
            public readonly double mCurrentRelativeDistance; // equal to ( ( ScoringInfoV01::mLapDist * this->mRelativeLaps ) + VehicleScoringInfoV01::mLapDist )

            // input/output
            public readonly int
                mRelativeLaps; // current formation/caution laps relative to safety car (should generally be zero except when safety car crosses s/f line); this can be decremented to implement 'wave around' or 'beneficiary rule' (a.k.a. 'lucky dog' or 'free pass')
            public readonly LmUTrackRulesColumn mColumnAssignment; // which column (line/lane) that participant is supposed to be in
            public readonly int mPositionAssignment; // 0-based position within column (line/lane) that participant is supposed to be located at (-1 is invalid)
            public readonly byte mAllowedToPit; // whether the rules allow this particular vehicle to enter pits right now

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public readonly byte[] mUnused; //

            public readonly double mGoalRelativeDistance; // calculated based on where the leader is, and adjusted by the desired column spacing and the column/position assignments

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)]
            public readonly byte[] mMessage; // a message for this participant to explain what is going on (untranslated; it will get run through translator on client machines)

            // future expansion
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 192)]
            public readonly byte[] mExpansion;
        }


        //////////////////////////////////////////////////////////////////////////////////////////
        // Identical to TrackRulesStageV01, except where noted by MM_NEW/MM_NOT_USED comments.  Renamed to match plugin convention.
        //////////////////////////////////////////////////////////////////////////////////////////
        public enum LmUTrackRulesStage
        {
            FormationInit = 0, // initialization of the formation lap
            FormationUpdate, // update of the formation lap
            Normal, // normal (non-yellow) update
            CautionInit, // initialization of a full-course yellow
            CautionUpdate, // update of a full-course yellow
            //------------------
            Maximum // should be last
        }


        //////////////////////////////////////////////////////////////////////////////////////////
        // Identical to TrackRulesV01, except where noted by MM_NEW/MM_NOT_USED comments.
        //////////////////////////////////////////////////////////////////////////////////////////
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUTrackRules
        {
            // input only
            public readonly double mCurrentET; // current time
            public readonly LmUTrackRulesStage mStage; // current stage
            public readonly LmUTrackRulesColumn mPoleColumn; // column assignment where pole position seems to be located
            public readonly int mNumActions; // number of recent actions

            // MM_NOT_USED
            // TrackRulesActionV01 *mAction;         // array of recent actions
            // MM_NEW
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public readonly byte[] pointer1;

            public readonly int mNumParticipants; // number of participants (vehicles)

            public readonly byte mYellowFlagDetected; // whether yellow flag was requested or sum of participant mYellowSeverity's exceeds mSafetyCarThreshold
            public readonly byte mYellowFlagLapsWasOverridden; // whether mYellowFlagLaps (below) is an admin request

            public readonly byte mSafetyCarExists; // whether safety car even exists
            public readonly byte mSafetyCarActive; // whether safety car is active
            public readonly int mSafetyCarLaps; // number of laps
            public readonly float
                mSafetyCarThreshold; // the threshold at which a safety car is called out (compared to the sum of TrackRulesParticipantV01::mYellowSeverity for each vehicle)
            public readonly double mSafetyCarLapDist; // safety car lap distance
            public readonly float mSafetyCarLapDistAtStart; // where the safety car starts from

            public readonly float mPitLaneStartDist; // where the waypoint branch to the pits breaks off (this may not be perfectly accurate)
            public readonly float mTeleportLapDist; // the front of the teleport locations (a useful first guess as to where to throw the green flag)

            // future input expansion
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public readonly byte[] mInputExpansion;

            // input/output
            public readonly sbyte mYellowFlagState; // see ScoringInfoV01 for values
            public readonly short mYellowFlagLaps; // suggested number of laps to run under yellow (may be passed in with admin command)

            public readonly int mSafetyCarInstruction; // 0=no change, 1=go active, 2=head for pits
            public readonly float mSafetyCarSpeed; // maximum speed at which to drive
            public readonly float mSafetyCarMinimumSpacing; // minimum spacing behind safety car (-1 to indicate no limit)
            public readonly float mSafetyCarMaximumSpacing; // maximum spacing behind safety car (-1 to indicate no limit)

            public readonly float mMinimumColumnSpacing; // minimum desired spacing between vehicles in a column (-1 to indicate indeterminate/unenforced)
            public readonly float mMaximumColumnSpacing; // maximum desired spacing between vehicles in a column (-1 to indicate indeterminate/unenforced)

            public readonly float mMinimumSpeed; // minimum speed that anybody should be driving (-1 to indicate no limit)
            public readonly float mMaximumSpeed; // maximum speed that anybody should be driving (-1 to indicate no limit)

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)]
            public readonly byte[] mMessage; // a message for everybody to explain what is going on (which will get run through translator on client machines)

            // MM_NOT_USED
            // TrackRulesParticipantV01 *mParticipant;         // array of partipants (vehicles)
            // MM_NEW
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public readonly byte[] pointer2;

            // future input/output expansion
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public readonly byte[] mInputOutputExpansion;
        };


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUMappedBufferVersionBlock
        {
            // If both version variables are equal, buffer is not being written to, or we're extremely unlucky and second check is necessary.
            // If versions don't match, buffer is being written to, or is incomplete (game crash, or missed transition).
            public readonly uint mVersionUpdateBegin; // Incremented right before buffer is written to.
            public readonly uint mVersionUpdateEnd; // Incremented after buffer write is done.
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUMappedBufferVersionBlockWithSize
        {
            public readonly uint mVersionUpdateBegin; // Incremented right before buffer is written to.
            public readonly uint mVersionUpdateEnd; // Incremented after buffer write is done.

            public readonly int mBytesUpdatedHint; // How many bytes of the structure were written during the last update.
            // 0 means unknown (whole buffer should be considered as updated).
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUPathData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = LmuConstants.MaxPathLength)]
            public readonly byte[] userData;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = LmuConstants.MaxPathLength)]
            public readonly byte[] customVariables;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = LmuConstants.MaxPathLength)]
            public readonly byte[] stewardResults;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = LmuConstants.MaxPathLength)]
            public readonly byte[] playerProfile;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = LmuConstants.MaxPathLength)]
            public readonly byte[] pluginsFolder;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUEvent
        {
            public readonly uint SME_ENTER;
            public readonly uint SME_EXIT;
            public readonly uint SME_STARTUP;
            public readonly uint SME_SHUTDOWN;
            public readonly uint SME_LOAD;
            public readonly uint SME_UNLOAD;
            public readonly uint SME_START_SESSION;
            public readonly uint SME_END_SESSION;
            public readonly uint SME_ENTER_REALTIME;
            public readonly uint SME_EXIT_REALTIME;
            public readonly uint SME_UPDATE_SCORING;
            public readonly uint SME_UPDATE_TELEMETRY;
            public readonly uint SME_INIT_APPLICATION;
            public readonly uint SME_UNINIT_APPLICATION;
            public readonly uint SME_SET_ENVIRONMENT;
            public readonly uint SME_FFB;
            //ommited
            /*public readonly uint SME_MAX;*/
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUGeneric
        {
            public readonly LmUEvent Event;
            public readonly int gameVersion;
            public readonly float FFBTorque;
            public readonly LmUApplicationState ApplicationState;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUObjectOut
        {
            public readonly LmUGeneric Generic;
            public readonly LmUPathData PathData;
            public readonly LmUScoring Scoring;
            public readonly LmUTelemetry Telemetry;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmULayout
        {
            public readonly LmUObjectOut ObjectOut;
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUTelemetry
        {
            public readonly byte activeVehicles;
            public readonly byte playerVehicleIdx;
            public readonly byte playerHasVehicle;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = LmuConstants.MaxMappedVehicles)]
            public readonly LmUVehicleTelemetry[] mVehicles;
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public readonly struct LmUScoring
        {
            public readonly LmUScoringInfo mScoringInfo;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public readonly byte[] scoringStreamSize;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = LmuConstants.MaxMappedVehicles)]
            public readonly LmUVehicleScoring[] mVehicles;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 65536)]
            public readonly byte[] scoringSteam;
        }
    }
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
#pragma warning restore RCS1036
#pragma warning restore SA1132
#pragma warning restore SA1649