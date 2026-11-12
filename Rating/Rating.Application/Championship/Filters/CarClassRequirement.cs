namespace SecondMonitor.Rating.Application.Championship.Filters
{
    using Common.DataModel.Championship;
    using DataModel.Snapshot;

    public class CarClassRequirement : IChampionshipCondition
    {
        public string GetDescription(ChampionshipDto championshipDto)
        {
            if (championshipDto.ChampionshipState == ChampionshipState.NotStarted && !championshipDto.IsClassChangeEnabled)
            {
                return "Use class you would like to run the whole championship with.";
            }

            if (!championshipDto.IsMysteryClass && !championshipDto.IsClassChangeEnabled)
            {
                return $"Use one of the cars from class: {championshipDto.ClassName} ";
            }

            return string.IsNullOrEmpty(championshipDto.ClassName) ? "Use one of the cars from any class you want." : $"Use one of the cars from class {championshipDto.ClassName}, or any other class you want.";
        }

        public ConditionResult Evaluate(ChampionshipDto championshipDto, SimulatorDataSet dataSet)
        {
            if (string.IsNullOrWhiteSpace(dataSet.PlayerInfo.CarClassName))
            {
                return new ConditionResult(RequirementResultKind.DoesNotMatch);
            }

            if (championshipDto.ChampionshipState == ChampionshipState.NotStarted)
            {
                return new ConditionResult(RequirementResultKind.CanMatch);
            }

            if (dataSet.PlayerInfo.CarClassName != championshipDto.ClassName && championshipDto.IsClassChangeEnabled)
            {
                return new ConditionResult(RequirementResultKind.CanMatch);
            }

            return dataSet.PlayerInfo.CarClassName == championshipDto.ClassName ? new ConditionResult(RequirementResultKind.PerfectMatch) : new ConditionResult(RequirementResultKind.DoesNotMatch, $"Current class is {dataSet.PlayerInfo.CarClassName}");
        }
    }
}