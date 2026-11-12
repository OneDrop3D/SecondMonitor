namespace SecondMonitor.Rating.Application.Championship.Filters
{
    using System.Linq;

    using Common.DataModel.Championship;
    using DataModel.Snapshot;

    public class PlayerPresentRequirement : IChampionshipCondition
    {
        public string GetDescription(ChampionshipDto championshipDto)
        {
            return string.Empty;
        }

        public ConditionResult Evaluate(ChampionshipDto championshipDto, SimulatorDataSet dataSet)
        {
            return string.IsNullOrWhiteSpace(dataSet.PlayerInfo?.DriverSessionId) || !dataSet.DriversInfo.Any(x => x.IsPlayer) ? new ConditionResult(RequirementResultKind.DoesNotMatch, "No Driver In Session")
                : new ConditionResult(RequirementResultKind.PerfectMatch);
        }
    }
}