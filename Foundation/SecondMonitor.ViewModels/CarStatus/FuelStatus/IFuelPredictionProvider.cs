namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    using System;

    public interface IFuelPredictionProvider
    {
        TimeSpan GetRemainingFuelTime();

        double GetLapsLeft();
    }
}