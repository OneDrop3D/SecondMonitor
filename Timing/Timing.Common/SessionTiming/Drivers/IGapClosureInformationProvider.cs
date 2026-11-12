namespace SecondMonitor.Timing.Common.SessionTiming.Drivers
{
    public interface IGapClosureInformationProvider
    {
        public int? LapsToClose { get; }
        void GenerateClosureInformation(DriverTiming driverTiming);
    }
}