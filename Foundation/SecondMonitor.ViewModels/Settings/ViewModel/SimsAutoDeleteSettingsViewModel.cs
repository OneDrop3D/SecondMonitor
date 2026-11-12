namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using System.Collections.Generic;

    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Settings.Model.Layout;

    public class SimsAutoDeleteSettingsViewModel : AbstractViewModel<SimsAutoDeleteSettings>
    {
        public SimsAutoDeleteSettingsViewModel(IViewModelFactory viewModelFactory)
        {
            R3EReplayAutoDeleteSettings = viewModelFactory.Set("title").To("R3E Replays").Create<AutoDeleteSettingsViewModel>();
            AccSaveGameAutoDeleteSettings = viewModelFactory.Set("title").To("ACC Save Games").Create<AutoDeleteSettingsViewModel>();
            LmUReplayAutoDeleteSettings = viewModelFactory.Set("title").To("LmU Replays").Create<AutoDeleteSettingsViewModel>();

            AllAutoDeleteSettings = new List<AutoDeleteSettingsViewModel>()
            {
                AccSaveGameAutoDeleteSettings,
                R3EReplayAutoDeleteSettings,
                LmUReplayAutoDeleteSettings
            };
        }

        public List<AutoDeleteSettingsViewModel> AllAutoDeleteSettings { get; }

        public AutoDeleteSettingsViewModel R3EReplayAutoDeleteSettings { get; }

        public AutoDeleteSettingsViewModel LmUReplayAutoDeleteSettings { get; }

        public AutoDeleteSettingsViewModel AccSaveGameAutoDeleteSettings { get; }

        protected override void ApplyModel(SimsAutoDeleteSettings model)
        {
            R3EReplayAutoDeleteSettings.FromModel(model.R3EReplayAutoDeleteSettings);
            AccSaveGameAutoDeleteSettings.FromModel(model.AccSaveGameAutoDeleteSettings);
            LmUReplayAutoDeleteSettings.FromModel(model.LmUReplayAutoDeleteSettings);
        }

        public override SimsAutoDeleteSettings SaveToNewModel()
        {
            return new SimsAutoDeleteSettings()
            {
                AccSaveGameAutoDeleteSettings = AccSaveGameAutoDeleteSettings.SaveToNewModel(),
                R3EReplayAutoDeleteSettings = R3EReplayAutoDeleteSettings.SaveToNewModel(),
                LmUReplayAutoDeleteSettings = LmUReplayAutoDeleteSettings.SaveToNewModel(),
            };
        }
    }
}
