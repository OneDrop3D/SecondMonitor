namespace SecondMonitor.F12024Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketTyreSetData
    {
        public PacketHeader Header; // Header

        public byte CarIdx; //carnIndex

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public TyreSetData[] TyreSet; // Lap data for all cars on track

        public byte FittedIdx; // Index into array of fitted tyre
    }
}
