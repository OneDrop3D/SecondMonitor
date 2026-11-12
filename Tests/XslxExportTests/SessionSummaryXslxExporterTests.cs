namespace SecondMonitor.XslxExportTests
{
    using System;
    using System.IO;
    using System.Xml.Serialization;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Summary;

    using NUnit.Framework;

    using DataModel.Snapshot.Drivers;
    using DataModel.Summary.FuelConsumption;
    using DataModel.Telemetry;
    using Newtonsoft.Json;
    using XslxExport;

    [TestFixture]
    public class SessionSummaryXslxExporterTests
    {
        private SessionSummaryXslxExporter _testee;

        private Random _random;

        [SetUp]
        public void Setup()
        {
            _random = new Random();
            _testee = new SessionSummaryXslxExporter() { VelocityUnits = VelocityUnits.Kph };
        }

        /*[Test]
        public void TestExportSummaryLaps()
        {
            // Arrange
            string fileName = @"Laps.xlsx";
            SessionSummary sessionSummary = GetBlankSessionSummary();
            sessionSummary.TotalNumberOfLaps = 20;
            sessionSummary.SessionLengthType = SessionLengthType.Laps;
            AddDrivers(20, sessionSummary);
            sessionSummary.Drivers.ForEach((s) => FillLaps(s, 140, 20));

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            // Act
            _testee.ExportSessionSummary(sessionSummary, fileName);

            // Assert
            Assert.That(File.Exists(fileName), Is.True);
        }*/

        [Test]
        [Ignore("Only run on demand")]
        public void TestExportSummaryLapsStored()
        {
            // Arrange
            string fileName = @"c:\Games\SecondMonitorFiles\Reports\Laps.xlsx";

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(SessionSummary));
            SessionSummary sessionSummary;
            using (TextReader file = File.OpenText(@"c:\Games\SecondMonitorFiles\Reports\Report_2020-06-26T18-05-55_FRX-90Cup_Paul Ricard_Race.xlsx.xml"))
            {
                sessionSummary = xmlSerializer.Deserialize(file) as SessionSummary;
            }

            sessionSummary.FuelConsumptionInformation = new SessionFuelConsumptionDto(string.Empty, string.Empty, 1200, string.Empty, SessionType.Race, 50 * 60, 97200, Volume.FromLiters(101), DateTime.Now, false);

            sessionSummary.PitStops.Add(new PitStopSummary()
            {
                EntryLapNumber = 10,
                FuelTaken = Volume.FromLiters(30),
                IsFrontTyresChanged = true,
                IsRearTyresChanged = true,
                NewFrontCompound = "Prime",
                NewRearCompound = "Prime",
                OverallDuration = TimeSpan.FromSeconds(50.52),
                StallDuration = TimeSpan.FromSeconds(15.25),
                WasDriverThrough = false,
            });

            sessionSummary.PitStops.Add(new PitStopSummary()
            {
                EntryLapNumber = 10,
                FuelTaken = Volume.FromLiters(30),
                IsFrontTyresChanged = true,
                IsRearTyresChanged = true,
                NewFrontCompound = "Prime",
                NewRearCompound = "Prime",
                OverallDuration = TimeSpan.FromSeconds(50.52),
                StallDuration = TimeSpan.FromSeconds(15.25),
                WasDriverThrough = true,
            });

            sessionSummary.Drivers.ForEach(x => x.BindLaps());

            Random random = new Random();
            sessionSummary.Drivers.ForEach(x =>
            {
                x.Laps.ForEach(y =>
                {
                    y.LapTime = TimeSpan.FromSeconds(40 + random.Next(10));
                    y.Sector1 = TimeSpan.FromSeconds(y.LapTime.TotalSeconds / 3);
                    y.Sector2 = TimeSpan.FromSeconds(y.LapTime.TotalSeconds / 3);
                    y.Sector3 = TimeSpan.FromSeconds(y.LapTime.TotalSeconds / 3);
                });
            });

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            // Act
            _testee.ExportSessionSummary(sessionSummary, fileName);

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
            };

            settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            File.WriteAllText(fileName + ".json", JsonConvert.SerializeObject(sessionSummary, settings));

            // Assert
            Assert.That(File.Exists(fileName), Is.True);
            //System.Diagnostics.Process.Start(fileName);
        }

