namespace SecondMonitor.Test.Connector.LmU
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    using NUnit.Framework;

    using SecondMonitor.LmUConnector.SharedMemory.lmuData;

    [TestFixture]
    public class LmUMarshalTest
    {
        public static IEnumerable<(Type Type, int ExpectedSize)> StructuresWithSize =
        [
            (typeof(LmUVec3), 24),
            (typeof(LmUWheel), 260),
            (typeof(LmUVehicleScoring), 584),
            (typeof(LmUScoringInfo), 548),
            (typeof(LmUApplicationState), 260),
            (typeof(LmUScoring), 126832),
            (typeof(LmUTelemetry), 196356),
            (typeof(LmUPathData), 1300),
            (typeof(LmUEvent), 64),
            (typeof(LmUGeneric), 332),
            (typeof(LmUObjectOut), 324820),
            (typeof(LmULayout), 324820),
        ];

        [TestCaseSource(nameof(StructuresWithSize))]
        public void Marshal_WhenSizeOf_ThenSizeOfStructureIsCorrect((Type Type, int ExpectedSize) typeWithExpectedSize)
        {
            // Arrange

            // Act
            int realSize = Marshal.SizeOf(typeWithExpectedSize.Type);

            // Assert
            Assert.That(realSize, Is.EqualTo(typeWithExpectedSize.ExpectedSize));
        }
    }
}
