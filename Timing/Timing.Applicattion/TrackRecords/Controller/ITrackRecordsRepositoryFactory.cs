namespace SecondMonitor.Timing.Application.TrackRecords.Controller
{
    public interface ITrackRecordsRepositoryFactory
    {
        TrackRecordsRepository Create();
    }
}