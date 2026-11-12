namespace SecondMonitor.Telemetry.TelemetryManagement.Settings
{
    using System.Collections.Generic;
    using System.Linq;

    public class CarsProperties
    {
        public CarsProperties()
        {
            Cars = new List<CarWithChartPropertiesDto>();
        }

        public List<CarWithChartPropertiesDto> Cars { get; set; }

        public bool TryGetCarProperties(string simulator, string carName, out CarWithChartPropertiesDto carWithChartProperties)
        {
            carWithChartProperties = Cars.FirstOrDefault(x => x.CarPropertiesDto.CarName == carName && x.CarPropertiesDto.Simulator == simulator);
            return carWithChartProperties != null;
        }

        public void SaveCarProperties(CarWithChartPropertiesDto carWithChartProperties)
        {
            Cars.RemoveAll(x => x.CarPropertiesDto.CarName == carWithChartProperties.CarPropertiesDto.CarName && x.CarPropertiesDto.Simulator == carWithChartProperties.CarPropertiesDto.Simulator);
            Cars.Add(carWithChartProperties);
        }
    }
}