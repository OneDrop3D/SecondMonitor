namespace SecondMonitor.F12021Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LapData
    {
        public uint LastLapTimeMs; // Last lap time in seconds
        public uint CurrentLapTimeMs; // Current time around the lap in seconds

        //UPDATED in Beta 3:
        public ushort Sector1TimeInMs; // Sector 1 time in milliseconds
        public ushort Sector2TimeInMs; // Sector 2 time in milliseconds

        public float LapDistance; // Distance vehicle is around current lap in metres – could

        // be negative if line hasn’t been crossed yet
        public float TotalDistance; // Total distance travelled in session in metres – could

        // be negative if line hasn’t been crossed yet
        public float SafetyCarDelta; // Delta in seconds for safety car
        public byte CarPosition; // Car race position
        public byte CurrentLapNum; // Current lap number
        public byte PitStatus; // 0 = none, 1 = pitting, 2 = in pit area
        public byte NumPitStops;
        public byte Sector; // 0 = sector1, 1 = sector2, 2 = sector3
        public byte CurrentLapInvalid; // Current lap invalid - 0 = valid, 1 = invalid
        public byte Penalties; // Accumulated time penalties in seconds to be added
        public byte Warnings; // Accumulated time penalties in seconds to be added
        public byte NumUnServedDriveThroughPens; // Accumulated time penalties in seconds to be added
        public byte NumUnServedStopGoPens; // Accumulated time penalties in seconds to be added
        public byte GridPosition; // Grid position the vehicle started the race in

        // Status of driver - 0 = in garage, 1 = flying lap
        // 2 = in lap, 3 = out lap, 4 = on track
        public byte DriverStatus;

        // Result status - 0 = invalid, 1 = inactive, 2 = active
        // 3 = finished, 4 = disqualified, 5 = not classified
        // 6 = retired
        public byte ResultStatus;

        public byte PitLaneTimerActive; // Pit lane timing, 0 = inactive, 1 = active
        public ushort PitLaneTimeInLaneInMS; // If active, the current time spent in the pit lane in ms
        public ushort PitStopTimerInMS; // Time of the actual pit stop in ms
        public byte PitStopShouldServePen; // Whether the car should serve a penalty at this stop
    }
}