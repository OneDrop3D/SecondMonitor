namespace SecondMonitor.SimdataManagement.DriverPresentation
{
    using System;

    using SecondMonitor.Contracts.SimSettings;
    using SecondMonitor.DataModel.DriversPresentation;

    public interface IAutomaticPresentationProvider
    {
        event EventHandler<DriverCustomColorEnabledArgs> DriverCustomColorChanged;

        bool TryGetDriverPresentation(string driverLongName, out DriverPresentationDto driverPresentation);
    }
}
