namespace RemoteTest
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using SecondMonitor.DataModel;
    using SecondMonitor.Rating.Application.Calculator.Glicko;
    using SecondMonitor.Rating.Common.Configuration;
    using SecondMonitor.Rating.Common.DataModel.Player;
    using SecondMonitor.Rating.Common.DataModel.Results;

    public class GlickoCalculatorTests
    {
        private List<MatchResult> _results;
        private DriversRating _playerRating;
        private GlickoCalculator _glickoCalculator;
        private SimulatorRatingConfiguration _simulatorRatingConfiguration;

        [SetUp]
        public void Setup()
        {
            const int deviation = 40;
            _results = new List<MatchResult>()
            {
                new MatchResult(new DriversRating(2641, deviation, 0.06), MatchOutcome.PlayerLost),
                new MatchResult(new DriversRating(2612, deviation, 0.06), MatchOutcome.PlayerLost),
                new MatchResult(new DriversRating(2560, deviation, 0.06), MatchOutcome.PlayerLost),
                new MatchResult(new DriversRating(2574, deviation, 0.06), MatchOutcome.PlayerLost),
                new MatchResult(new DriversRating(2550, deviation, 0.06), MatchOutcome.PlayerWon),
                new MatchResult(new DriversRating(2553, deviation, 0.06), MatchOutcome.PlayerWon),
                new MatchResult(new DriversRating(2520, deviation, 0.06), MatchOutcome.PlayerWon),
                new MatchResult(new DriversRating(2527, deviation, 0.06), MatchOutcome.PlayerWon),
                new MatchResult(new DriversRating(2509, deviation, 0.06), MatchOutcome.PlayerWon),
                new MatchResult(new DriversRating(2502, deviation, 0.06), MatchOutcome.PlayerWon),
                new MatchResult(new DriversRating(2479, deviation, 0.06), MatchOutcome.PlayerWon),
                new MatchResult(new DriversRating(2482, deviation, 0.06), MatchOutcome.PlayerWon),
                new MatchResult(new DriversRating(2440, deviation, 0.06), MatchOutcome.PlayerWon),
                new MatchResult(new DriversRating(2612, deviation, 0.06), MatchOutcome.PlayerLost),
                new MatchResult(new DriversRating(2607, deviation, 0.06), MatchOutcome.PlayerLost),
                new MatchResult(new DriversRating(2580, deviation, 0.06), MatchOutcome.PlayerLost),
                new MatchResult(new DriversRating(2426, deviation, 0.06), MatchOutcome.PlayerWon),
                new MatchResult(new DriversRating(2434, deviation, 0.06), MatchOutcome.PlayerWon),
                new MatchResult(new DriversRating(2395, deviation, 0.06), MatchOutcome.PlayerWon),
                new MatchResult(new DriversRating(2398, deviation, 0.06), MatchOutcome.PlayerWon),
                new MatchResult(new DriversRating(2371, deviation, 0.06), MatchOutcome.PlayerWon),
                new MatchResult(new DriversRating(2373, deviation, 0.06), MatchOutcome.PlayerWon),
                new MatchResult(new DriversRating(2354, deviation, 0.06), MatchOutcome.PlayerWon),
                new MatchResult(new DriversRating(2351, deviation, 0.06), MatchOutcome.PlayerWon),
                new MatchResult(new DriversRating(2331, deviation, 0.06), MatchOutcome.PlayerWon),
            };

            _playerRating = new DriversRating(2639, 29, 0.06);
            _glickoCalculator = new GlickoCalculator();

            _simulatorRatingConfiguration = new SimulatorRatingConfigurationProvider().GetRatingConfiguration(SimulatorsNameMap.R3ESimName);
        }

        [Test]
        [Ignore("Functionality disabled")]
        public void Test1()
        {
            DriversRating newRating1 = _glickoCalculator.CalculateNewRating(_playerRating, _results, _simulatorRatingConfiguration);
            DriversRating newRating2 = _glickoCalculator.CalculateNewRating(_playerRating,   System.Linq.Enumerable.Reverse(_results), _simulatorRatingConfiguration);

            Assert.That(newRating1.Rating, Is.EqualTo(2642));
            Assert.That(newRating1.Rating, Is.EqualTo(newRating2.Rating));
        }
    }
}