namespace SecondMonitor.F12020Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct CarStatusData
    {
        public byte TractionControl; // 0 (off) - 2 (high)

        public byte AntiLockBrakes; // 0 (off) - 1 (on)

        public byte FuelMix; // Fuel mix - 0 = lean, 1 = standard, 2 = rich, 3 = max

        public byte FrontBrakeBias; // Front brake bias (percentage)

        public byte PitLimiterStatus; // Pit limiter status - 0 = off, 1 = on

        public float FuelInTank; // Current fuel mass

        public float FuelCapacity; // Fuel capacity

        public float FuelRemainingLaps; // Fuel remaining in terms of laps (value on MFD)

        public ushort MaxRpm; // Cars max RPM, point of rev limiter

        public ushort IdleRpm; // Cars idle RPM

        public byte MaxGears; // Maximum number of gears

        public byte DrsAllowed; // 0 = not allowed, 1 = allowed, -1 = unknown

        public ushort DrsActivationDistance;    // 0 = DRS not available, non-zero - DRS will be available in [X] metres

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] TyresWear; // Tyre wear percentage

        public byte ActualTyreCompound; // F1 Modern - 16 = C5, 17 = C4, 18 = C3, 19 = C2, 20 = C1

        // 7 = inter, 8 = wet
        // F1 Classic - 9 = dry, 10 = wet
        // F2 – 11 = super soft, 12 = soft, 13 = medium, 14 = hard
        // 15 = wet
        public byte TyreVisualCompound; // F1 visual (can be different from actual compound)  16 = soft, 17 = medium, 18 = hard, 7 = inter, 8 = wet  F1 Classic – same as above// F2 – same as above

        public byte TyresAgeLaps;      // Age in laps of the current set of tyres

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] TyresDamage; // Tyre damage (percentage)

        public byte FrontLeftWingDamage; // Front left wing damage (percentage)

        public byte FrontRightWingDamage; // Front right wing damage (percentage)

        public byte RearWingDamage; // Rear wing damage (percentage)

        public byte DrsFault; // Indicator for DRS fault, 0 = OK, 1 = fault

        public byte EngineDamage; // Engine damage (percentage)

        public byte GearBoxDamage; // Gear box damage (percentage)

        public sbyte VehicleFiaFlags; // -1 = invalid/unknown, 0 = none, 1 = green,  2 = blue, 3 = yellow, 4 = red

        public float ErsStoreEnergy; // ERS energy store in Joules

        public byte ErsDeployMode; // ERS deployment mode, 0 = none, 1 = low, 2 = medium, 3 = high, 4 = overtake, 5 = hotlap

        public float ErsHarvestedThisLapMguk; // ERS energy harvested this lap by MGU-K

        public float ErsHarvestedThisLapMguh; // ERS energy harvested this lap by MGU-H

        public float ErsDeployedThisLap; // ERS energy deployed this lap
    }
}