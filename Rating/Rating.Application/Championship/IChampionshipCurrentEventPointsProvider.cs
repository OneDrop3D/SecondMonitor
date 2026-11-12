namespace SecondMonitor.Rating.Application.Championship
{
    public interface IChampionshipCurrentEventPointsProvider
    {
        bool TryGetPointsAndPositionForDriver(string driverName, out int points, out int position);
    }
}