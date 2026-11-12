namespace SecondMonitor.F12024Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketLapData
    {
        public PacketHeader Header; // Header

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
        public LapData[] LapData; // Lap data for all cars on track
        public byte TimeTrialPBCarIdx; // Index of Personal Best car in time trial (255 if invalid)
        public byte TmeTrialRivalCarIdx; // Index of Rival car in time trial (255 if invalid)
    }
}
