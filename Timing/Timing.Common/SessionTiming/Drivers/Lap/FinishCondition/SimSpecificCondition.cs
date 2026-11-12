namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap.FinishCondition
{
    using SecondMonitor.DataModel.Snapshot;

    public abstract class SimSpecificCondition : ILapFinishCondition
    {
        private readonly string _simulator;

        protected SimSpecificCondition(string simulator)
        {
            _simulator = simulator;
        }

        public bool IsLapFinished(SimulatorDataSet dataSet, ILapInfo lapInfo, DriverTiming driverTiming, out LapCompletionMethod lapCompletionMethod)
        {
            if (dataSet.Source != _simulator)
            {
                lapCompletionMethod = LapCompletionMethod.None;
                return false;
            }

            return InternalIsFinished(dataSet, lapInfo, driverTiming, out lapCompletionMethod);
        }

        protected abstract bool InternalIsFinished(SimulatorDataSet dataSet, ILapInfo lapInfo, DriverTiming driverTiming, out LapCompletionMethod lapCompletionMethod);
    }
}
