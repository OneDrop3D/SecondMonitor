namespace SecondMonitor.Rating.Common.Configuration
{
    using System.Linq;

    using SecondMonitor.DataModel;

    public class SimulatorRatingConfigurationProvider : ISimulatorRatingConfigurationProvider
    {
        private readonly SimulatorsRatingConfiguration _simulatorsRatingConfiguration;

        public SimulatorRatingConfigurationProvider()
        {
            _simulatorsRatingConfiguration = new SimulatorsRatingConfiguration()
            {
                SimulatorRatingConfigurations = new[]
                {
                    new SimulatorRatingConfiguration()
                    {
                        SimulatorName = SimulatorsNameMap.R3ESimName,
                        MinimumAiLevel = 80,
                        MaximumAiLevel = 120,
                        RatingPerLevel = 100,
                        DefaultPlayerRating = 1500,
                        DefaultPlayerDeviation = 350,
                        DefaultPlayerVolatility = 0.06,
                        MinimumRating = 100,
                        AiTimeDifferencePerLevel = 0.50,
                        AiRatingNoise = 25,
                        QuickRaceAiRatingForPlace = 10,
                    },
                    new SimulatorRatingConfiguration()
                    {
                        SimulatorName = "Assetto Corsa",
                        MinimumAiLevel = 70,
                        MaximumAiLevel = 100,
                        RatingPerLevel = 100,
                        DefaultPlayerRating = 1500,
                        DefaultPlayerDeviation = 350,
                        DefaultPlayerVolatility = 0.06,
                        MinimumRating = 100,
                        AiTimeDifferencePerLevel = 1,
                        AiRatingNoise = 25,
                        QuickRaceAiRatingForPlace = 20,
                    },
                    new SimulatorRatingConfiguration()
                    {
                        SimulatorName = SimulatorsNameMap.AmsSimName,
                        MinimumAiLevel = 70,
                        MaximumAiLevel = 120,
                        RatingPerLevel = 100,
                        DefaultPlayerRating = 1500,
                        DefaultPlayerDeviation = 350,
                        DefaultPlayerVolatility = 0.06,
                        MinimumRating = 100,
                        AiTimeDifferencePerLevel = 0.75,
                        AiRatingNoise = 25,
                        QuickRaceAiRatingForPlace = 10,
                    },
                    new SimulatorRatingConfiguration()
                    {
                        SimulatorName = SimulatorsNameMap.Gtr2SimName,
                        MinimumAiLevel = 70,
                        MaximumAiLevel = 120,
                        RatingPerLevel = 100,
                        DefaultPlayerRating = 1500,
                        DefaultPlayerDeviation = 350,
                        DefaultPlayerVolatility = 0.06,
                        MinimumRating = 100,
                        AiTimeDifferencePerLevel = 1,
                        AiRatingNoise = 25,
                        QuickRaceAiRatingForPlace = 10,
                    },
                    new SimulatorRatingConfiguration()
                    {
                        SimulatorName = SimulatorsNameMap.Rf2SimName,
                        MinimumAiLevel = 70,
                        MaximumAiLevel = 120,
                        RatingPerLevel = 100,
                        DefaultPlayerRating = 1500,
                        DefaultPlayerDeviation = 350,
                        DefaultPlayerVolatility = 0.06,
                        MinimumRating = 100,
                        AiTimeDifferencePerLevel = 0.75,
                        AiRatingNoise = 25,
                        QuickRaceAiRatingForPlace = 10,
                    },
                    new SimulatorRatingConfiguration()
                    {
                        SimulatorName = SimulatorsNameMap.PCars2SimName,
                        MinimumAiLevel = 60,
                        MaximumAiLevel = 120,
                        RatingPerLevel = 100,
                        DefaultPlayerRating = 1500,
                        DefaultPlayerDeviation = 350,
                        DefaultPlayerVolatility = 0.06,
                        MinimumRating = 100,
                        AiTimeDifferencePerLevel = 0.75,
                        AiRatingNoise = 25,
                        QuickRaceAiRatingForPlace = 20,
                    },

                    new SimulatorRatingConfiguration()
                    {
                        SimulatorName = SimulatorsNameMap.PCarsSimName,
                        MinimumAiLevel = 40,
                        MaximumAiLevel = 100,
                        RatingPerLevel = 100,
                        DefaultPlayerRating = 2500,
                        DefaultPlayerDeviation = 350,
                        DefaultPlayerVolatility = 0.06,
                        MinimumRating = 100,
                        AiTimeDifferencePerLevel = 0.75,
                        AiRatingNoise = 25,
                        QuickRaceAiRatingForPlace = 20,
                    },

                    new SimulatorRatingConfiguration()
                    {
                        SimulatorName = SimulatorsNameMap.Ams2SimName,
                        MinimumAiLevel = 60,
                        MaximumAiLevel = 120,
                        RatingPerLevel = 100,
                        DefaultPlayerRating = 1500,
                        DefaultPlayerDeviation = 350,
                        DefaultPlayerVolatility = 0.06,
                        MinimumRating = 100,
                        AiTimeDifferencePerLevel = 0.50,
                        AiRatingNoise = 25,
                        QuickRaceAiRatingForPlace = 20,
                    },

                    new SimulatorRatingConfiguration()
                    {
                        SimulatorName = SimulatorsNameMap.F12019,
                        MinimumAiLevel = 30,
                        MaximumAiLevel = 110,
                        RatingPerLevel = 80,
                        DefaultPlayerRating = 4000,
                        DefaultPlayerDeviation = 350,
                        DefaultPlayerVolatility = 0.06,
                        MinimumRating = 100,
                        AiTimeDifferencePerLevel = 0.75,
                        AiRatingNoise = 25,
                        QuickRaceAiRatingForPlace = 20,
                    },

                    new SimulatorRatingConfiguration()
                    {
                        SimulatorName = SimulatorsNameMap.F12024,
                        MinimumAiLevel = 30,
                        MaximumAiLevel = 110,
                        RatingPerLevel = 80,
                        DefaultPlayerRating = 4000,
                        DefaultPlayerDeviation = 350,
                        DefaultPlayerVolatility = 0.06,
                        MinimumRating = 100,
                        AiTimeDifferencePerLevel = 0.75,
                        AiRatingNoise = 25,
                        QuickRaceAiRatingForPlace = 20,
                    },

                    new SimulatorRatingConfiguration()
                    {
                        SimulatorName = SimulatorsNameMap.AccSimName,
                        MinimumAiLevel = 80,
                        MaximumAiLevel = 100,
                        RatingPerLevel = 200,
                        DefaultPlayerRating = 2000,
                        DefaultPlayerDeviation = 50,
                        DefaultPlayerVolatility = 0.06,
                        MinimumRating = 200,
                        AiTimeDifferencePerLevel = 1,
                        AiRatingNoise = 25,
                        QuickRaceAiRatingForPlace = 30,
                    },
                    new SimulatorRatingConfiguration()
                    {
                        SimulatorName = SimulatorsNameMap.LmUSimName,
                        MinimumAiLevel = 70,
                        MaximumAiLevel = 120,
                        RatingPerLevel = 100,
                        DefaultPlayerRating = 1500,
                        DefaultPlayerDeviation = 350,
                        DefaultPlayerVolatility = 0.06,
                        MinimumRating = 100,
                        AiTimeDifferencePerLevel = 0.75,
                        AiRatingNoise = 25,
                        QuickRaceAiRatingForPlace = 10,
                    },
                }
            };
        }

        public SimulatorRatingConfiguration GetRatingConfiguration(string simulatorName)
        {
            return _simulatorsRatingConfiguration.SimulatorRatingConfigurations.First(x => x.SimulatorName == simulatorName);
        }
    }
}
