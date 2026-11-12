namespace SecondMonitor.F12021Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FinalClassificationData
    {
        public byte Position; // Finishing position
        public byte NumLaps; // Number of laps completed
        public byte GridPosition; // Grid position of the car
        public byte Points; // Number of points scored
        public byte NumPitStops; // Number of pit stops made

        public byte ResultStatus; // Result status - 0 = invalid, 1 = inactive, 2 = active

        // 3 = finished, 4 = disqualified, 5 = not classified
        // 6 = retired
        public float BestLapTime; // Best lap time of the session in seconds
        public double TotalRaceTime; // Total race time in seconds without penalties
        public byte PenaltiesTime; // Total penalties accumulated in seconds
        public byte NumPenalties; // Number of penalties applied to this driver
        public byte NumTyreStints; // Number of tyres stints up to maximum
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] TyreStintsActual; // Actual tyres used by this driver
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] TyreStintsVisual; // Visual tyres used by this driver
    }
}