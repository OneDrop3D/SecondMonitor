namespace SecondMonitor.Timing.Application.LapTimings.ViewModel
{
    using System.Windows;
    using Common.SessionTiming.Drivers;

    internal class DriverLapWindowCacheItem
    {
        public DriverLapWindowCacheItem(Window window, DriverTiming driverTiming, DriverLapsViewModel driverLapsViewModel)
        {
            Window = window;
            DriverTiming = driverTiming;
            DriverLapsViewModel = driverLapsViewModel;
        }

        public Window Window { get; }
        public DriverTiming DriverTiming { get; set; }
        public DriverLapsViewModel DriverLapsViewModel { get; }
    }
}