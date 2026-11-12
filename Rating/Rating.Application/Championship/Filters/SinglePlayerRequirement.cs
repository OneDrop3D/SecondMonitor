namespace SecondMonitor.Rating.Application.Championship.Filters
{
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    public class SinglePlayerRequirement : IChampionshipCondition
    {
        private readonly DisplaySettingsViewModel _displaySettingsViewModel;
        public SinglePlayerRequirement(ISettingsProvider settingsProvider)
        {
            _displaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
        }

        public string GetDescription(ChampionshipDto championshipDto)
        {
            return string.Empty;
        }

        public ConditionResult Evaluate(ChampionshipDto championshipDto, SimulatorDataSet dataSet)
        {
            return _displaySettingsViewModel.ChampionshipSettingViewModel.IsMultiplayerEnabled || !dataSet.SessionInfo.IsMultiplayer ? new ConditionResult(RequirementResultKind.PerfectMatch)
                : new ConditionResult(RequirementResultKind.DoesNotMatch, "Needs to be a SP Session");
        }
    }
}
