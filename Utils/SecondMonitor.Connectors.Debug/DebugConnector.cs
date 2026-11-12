namespace SecondMonitor.Connectors.Debug
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Versioning;
    using System.Threading;
    using System.Threading.Tasks;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using DataModel.Snapshot.Systems;
    using SecondMonitor.Foundation.Connectors;
    using ViewModels.Settings;

    [SupportedOSPlatform("windows7.0")]
    public class DebugConnector : IRawTelemetryConnector
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly Dictionary<string, DriverInfo> _players = new();

        private CancellationTokenSource _loopToken;
        private Task _loopTask;

        private double _brakePedalPosition = 0;
        private double _brakePedalStep = 0.0001;
        private double _throttlePedalPosition = 0;
        private double _throttlePedalStep = 0.001;
        private double _clutchPedalPosition = 0;
        private double _clutchPedalStep = 0.00001;

        private double _wheelAngle = 0;
        private double _wheelAngleStep = 0.1;

        private double _tyreTemp = 50;
        private double _tyreStep = 0.1;
        private double _brakeTemp = 50;
        private double _brakeStep = 1;

        private double _fuel = 2000;
        private double _totalFuel = 2000;
        private double _fuelStep = -0.01;

        private double _layoutLength = 1000;

        private double _playerLocationStep = 2;

        private double _engineWaterTemp = 30;
        private double _engineWaterTempStep = 0.01;

        private double _oilTemp = 30;
        private double _oilTempStep = 0.1;

        private DateTime _lastTick = DateTime.Now;
        private TimeSpan _sessionTime = new TimeSpan(0, 0, 1);

        public DebugConnector(ISettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider;
        }

        public event EventHandler<DataEventArgs> DataLoaded;
        public event EventHandler<EventArgs> ConnectedEvent;
        public event EventHandler<EventArgs> Disconnected;
        public event EventHandler<DataEventArgs> SessionStarted;

#pragma warning disable CS0067
        public event EventHandler<MessageArgs> DisplayMessage;
