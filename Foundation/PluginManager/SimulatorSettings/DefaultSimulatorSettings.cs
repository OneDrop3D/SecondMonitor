namespace SecondMonitor.PluginManager.SimulatorSettings
{
    using Contracts.SimSettings;

    public class DefaultSimulatorSettings : ISimSettings
    {
        public bool IsEngineDamageProvided => true;
        public bool IsTransmissionDamageProvided => true;
        public bool IsSuspensionDamageProvided => true;
        public bool IsBodyworkDamageProvided => true;
        public bool IsTyresDirtProvided => true;
        public bool IsDrsInformationProvided => true;
        public bool IsBoostInformationProvided => true;
        public bool IsTurboBoostPressureProvided => true;
        public bool IsCoolantPressureProvided => true;
        public bool IsOilPressureProvided => true;
        public bool IsFuelPressureProvided => true;
        public bool IsBrakesDamageProvided => true;
        public bool IsClutchDamageProvided => false;
        public bool IsAlternatorStatusShown => true;
        public bool IsSessionLengthAvailableBeforeStart => false;
        public bool DetermineIdealPressureByTemperature => true;
    }
}