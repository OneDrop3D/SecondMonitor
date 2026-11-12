namespace SecondMonitor.Rating.Application.Championship.Filters
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Common.DataModel.Championship;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using NLog;

    public class OpponentsRequirements : IChampionshipCondition
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public string GetDescription(ChampionshipDto championshipDto)
        {
            if (championshipDto.ChampionshipState == ChampionshipState.NotStarted)
            {
                return championshipDto.AiNamesCanChange ? "Use the opponents count, you would like to run the whole championship with. Their names do not matter." : "Use opponents, you would like to run the championship with. Their names need to match for all races";
            }

            return championshipDto.AiNamesCanChange ? $"Requires {championshipDto.TotalDrivers - 1} opponents. Opponents names can change from previous event, but their names need to be unique." : $"Requires {championshipDto.TotalDrivers - 1} opponents. Opponents names has to be the same, as in previous events. Check championship details if you need the list of all drivers.";
        }

        public ConditionResult Evaluate(ChampionshipDto championshipDto, SimulatorDataSet dataSet)
        {
            List<string> filteredDrivers = dataSet.GetDriversInfoByMultiClass().Select(x => x.DriverLongName).ToList();
            StringBuilder notes;
            if (filteredDrivers.Count != filteredDrivers.Distinct().Count())
            {
                var driversGroups = dataSet.DriversInfo.GroupBy(x => x.DriverLongName);
                notes = new StringBuilder("Not all drivers have unique names: \n");
                foreach (IGrouping<string, DriverInfo> driversGroup in driversGroups.Where(x => x.Count() > 1))
                {
                    notes.Append($"Driver: {driversGroup.Key}, is present {driversGroup.Count()} times.\n");
                }

                Logger.Info(notes.ToString);
                return new ConditionResult(RequirementResultKind.DoesNotMatch, notes.ToString());
            }

            if (dataSet.DriversInfo.Length < 2)
            {
                Logger.Info($"Not enough drivers {dataSet.DriversInfo.Length}");
                return new ConditionResult(RequirementResultKind.DoesNotMatch, "Not enough drivers in session");
            }

            if (championshipDto.ChampionshipState == ChampionshipState.NotStarted)
            {
                return new ConditionResult(RequirementResultKind.CanMatch);
            }

            if (championshipDto.AiNamesCanChange)
            {
                Logger.Info($"Required count {championshipDto.Drivers.Count}, drivers count {filteredDrivers.Count + 1}.");
                return championshipDto.Drivers.Count == filteredDrivers.Count ? new ConditionResult(RequirementResultKind.PerfectMatch) : new ConditionResult(RequirementResultKind.CanMatch, $"Driver count in session and championship differ, drivers in Session : {filteredDrivers.Count}, Drivers in Championship: {championshipDto.TotalDrivers}");
            }

            List<string> missingDrivers = filteredDrivers.Where(x => championshipDto.Drivers.All(y => y.LastUsedName != x)).ToList();

            if (missingDrivers.Count == 0)
            {
                return new ConditionResult(RequirementResultKind.PerfectMatch);
            }

            notes = new StringBuilder("Missing Drivers: \n");
            missingDrivers.ForEach(x => notes.AppendLine(x));
            return new ConditionResult(RequirementResultKind.DoesNotMatch, notes.ToString());
        }
    }
}