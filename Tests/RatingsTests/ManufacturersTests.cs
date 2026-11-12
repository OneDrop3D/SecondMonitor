namespace RemoteTest
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using SecondMonitor.Rating.Application.Championship.Manufacturers;

    public class ManufacturersTests
    {
        private static IReadOnlyList<Manufacturer> _allManufacturersSource = AllManufacturers.Manufacturers;
        private ManufacturerProvider _manufacturerProvider;

        [SetUp]
        public void SetUp()
        {
            _manufacturerProvider = new ManufacturerProvider();
        }

        [TestCaseSource(nameof(_allManufacturersSource))]
        public void AllManufacturers_EveryManufacturer_IsUnique(Manufacturer manufacturer)
        {
            // Arrange

            // Act
            bool isSuccess = _manufacturerProvider.TryGetManufacturer(manufacturer.Name, out Manufacturer returnedManufacturer);

            // Asset
            Assert.That(isSuccess, Is.True);
            Assert.That(returnedManufacturer, Is.EqualTo(manufacturer));
        }
    }
}