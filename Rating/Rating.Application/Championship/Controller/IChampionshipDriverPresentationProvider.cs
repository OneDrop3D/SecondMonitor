namespace SecondMonitor.Rating.Application.Championship.Controller
{
    using SecondMonitor.Rating.Common.DataModel.Championship;

    public interface IChampionshipDriverPresentationProvider
    {
        public void UpdateDriversOutLines(ChampionshipDto championshipDto);
    }
}
