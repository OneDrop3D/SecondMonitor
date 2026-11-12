namespace SecondMonitor.R3EConnector
{
    using Contracts.SimSettings;

    public class R3ESimSettings : ISimSettings
    {
        public bool IsEngineDamageProvided => true;
        public bool IsTransmissionDamageProvided => true;
        public bool IsSuspensionDamageProvided => true;
        public bool IsBodyworkDamageProvided => true;
        public bool IsTyresDirtProvided => true;
        public bool IsDrsInformationProvided => true;
        public bool IsBoostInformationProvided => true;
        public bool IsTurboBoostPressureProvided => true;
        public bool IsCoolantPressureProvided => false;
        public bool IsOilPressureProvided => true;
        public bool IsFuelPressureProvided => true;
        public bool IsBrakesDamageProvided => false;
        public bool IsClutchDamageProvided => false;
        public bool IsAlternatorStatusShown => false;

        public bool IsSessionLengthAvailableBeforeStart => true;
        public bool DetermineIdealPressureByTemperature => true;
    }
}