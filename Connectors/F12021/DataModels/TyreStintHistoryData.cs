namespace SecondMonitor.F12021Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TyreStintHistoryData
    {
        public byte EndLap; // Lap the tyre usage ends on (255 of current tyre)
        public byte TyreActualCompound; // Actual tyres used by this driver
        public byte TyreVisualCompound; // Visual tyres used by this driver
    }
}