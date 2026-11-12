namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap.FinishCondition
{
    using SecondMonitor.DataModel;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;

    public class Ams2TimeTrialFirstLapCondition : SimSpecificCondition
    {
        public Ams2TimeTrialFirstLapCondition() : base(SimulatorsNameMap.Ams2SimName)
        {
        }

        protected override bool InternalIsFinished(SimulatorDataSet dataSet, ILapInfo lapInfo, DriverTiming driverTiming,  out LapCompletionMethod lapCompletionMethod)
        {
            // AMS 2, crossing line at the first lap at time trial
            if (dataSet.SessionInfo.SessionType == SessionType.Practice && dataSet.SessionInfo.SessionLengthType == SessionLengthType.Na &&
                driverTiming.DriverInfo.LapDistance > 0 && driverTiming.PreviousLapDistance == 0 &&
                lapInfo.Driver.CurrentLap.LapInvalidationReasonKind == LapInvalidationReasonKind.InvalidatedFirstLap)
            {
                lapCompletionMethod = LapCompletionMethod.ByCrossingTheLine;
                return true;
            }

            lapCompletionMethod = LapCompletionMethod.None;
            return false;
        }
    }
}
