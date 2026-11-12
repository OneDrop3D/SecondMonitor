namespace SecondMonitor.Rating.Application.Championship.Filters
{
    using Common.DataModel.Championship;
    using DataModel.Snapshot;

    public class SimulatorRequirement : IChampionshipCondition
    {
        public string GetDescription(ChampionshipDto championshipDto)
        {
            return $"Simulator has to be {championshipDto.SimulatorName}";
        }

        public ConditionResult Evaluate(ChampionshipDto championshipDto, SimulatorDataSet dataSet)
        {
            return championshipDto.SimulatorName == dataSet.Source ? new ConditionResult(RequirementResultKind.PerfectMatch) : new ConditionResult(RequirementResultKind.DoesNotMatch, $"Current simulator is {dataSet.Source}");
        }
    }
}