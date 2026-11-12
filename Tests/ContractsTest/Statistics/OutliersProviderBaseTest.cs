namespace SecondMonitor.ContractsTest.Statistics
{
    using System.Collections.Generic;

    using NUnit.Framework;
    using NUnit.Framework.Legacy;

    using SecondMonitor.Contracts.Statistics;

    [TestFixture]
    public class OutliersProviderBaseTest
    {
        private OutliersProviderStub _testee;

        [SetUp]
        public void Setup()
        {
            _testee = new OutliersProviderStub();
        }

        public static List<OutliersTestItem> OutliersTestItems => new List<OutliersTestItem>()
        {
            new OutliersTestItem(new List<double>() { 1, 5, 2, 3, 45, 4.5, 5, 8, 9, 10, 2, 3 }, new List<double>() { 45 }),
            new OutliersTestItem(new List<double>() { 45, 44, 43, 42, 45, 43, 44, 43, 90, 100, 32, 10, }, new List<double>() { 10, 32, 90, 100 })
        };

        [TestCaseSource(nameof(OutliersTestItems))]
        public void Test(OutliersTestItem outliersTestItem)
        {
            // Arrange
            IList<double> values = outliersTestItem.Values;

            // Act
            IList<double> outliers = _testee.GetOutliers(values);

            // Assert
            CollectionAssert.AreEquivalent(outliers, outliersTestItem.ExpectedOutliers);
        }

        public class OutliersTestItem
        {
            public OutliersTestItem(IList<double> values, IList<double> expectedOutliers)
            {
                Values = values;
                ExpectedOutliers = expectedOutliers;
            }

            public IList<double> Values { get; }

            public IList<double> ExpectedOutliers { get; }
        }

        private class OutliersProviderStub : OutliersProviderBase<double>
        {
            protected override double GetValue(double element)
            {
                return element;
            }
        }
    }
}
