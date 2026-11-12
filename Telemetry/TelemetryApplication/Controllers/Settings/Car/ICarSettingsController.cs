namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Settings.Car
{
    using System;
    using System.Collections.Generic;
    using SecondMonitor.ViewModels.Controllers;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;

    public interface ICarSettingsController : IChildController<ISettingsController>
    {
        event EventHandler<CarPropertiesArgs> CarPropertiesChanges;

        CarWithChartPropertiesDto CurrentCarWithChartProperties { get; }

        CarWithChartPropertiesDto ResetAndGetCarPropertiesForCurrentCar();

        CarWithChartPropertiesDto GetDefaultCarPropertiesForCurrentCar();

        CarWithChartPropertiesDto GetCarProperties(string simulator, string carName);

        CarWithChartPropertiesDto ResetAndGetCarProperties(string simulator, string carName);

        void UpdateCarProperties(CarWithChartPropertiesDto carWithChartPropertiesDto);

        CarPropertiesDto GetCarProperties(LapTelemetryDto lapTelemetryDto);

        CarPropertiesDto GetCarProperties(LapSummaryDto lapSummaryDto);

        IEnumerable<(LapTelemetryDto LapTelemetryDto, CarPropertiesDto CarPropertiesDto)> FillCarProperties(IEnumerable<LapTelemetryDto> lapTelemetries);
    }
}