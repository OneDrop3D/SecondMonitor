namespace SecondMonitor.Rating.Application.Championship.Filters
{
    using Common.DataModel.Championship;
    using DataModel.Snapshot;

    public interface IChampionshipCondition
    {
        string GetDescription(ChampionshipDto championshipDto);

        ConditionResult Evaluate(ChampionshipDto championshipDto, SimulatorDataSet dataSet);
    }
}