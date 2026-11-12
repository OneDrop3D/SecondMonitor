namespace SecondMonitor.DataModel.Summary
{
    using System;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;

    public class QuantityStatusSnapshot<T> where T : IQuantity
    {
        protected QuantityStatusSnapshot(SimulatorDataSet dataSet)
        {
            SessionTime = dataSet.SessionInfo.SessionTime;
            TotalDistance = dataSet.PlayerInfo.TotalDistance;
        }

        public T QuantityLevel { get; protected set; }
        public TimeSpan SessionTime { get; }
        public double TotalDistance { get; }
    }
}
