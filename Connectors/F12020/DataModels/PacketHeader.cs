namespace SecondMonitor.F12020Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketHeader
    {
        public ushort PacketFormat; // 2020
        public byte GameMajorVersion; // Game major version - "X.00"
        public byte GameMinorVersion; // Game minor version - "1.XX"
        public byte PacketVersion; // Version of this packet type, all start from 1
        public byte PacketId; // Identifier for the packet type, see below
        public ulong SessionUID; // Unique identifier for the session
        public float SessionTime; // Session timestamp
        public uint FrameIdentifier; // Identifier for the frame the data was retrieved on
        public byte PlayerCarIndex; // Index of player's car in the array

        // ADDED IN BETA 2:
        public byte SecondaryPlayerCarIndex; // Index of secondary player's car in the array (splitscreen)
        // 255 if no second player
    }
}