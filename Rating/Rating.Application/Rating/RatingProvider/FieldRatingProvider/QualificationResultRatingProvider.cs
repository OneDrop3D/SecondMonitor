namespace SecondMonitor.Rating.Application.Rating.RatingProvider.FieldRatingProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.DataModel.Player;
    using Controller.SimulatorRating;

    using DataModel.Snapshot.Drivers;
    using DataModel.Summary;
    using NLog;
    using ReferenceRatingProviders;

    using SecondMonitor.DataModel.Extensions;

    public class QualificationResultRatingProvider : IQualificationResultRatingProvider
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ISimulatorRatingController _simulatorRatingController;
        private readonly IReferenceRatingProviderFactory _referenceRatingProviderFactory;
        private readonly int _maxNoise;
        private readonly Random _random;
        private readonly int _minimumRating;
        private TimeSpan _referenceTime;
        private int _referenceRating;

        public QualificationResultRatingProvider(ISimulatorRatingController simulatorRatingController, IReferenceRatingProviderFactory referenceRatingProviderFactory)
        {
            _random = new Random();
            _simulatorRatingController = simulatorRatingController;
            _referenceRatingProviderFactory = referenceRatingProviderFactory;
            _maxNoise = (int)(_simulatorRatingController.RatingPerLevel * _simulatorRatingController.AiRatingNoise / 100);
            _minimumRating = _simulatorRatingController.MinimumAiDifficulty;
        }

        public Dictionary<string, DriversRating> CreateFieldRatingFromQualificationResult(List<Driver> qualificationResult, int difficulty)
        {
            InitializeReferenceTime(qualificationResult);
            InitializeReferenceRating(difficulty);

            Dictionary<string, DriversRating> ratings = new Dictionary<string, DriversRating>();
            int lastRating = 0;
            foreach (Driver driver in qualificationResult.Where(x => x.IsPlayer || (x.BestPersonalLap != null && x.BestPersonalLap.LapTime != TimeSpan.Zero)))
            {
                if (driver.IsPlayer)
                {
                    ratings[driver.DriverId] = _simulatorRatingController.GetPlayerRating(driver.ClassName).DifficultyRating;
                    continue;
                }

                DriverWithoutRating aiDriver = _simulatorRatingController.GetAiRating(driver.DriverId);
                lastRating = ComputeRating(driver.BestPersonalLap.LapTime);
                var newRating = new DriversRating()
                {
                    Deviation = aiDriver.Deviation,
                    Volatility = aiDriver.Volatility,
                    Rating = lastRating
                };
                Logger.Info($"Computed rating for AI {driver.DriverId} is {newRating.Rating}");
                ratings[driver.DriverId] = newRating;
            }

            foreach (Driver driver in qualificationResult.Where(x => !x.IsPlayer && (x.BestPersonalLap == null || x.BestPersonalLap.LapTime == TimeSpan.Zero)))
            {
                DriverWithoutRating aiDriver = _simulatorRatingController.GetAiRating(driver.DriverLongName);
                ratings[driver.DriverId] = new DriversRating()
                {
                    Deviation = aiDriver.Deviation,
                    Volatility = aiDriver.Volatility,
                    Rating = lastRating
                };
            }

            return ratings;
        }

        public Dictionary<string, DriversRating> CreateFieldRating(DriverInfo[] fieldDrivers, int difficulty)
        {
            InitializeReferenceRating(difficulty);
            fieldDrivers = fieldDrivers.OrderBy(x => x.Position).ToArray();
            Dictionary<string, DriversRating> ratings = new Dictionary<string, DriversRating>();
            int middleDriverIndex = _referenceRatingProviderFactory.CreateReferenceRatingProvider().GetReferenceDriverIndex(fieldDrivers.Length);
            int ratingBetweenPlaces = _simulatorRatingController.QuickRaceAiRatingForPlace;
            for (int i = 0; i < fieldDrivers.Length; i++)
            {
                DriverInfo driver = fieldDrivers[i];
                Logger.Info($"Creating basic rating for {driver.DriverSessionId}, of class {driver.CarClassId}");
                if (driver.IsPlayer)
                {
                    ratings[driver.DriverSessionId] = _simulatorRatingController.GetPlayerRating(driver.CarClassName).DifficultyRating;
                    continue;
                }

                DriverWithoutRating aiDriver = _simulatorRatingController.GetAiRating(driver.DriverSessionId);
                int rating = AddNoise(_referenceRating + ((middleDriverIndex - i) * ratingBetweenPlaces));
                ratings[driver.DriverSessionId] = new DriversRating()
                {
                    Deviation = aiDriver.Deviation,
                    Volatility = aiDriver.Volatility,
                    Rating = rating
                };
            }

            return ratings;
        }

        private int ComputeRating(TimeSpan driversTime)
        {
            double percentageDifference = 100 - ((driversTime.TotalSeconds / _referenceTime.TotalSeconds) * 100);
            percentageDifference = Math.Max(percentageDifference, -4);
            double levelsOfDifference = percentageDifference / _simulatorRatingController.AiTimeDifferencePerLevel;
            int rating = AddNoise((int)(_referenceRating + (levelsOfDifference * _simulatorRatingController.RatingPerLevel)));
            return rating;
        }

        private int AddNoise(int rating)
        {
            return rating;
            //return Math.Max(rating + _random.Next(_maxNoise) - (_maxNoise / 2), _minimumRating);
        }

        private void InitializeReferenceRating(int difficulty)
        {
            _referenceRating = AddNoise(_simulatorRatingController.GetRatingForDifficulty(difficulty));
        }

        private void InitializeReferenceTime(List<Driver> drivers)
        {
            _referenceTime = _referenceRatingProviderFactory.CreateReferenceRatingProvider().GetReferenceTime(drivers);
            Logger.Info($"Reference time for rating is: {_referenceTime.FormatToDefault()}");
        }
    }
}