namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using System;
    using Model;

    [Serializable]
    public class DashboardSettingsViewModel : AbstractViewModel<DashboardSettings>
    {
        private bool _engineDamageEnabled;
        private bool _transmissionDamageEnabled;
        private bool _suspensionDamageEnabled;
        private bool _clutchDamageEnabled;
        private bool _bodyWorkDamageEnabled;
        private bool _brakeDamageEnabled;
        private bool _drsEnabled;
        private bool _tyreDirtEnabled;
        private bool _pitLimiterEnabled;
        private bool _ersEnabled;
        private bool _alternatorEnabled;
        private bool _lightsEnabled;
        private bool _tcEnabled;
        private bool _absEnabled;
        private int _tcAndAbsTimeout;

        public bool EngineDamageEnabled
        {
            get => _engineDamageEnabled;
            set => SetProperty(ref _engineDamageEnabled, value);
        }

        public bool TransmissionDamageEnabled
        {
            get => _transmissionDamageEnabled;
            set => SetProperty(ref _transmissionDamageEnabled, value);
        }

        public bool SuspensionDamageEnabled
        {
            get => _suspensionDamageEnabled;
            set => SetProperty(ref _suspensionDamageEnabled, value);
        }

        public bool ClutchDamageEnabled
        {
            get => _clutchDamageEnabled;
            set => SetProperty(ref _clutchDamageEnabled, value);
        }

        public bool BrakeDamageEnabled
        {
            get => _brakeDamageEnabled;
            set => SetProperty(ref _brakeDamageEnabled, value);
        }

        public bool DrsEnabled
        {
            get => _drsEnabled;
            set => SetProperty(ref _drsEnabled, value);
        }

        public bool TyreDirtEnabled
        {
            get => _tyreDirtEnabled;
            set => SetProperty(ref _tyreDirtEnabled, value);
        }

        public bool PitLimiterEnabled
        {
            get => _pitLimiterEnabled;
            set => SetProperty(ref _pitLimiterEnabled, value);
        }

        public bool ErsEnabled
        {
            get => _ersEnabled;
            set => SetProperty(ref _ersEnabled, value);
        }

        public bool AlternatorEnabled
        {
            get => _alternatorEnabled;
            set => SetProperty(ref _alternatorEnabled, value);
        }

        public bool LightsEnabled
        {
            get => _lightsEnabled;
            set => SetProperty(ref _lightsEnabled, value);
        }

        public bool TcEnabled
        {
            get => _tcEnabled;
            set => SetProperty(ref _tcEnabled, value);
        }

        public bool AbsEnabled
        {
            get => _absEnabled;
            set => SetProperty(ref _absEnabled, value);
        }

        public bool BodyWorkDamageEnabled
        {
            get => _bodyWorkDamageEnabled;
            set => SetProperty(ref _bodyWorkDamageEnabled, value);
        }

        public int TcAndAbsTimeout
        {
            get => _tcAndAbsTimeout;
            set => SetProperty(ref _tcAndAbsTimeout, value);
        }

        protected override void ApplyModel(DashboardSettings model)
        {
            EngineDamageEnabled = model.EngineDamageEnabled;
            TransmissionDamageEnabled = model.TransmissionDamageEnabled;
            SuspensionDamageEnabled = model.SuspensionDamageEnabled;
            ClutchDamageEnabled = model.ClutchDamageEnabled;
            BrakeDamageEnabled = model.BrakeDamageEnabled;
            DrsEnabled = model.DrsEnabled;
            TyreDirtEnabled = model.TyreDirtEnabled;
            PitLimiterEnabled = model.PitLimiterEnabled;
            ErsEnabled = model.ErsEnabled;
            AlternatorEnabled = model.AlternatorEnabled;
            LightsEnabled = model.LightsEnabled;
            TcEnabled = model.TcEnabled;
            AbsEnabled = model.AbsEnabled;
            BodyWorkDamageEnabled = model.BodyWorkDamageEnabled;
            TcAndAbsTimeout = model.TcAndAbsTimeout;
        }

        public override DashboardSettings SaveToNewModel()
        {
            return new DashboardSettings()
            {
                EngineDamageEnabled = EngineDamageEnabled,
                BodyWorkDamageEnabled = BodyWorkDamageEnabled,
                TransmissionDamageEnabled = TransmissionDamageEnabled,
                SuspensionDamageEnabled = SuspensionDamageEnabled,
                ClutchDamageEnabled = ClutchDamageEnabled,
                BrakeDamageEnabled = BrakeDamageEnabled,
                DrsEnabled = DrsEnabled,
                TyreDirtEnabled = TyreDirtEnabled,
                PitLimiterEnabled = PitLimiterEnabled,
                ErsEnabled = ErsEnabled,
                AlternatorEnabled = AlternatorEnabled,
                LightsEnabled = LightsEnabled,
                TcEnabled = TcEnabled,
                AbsEnabled = AbsEnabled,
                TcAndAbsTimeout = TcAndAbsTimeout,
            };
        }
    }
}