#pragma warning restore CS0067

        private long UpdateFrequency => _settingsProvider?.DisplaySettingsViewModel.TelemetrySettingsViewModel.LoggingInterval ?? 15L;
        public string Name => "Mocked";

        public bool IsConnected { get; private set; }

        public bool IsEnabledByDefault => false;
        public Action ConnectorOptionsDelegate => null;

        public bool TryConnect()
        {
            IsConnected = true;
            _loopToken = new CancellationTokenSource();
            RaiseConnectedEvent();
            return true;
        }

        public Task FinnishConnectorAsync()
        {
            _loopToken.Cancel();
            return _loopTask;
        }

        public void StartConnectorLoop()
        {
            _loopToken = new CancellationTokenSource();
            _loopTask = Task.Run(this.TestingThreadExecutor, _loopToken.Token);
        }

        private async Task TestingThreadExecutor()
        {
            while (!_loopToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(2000));
                this.ConnectDriver("Lorem Ipsum", true);
                SimulatorDataSet set = this.PrepareDataSet();
                this.RaiseSessionStartedEvent(set);
                await Task.Delay(TimeSpan.FromMilliseconds(1000));
                while (!_loopToken.IsCancellationRequested && set.SessionInfo.SessionTime.TotalSeconds <= 180)
                {
                    var stopwatch = Stopwatch.StartNew();
                    if (set.SessionInfo.SessionTime.TotalSeconds > 10 && !_players.ContainsKey("Driver 2") && set.SessionInfo.SessionTime.TotalSeconds < 60)
                    {
                        this.ConnectDriver("Driver 2", false);
                    }

                    if (set.SessionInfo.SessionTime.TotalSeconds > 70 && _players.ContainsKey("Driver 2") && set.SessionInfo.SessionTime.TotalSeconds < 80)
                    {
                        _players.Remove("Driver 2");
                    }

                    if (set.SessionInfo.SessionTime.TotalSeconds > 120 && !_players.ContainsKey("Driver 2"))
                    {
                        this.ConnectDriver("Driver 2", false);
                    }

                    if (set.SessionInfo.SessionTime.TotalSeconds > 12)
                    {
                        this.ConnectDriver("Driver 3", false);
                    }

                    if (set.SessionInfo.SessionTime.TotalSeconds > 15)
                    {
                        this.ConnectDriver("Driver 4", false);
                    }

                    if (set.SessionInfo.SessionTime.TotalSeconds > 35)
                    {
                        this.ConnectDriver("Driver 5", false);
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(10));
                    set = this.PrepareDataSet();
                    this.RaiseDataLoadedEvent(set);

                    _brakePedalPosition += _brakePedalStep;
                    _throttlePedalPosition += _throttlePedalStep;
                    _clutchPedalPosition += _clutchPedalStep;
                    _wheelAngle += _wheelAngleStep;
                    _brakeTemp += _brakeStep;
                    _tyreTemp += _tyreStep;
                    _fuel += _fuelStep;
                    _engineWaterTemp += _engineWaterTempStep;
                    _oilTemp += _oilTempStep;

                    if (_brakePedalPosition > 1 || _brakePedalPosition < 0)
                    {
                        _brakePedalStep = -_brakePedalStep;
                    }

                    if (_throttlePedalPosition > 1 || _throttlePedalPosition < 0)
                    {
                        _throttlePedalStep = -_throttlePedalStep;
                    }

                    if (_clutchPedalPosition > 1 || _clutchPedalPosition < 0)
                    {
                        _clutchPedalStep = -_clutchPedalStep;
                    }

                    if (_wheelAngle > 270 || _wheelAngle < -270)
                    {
                        _wheelAngleStep = -_wheelAngleStep;
                    }

                    if (_brakeTemp > 1500 || _brakeTemp < 30)
                    {
                        _brakeStep = -_brakeStep;
                    }

                    if (_tyreTemp > 150 || _tyreTemp < 30)
                    {
                        _tyreStep = -_tyreStep;
                    }

                    if (_fuel < 0)
                    {
                        _fuel = _totalFuel;
                    }

                    if (_engineWaterTemp < 20 || _engineWaterTemp > 130)
                    {
                        _engineWaterTempStep = -_engineWaterTempStep;
                    }

                    if (_oilTemp < 20 || _oilTemp > 180)
                    {
                        _oilTempStep = -_oilTempStep;
                    }

                    stopwatch.Stop();
                    var sleepTime = Math.Max(this.UpdateFrequency - stopwatch.ElapsedMilliseconds, 0L);

                    await Task.Delay(TimeSpan.FromMilliseconds(sleepTime));
                }

                _players.Clear();
            }
        }

        private SimulatorDataSet PrepareDataSet()
        {
            DateTime tickTime = DateTime.Now;
            _sessionTime = _sessionTime.Add(tickTime.Subtract(_lastTick));
            _lastTick = tickTime;
            SimulatorDataSet simulatorDataSet = new SimulatorDataSet("Test Source");
            simulatorDataSet.SessionInfo.SessionTime = _sessionTime;

            simulatorDataSet.SessionInfo.SessionPhase = SessionPhase.Green;
            simulatorDataSet.SimulatorSourceInfo.WorldPositionInvalid = true;
            simulatorDataSet.SessionInfo.TrackInfo.TrackName = "Slovakia Ring";
            simulatorDataSet.SessionInfo.TrackInfo.TrackLayoutName = "Grand Prix";
            simulatorDataSet.SessionInfo.TrackInfo.LayoutLength = Distance.FromMeters(_layoutLength);
            simulatorDataSet.SessionInfo.WeatherInfo.AirTemperature = Temperature.FromCelsius(25);
            simulatorDataSet.SessionInfo.WeatherInfo.TrackTemperature = Temperature.FromCelsius(31);
            simulatorDataSet.SessionInfo.WeatherInfo.RainIntensity = 0;
            simulatorDataSet.SessionInfo.IsActive = true;
            simulatorDataSet.SimulatorSourceInfo.GlobalTyreCompounds = true;
            simulatorDataSet.SessionInfo.SessionType = SessionType.Race;
            simulatorDataSet.SessionInfo.IsMultiClass = _sessionTime.Seconds > 15;
            simulatorDataSet.SessionInfo.PitWindow.PitWindowState = PitWindowState.InPitWindow;

            foreach (DriverInfo driver in _players.Values)
            {
                this.UpdateDriver(driver);
                if (driver.IsPlayer)
                {
                    driver.Speed = Velocity.FromKph(DateTime.Now.Second);
                    simulatorDataSet.PlayerInfo = driver;
                }
            }

            simulatorDataSet.DriversInfo = _players.Values.ToArray();
            simulatorDataSet.PlayerInfo.CarInfo.WheelsInfo.FrontLeft = new WheelInfo(WheelKind.FrontLeft);
            simulatorDataSet.PlayerInfo.CarInfo.WheelsInfo.FrontRight = new WheelInfo(WheelKind.FrontRight);
            simulatorDataSet.PlayerInfo.CarInfo.WheelsInfo.RearRight = new WheelInfo(WheelKind.RearRight);
            simulatorDataSet.PlayerInfo.CarInfo.WheelsInfo.RearLeft = new WheelInfo(WheelKind.RearLeft);
            simulatorDataSet.InputInfo.BrakePedalPosition = _brakePedalPosition;
            simulatorDataSet.InputInfo.ThrottlePedalPosition = _throttlePedalPosition;
            simulatorDataSet.InputInfo.ClutchPedalPosition = _clutchPedalPosition;
            simulatorDataSet.InputInfo.WheelAngle = _wheelAngle;
            if (DateTime.Now.Minute % 2 == 0)
            {
                simulatorDataSet.SessionInfo.ActiveFlags |= FlagKind.YellowSector1;
            }

            this.UpdateWheelInfo(simulatorDataSet.PlayerInfo.CarInfo.WheelsInfo.FrontLeft);
            this.UpdateWheelInfo(simulatorDataSet.PlayerInfo.CarInfo.WheelsInfo.FrontRight);
            this.UpdateWheelInfo(simulatorDataSet.PlayerInfo.CarInfo.WheelsInfo.RearRight);
            this.UpdateWheelInfo(simulatorDataSet.PlayerInfo.CarInfo.WheelsInfo.RearLeft);
            return simulatorDataSet;
        }

        private void ConnectDriver(string name, bool isPlayer)
        {
            if (_players.ContainsKey(name))
            {
                return;
            }

            DriverInfo driver = new DriverInfo();
            _players[name] = driver;
            driver.CarName = "Mazda 626";
            driver.CompletedLaps = 0;
            driver.CurrentLapValid = true;
            driver.DistanceToPlayer = 0;
            driver.DriverSessionId = driver.DriverShortName = driver.DriverLongName = name;
            driver.InPits = false;
            driver.IsPlayer = isPlayer;
            driver.LapDistance = 0;
            driver.Position = _players.Values.Count;
            driver.PositionInClass = driver.Position;
            driver.CarClassId = "ClassId";
            driver.CarClassName = "Group B";
            driver.RatingInfo.Rating = 2000;
            driver.RatingInfo.IsFilled = true;
            driver.RatingInfo.RacesCompleted = 150;
            driver.RatingInfo.Reputation = 0;
            if (!isPlayer)
            {
                return;
            }

            driver.CarInfo.FuelSystemInfo.FuelCapacity = Volume.FromLiters(_totalFuel);
            driver.CarInfo.FuelSystemInfo.FuelRemaining = Volume.FromLiters(_fuel);
            driver.CarInfo.WaterSystemInfo.OptimalWaterTemperature.ActualQuantity = Temperature.FromCelsius(_engineWaterTemp);
            driver.CarInfo.OilSystemInfo.OptimalOilTemperature.ActualQuantity = Temperature.FromCelsius(_oilTemp);
            return;
        }

        private void UpdateDriver(DriverInfo driverInfo)
        {
            driverInfo.LapDistance += _playerLocationStep;
            driverInfo.TotalDistance += _playerLocationStep;

            if (driverInfo.LapDistance >= _layoutLength)
            {
                driverInfo.LapDistance = 0;
                driverInfo.CompletedLaps++;
            }

            if (!driverInfo.IsPlayer)
            {
                return;
            }

            driverInfo.CarInfo.FuelSystemInfo.FuelCapacity = Volume.FromLiters(_totalFuel);
            driverInfo.CarInfo.FuelSystemInfo.FuelRemaining = Volume.FromLiters(_fuel);
            driverInfo.CarInfo.WaterSystemInfo.OptimalWaterTemperature = new OptimalQuantity<Temperature>()
            {
                IdealQuantity = driverInfo.CarInfo.WaterSystemInfo.OptimalWaterTemperature.IdealQuantity,
                IdealQuantityWindow = driverInfo.CarInfo.WaterSystemInfo.OptimalWaterTemperature.IdealQuantityWindow,
                ActualQuantity = Temperature.FromCelsius(_engineWaterTemp),
            };
            driverInfo.CarInfo.OilSystemInfo.OptimalOilTemperature = new OptimalQuantity<Temperature>()
            {
                ActualQuantity = Temperature.FromCelsius(_oilTemp),
                IdealQuantity = driverInfo.CarInfo.OilSystemInfo.OptimalOilTemperature.IdealQuantity,
                IdealQuantityWindow = driverInfo.CarInfo.OilSystemInfo.OptimalOilTemperature.IdealQuantityWindow,
            };
        }

        private void UpdateWheelInfo(WheelInfo info)
        {
            info.LeftTyreTemp.ActualQuantity = Temperature.FromCelsius(_tyreTemp - 5);
            info.CenterTyreTemp.ActualQuantity = Temperature.FromCelsius(_tyreTemp);
            info.RightTyreTemp.ActualQuantity = Temperature.FromCelsius(_tyreTemp + 5);
            info.TyreCoreTemperature.ActualQuantity = Temperature.FromCelsius(_tyreTemp);
            info.TyreType = "Slick";
            info.Camber = Angle.GetFromDegrees(-0.1);
            info.BrakeTemperature.ActualQuantity = Temperature.FromCelsius(_brakeTemp);
            info.TyrePressure.ActualQuantity = Pressure.FromKiloPascals(200);
        }

        private void RaiseConnectedEvent()
        {
            EventArgs args = new EventArgs();
            this.ConnectedEvent?.Invoke(this, args);
        }

        private void RaiseDisconnectedEvent()
        {
            EventArgs args = new EventArgs();
            this.Disconnected?.Invoke(this, args);
        }

        private void RaiseSessionStartedEvent(SimulatorDataSet data)
        {
            DataEventArgs args = new DataEventArgs(data);
            EventHandler<DataEventArgs> handler = this.SessionStarted;
            _lastTick = DateTime.Now;
            _sessionTime = new TimeSpan(0, 0, 1);
            handler?.Invoke(this, args);
        }

        private void RaiseDataLoadedEvent(SimulatorDataSet data)
        {
            DataEventArgs args = new DataEventArgs(data);
            this.DataLoaded?.Invoke(this, args);
        }
    }
}
