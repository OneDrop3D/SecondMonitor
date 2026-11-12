namespace SecondMonitor.AccConnector.UDPData
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Structs;

    public class FullCarData
    {
        private static readonly Dictionary<byte, string> CarModelsMap = new Dictionary<byte, string>()
        {
            { 0, "Porsche 991 GT3" },
            { 1, "Mercedes AMG GT3" },
            { 2, "Ferrari 488 GT3" },
            { 3, "Audi R8 GT3 2015" },
            { 4, "Lamborghini Huracan GT3" },
            { 5, "McLaren 650s GT3" },
            { 6, "Nissan GT-R Nismo GT3 2018" },
            { 7, "BMW M6 GT3" },
            { 8, "Bentley Continental GT3 2018" },
            { 9, "Porsche 991 II GT3 Cup" },
            { 10, "Nissan GT-R Nismo GT3 2015" },
            { 11, "Bentley Continental GT3 2015" },
            { 12, "Aston Martin Vantage V12 GT3" },
            { 13, "Lamborghini Gallardo R-EX" },
            { 14, "Jaguar G3" },
            { 15, "Lexus RC F GT3" },
            { 16, "Lamborghini Huracan Evo 2019" },
            { 17, "Honda NSX GT3" },
            { 18, "Lamborghini Huracan SuperTrofeo" },
            { 19, "Audi R8 LMS Evo 2019" },
            { 20, "Aston Martin Vantage V8 2019" },
            { 21, "Honda NSX Evo 2019" },
            { 22, "McLaren 720S GT3 Specia" },
            { 23, "Porsche 991 II GT3 R 2019" },
            { 24, "Ferrari 488 GT3 Evo" },
            { 25, "Mercedes AMG GT3 2020" },
            { 26, "Ferrari 488 GT3 Challenge Evo" },
            { 27, "BMW M2 CS Racing" },
            { 28, "Porsche 911 GT3 Cup (Type 992)" },
            { 29, "Lamborghini Huracan Super Trofeo EVO 2" },
            { 30, "BMW M4 GT3" },
            { 31, "Audi R8 LMS GT3 EVO II" },
            { 32, "Ferrari 296 GT3" },
            { 33, "Lamborghini Huracan GT3 EVO 2" },
            { 34, "Porsche 992 GT3 R" },
            { 35, "McLaren 720S GT3 Evo 2023" },
            { 36, "Ford Mustang GT3" },
            //GT4
            { 50, "Alpine A1110 GT4" },
            { 51, "Aston Martin Vantage GT4" },
            { 52, "Audi R8 LMS GT4" },
            { 53, "BMW M4 GT4" },
            { 55, "Chevrolet Camaro GT4" },
            { 56, "Ginetta G55 GT4" },
            { 57, "KTM X-Bow GT4" },
            { 58, "Maserati MC GT4" },
            { 59, "McLaren 570S GT4" },
            { 60, "Mercedes AMG GT4" },
            { 61, "Porsche 718 Cayman GT4" },
            { 80, "Audi R8 LMS GT2" },
            { 82, "KTM X-Bow GT2" },
            { 83, "Maserati MC20 GT2" },
            { 84, "Mercedes-AMG GT2" },
            { 85, "Porsche 911 GT2 RS Clubsport Evo" },
            { 86, "Porsche 935" },
        };

        private readonly Stopwatch _lastUpdateStopwatch;

        public FullCarData(CarInfo carInfo)
        {
            _lastUpdateStopwatch = Stopwatch.StartNew();
            UpdateCarInfo(carInfo);
        }

        public string ShortName { get; private set; }

        public string LongName { get; private set; }

        public CarInfo CarInfo { get; private set; }

        public RealtimeCarUpdate LastUpdate { get; private set; }

        public bool CausingYellow { get; set; }

        public string CarName { get; set; }

        public string ClassName { get; private set; }

        public bool IsFinished { get; set; }

        public TimeSpan TimeSinceLastUpdate => _lastUpdateStopwatch.Elapsed;

        public void UpdateRealtimeUpdate(RealtimeCarUpdate realtimeCarUpdate, RealtimeUpdate realtimeUpdate)
        {
            _lastUpdateStopwatch.Restart();
            if (realtimeUpdate.Phase == SessionPhase.FormationLap || realtimeUpdate.Phase == SessionPhase.NONE || realtimeUpdate.Phase == SessionPhase.PreFormation || realtimeUpdate.Phase == SessionPhase.PreSession || realtimeUpdate.Phase == SessionPhase.Session)
            {
                IsFinished = false;
            }

            LastUpdate = realtimeCarUpdate;
            ShortName = CarInfo.Drivers[realtimeCarUpdate.DriverIndex].FullName;
        }

        public void UpdateCarInfo(CarInfo carInfo)
        {
            CarInfo = carInfo;
            LongName = string.Join(", ", carInfo.Drivers.Select(x => x.FullName).Distinct()) + $" (#{carInfo.RaceNumber})";
            ShortName = carInfo.Drivers[carInfo.CurrentDriverIndex].FullName;
            ClassName = carInfo.CarModelType switch
            {
                18 => "Super Trofeo 2018",
                29 => "Super Trofeo 2022",
                27 => "TCX",
                26 => "Ferrari Challenge 2020",
                9 => "Porsche GT3 Cup 2018",
                28 => "Porsche GT3 Cup 2021",
                >=80 => "GT2",
                >=50 => "GT4",
                _ => "GT3",
                    /*switch (carInfo.CupCategory)
                {
                    case 0:
                        ClassName = "GT4 Pro";
                        break;
                    case 1:
                        ClassName = "GT4 Pro-Am";
                        break;
                    case 2:
                        ClassName = "GT4 Am";
                        break;
                    case 3:
                        ClassName = "GT4 Silver";
                        break;
                    case 4:
                        ClassName = "GT4 National";
                        break;
                    default:
                        ClassName = string.Empty;
                        break;
                }*/
            };

            CarName = CarModelsMap.TryGetValue(carInfo.CarModelType, out string carName) ? carName : $"Model {carInfo.CarModelType}";
        }
    }
}