namespace SecondMonitor.Test.Connector.F12021
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using F12021Connector;
    using F12021Connector.DataModels;
    using NUnit.Framework;
    using Assert = NUnit.Framework.Assert;

    [TestFixture]
    public class F12021MarshalTest
    {
        public static IEnumerable<(Type Type, int ExpectedSize)> StructuresWithSize = new[]
        {
            (typeof(PacketMotionData), 1464),
            (typeof(PacketSessionData), 625),
            (typeof(PacketLapData), 970),
            (typeof(PacketEventData), 36),
            (typeof(PacketParticipantsData), 1257),
            (typeof(PacketCarSetupData), 1102),
            (typeof(PacketCarTelemetryData), 1347),
            (typeof(PacketCarStatusData), 1058),
            (typeof(PacketCarDamageData), 882),
            (typeof(PacketSessionHistoryData), 1155),
        };

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

        [TestCase(43, "Campos Vexatec Racing")]
        [TestCase(9, "Alfa Romeo")]
        [TestCase(74, "DAMS")]
        [TestCase(93, "McLaren")]
        public void TranslationTable_WhenIndexGiven_NameOfTeamIsCorrect(int index, string expectedTeamName)
        {
            // Arrange & Act
            string teamName = TranslationTable.GetCarName(index);

            // Assert
            Assert.That(expectedTeamName, Is.EqualTo(teamName));
        }
    }
}
