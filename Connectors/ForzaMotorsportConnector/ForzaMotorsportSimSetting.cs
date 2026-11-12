namespace SecondMonitor.ForzaMotorsportConnector
{
    using Contracts.SimSettings;

    public class ForzaMotorsportSimSetting : ISimSettings
    {
        public bool IsEngineDamageProvided => false;
        public bool IsTransmissionDamageProvided => false;
        public bool IsSuspensionDamageProvided => false;
        public bool IsBodyworkDamageProvided => false;
        public bool IsTyresDirtProvided => false;
        public bool IsDrsInformationProvided => false;
        public bool IsBoostInformationProvided => false;
        public bool IsTurboBoostPressureProvided => true;
        public bool IsCoolantPressureProvided => false;
        public bool IsOilPressureProvided => false;
        public bool IsFuelPressureProvided => false;
        public bool IsBrakesDamageProvided => false;
        public bool IsClutchDamageProvided => false;
        public bool IsAlternatorStatusShown => false;
        public bool IsSessionLengthAvailableBeforeStart => false;
        public bool DetermineIdealPressureByTemperature => true;
    }
}