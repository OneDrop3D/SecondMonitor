namespace SecondMonitor.F12021Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct PacketCarTelemetryData
    {
        public PacketHeader Header; // Header

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
        public CarTelemetryData[] CarTelemetryData;

        public byte MfdPanelIndex; // Index of MFD panel open - 255 = MFD closed

        // Single player, race – 0 = Car setup, 1 = Pits
        // 2 = Damage, 3 =  Engine, 4 = Temperatures
        // May vary depending on game mode
        public byte MfdPanelIndexSecondaryPlayer; // See above

        public sbyte SuggestedGear; // Suggested gear for the player (1-8)
        // 0 if no gear suggested
    }
}