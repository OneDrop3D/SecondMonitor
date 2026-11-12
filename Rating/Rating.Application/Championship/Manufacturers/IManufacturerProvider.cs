namespace SecondMonitor.Rating.Application.Championship.Manufacturers
{
    public interface IManufacturerProvider
    {
        public bool TryGetManufacturer(string carName, out Manufacturer manufacturer);
    }
}