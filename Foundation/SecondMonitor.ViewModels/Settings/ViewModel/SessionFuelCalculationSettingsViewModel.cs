namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using Model;

    public class SessionFuelCalculationSettingsViewModel : AbstractViewModel<SessionFuelCalculationSettings>
    {
        private bool _autoOpenOnSessionStart;
        private bool _autoOpenWhenInPits;
        private int _laps;
        private int _minutes;

        public bool AutoOpenOnSessionStart
        {
            get => _autoOpenOnSessionStart;
            set => SetProperty(ref _autoOpenOnSessionStart, value);
        }

        public bool AutoOpenWhenInPits
        {
            get => _autoOpenWhenInPits;
            set => SetProperty(ref _autoOpenWhenInPits, value);
        }

        public int Laps
        {
            get => _laps;
            set => SetProperty(ref _laps, value);
        }

        public int Minutes
        {
            get => _minutes;
            set => SetProperty(ref _minutes, value);
        }

        protected override void ApplyModel(SessionFuelCalculationSettings model)
        {
            AutoOpenOnSessionStart = model.AutoOpenOnSessionStart;
            AutoOpenWhenInPits = model.AutoOpenWhenInPits;
            Laps = model.Laps;
            Minutes = model.Minutes;
        }

        public override SessionFuelCalculationSettings SaveToNewModel()
        {
            return new SessionFuelCalculationSettings(AutoOpenOnSessionStart, AutoOpenWhenInPits, Laps, Minutes);
        }
    }
}