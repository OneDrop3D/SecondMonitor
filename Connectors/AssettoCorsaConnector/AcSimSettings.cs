namespace SecondMonitor.AssettoCorsaConnector
{
    using Contracts.SimSettings;

    public class AcSimSettings : ISimSettings
    {
        public bool IsEngineDamageProvided => true;
        public bool IsTransmissionDamageProvided => false;
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
        public bool IsAlternatorStatusShown => true;
        public bool IsSessionLengthAvailableBeforeStart => false;
        public bool DetermineIdealPressureByTemperature => false;
        public bool IsClutchDamageProvided => false;
    }
}