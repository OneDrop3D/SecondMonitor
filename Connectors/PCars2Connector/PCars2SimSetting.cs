namespace SecondMonitor.PCars2Connector
{
    using Contracts.SimSettings;

    public class PCars2SimSetting : ISimSettings
    {
        public bool IsEngineDamageProvided => true;
        public bool IsTransmissionDamageProvided => true;
        public bool IsSuspensionDamageProvided => true;
        public bool IsBodyworkDamageProvided => true;
        public bool IsTyresDirtProvided => false;
        public bool IsDrsInformationProvided => true;
        public bool IsBoostInformationProvided => true;
        public bool IsTurboBoostPressureProvided => true;
        public bool IsCoolantPressureProvided => true;
        public bool IsOilPressureProvided => true;
        public bool IsFuelPressureProvided => false;
        public bool IsBrakesDamageProvided => true;
        public bool IsClutchDamageProvided => false;
        public bool IsAlternatorStatusShown => true;
        public bool IsSessionLengthAvailableBeforeStart => true;
        public bool DetermineIdealPressureByTemperature => true;
    }
}