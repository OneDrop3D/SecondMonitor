namespace SecondMonitor.F12021Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketSessionHistoryData
    {
        public PacketHeader Header; // Header
        public byte CarIdx; // Index of the car this lap data relates to
        public byte NumLaps; // Num laps in the data (including current partial lap)
        public byte NumTyreStints; // Number of tyre stints in the data

        public byte BestLapTimeLapNum; // Lap the best lap time was achieved on
        public byte BestSector1LapNum; // Lap the best Sector 1 time was achieved on
        public byte BestSector2LapNum; // Lap the best Sector 2 time was achieved on
        public byte BestSector3LapNum; // Lap the best Sector 3 time was achieved on

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        public LapHistoryData[] LapHistoryData;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public TyreStintHistoryData[] TyreStintHistoryData;
    }
}