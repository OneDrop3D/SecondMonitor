namespace SecondMonitor.Contracts.SimSettings
{
    public interface ISimSettings
    {
        bool IsEngineDamageProvided { get; }

        bool IsTransmissionDamageProvided { get; }

        bool IsSuspensionDamageProvided { get; }

        bool IsBodyworkDamageProvided { get; }

        bool IsTyresDirtProvided { get; }

        bool IsDrsInformationProvided { get; }

        bool IsBoostInformationProvided { get; }

        bool IsTurboBoostPressureProvided { get; }

        bool IsCoolantPressureProvided { get; }

        bool IsOilPressureProvided { get; }

        bool IsFuelPressureProvided { get; }

        bool IsBrakesDamageProvided { get; }

        bool IsClutchDamageProvided { get; }

        bool IsAlternatorStatusShown { get; }

        bool IsSessionLengthAvailableBeforeStart { get; }

        bool DetermineIdealPressureByTemperature { get; }
    }
}