namespace SecondMonitor.SimdataManagement.ViewModel
{
    public interface ITyreSettingsViewModel
    {
        string PressureUnitSymbol { get; }
        string CompoundName { get; set; }
        bool IsGlobalCompound { get; }

        double FrontMinimalIdealTyreTemperature { get; set; }
        double FrontMaximumIdealTyreTemperature { get; set; }

        double FrontIdealPressure { get; set; }
        double FrontIdealPressureWindow { get; set; }
        double RearIdealPressure { get; set; }
        double RearIdealPressureWindow { get; set; }
        double NoWearLimit { get; set; }
        double LowWearLimit { get; set; }
        double HeavyWearLimit { get; set; }
    }
}