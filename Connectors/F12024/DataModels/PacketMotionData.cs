namespace SecondMonitor.F12024Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketMotionData
    {
        public PacketHeader MHeader; // Header

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
        public CarMotionData[] CarMotionData; // Data for all cars on track
    }
}