#nullable enable
namespace SecondMonitor.Contracts.Session
{
    using System;

    using SecondMonitor.DataModel.Snapshot.Drivers;

    public class ReturnPosition
    {
        public ReturnPosition(int position, DriverInfo? driverInFront, DriverInfo? driverBehind, TimeSpan? gapAhead, TimeSpan? gapBehind)
        {
            Position = position;
            DriverInFront = driverInFront;
            DriverBehind = driverBehind;
            GapAhead = gapAhead;
            GapBehind = gapBehind;
        }

        public static ReturnPosition Empty { get; } = new ReturnPosition(0, null, null, null, null);

        public int Position { get; }

        public DriverInfo? DriverInFront { get; }

        public DriverInfo? DriverBehind { get; }

        public TimeSpan? GapAhead { get; }

        public TimeSpan? GapBehind { get; }
    }
}
