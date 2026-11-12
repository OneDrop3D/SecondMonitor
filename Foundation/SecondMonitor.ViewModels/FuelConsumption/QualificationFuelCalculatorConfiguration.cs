namespace SecondMonitor.ViewModels.FuelConsumption
{
    using DataModel.BasicProperties;
    using SessionEvents;
    using Settings;
    using Settings.ViewModel;

    public class QualificationFuelCalculatorConfiguration : NonRaceFuelCalculatorConfiguration
    {
        public QualificationFuelCalculatorConfiguration(ISettingsProvider settingsProvider, ISessionEventProvider sessionEventProvider) : base(settingsProvider, sessionEventProvider)
        {
        }

        public override SessionType SessionType => SessionType.Qualification;
        protected override SessionFuelCalculationSettingsViewModel GetSessionFuelCalculationSettingsViewModel()
        {
            return DisplaySettingsViewModel.QualificationFuelSettings;
        }
    }
}