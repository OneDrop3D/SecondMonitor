namespace SecondMonitor.DataModelTests
{
    using System;

    using NUnit.Framework;

    using SecondMonitor.DataModel.Extensions;

    [TestFixture]
    public class DateTimeExtensionTest
    {
        [TestCase(DayOfWeek.Friday)]
        [TestCase(DayOfWeek.Monday)]
        [TestCase(DayOfWeek.Saturday)]
        [TestCase(DayOfWeek.Sunday)]
        [TestCase(DayOfWeek.Thursday)]
        [TestCase(DayOfWeek.Tuesday)]
        [TestCase(DayOfWeek.Wednesday)]
        public void Next_WhenDayOfWeekGiven_ThenCorrectDateTimeReturned(DayOfWeek dayOfWeek)
        {
            // Arrange
            DateTime testee = new DateTime(2022, 11, 1);

            // Act
            DateTime returnValue = testee.Next(dayOfWeek);

            // Assert
            Assert.That(returnValue.DayOfWeek, Is.EqualTo(dayOfWeek));
            Assert.That((returnValue - testee).TotalDays <= 7, Is.True);
        }

        [TestCase(DayOfWeek.Friday)]
        [TestCase(DayOfWeek.Monday)]
        [TestCase(DayOfWeek.Saturday)]
        [TestCase(DayOfWeek.Sunday)]
        [TestCase(DayOfWeek.Thursday)]
        [TestCase(DayOfWeek.Tuesday)]
        [TestCase(DayOfWeek.Wednesday)]
        public void Previous_WhenDayOfWeekGiven_ThenCorrectDateTimeReturned(DayOfWeek dayOfWeek)
        {
            // Arrange
            DateTime testee = new DateTime(2022, 11, 1);

            // Act
            DateTime returnValue = testee.Previous(dayOfWeek);

            // Assert
            Assert.That(returnValue.DayOfWeek, Is.EqualTo(dayOfWeek));
            Assert.That((returnValue - testee).TotalDays >= -7, Is.True);
        }
    }
}