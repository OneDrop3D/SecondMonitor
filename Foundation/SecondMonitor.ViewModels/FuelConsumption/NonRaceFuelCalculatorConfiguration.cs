namespace SecondMonitor.ViewModels.FuelConsumption
{
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Summary.FuelConsumption;
    using SessionEvents;
    using Settings;
    using Settings.ViewModel;

    public abstract class NonRaceFuelCalculatorConfiguration : ISessionFuelCalculatorConfiguration
    {
        private bool _sessionStartCheckPerformed;
        private bool _isAutoOpened;

        protected NonRaceFuelCalculatorConfiguration(ISettingsProvider settingsProvider, ISessionEventProvider sessionEventProvider)
        {
            DisplaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
        }

        public abstract SessionType SessionType { get; }

        protected DisplaySettingsViewModel DisplaySettingsViewModel { get; }

        public (int Laps, int Minutes, int ExtraFuelLiters) GetRequiredRunTime(SimulatorDataSet dataSet, OverallFuelConsumptionHistory fuelConsumptionHistory)
        {
            SessionFuelCalculationSettingsViewModel currentSessionSettings = GetSessionFuelCalculationSettingsViewModel();
            return (currentSessionSettings.Laps, currentSessionSettings.Minutes, 0);
        }

        public bool ShouldAutoOpen(SimulatorDataSet dataSet)
        {
            if (_isAutoOpened)
            {
                return false;
            }

            SessionFuelCalculationSettingsViewModel currentSessionSettings = GetSessionFuelCalculationSettingsViewModel();

            if (_sessionStartCheckPerformed || dataSet.SessionInfo.SessionType != SessionType)
            {
                return false;
            }

            _sessionStartCheckPerformed = true;
            _isAutoOpened = currentSessionSettings.AutoOpenOnSessionStart;
            return _isAutoOpened;
        }

        public bool ShouldAutoClose(SimulatorDataSet dataSet)
        {
            if (!_isAutoOpened || string.IsNullOrWhiteSpace(dataSet?.PlayerInfo?.CarName))
            {
                return false;
            }

            return !dataSet.PlayerInfo.InPits || dataSet.PlayerInfo.Speed.InKph > 30;
        }

        public void OnUserClosed()
        {
        }

        public void OnAutoClose()
        {
            _isAutoOpened = false;
        }

        public void Reset()
        {
            _isAutoOpened = false;
            _sessionStartCheckPerformed = false;
        }

        protected abstract SessionFuelCalculationSettingsViewModel GetSessionFuelCalculationSettingsViewModel();
    }
}