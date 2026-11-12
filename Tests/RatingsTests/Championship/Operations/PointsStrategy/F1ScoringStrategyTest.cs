namespace RemoteTest.Championship.Operations.PointsStrategy
{
    using System.Collections.Generic;
    using System.Linq;

    using NUnit.Framework;

    using SecondMonitor.Rating.Application.Championship.Operations.PointsStrategy;
    using SecondMonitor.Rating.Common.DataModel.Championship;

    [TestFixture]
    public class F1ScoringStrategyTest
    {
        private F1ScoringStrategy _testee;
        
        [SetUp]
        public void Setup()
        {
            _testee = new F1ScoringStrategy();
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
                    1,
                ]
            };

            // Act
            List<ParticipantPoints> result = _testee.ApplyScoring(finishPositions, scoringDto);

            // Assert
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result.Single(x => x.Name == "1").Points, Is.EqualTo(15));
            Assert.That(result.Single(x => x.Name == "2").Points, Is.EqualTo(1));
            Assert.That(result.Single(x => x.Name == "3").Points, Is.EqualTo(0));
        }
    }
}
