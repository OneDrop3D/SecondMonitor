namespace SecondMonitor.Rating.Application.Rating.RatingProvider
{
    using Common.DataModel.Player;

    public interface ISessionRatingProvider
    {
        bool TryGetRatingForDriverCurrentSession(string driverId, out DriversRating driversRating);
    }
}