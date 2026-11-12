namespace SecondMonitor.ViewModels.FuelConsumption
{
    using DataModel.BasicProperties;
    using SessionEvents;
    using Settings;
    using Settings.ViewModel;

    public class PracticeFuelCalculatorConfiguration : NonRaceFuelCalculatorConfiguration
    {
        public PracticeFuelCalculatorConfiguration(ISettingsProvider settingsProvider, ISessionEventProvider sessionEventProvider) : base(settingsProvider, sessionEventProvider)
        {
        }

        public override SessionType SessionType => SessionType.Practice;
        protected override SessionFuelCalculationSettingsViewModel GetSessionFuelCalculationSettingsViewModel()
        {
            return DisplaySettingsViewModel.PracticeFuelSettings;
        }
    }
}