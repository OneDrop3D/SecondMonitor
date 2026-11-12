namespace SecondMonitor.RFactorConnector
{
    using Contracts.SimSettings;

    public class RFactorSimSettings : ISimSettings
    {
        public bool IsEngineDamageProvided => true;
        public bool IsTransmissionDamageProvided => true;
        public bool IsSuspensionDamageProvided => true;
        public bool IsBodyworkDamageProvided => true;
        public bool IsTyresDirtProvided => false;
        public bool IsDrsInformationProvided => false;
        public bool IsBoostInformationProvided => false;
        public bool IsTurboBoostPressureProvided => false;
        public bool IsCoolantPressureProvided => false;
        public bool IsOilPressureProvided => false;
        public bool IsFuelPressureProvided => false;
        public bool IsBrakesDamageProvided => false;
        public bool IsAlternatorStatusShown => true;
        public bool IsClutchDamageProvided => false;

        public bool IsSessionLengthAvailableBeforeStart => true;
        public bool DetermineIdealPressureByTemperature => true;
    }
}