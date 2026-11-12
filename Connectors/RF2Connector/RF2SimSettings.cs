namespace SecondMonitor.RF2Connector
{
    using Contracts.SimSettings;

    public class Rf2SimSettings : ISimSettings
    {
        public bool IsEngineDamageProvided => true;
        public bool IsTransmissionDamageProvided => true;
        public bool IsSuspensionDamageProvided => true;
        public bool IsBodyworkDamageProvided => true;
        public bool IsTyresDirtProvided => false;
        public bool IsDrsInformationProvided => true;
        public bool IsBoostInformationProvided => false;
        public bool IsTurboBoostPressureProvided => true;
        public bool IsCoolantPressureProvided => false;
        public bool IsOilPressureProvided => false;
        public bool IsFuelPressureProvided => false;
        public bool IsBrakesDamageProvided => false;
        public bool IsAlternatorStatusShown => true;
        public bool IsClutchDamageProvided => false;

        public bool IsSessionLengthAvailableBeforeStart => false;
        public bool DetermineIdealPressureByTemperature => true;
    }
}