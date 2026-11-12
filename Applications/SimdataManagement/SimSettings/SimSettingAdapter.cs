namespace SecondMonitor.SimdataManagement.SimSettings
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Contracts.SimSettings;
    using DataModel.BasicProperties;
    using DataModel.OperationalRange;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using IdealPressure;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.DataModel.Snapshot.Drivers;

    public class SimSettingAdapter : ISimulatorDataSetVisitor
    {
        private readonly ICarSpecificationProvider _carSpecificationProvider;
        private readonly ISimSettingsFactory _settingsFactory;
        private readonly IIdealPressureByTemperatureCalculator _idealPressureByTemperatureCalculator;

        // Need to store these in case user would like to "revert to sim settings"
        private readonly Dictionary<string, OriginalSimProperties> _originalSimProperties;
        private readonly Stopwatch _refreshCarCustomNamesWatch;

        private DataSourceProperties _dataSourceProperties;

        private KeyValuePair<string, CarModelProperties> _lastCarProperties;
        private KeyValuePair<string, TyreCompoundProperties> _lastCompound;
        private Dictionary<string, string> _customCarNamesMap;
        private List<CarModelProperties> _propertiesOfCarsInSession;

        public SimSettingAdapter(ICarSpecificationProvider carSpecificationProvider, ISimSettingsFactory settingsFactory, IIdealPressureByTemperatureCalculator idealPressureByTemperatureCalculator)
        {
            _propertiesOfCarsInSession = new List<CarModelProperties>();
            _refreshCarCustomNamesWatch = new Stopwatch();
            _originalSimProperties = new Dictionary<string, OriginalSimProperties>();
            _carSpecificationProvider = carSpecificationProvider;
            _settingsFactory = settingsFactory;
            _idealPressureByTemperatureCalculator = idealPressureByTemperatureCalculator;
            _customCarNamesMap = new Dictionary<string, string>();
        }

        public CarModelProperties LastUsedCarProperties => _lastCarProperties.Value;

        public TyreCompoundProperties LastUsedCompound => _lastCompound.Value;

        public List<TyreCompoundProperties> GlobalTyreCompoundsProperties
        {
            get => _dataSourceProperties.TyreCompoundsProperties;
            set
            {
                _dataSourceProperties.TyreCompoundsProperties = value;
                _lastCarProperties = new KeyValuePair<string, CarModelProperties>(string.Empty, _lastCarProperties.Value);
                _lastCompound = new KeyValuePair<string, TyreCompoundProperties>(string.Empty, _lastCompound.Value);
                _carSpecificationProvider.SaveDataSourceProperties(_dataSourceProperties);
            }
        }

        public void Visit(SimulatorDataSet simulatorDataSet)
        {
            if (_dataSourceProperties?.SourceName != simulatorDataSet.Source)
            {
                ReloadDataSourceProperties(simulatorDataSet.Source);
                if (!simulatorDataSet.SimulatorSourceInfo.GlobalTyreCompounds && _dataSourceProperties.TyreCompoundsProperties.Count > 0)
                {
                    _dataSourceProperties.TyreCompoundsProperties.Clear();
                }
            }

            ApplyCustomCarName(simulatorDataSet);
            if (string.IsNullOrWhiteSpace(simulatorDataSet?.PlayerInfo?.CarName))
            {
                return;
            }

            CarModelProperties carModel = GetCarModelProperties(simulatorDataSet);
            ApplyCarModelForPlayer(simulatorDataSet, carModel);
        }

        private void ApplyCustomCarName(SimulatorDataSet simulatorDataSet)
        {
            if (!_refreshCarCustomNamesWatch.IsRunning || _refreshCarCustomNamesWatch.Elapsed.TotalSeconds > 10)
            {
                _refreshCarCustomNamesWatch.Restart();
                RefreshCustomCarNamesMap(simulatorDataSet);
            }

            if (_customCarNamesMap.Count == 0)
            {
                return;
            }

            simulatorDataSet.DriversInfo.ForEach(x => x.CarDisplayName = _customCarNamesMap.GetValueOrDefault(x.CarName, x.CarName));

            if (simulatorDataSet.LeaderInfo != null)
            {
                simulatorDataSet.LeaderInfo.CarDisplayName = _customCarNamesMap.GetValueOrDefault(simulatorDataSet.LeaderInfo.CarName, simulatorDataSet.LeaderInfo.CarName);
            }

            if (simulatorDataSet.PlayerInfo != null)
            {
                simulatorDataSet.PlayerInfo.CarDisplayName = _customCarNamesMap.GetValueOrDefault(simulatorDataSet.PlayerInfo.CarName, simulatorDataSet.PlayerInfo.CarName);
            }
        }

        private void RefreshCustomCarNamesMap(SimulatorDataSet simulatorDataSet)
        {
            _propertiesOfCarsInSession = simulatorDataSet.DriversInfo
                .DistinctBy(x => x.CarName)
                .Select(x => GetOrCreateCarModelProperties(simulatorDataSet, x))
                .Where(x => x != null)
                .ToList();

            _customCarNamesMap = _propertiesOfCarsInSession
                .Where(x => !string.IsNullOrWhiteSpace(x.CarCustomName))
                .ToDictionary(x => x.Name, x => x.CarCustomName);
        }

        public void Reset()
        {
            _originalSimProperties.Clear();
            _propertiesOfCarsInSession.Clear();
            _refreshCarCustomNamesWatch.Stop();
        }

        public void ReplaceCarModelProperties(CarModelProperties carModelProperties)
        {
            _dataSourceProperties.ReplaceCarModel(carModelProperties);
            _lastCarProperties = new KeyValuePair<string, CarModelProperties>(carModelProperties.Name, carModelProperties);
            _carSpecificationProvider.SaveDataSourceProperties(_dataSourceProperties);
        }

        private void ApplyCarModelForPlayer(SimulatorDataSet simulatorDataSet, CarModelProperties carModel)
        {
            Wheels wheels = simulatorDataSet.PlayerInfo.CarInfo.WheelsInfo;
            if (!simulatorDataSet.InputInfo.WheelAngleFilled)
            {
                simulatorDataSet.InputInfo.WheelAngleFilled = true;
                simulatorDataSet.InputInfo.WheelAngle = ((carModel.WheelRotation) / 2.0) * simulatorDataSet.InputInfo.SteeringInput;
            }

            ApplyRideHeightProperty(simulatorDataSet, carModel);

            if (string.IsNullOrEmpty(wheels.FrontLeft.TyreType))
            {
                return;
            }

            TyreCompoundProperties tyreCompound = GetTyreCompound(simulatorDataSet, simulatorDataSet.PlayerInfo, wheels.FrontLeft, wheels.RearLeft, carModel);
            ApplyWheelProperty(wheels.FrontLeft, true, carModel, tyreCompound);
            ApplyWheelProperty(wheels.FrontRight, true, carModel, tyreCompound);
            ApplyWheelProperty(wheels.RearLeft, false, carModel, tyreCompound);
            ApplyWheelProperty(wheels.RearRight, false, carModel, tyreCompound);
        }

        private void ApplyRideHeightProperty(SimulatorDataSet simData, CarModelProperties carModel)
        {
            if (simData.PlayerInfo.CarInfo.WheelsInfo.IsRideHeightFilled ||
                !simData.SimulatorSourceInfo.TelemetryInfo.ContainsSuspensionTravel ||
                carModel.FrontStaticRideHeight.IsZero ||
                carModel.RearStaticRideHeight.IsZero)
            {
                return;
            }

            simData.PlayerInfo.CarInfo.WheelsInfo.IsRideHeightFilled = true;
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.RideHeight = Distance.FromMeters(carModel.FrontStaticRideHeight.InMeters - carModel.MotionRatio * simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.SuspensionTravel.InMeters);
            simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.RideHeight = Distance.FromMeters(carModel.FrontStaticRideHeight.InMeters - carModel.MotionRatio * simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.SuspensionTravel.InMeters);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.RideHeight = Distance.FromMeters(carModel.RearStaticRideHeight.InMeters - carModel.MotionRatio * simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.SuspensionTravel.InMeters);
            simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.RideHeight = Distance.FromMeters(carModel.RearStaticRideHeight.InMeters - carModel.MotionRatio * simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.SuspensionTravel.InMeters);
            simData.PlayerInfo.CarInfo.FrontHeight = Distance.FromMeters((simData.PlayerInfo.CarInfo.WheelsInfo.FrontLeft.RideHeight.InMeters + simData.PlayerInfo.CarInfo.WheelsInfo.FrontRight.RideHeight.InMeters) / 2.0);
            simData.PlayerInfo.CarInfo.RearHeight = Distance.FromMeters((simData.PlayerInfo.CarInfo.WheelsInfo.RearLeft.RideHeight.InMeters + simData.PlayerInfo.CarInfo.WheelsInfo.RearRight.RideHeight.InMeters) / 2.0);
        }

        private void ApplyWheelProperty(WheelInfo wheelInfo, bool isFront, CarModelProperties carModel, TyreCompoundProperties tyreCompound)
        {
            wheelInfo.BrakeTemperature.IdealQuantity.InCelsius = carModel.OptimalBrakeTemperature.InCelsius;
            wheelInfo.BrakeTemperature.IdealQuantityWindow.InCelsius = carModel.OptimalBrakeTemperatureWindow.InCelsius;

            if (wheelInfo?.LeftTyreTemp == null || string.IsNullOrWhiteSpace(wheelInfo.TyreType) || wheelInfo.TyreType == "\u0001")
            {
                return;
            }

            if (!tyreCompound.IsValid)
            {
                return;
            }

            if (tyreCompound.DetermineIdealPressureByTemperature)
            {
                _idealPressureByTemperatureCalculator.FillIdealPressure(wheelInfo);
            }
            else
            {
                wheelInfo.TyrePressure.IdealQuantity.InKpa = isFront ? tyreCompound.FrontIdealPressure.InKpa : tyreCompound.RearIdealPressure.InKpa;
                wheelInfo.TyrePressure.IdealQuantityWindow.InKpa = isFront ? tyreCompound.FrontIdealPressureWindow.InKpa : tyreCompound.RearIdealPressureWindow.InKpa;
            }

            wheelInfo.LeftTyreTemp.IdealQuantity.InCelsius = isFront ? tyreCompound.FrontIdealTemperature.InCelsius : tyreCompound.RearIdealTemperature.InCelsius;
            wheelInfo.LeftTyreTemp.IdealQuantityWindow.InCelsius = isFront ? tyreCompound.FrontIdealTemperatureWindow.InCelsius : tyreCompound.RearIdealTemperatureWindow.InCelsius;

            wheelInfo.RightTyreTemp.IdealQuantity.InCelsius = isFront ? tyreCompound.FrontIdealTemperature.InCelsius : tyreCompound.RearIdealTemperature.InCelsius;
            wheelInfo.RightTyreTemp.IdealQuantityWindow.InCelsius = isFront ? tyreCompound.FrontIdealTemperatureWindow.InCelsius : tyreCompound.RearIdealTemperatureWindow.InCelsius;

            wheelInfo.CenterTyreTemp.IdealQuantity.InCelsius = isFront ? tyreCompound.FrontIdealTemperature.InCelsius : tyreCompound.RearIdealTemperature.InCelsius;
            wheelInfo.CenterTyreTemp.IdealQuantityWindow.InCelsius = isFront ? tyreCompound.FrontIdealTemperatureWindow.InCelsius : tyreCompound.RearIdealTemperatureWindow.InCelsius;

            wheelInfo.TyreCoreTemperature.IdealQuantity.InCelsius = isFront ? tyreCompound.FrontIdealTemperature.InCelsius : tyreCompound.RearIdealTemperature.InCelsius;
            wheelInfo.TyreCoreTemperature.IdealQuantityWindow.InCelsius = isFront ? tyreCompound.FrontIdealTemperatureWindow.InCelsius : tyreCompound.RearIdealTemperatureWindow.InCelsius;

            wheelInfo.TyreWear.NoWearWearLimit = tyreCompound.NoWearLimit;
            wheelInfo.TyreWear.LightWearLimit = tyreCompound.LowWearLimit;
            wheelInfo.TyreWear.HeavyWearLimit = tyreCompound.HeavyWearLimit;
        }

        private TyreCompoundProperties GetTyreCompound(SimulatorDataSet simulatorDataSet, DriverInfo driverInfo, WheelInfo frontWheel, WheelInfo rearWheel, CarModelProperties carModel)
        {
            string compoundName = frontWheel.TyreType;
            if (_lastCompound.Key == compoundName)
            {
                return _lastCompound.Value;
            }

            TyreCompoundProperties tyreCompound = carModel.GetTyreCompound(compoundName);
            if (tyreCompound?.IsValid == true)
            {
                StoreSimDefaultValues(driverInfo, frontWheel, rearWheel);
                _lastCompound = new KeyValuePair<string, TyreCompoundProperties>(tyreCompound.CompoundName, tyreCompound);
                return tyreCompound;
            }

            tyreCompound = _dataSourceProperties.GetTyreCompound(compoundName);
            if (tyreCompound?.IsValid != true)
            {
                StoreSimDefaultValues(driverInfo, frontWheel, rearWheel);
                tyreCompound = CreateTyreCompound(simulatorDataSet.Source, carModel, frontWheel, rearWheel);
                if (simulatorDataSet.SimulatorSourceInfo.GlobalTyreCompounds)
                {
                    _dataSourceProperties.AddTyreCompound(tyreCompound);
                }
                else
                {
                    carModel.AddTyreCompound(tyreCompound);
                }

                _carSpecificationProvider.SaveDataSourceProperties(_dataSourceProperties);
            }

            StoreSimDefaultValues(driverInfo, frontWheel, rearWheel);
            ApplyDefaultSimPropertiesToTyreCompound(carModel, tyreCompound, frontWheel, rearWheel);

            _lastCompound = new KeyValuePair<string, TyreCompoundProperties>(tyreCompound.CompoundName, tyreCompound);
            return tyreCompound;
        }

        private void StoreSimDefaultValues(DriverInfo driverInfo, WheelInfo frontWheel, WheelInfo rearWheel)
        {
            OriginalSimProperties originalSimProperties = new OriginalSimProperties();
            originalSimProperties.IdealBrakeTemperature = frontWheel.BrakeTemperature.IdealQuantity.InCelsius;
            originalSimProperties.IdealBrakeTemperatureWindow = frontWheel.BrakeTemperature.IdealQuantityWindow.InCelsius;

            originalSimProperties.FrontIdealTyreTemp = frontWheel.CenterTyreTemp.IdealQuantity.InCelsius;
            originalSimProperties.FrontIdealTyreTempWindow = frontWheel.CenterTyreTemp.IdealQuantityWindow.InCelsius;
            originalSimProperties.FrontIdealTyrePressure = frontWheel.TyrePressure.IdealQuantity.InKpa;
            originalSimProperties.FrontIdealTyrePressureWindow = frontWheel.TyrePressure.IdealQuantityWindow.InKpa;

            originalSimProperties.RearIdealTyreTemp = rearWheel.CenterTyreTemp.IdealQuantity.InCelsius;
            originalSimProperties.RearIdealTyreTempWindow = rearWheel.CenterTyreTemp.IdealQuantityWindow.InCelsius;
            originalSimProperties.RearIdealTyrePressure = rearWheel.TyrePressure.IdealQuantity.InKpa;
            originalSimProperties.RearIdealTyrePressureWindow = rearWheel.TyrePressure.IdealQuantityWindow.InKpa;

            _originalSimProperties[driverInfo.CarName] = originalSimProperties;
        }

        private CarModelProperties GetCarModelProperties(SimulatorDataSet simulatorDataSet)
        {
            string carName = simulatorDataSet.PlayerInfo.CarName;

            if (carName == _lastCarProperties.Key)
            {
                return _lastCarProperties.Value;
            }

            CarModelProperties carModelProperties = GetOrCreateCarModelProperties(simulatorDataSet, simulatorDataSet.PlayerInfo);

            _lastCarProperties = new KeyValuePair<string, CarModelProperties>(carModelProperties.Name, carModelProperties);
            return carModelProperties;
        }

        private CarModelProperties GetOrCreateCarModelProperties(SimulatorDataSet simulatorDataSet, DriverInfo driverInfo)
        {
            CarModelProperties carModelProperties = _dataSourceProperties.GetCarModel(driverInfo.CarName);

            if (carModelProperties == null || carModelProperties.OriginalContainsOptimalTemperature != simulatorDataSet.SimulatorSourceInfo.TelemetryInfo.ContainsOptimalTemperatures)
            {
                StoreSimDefaultValues(driverInfo, driverInfo.CarInfo.WheelsInfo.FrontLeft, driverInfo.CarInfo.WheelsInfo.RearLeft);
                _dataSourceProperties.CarModelsProperties.RemoveAll(x => x.Name == driverInfo.CarName);
                carModelProperties = CreateNewCarModelProperties(driverInfo.CarName, simulatorDataSet);
            }

            return carModelProperties;
        }

        private void ApplyDefaultSimPropertiesToTyreCompound(CarModelProperties carModelProperties, TyreCompoundProperties tyreCompound, WheelInfo frontWheel, WheelInfo rearWheel)
        {
            if (!tyreCompound.IsSimOriginal || !_originalSimProperties.TryGetValue(carModelProperties.Name, out OriginalSimProperties originalSimProperties))
            {
                return;
            }

            tyreCompound.FrontIdealPressure = Pressure.FromKiloPascals(originalSimProperties.FrontIdealTyrePressure);
            tyreCompound.FrontIdealPressureWindow = Pressure.FromKiloPascals(originalSimProperties.FrontIdealTyrePressureWindow);

            tyreCompound.RearIdealPressure = Pressure.FromKiloPascals(originalSimProperties.RearIdealTyrePressure);
            tyreCompound.RearIdealPressureWindow = Pressure.FromKiloPascals(originalSimProperties.RearIdealTyrePressureWindow);

            tyreCompound.RearIdealTemperature = Temperature.FromCelsius(originalSimProperties.FrontIdealTyreTemp);
            tyreCompound.RearIdealTemperatureWindow = Temperature.FromCelsius(originalSimProperties.FrontIdealTyreTempWindow);

            tyreCompound.FrontIdealTemperature = Temperature.FromCelsius(originalSimProperties.RearIdealTyreTemp);
            tyreCompound.FrontIdealTemperatureWindow = Temperature.FromCelsius(originalSimProperties.RearIdealTyreTempWindow);

            tyreCompound.NoWearLimit = frontWheel.TyreWear.NoWearWearLimit;
            tyreCompound.LowWearLimit = frontWheel.TyreWear.LightWearLimit;
            tyreCompound.HeavyWearLimit = frontWheel.TyreWear.HeavyWearLimit;
        }

        private CarModelProperties CreateNewCarModelProperties(string carName, SimulatorDataSet simulatorDataSet)
        {
            CarModelProperties newCarModelProperties = new CarModelProperties { Name = carName };
            ApplyDefaultSimPropertiesToCarModelProperties(newCarModelProperties, simulatorDataSet);

            _dataSourceProperties.AddCarModel(newCarModelProperties);
            _carSpecificationProvider.SaveDataSourceProperties(_dataSourceProperties);
            return newCarModelProperties;
        }

        private void ApplyDefaultSimPropertiesToCarModelProperties(CarModelProperties carModel, SimulatorDataSet simulatorDataSet)
        {
            if (!_originalSimProperties.TryGetValue(carModel.Name, out OriginalSimProperties originalSimProperties))
            {
                return;
            }

            carModel.OriginalContainsOptimalTemperature = simulatorDataSet.SimulatorSourceInfo.TelemetryInfo.ContainsOptimalTemperatures;
            carModel.OptimalBrakeTemperature = Temperature.FromCelsius(originalSimProperties.IdealBrakeTemperature);
            carModel.OptimalBrakeTemperatureWindow = Temperature.FromCelsius(originalSimProperties.IdealBrakeTemperatureWindow);
        }

        private TyreCompoundProperties CreateTyreCompound(string simulatorName, CarModelProperties carModel,  WheelInfo frontWheel, WheelInfo rearWheel)
        {
            var simSettings = _settingsFactory.Create(simulatorName);
            TyreCompoundProperties tyreCompound = new TyreCompoundProperties()
            {
                CompoundName = frontWheel.TyreType,
                IsSimOriginal = true,
                DetermineIdealPressureByTemperature = simSettings.DetermineIdealPressureByTemperature,
            };

            ApplyDefaultSimPropertiesToTyreCompound(carModel, tyreCompound, frontWheel, rearWheel);

            return tyreCompound;
        }

        private void ReloadDataSourceProperties(string sourceName)
        {
            _dataSourceProperties = _carSpecificationProvider.GetSimulatorProperties(sourceName);
        }

        public void ResetCarSettings(CarModelProperties originalModel, SimulatorDataSet dataSet)
        {
            ApplyDefaultSimPropertiesToCarModelProperties(originalModel, dataSet);
            originalModel.TyreCompoundsProperties.Clear();
            originalModel.TyreCompoundsProperties.Add(CreateTyreCompound(dataSet.Source, originalModel, dataSet.PlayerInfo.CarInfo.WheelsInfo.FrontLeft, dataSet.PlayerInfo.CarInfo.WheelsInfo.RearLeft));
        }

        public IEnumerable<CarModelProperties> GetAllSessionCarsProperties()
        {
            return _propertiesOfCarsInSession;
        }
    }
}