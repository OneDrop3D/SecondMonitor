namespace SecondMonitor.Remote.Debug.Simulation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using DataModel.Snapshot.Systems;

    public static class RaceSessionExtensions
    {
        public static SimulatorDataSet CreateSimulatorDataSetForDriver(this RaceSession raceSession, Driver driver, SessionPhase sessionPhase)
        {
            var simulatorDataSet = new SimulatorDataSet("Simulated Source");
            simulatorDataSet.SimulatorSourceInfo.WorldPositionInvalid = true;
            simulatorDataSet.SimulatorSourceInfo.GlobalTyreCompounds = true;
            simulatorDataSet.SessionInfo.SessionPhase = sessionPhase;
            simulatorDataSet.SessionInfo.TrackInfo.TrackName = raceSession.Track.TrackName;
            simulatorDataSet.SessionInfo.TrackInfo.TrackLayoutName = "Grand Prix";
            simulatorDataSet.SessionInfo.TrackInfo.LayoutLength = Distance.FromMeters(raceSession.Track.TrackLength);

            simulatorDataSet.SessionInfo.WeatherInfo.AirTemperature = Temperature.FromCelsius(25);
            simulatorDataSet.SessionInfo.WeatherInfo.TrackTemperature = Temperature.FromCelsius(31);
            simulatorDataSet.SessionInfo.WeatherInfo.RainIntensity = 0;

            simulatorDataSet.SessionInfo.IsActive = true;
            simulatorDataSet.SessionInfo.SessionType = SessionType.Race;

            var driverInfos = new List<DriverInfo>(16);

            foreach (var entry in raceSession.Entries)
            {
                var driverInfo = new DriverInfo();
                driverInfo.CarName = entry.Car.Name;
                driverInfo.CompletedLaps = entry.CompletedLaps;
                driverInfo.CurrentLapValid = true;
                driverInfo.DistanceToPlayer = 0;
                driverInfo.DriverShortName = driverInfo.DriverLongName = entry.CurrentDriver.Name;
                driverInfo.DriverSessionId = string.Join("-", entry.Drivers.OrderBy(x => x.Name).Select(x => x.Name));
                driverInfo.InPits = entry.InPits;
                driverInfo.LapDistance = Math.Max(entry.LapDistance, 0);
                driverInfo.PositionInClass = driverInfo.Position = entry.Position;
                driverInfo.CarClassId = entry.Car.Class;
                driverInfo.Timing.CurrentLapTime = entry.CurrentLaptime;
                driverInfo.Timing.LastLapTime = entry.LastLaptime;

                driverInfos.Add(driverInfo);

                if (driverInfo.Position == 1)
                {
                    simulatorDataSet.LeaderInfo = driverInfo;
                }

                if (driver != entry.CurrentDriver)
                {
                    continue;
                }

                driverInfo.CarInfo.WheelsInfo.FrontLeft = new WheelInfo(WheelKind.FrontLeft);
                driverInfo.CarInfo.WheelsInfo.FrontRight = new WheelInfo(WheelKind.FrontRight);
                driverInfo.CarInfo.WheelsInfo.RearRight = new WheelInfo(WheelKind.RearRight);
                driverInfo.CarInfo.WheelsInfo.RearLeft = new WheelInfo(WheelKind.RearLeft);
                UpdateWheelInfo(driverInfo.CarInfo.WheelsInfo.FrontLeft);
                UpdateWheelInfo(driverInfo.CarInfo.WheelsInfo.FrontRight);
                UpdateWheelInfo(driverInfo.CarInfo.WheelsInfo.RearRight);
                UpdateWheelInfo(driverInfo.CarInfo.WheelsInfo.RearLeft);
                simulatorDataSet.PlayerInfo = driverInfo;
                simulatorDataSet.InputInfo.BrakePedalPosition = entry.Car.BrakePosition;
                simulatorDataSet.InputInfo.ThrottlePedalPosition = entry.Car.ThrottlePosition;
                simulatorDataSet.InputInfo.ClutchPedalPosition = entry.Car.ClutchPosition;
            }

            simulatorDataSet.DriversInfo = driverInfos.ToArray();

            return simulatorDataSet;
        }

        private static void UpdateWheelInfo(WheelInfo info)
        {
            const double tyreTemp = 50;
            const double brakeTemp = 50;

            info.LeftTyreTemp.ActualQuantity = Temperature.FromCelsius(tyreTemp - 5);
            info.CenterTyreTemp.ActualQuantity = Temperature.FromCelsius(tyreTemp);
            info.RightTyreTemp.ActualQuantity = Temperature.FromCelsius(tyreTemp + 5);
            info.TyreCoreTemperature.ActualQuantity = Temperature.FromCelsius(tyreTemp);
            info.TyreType = "Slick";
            info.Camber = Angle.GetFromDegrees(-0.1);
            info.BrakeTemperature.ActualQuantity = Temperature.FromCelsius(brakeTemp);
            info.TyrePressure.ActualQuantity = Pressure.FromKiloPascals(200);
        }
    }
}