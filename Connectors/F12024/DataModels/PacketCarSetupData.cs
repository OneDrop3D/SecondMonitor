namespace SecondMonitor.F12024Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct PacketCarSetupData
    {
        public PacketHeader Header;            // Header

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
        public CarSetupData[] MCarSetups;

        public float NextFrontWingValue; // Value of front wing after next pit stop - player only
    }
}