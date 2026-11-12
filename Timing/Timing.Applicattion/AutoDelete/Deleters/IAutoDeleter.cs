namespace SecondMonitor.Timing.Application.AutoDelete.Deleters
{
    using SecondMonitor.ViewModels.Settings.ViewModel;

    public interface IAutoDeleter
    {
        void CheckSettingsAndSetDefaults(SimsAutoDeleteSettingsViewModel autoDeleteSettingsViewModel);

        void PerformAutoDelete();
    }
}
