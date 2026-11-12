namespace SecondMonitor.ViewModels.Settings.Model
{
    public class DashboardSettings
    {
        public bool EngineDamageEnabled { get; set; } = true;
        public bool TransmissionDamageEnabled { get; set; } = true;
        public bool SuspensionDamageEnabled { get; set; } = true;
        public bool ClutchDamageEnabled { get; set; } = true;
        public bool BrakeDamageEnabled { get; set; } = true;
        public bool DrsEnabled { get; set; } = true;
        public bool TyreDirtEnabled { get; set; } = true;
        public bool PitLimiterEnabled { get; set; } = true;
        public bool ErsEnabled { get; set; } = true;
        public bool AlternatorEnabled { get; set; } = true;
        public bool LightsEnabled { get; set; } = true;
        public bool TcEnabled { get; set; } = true;
        public bool AbsEnabled { get; set; } = true;
        public int TcAndAbsTimeout { get; set; } = 8;
        public bool BodyWorkDamageEnabled { get; set; } = true;
    }
}