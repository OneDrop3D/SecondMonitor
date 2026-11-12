namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using SecondMonitor.ViewModels.Settings.Model;

    public class RaceSessionFuelCalculationSettingsViewModel : AbstractViewModel<RaceSessionFuelCalculationSettings>
    {
        private int _extraLaps;
        private int _extraMinutes;

        public RaceSessionFuelCalculationSettingsViewModel()
        {
        }

        public int ExtraLaps
        {
            get => _extraLaps;
            set => SetProperty(ref _extraLaps, value);
        }

        public int ExtraMinutes
        {
            get => _extraMinutes;
            set => SetProperty(ref _extraMinutes, value);
        }

        protected override void ApplyModel(RaceSessionFuelCalculationSettings model)
        {
            ExtraLaps = model.ExtraLaps;
            ExtraMinutes = model.ExtraMinutes;
        }

        public override RaceSessionFuelCalculationSettings SaveToNewModel()
        {
            return new RaceSessionFuelCalculationSettings(ExtraLaps, ExtraMinutes);
        }
    }
}