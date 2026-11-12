namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap.FinishCondition
{
    using SecondMonitor.DataModel.Snapshot;

    public interface ILapFinishCondition
    {
        bool IsLapFinished(SimulatorDataSet dataSet, ILapInfo lapInfo, DriverTiming driverTiming, out LapCompletionMethod lapCompletionMethod);
    }
}
