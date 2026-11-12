namespace SecondMonitor.AccConnector
{
    using Contracts.SimSettings;

    public class AccSimSettings : ISimSettings
    {
        public bool IsEngineDamageProvided => false;

        public bool IsTransmissionDamageProvided => false;

        public bool IsSuspensionDamageProvided => true;

        public bool IsBodyworkDamageProvided => true;

        public bool IsTyresDirtProvided => false;

        public bool IsDrsInformationProvided => false;

        public bool IsBoostInformationProvided => false;

        public bool IsTurboBoostPressureProvided => true;

        public bool IsCoolantPressureProvided => false;

        public bool IsOilPressureProvided => false;

        public bool IsFuelPressureProvided => false;

        public bool IsBrakesDamageProvided => true;

        public bool IsAlternatorStatusShown => true;

        public bool IsSessionLengthAvailableBeforeStart => true;

        public bool DetermineIdealPressureByTemperature => false;
        public bool IsClutchDamageProvided => false;
    }
}