namespace SecondMonitor.F12024Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TyreSetData
    {
        public byte ActualTyreCompound; // Actual tyre compound used
        public byte VisualTyreCompound; // Visual tyre compound used
        public byte Wear; // Tyre wear (percentage)
        public byte Available; // Whether this set is currently available
        public byte RecommendedSession; // Recommended session for tyre set, see appendix
        public byte LifeSpan; // Laps left in this tyre set
        public byte UsableLife; // Max number of laps recommended for this compound
        public ushort LapDeltaTime; // Lap delta time in milliseconds compared to fitted set
        public byte Fitted; // Whether the set is fitted or not
    }
}
