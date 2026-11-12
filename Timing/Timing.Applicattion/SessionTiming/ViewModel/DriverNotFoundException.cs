namespace SecondMonitor.Timing.Application.SessionTiming.ViewModel
{
    using System;

    public class DriverNotFoundException : Exception
    {
        public DriverNotFoundException(string message) : base(message)
        {
        }

        public DriverNotFoundException(string message, Exception cause) : base(message, cause)
        {
        }
    }
}