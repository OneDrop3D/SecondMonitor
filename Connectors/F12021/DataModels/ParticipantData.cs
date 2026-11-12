namespace SecondMonitor.F12021Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct ParticipantData
    {
        public byte AiControlled;           // Whether the vehicle is AI (1) or Human (0) controlled

        public byte DriverId;       // Driver id - see appendix

        public byte NetworkId;       // Network id – unique identifier for network players

        public byte TeamId;                 // Team id - see appendix

        public byte MyTeam;                 // My team flag – 1 = My Team, 0 = otherwise

        public byte RaceNumber;             // Race number of the car

        public byte Nationality;            // Nationality of the driver

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
        public byte[] Name;               // Name of participant in UTF-8 format – null terminated Will be truncated with … (U+2026) if too long

        public byte YourTelemetry;          // The player's UDP setting, 0 = restricted, 1 = public
    }
}