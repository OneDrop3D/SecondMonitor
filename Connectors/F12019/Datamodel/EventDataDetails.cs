#pragma warning disable SA1403
#pragma warning disable SA1201
#pragma warning disable SA1106
#pragma warning disable SA1400
#pragma warning disable SA1513
#pragma warning disable SA1306
#pragma warning disable SA1300
#pragma warning disable SA1310
#pragma warning disable SA1307
#pragma warning disable SA1120
#pragma warning disable SA1121
#pragma warning disable SA1002
#pragma warning disable SA1507
#pragma warning disable SA1505
#pragma warning disable SA1025
#pragma warning disable SX1309
#pragma warning disable RCS1013

namespace SecondMonitor.F12019Connector.Datamodel
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    internal struct EventDataDetails
    {
        [FieldOffset(0)]
        public Retirement retirement;
        [FieldOffset(0)]
        public TeamMateInPits teamMateInPits;
        [FieldOffset(0)]
        public RaceWinner raceWinner;
        [FieldOffset(0)]
        public FastestLap fastestLap;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct FastestLap
    {
        public byte vehicleIdx;
        public float lapTime;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct RaceWinner
    {
        public byte vehicleIdx;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct TeamMateInPits
    {
        public byte vehicleIdx;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Retirement
    {
        public byte vehicleIdx;
    }
}

#pragma warning restore SA1403
#pragma warning restore SA1201
#pragma warning restore SA1106
#pragma warning restore SA1400
#pragma warning restore SA1513
#pragma warning restore SA1306
#pragma warning restore SA1300
#pragma warning restore SA1310
#pragma warning restore SA1307
#pragma warning restore SA1120
#pragma warning restore SA1121
#pragma warning restore SA1002
#pragma warning restore SA1507
#pragma warning restore SA1505
#pragma warning restore SA1025
#pragma warning restore SX1309
#pragma warning restore RCS1013