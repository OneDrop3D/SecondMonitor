namespace SecondMonitor.Rating.Common.Repository
{
    using DataModel;

    public interface IRatingRepository
    {
        Ratings SimulatorsRatings { get; }

        void Save(Ratings ratings);
    }
}