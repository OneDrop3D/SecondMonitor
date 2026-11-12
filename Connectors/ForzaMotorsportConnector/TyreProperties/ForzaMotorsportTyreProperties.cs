namespace SecondMonitor.ForzaMotorsportConnector.TyreProperties
{
    using System.Collections.Generic;
    using DataModel.BasicProperties;
    using DataModel.Snapshot.Drivers;
    using Foundation.Connectors.WheelInformation;

    public class ForzaMotorsportTyreProperties : AbstractCodeDefinedIdealWheelQuantitiesByClass
    {
        private static readonly TyresProperties StreetTyres = new TyresProperties(new TyreProperties(90, 15, 210, 10), new TyreProperties(90, 15, 210, 10));
        private static readonly TyresProperties PerformanceTyres = new TyresProperties(new TyreProperties(90, 15, 210, 10), new TyreProperties(90, 15, 210, 10));
        private static readonly TyresProperties RaceTyres = new TyresProperties(new TyreProperties(90, 15, 210, 10), new TyreProperties(90, 15, 210, 10));
        private static readonly TyresProperties DragTyres = new TyresProperties(new TyreProperties(90, 15, 210, 10), new TyreProperties(90, 15, 210, 10));
        private static readonly TyresProperties WetTyres = new TyresProperties(new TyreProperties(90, 15, 210, 10), new TyreProperties(90, 15, 210, 10));

        public ForzaMotorsportTyreProperties()
        {
            ClassTyrePropertiesMap = new Dictionary<string, TyresProperties>()
            {
                { "X", RaceTyres },
                { "P", RaceTyres },
                { "R", RaceTyres },
                { "A", StreetTyres },
                { "B", StreetTyres },
                { "C", StreetTyres },
                { "D", StreetTyres },
            };
        }

        protected override Dictionary<string, TyresProperties> ClassTyrePropertiesMap { get; }

        protected override OptimalQuantity<Pressure> GetIdealTyrePressureFront(DriverInfo driver)
        {
            var optimalPressure = base.GetIdealTyrePressureFront(driver);
            return optimalPressure;
        }

        protected override OptimalQuantity<Temperature> GetIdealTyreTemperaturesFront(DriverInfo driver)
        {
            if (driver.CarInfo.WheelsInfo.FrontLeft.TyreType.ToLower().Contains("hard"))
            {
                return new OptimalQuantity<Temperature>()
                {
                    IdealQuantity = Temperature.FromCelsius(87.5),
                    IdealQuantityWindow = Temperature.FromCelsius(17.5),
                };
            }

            if (driver.CarInfo.WheelsInfo.FrontLeft.TyreType.ToLower().Contains("wet"))
            {
                return new OptimalQuantity<Temperature>()
                {
                    IdealQuantity = Temperature.FromCelsius(70),
                    IdealQuantityWindow = Temperature.FromCelsius(25),
                };
            }

            return base.GetIdealTyreTemperaturesFront(driver);
        }

        protected override OptimalQuantity<Pressure> GetIdealTyrePressureRear(DriverInfo driver)
        {
            var optimalPressure = base.GetIdealTyrePressureRear(driver);
            return optimalPressure;
        }

        protected override OptimalQuantity<Temperature> GetIdealTyreTemperaturesRear(DriverInfo driver)
        {
            if (driver.CarInfo.WheelsInfo.FrontLeft.TyreType.ToLower().Contains("hard"))
            {
                return new OptimalQuantity<Temperature>()
                {
                    IdealQuantity = Temperature.FromCelsius(87.5),
                    IdealQuantityWindow = Temperature.FromCelsius(17.5),
                };
            }

            if (driver.CarInfo.WheelsInfo.FrontLeft.TyreType.ToLower().Contains("wet"))
            {
                return new OptimalQuantity<Temperature>()
                {
                    IdealQuantity = Temperature.FromCelsius(70),
                    IdealQuantityWindow = Temperature.FromCelsius(25),
                };
            }

            var optimalTemperature = base.GetIdealTyreTemperaturesRear(driver);
            return optimalTemperature;
        }
    }
}