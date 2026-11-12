namespace SecondMonitor.Rating.Application.Championship.Manufacturers
{
    using System.Collections.Generic;
    using System.Linq;

    public class ManufacturerProvider : IManufacturerProvider
    {
        private readonly IReadOnlyCollection<Manufacturer> _allManufacturers = AllManufacturers.Manufacturers;

        public bool TryGetManufacturer(string carName, out Manufacturer manufacturer)
        {
            manufacturer = _allManufacturers.Where(x => x.IsManufacturerOf(carName))
                .OrderByDescending(x => x.Name.Length).FirstOrDefault();

            return manufacturer != null;
        }
    }
}