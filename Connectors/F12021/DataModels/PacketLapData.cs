namespace SecondMonitor.F12021Connector.DataModels
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
    }
}