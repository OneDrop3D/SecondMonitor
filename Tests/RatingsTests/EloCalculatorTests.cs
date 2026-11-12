namespace RemoteTest
{
    using System.Collections.Generic;
    using Moq;
    using NUnit.Framework;
    using SecondMonitor.DataModel;
    using SecondMonitor.Rating.Application.Calculator.Elo;
    using SecondMonitor.Rating.Common.Configuration;
    using SecondMonitor.Rating.Common.DataModel.Player;
    using SecondMonitor.Rating.Common.DataModel.Results;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;
    using SecondMonitor.ViewModels.Settings.ViewModel.Overrides;

    [TestFixture]
    public class EloCalculatorTests
    {
        private List<MatchResult> _results;
        private DriversRating _playerRating;
        private EloCalculator _eloCalculator;
        private SimulatorRatingConfiguration _simulatorRatingConfiguration;
        private Mock<ISettingsProvider> _settingsProviderMock;

        [SetUp]
        public void Setup()
        {
            const int deviation = 40;
            _settingsProviderMock = new Mock<ISettingsProvider>();
            RatingSettingsViewModel ratingSettings = new RatingSettingsViewModel
            {
                EloK = 16,
                AiCompetitiveRange = 2.2
            };

            DisplaySettingsViewModel displaySettings = new DisplaySettingsViewModel(Mock.Of<IViewModelFactory>(), new List<IUomOverride>())
            {
                RatingSettingsViewModel = ratingSettings,
            };

            _settingsProviderMock.Setup(x => x.DisplaySettingsViewModel).Returns(displaySettings);

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
            _eloCalculator = new EloCalculator(_settingsProviderMock.Object);
            _simulatorRatingConfiguration = new SimulatorRatingConfigurationProvider().GetRatingConfiguration(SimulatorsNameMap.R3ESimName);
        }

        [Test]
        public void Test2()
        {
            DriversRating player2Rating = new DriversRating(2539, 29, 0.06);
            List<MatchResult> results = new List<MatchResult>() { new MatchResult(player2Rating, MatchOutcome.PlayerLost) };
            DriversRating newRating1 = _eloCalculator.CalculateNewRating(_playerRating,   System.Linq.Enumerable.Reverse(results), _simulatorRatingConfiguration);

            /*Assert.That(newRating1.Rating, Is.EqualTo(2642));*/
        }

        [Test]
        public void CalculateNewRating_WhenResultsReversed_ThenComputedRatingDoesntChange()
        {
            DriversRating newRating1 = _eloCalculator.CalculateNewRating(_playerRating, _results, _simulatorRatingConfiguration);
            DriversRating newRating2 = _eloCalculator.CalculateNewRating(_playerRating, System.Linq.Enumerable.Reverse(_results), _simulatorRatingConfiguration);

            /*Assert.That(newRating1.Rating, Is.EqualTo(2642));*/
            Assert.That(newRating1.Rating, Is.EqualTo(newRating2.Rating));
        }

        [Test]
        public void Test3()
        {
            double result = EloCalculator.ExpectationToWin(1600, 1500, 100 * 2) * 100;
        }
    }
}