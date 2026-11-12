namespace SecondMonitor.DataModel.Summary
{
    using System;
    using BasicProperties;

    public class PitStopSummary
    {
        public int EntryLapNumber { get; set; }

        public int ExitLapNumber { get; set; }

        public TimeSpan OverallDuration { get; set; }

        public TimeSpan StallDuration { get; set; }

        public bool WasDriverThrough { get; set; }

        public Volume FuelTaken { get; set; }

        public Volume AfterStopFuel { get; set; }

        public string NewFrontCompound { get; set; }

        public string NewRearCompound { get; set; }

        public bool IsFrontTyresChanged { get; set; }

        public bool IsRearTyresChanged { get; set; }

        public TimeSpan TotalRaceTimeLost { get; set; }

        public Percentage VirtualEnergyTaken { get; set; }
        
        public Percentage AfterStopVirtualEnergy { get; set; }
    }
}