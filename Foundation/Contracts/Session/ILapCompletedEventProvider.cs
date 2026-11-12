namespace SecondMonitor.Contracts.Session
{
    using System;

    public interface ILapCompletedEventProvider
    {
        event EventHandler<LapSummaryArgs> LapCompletedSummary;
    }
}
