namespace SecondMonitor.F12024Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct PacketParticipantsData
    {
        public PacketHeader MHeader;            // Header

        public byte MNumActiveCars;  // Number of active cars in the data – should match number of cars on HUD

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
        public ParticipantData[] MParticipants;
    }
}