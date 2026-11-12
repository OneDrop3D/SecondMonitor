namespace SecondMonitor.Contracts.Session
{
    using System;

    [Flags]
    public enum PitPredictionReason
    {
        None = 0,
        AlwaysEnabled = 1,
        PitRequested = 2,
        PitWindow = 4,
        TyreStatus = 8,
        FuelLevel = 16,
        StintEnding = 32,
    }
}