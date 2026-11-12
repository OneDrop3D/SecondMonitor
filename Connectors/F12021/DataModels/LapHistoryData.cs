namespace SecondMonitor.F12021Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LapHistoryData
    {
        public uint LapTimeInMS; // Lap time in milliseconds
        public ushort Sector1TimeInMS; // Sector 1 time in milliseconds
        public ushort Sector2TimeInMS; // Sector 2 time in milliseconds
        public ushort Sector3TimeInMS; // Sector 3 time in milliseconds

        public byte LapValidBitFlags; // 0x01 bit set-lap valid,      0x02 bit set-sector 1 valid
        // 0x04 bit set-sector 2 valid, 0x08 bit set-sector 3 valid
    }
}