namespace SecondMonitor.Rating.Application.Rating.Controller.RaceObserver.States
{
    using System.Threading.Tasks;
    using Common.DataModel.Player;
    using Context;
    using DataModel.Snapshot;
    using DataModel.Summary;

    public interface IRaceState
    {
        SessionKind SessionKind { get; }
        SessionPhaseKind SessionPhaseKind { get; }
        IRaceState NextState { get; }
        string SessionDescription { get; }
        SharedContext SharedContext { get; }
        bool ShowRatingChange { get; }

        bool CanUserSelectClass { get; }
        Task DoSessionCompletion(SessionSummary sessionSummary);
        Task DoDataLoaded(SimulatorDataSet simulatorDataSet);
        bool TryGetDriverRating(string driverName, out DriversRating driversRating);
    }
}