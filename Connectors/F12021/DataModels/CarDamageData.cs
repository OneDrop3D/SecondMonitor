namespace SecondMonitor.F12021Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CarDamageData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] TyresWear; // Tyre wear (percentage)

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] TyresDamage; // Tyre damage (percentage)

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] BrakesDamage; // Brakes damage (percentage)

        public byte FrontLeftWingDamage; // Front left wing damage (percentage)
        public byte FrontRightWingDamage; // Front right wing damage (percentage)
        public byte RearWingDamage; // Rear wing damage (percentage)
        public byte FloorDamage; // Floor damage (percentage)
        public byte DiffuserDamage; // Diffuser damage (percentage)
        public byte SidePodDamage; // Sidepod damage (percentage)
        public byte DrsFault; // Indicator for DRS fault, 0 = OK, 1 = fault
        public byte GearBoxDamage; // Gear box damage (percentage)
        public byte EngineDamage; // Engine damage (percentage)
        public byte EngineMGUHWear; // Engine wear MGU-H (percentage)
        public byte EngineESWear; // Engine wear ES (percentage)
        public byte EngineCEWear; // Engine wear CE (percentage)
        public byte EngineICEWear; // Engine wear ICE (percentage)
        public byte EngineMGUKWear; // Engine wear MGU-K (percentage)
        public byte EngineTCWear; // Engine wear TC (percentage)
    }
}