namespace SecondMonitor.Timing.Application.SessionTiming.Drivers.Presentation.ViewModel.PitStops
{
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.ViewModels.Settings;

    public class PitStopInfoViewModelFactory
    {
        private readonly ISettingsProvider _settingsProvider;

        public PitStopInfoViewModelFactory(ISettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider;
        }

        public IPitStopInfoViewModel Create(SimulatorDataSet dataSet)
        {
            return dataSet.SessionInfo.PitWindow.PitWindowState != PitWindowState.None ? new PitWindowPitViewModel(_settingsProvider) : CreateDefault();
        }

        public IPitStopInfoViewModel CreateDefault()
        {
            return new NoPitWindowPitViewModel(_settingsProvider);
        }
    }
}