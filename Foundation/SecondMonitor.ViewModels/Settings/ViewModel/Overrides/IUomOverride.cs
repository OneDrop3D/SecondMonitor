namespace SecondMonitor.ViewModels.Settings.ViewModel.Overrides
{
    using System;
    using DataModel.BasicProperties;

    public interface IUomOverride
    {
        event EventHandler<ActivationStateEventArgs> ActivationStateChanged;
        PressureUnits GetPressureUnits(DisplaySettingsViewModel displaySettingsViewModel, PressureUnits preferredPressureUnits);
    }
}