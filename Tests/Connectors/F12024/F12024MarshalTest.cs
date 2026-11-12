namespace SecondMonitor.Test.Connector.F12024
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    using NUnit.Framework;

    using SecondMonitor.F12024Connector;
    using SecondMonitor.F12024Connector.DataModels;

    using Assert = NUnit.Framework.Assert;

    [TestFixture]
    public class F12024MarshalTest
    {
        public static IEnumerable<(Type Type, int ExpectedSize)> StructuresWithSize =
        [
            (typeof(PacketMotionData), 1349),
            (typeof(PacketSessionData), 753),
            (typeof(PacketLapData), 1285),
            (typeof(PacketEventData), 45),
            (typeof(PacketParticipantsData), 1350),
            (typeof(PacketCarSetupData), 1133),
            (typeof(PacketCarTelemetryData), 1352),
            (typeof(PacketCarStatusData), 1239),
            (typeof(PacketFinalClassificationData), 1020),
            (typeof(PacketLobbyInfoData), 1306),
            (typeof(PacketCarDamageData), 953),
            (typeof(PacketSessionHistoryData), 1460),
            (typeof(PacketTyreSetData), 231),
            (typeof(PacketMotionExData), 237),
            (typeof(PacketTimeTrialData), 101)
        ];

        [Test]
        [TestCaseSource(nameof(StructuresWithSize))]
        public void Marshal_WhenSizeOf_ThenSizeOfStructureIsCorrect((Type Type, int ExpectedSize) typeWithExpectedSize)
        {
            // Arrange

            // Act
            int realSize = Marshal.SizeOf(typeWithExpectedSize.Type);

            // Assert
            Assert.That(realSize, Is.EqualTo(typeWithExpectedSize.ExpectedSize));
        }

        [TestCase(41, "F1 Generic")]
        [TestCase(104, "F1 Custom Team")]
        [TestCase(144, "Campos Racing")]
        [TestCase(158, "Art GP")]
        [TestCase(3, "Williams")]
        public void TranslationTable_WhenIndexGiven_NameOfTeamIsCorrect(int index, string expectedTeamName)
        {
            // Arrange & Act
            string teamName = TranslationTable.GetTeamName(index);

            // Assert
            Assert.That(expectedTeamName, Is.EqualTo(teamName));
        }

        [TestCase(41, "F1 Generic")]
        [TestCase(104, "F1 Custom Team")]
        [TestCase(144, "Dallara F2")]
        [TestCase(3, "Williams FW46")]
        public void TranslationTable_WhenIndexGiven_NameOfCarIsCorrect(int index, string expectedCarName)
        {
            // Arrange & Act
            string carName = TranslationTable.GetCarName(index);

            // Assert
            Assert.That(carName, Is.EqualTo(expectedCarName));
        }
    }
}
