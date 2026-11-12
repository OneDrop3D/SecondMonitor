namespace SecondMonitor.Timing.Application.PitBoard.DataProviders
{
    using System.Collections.Generic;
    using Common.SessionTiming.Drivers;
    using DataModel.Snapshot;
    using ViewModels.PitBoard.Controller;

    public abstract class AbstractPitBoardDataProvider : IAutonomousPitBoardDataProvider
    {
        protected IPitBoardController PitBoardController { get; private set; }

        protected bool IsStarted => PitBoardController != null;

        public virtual void StartDataProvider(IPitBoardController pitBoardController)
        {
            PitBoardController = pitBoardController;
        }

        public abstract void OnPlayerCompletedLap(SimulatorDataSet dataSet, IReadOnlyCollection<DriverTiming> driverTimingsModels);
        public abstract void Reset();
        public virtual void OnNextData(SimulatorDataSet dataSet)
        {
        }
    }
}