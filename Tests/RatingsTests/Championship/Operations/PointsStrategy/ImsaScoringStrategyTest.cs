namespace RemoteTest.Championship.Operations.PointsStrategy
{
    using System.Collections.Generic;
    using System.Linq;

    using NUnit.Framework;

    using SecondMonitor.Rating.Application.Championship.Operations.PointsStrategy;
    using SecondMonitor.Rating.Common.DataModel.Championship;

    [TestFixture]
    public class ImsaScoringStrategyTest
    {
        private ImsaScoringStrategy _testee;
        
        [SetUp]
        public void Setup()
        {
            _testee = new ImsaScoringStrategy();
        }

        [Test]
        public void ApplyScoring_WhenCalled_ScoringIsCorrectlyApplied()
        {
            // Arrange
            List<RepresentantFinishPosition> finishPositions =
            [
                new RepresentantFinishPosition("3", 5),
                new RepresentantFinishPosition("1", 2),
                new RepresentantFinishPosition("2", 3),
                new RepresentantFinishPosition("1", 1),
                new RepresentantFinishPosition("2", 4)
            ];

            ScoringDto scoringDto = new ScoringDto()
            {
                Scoring =
                [
                    10,
                    5,
                ]
            };

            // Act
            List<ParticipantPoints> result = _testee.ApplyScoring(finishPositions, scoringDto);

            // Assert
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result.Single(x => x.Name == "1").Points, Is.EqualTo(10));
            Assert.That(result.Single(x => x.Name == "2").Points, Is.EqualTo(5));
            Assert.That(result.Single(x => x.Name == "3").Points, Is.EqualTo(0));
        }
    }
}
