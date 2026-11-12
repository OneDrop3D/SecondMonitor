namespace SecondMonitor.F12024Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LobbyInfoData
    {
        public byte AiControlled; // Whether the vehicle is AI (1) or Human (0) controlled
        public byte TeamId; // Team id - see appendix (255 if no team currently selected)
        public byte Nationality; // Nationality of the driver
        public byte Platform; // 1 = Steam, 3 = PlayStation, 4 = Xbox, 6 = Origin, 255 = unknown
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
        public byte[] Name;               // Name of participant in UTF-8 format – null terminated Will be truncated with … (U+2026) if too long
        // Will be truncated with ... (U+2026) if too long
        public byte CarNumber; // Car number of the player
        public byte YourTelemetry; // The player's UDP setting, 0 = restricted, 1 = public
        public byte ShowOnlineNames; // The player's show online names setting, 0 = off, 1 = on
        public ushort TechLevel; // F1 World tech level    
        public byte ReadyStatus; // 0 = not ready, 1 = ready, 2 = spectating
    }
}
