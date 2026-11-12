namespace SecondMonitor.Contracts.SimSettings
{
    using System;
    using DataModel.BasicProperties;
    using DataModel.DriversPresentation;

    public interface IDriverPresentationsManager
    {
        event EventHandler<DriverCustomColorEnabledArgs> DriverCustomColorChanged;

        bool TryGetOutLineColor(string driverName, out ColorDto color);

        bool TryGetDriverPresentation(string driverLongName, out DriverPresentationDto driverPresentationDto);
    }
}