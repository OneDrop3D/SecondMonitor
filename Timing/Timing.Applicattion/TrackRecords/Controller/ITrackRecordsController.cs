namespace SecondMonitor.Timing.Application.TrackRecords.Controller
{
    using Common.SessionTiming.Drivers.Lap;
    using DataModel.Snapshot;

    using ViewModels.Controllers;
    using ViewModels.TrackRecords;

    public interface ITrackRecordsController : IController
    {
        ITrackRecordsViewModel TrackRecordsViewModel { get; }

        void OnSessionStarted(SimulatorDataSet dataSet);

        bool EvaluateFastestLapCandidate(ILapInfo lapInfo);
    }
}