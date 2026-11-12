namespace SecondMonitor.F12020Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LapData
    {
        public float LastLapTime; // Last lap time in seconds
        public float CurrentLapTime; // Current time around the lap in seconds

        //UPDATED in Beta 3:
        public ushort Sector1TimeInMs; // Sector 1 time in milliseconds
        public ushort Sector2TimeInMs; // Sector 2 time in milliseconds
        public float BestLapTime; // Best lap time of the session in seconds
        public byte BestLapNum; // Lap number best time achieved on
        public ushort BestLapSector1TimeInMs; // Sector 1 time of best lap in the session in milliseconds
        public ushort BestLapSector2TimeInMs; // Sector 2 time of best lap in the session in milliseconds
        public ushort BestLapSector3TimeInMs; // Sector 3 time of best lap in the session in milliseconds
        public ushort BestOverallSector1TimeInMs; // Best overall sector 1 time of the session in milliseconds
        public byte BestOverallSector1LapNum; // Lap number best overall sector 1 time achieved on
        public ushort BestOverallSector2TimeInMs; // Best overall sector 2 time of the session in milliseconds
        public byte BestOverallSector2LapNum; // Lap number best overall sector 2 time achieved on
        public ushort BestOverallSector3TimeInMs; // Best overall sector 3 time of the session in milliseconds
        public byte BestOverallSector3LapNum; // Lap number best overall sector 3 time achieved on

        public float LapDistance; // Distance vehicle is around current lap in metres – could

        // be negative if line hasn’t been crossed yet
        public float TotalDistance; // Total distance travelled in session in metres – could

        // be negative if line hasn’t been crossed yet
        public float SafetyCarDelta; // Delta in seconds for safety car
        public byte CarPosition; // Car race position
        public byte CurrentLapNum; // Current lap number
        public byte PitStatus; // 0 = none, 1 = pitting, 2 = in pit area
        public byte Sector; // 0 = sector1, 1 = sector2, 2 = sector3
        public byte CurrentLapInvalid; // Current lap invalid - 0 = valid, 1 = invalid
        public byte Penalties; // Accumulated time penalties in seconds to be added
        public byte GridPosition; // Grid position the vehicle started the race in

        // Status of driver - 0 = in garage, 1 = flying lap
        // 2 = in lap, 3 = out lap, 4 = on track
        public byte DriverStatus;

        // Result status - 0 = invalid, 1 = inactive, 2 = active
        // 3 = finished, 4 = disqualified, 5 = not classified
        // 6 = retired
        public byte ResultStatus;
    }
}