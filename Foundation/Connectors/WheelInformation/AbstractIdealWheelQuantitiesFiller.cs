namespace SecondMonitor.Foundation.Connectors.WheelInformation
{
    using SecondMonitor.Contracts.WheelInformation;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.DataModel.Snapshot.Systems;

    public abstract class AbstractIdealWheelQuantitiesFiller : IIdealWheelQuantitiesFiller
    {
        private string _lastCarName;
        private string _lastTyreCompound;
        private OptimalQuantity<Pressure> _optimalPressureFront;
        private OptimalQuantity<Temperature> _optimalTemperatureFront;

        private OptimalQuantity<Pressure> _optimalPressureRear;
        private OptimalQuantity<Temperature> _optimalTemperatureRear;

        public void FillWheelIdealQuantities(SimulatorDataSet dataSet)
        {
            DriverInfo driver = dataSet.PlayerInfo;
            if (string.IsNullOrEmpty(driver?.CarName))
            {
                return;
            }

            this.CheckAndRetrieveIdealQuantities(driver);

            if (_optimalPressureFront == null && _optimalTemperatureFront == null)
            {
                return;
            }

            dataSet.SimulatorSourceInfo.TelemetryInfo.ContainsOptimalTemperatures = true;
            Wheels wheels = driver.CarInfo.WheelsInfo;
            this.FillWheelIdealQuantities(wheels.FrontLeft, _optimalPressureFront, _optimalTemperatureFront);
            this.FillWheelIdealQuantities(wheels.FrontRight, _optimalPressureFront, _optimalTemperatureFront);

            this.FillWheelIdealQuantities(wheels.RearLeft, _optimalPressureRear, _optimalTemperatureRear);
            this.FillWheelIdealQuantities(wheels.RearRight, _optimalPressureRear, _optimalTemperatureRear);
        }

        public void FillWheelIdealPressures(SimulatorDataSet dataSet)
        {
            DriverInfo driver = dataSet.PlayerInfo;
            if (string.IsNullOrEmpty(driver?.CarName))
            {
                return;
            }

            this.CheckAndRetrieveIdealQuantities(driver);

            if (_optimalPressureFront == null || _optimalPressureRear == null)
            {
                return;
            }

            dataSet.SimulatorSourceInfo.TelemetryInfo.ContainsOptimalTemperatures = true;
            Wheels wheels = driver.CarInfo.WheelsInfo;
            this.FillWheelIdealQuantities(wheels.FrontLeft, _optimalPressureFront);
            this.FillWheelIdealQuantities(wheels.FrontRight, _optimalPressureFront);

            this.FillWheelIdealQuantities(wheels.RearLeft, _optimalPressureRear);
            this.FillWheelIdealQuantities(wheels.RearRight, _optimalPressureRear);
        }

        private void FillWheelIdealQuantities(WheelInfo wheel, OptimalQuantity<Pressure> optimalPressure, OptimalQuantity<Temperature> optimalTemperature)
        {
            if (optimalPressure != null)
            {
                wheel.TyrePressure.IdealQuantity.InKpa = optimalPressure.IdealQuantity.InKpa;
                wheel.TyrePressure.IdealQuantityWindow.InKpa = optimalPressure.IdealQuantityWindow.InKpa;
            }

            if (optimalTemperature != null)
            {
                wheel.TyreCoreTemperature.IdealQuantity.InCelsius = optimalTemperature.IdealQuantity.InCelsius;
                wheel.TyreCoreTemperature.IdealQuantityWindow.InCelsius = optimalTemperature.IdealQuantityWindow.InCelsius;

                wheel.LeftTyreTemp.IdealQuantity.InCelsius = optimalTemperature.IdealQuantity.InCelsius;
                wheel.LeftTyreTemp.IdealQuantityWindow.InCelsius = optimalTemperature.IdealQuantityWindow.InCelsius;

                wheel.CenterTyreTemp.IdealQuantity.InCelsius = optimalTemperature.IdealQuantity.InCelsius;
                wheel.CenterTyreTemp.IdealQuantityWindow.InCelsius = optimalTemperature.IdealQuantityWindow.InCelsius;

                wheel.RightTyreTemp.IdealQuantity.InCelsius = optimalTemperature.IdealQuantity.InCelsius;
                wheel.RightTyreTemp.IdealQuantityWindow.InCelsius = optimalTemperature.IdealQuantityWindow.InCelsius;
            }
        }

        private void FillWheelIdealQuantities(WheelInfo wheel, OptimalQuantity<Pressure> optimalPressure)
        {
            if (optimalPressure != null)
            {
                wheel.TyrePressure.IdealQuantity.InKpa = optimalPressure.IdealQuantity.InKpa;
                wheel.TyrePressure.IdealQuantityWindow.InKpa = optimalPressure.IdealQuantityWindow.InKpa;
            }
        }

        private void CheckAndRetrieveIdealQuantities(DriverInfo driver)
        {
            if (!string.IsNullOrEmpty(_lastCarName) && _lastCarName == driver.CarName && _lastTyreCompound == driver.CarInfo.WheelsInfo.AllWheels[0].TyreType)
            {
                return;
            }

            _optimalPressureFront = this.GetIdealTyrePressureFront(driver);
            _optimalTemperatureFront = this.GetIdealTyreTemperaturesFront(driver);

            _optimalPressureRear = this.GetIdealTyrePressureRear(driver);
            _optimalTemperatureRear = this.GetIdealTyreTemperaturesRear(driver);

            _lastCarName = driver.CarName;
            _lastTyreCompound = driver.CarInfo.WheelsInfo.AllWheels[0].TyreType;
        }

        protected abstract OptimalQuantity<Pressure> GetIdealTyrePressureFront(DriverInfo driver);
        protected abstract OptimalQuantity<Temperature> GetIdealTyreTemperaturesFront(DriverInfo driver);

        protected abstract OptimalQuantity<Pressure> GetIdealTyrePressureRear(DriverInfo driver);
        protected abstract OptimalQuantity<Temperature> GetIdealTyreTemperaturesRear(DriverInfo driver);
    }
}