/*

        [Test]
        public void TestExportSummaryTime()
        {
            // Arrange
            string fileName = @"time.xlsx";
            SessionSummary sessionSummary = GetBlankSessionSummary();
            sessionSummary.SessionLength = TimeSpan.FromMinutes(90);
            sessionSummary.SessionLengthType = SessionLengthType.Time;
            AddDrivers(20, sessionSummary);
            sessionSummary.Drivers.ForEach((s) => FillLaps(s, 140, 20));
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            // Act
            _testee.ExportSessionSummary(sessionSummary, fileName);

            // Assert
            Assert.That(File.Exists(fileName), Is.True);
            System.Diagnostics.Process.Start(fileName);
        }

        [Test]
        public void TimespanFormatTest()
        {
            // Arrange
            TimeSpan time = new TimeSpan(0, 0, 12);

            string formatted = time.ToString("mm\\:ss\\.fff");
        }*/

        private void FillLaps(Driver driver, int baseTimeSeconds, int totalLaps)
        {
            if (!driver.Finished)
            {
                totalLaps = _random.Next(1, totalLaps);
                driver.TotalLaps = totalLaps;
            }

            for (int i = 1; i <= totalLaps; i++)
            {
                double sectorBase = (double)baseTimeSeconds / 3;
                double sector1Add = _random.NextDouble() * 10;
                double sector2Add = _random.NextDouble() * 10;
                double sector3Add = _random.NextDouble() * 10;
                driver.Laps.Add(new Lap(driver, true)
                {
                    LapNumber = i,
                    LapTime = TimeSpan.FromSeconds(baseTimeSeconds + sector1Add + sector2Add + sector3Add),
                    Sector1 = TimeSpan.FromSeconds(sectorBase + sector1Add),
                    Sector2 = TimeSpan.FromSeconds(sectorBase + sector2Add),
                    Sector3 = TimeSpan.FromSeconds(sectorBase + sector3Add),
                    LapEndSnapshot = new TelemetrySnapshot(new DriverInfo() { Position = _random.Next(20) }, new WeatherInfo(), new InputInfo(), new SimulatorSourceInfo()),
                    LapStartSnapshot = new TelemetrySnapshot(new DriverInfo() { Position = _random.Next(20) }, new WeatherInfo(), new InputInfo(), new SimulatorSourceInfo()),
                    IsValid = _random.Next(4) != 3
                });
            }
        }

        private void AddDrivers(int driverCount, SessionSummary sessionSummary)
        {
            for (int i = 0; i < driverCount; i++)
            {
                sessionSummary.Drivers.Add(new Driver()
                                                {
                                                    DriverId = "Driver ID",
                                                    CarName = "A caaaar",
                                                    DriverLongName = "Driver " + i,
                                                    TotalLaps = 20,
                                                    TopSpeed = Velocity.FromKph(_random.Next(150, 250)),
                                                    FinishingPosition = i + 1,
                                                    Finished = _random.Next(0, 10) > 0 ? true : false,
                                                    IsPlayer = i == 0
                                                });
            }
        }

        private static SessionSummary GetBlankSessionSummary()
        {
            return new SessionSummary()
                       {
                           SessionRunTime = DateTime.Now,
                           SessionType = SessionType.Race,
                           Simulator = "R3E",
                           TrackInfo = new TrackInfo()
                                           {
                                               LayoutLength = Distance.FromMeters(2000),
                                               TrackLayoutName = "Grand Prix",
                                               TrackName = "Red bull Ring"
                                           }
                       };
        }
    }
}