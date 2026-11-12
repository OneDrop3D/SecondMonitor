namespace SecondMonitor.Timing.Application.PitBoard.DataProviders
{
    using System.Collections.Generic;
    using Common.SessionTiming.Drivers;
    using DataModel.Snapshot;
    using ViewModels.PitBoard.Controller;

    public interface IAutonomousPitBoardDataProvider
    {
        void StartDataProvider(IPitBoardController pitBoardController);

        void OnPlayerCompletedLap(SimulatorDataSet dataSet, IReadOnlyCollection<DriverTiming> driverTimingsModels);
        void Reset();
        void OnNextData(SimulatorDataSet dataSet);
    }
}