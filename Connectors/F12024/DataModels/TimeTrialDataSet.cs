namespace SecondMonitor.F12024Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TimeTrialDataSet
    {
        public byte CarIdx; // Index of the car this data relates to
        public byte TeamId; // Team id - see appendix
        public uint LapTimeInMS; // Lap time in milliseconds
        public uint Sector1TimeInMS; // Sector 1 time in milliseconds
        public uint Sector2TimeInMS; // Sector 2 time in milliseconds
        public uint Sector3TimeInMS; // Sector 3 time in milliseconds
        public byte TractionControl; // 0 = off, 1 = medium, 2 = full
        public byte GearboxAssist; // 1 = manual, 2 = manual & suggested gear, 3 = auto
        public byte AntiLockBrakes; // 0 (off) - 1 (on)
        public byte EqualCarPerformance; // 0 = Realistic, 1 = Equal
        public byte CustomSetup;
        public byte Valid;
    }
}
