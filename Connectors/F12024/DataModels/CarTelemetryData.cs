namespace SecondMonitor.F12024Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct CarTelemetryData
    {
        public ushort Speed; // Speed of car in kilometres per hour

        public float Throttle; // Amount of throttle applied (0.0 to 1.0)

        public float Steer; // Steering (-1.0 (full lock left) to 1.0 (full lock right))

        public float Brake; // Amount of brake applied (0.0 to 1.0)

        public byte Clutch; // Amount of clutch applied (0 to 100)

        public sbyte Gear; // Gear selected (1-8, N=0, R=-1)

        public ushort EngineRpm; // Engine RPM

        public byte Drs; // 0 = off, 1 = on

        public byte RevLightsPercent; // Rev lights indicator (percentage)

        public ushort RevLightsBitValue; // // Rev lights (bit 0 = leftmost LED, bit 14 = rightmost LED)

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] BrakesTemperature; // Brakes temperature (celsius)

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] TyresSurfaceTemperature; // Tyres surface temperature (celsius)

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] TyresInnerTemperature; // Tyres inner temperature (celsius)

        public ushort EngineTemperature; // Engine temperature (celsius)

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] TyresPressure; // Tyres pressure (PSI)

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] SurfaceType; // Driving surface, see appendices
    }
}