namespace SecondMonitor.F12024Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LapHistoryData
    {
        public uint LapTimeInMS; // Lap time in milliseconds
        public ushort Sector1TimeInMs; // Sector 1 time in milliseconds
        public byte Sector1TimeMinutesPart; // Sector 1 whole minute part
        public ushort Sector2TimeInMs; // Sector 2 time in milliseconds
        public byte Sector2TimeMinutesPart; // Sector 2 whole minute part
        public ushort Sector3TimeInMs; // Sector 2 time in milliseconds
        public byte Sector3TimeMinutesPart; // Sector 2 whole minute part

        public byte LapValidBitFlags; // 0x01 bit set-lap valid,      0x02 bit set-sector 1 valid
        // 0x04 bit set-sector 2 valid, 0x08 bit set-sector 3 valid
    }
}