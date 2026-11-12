namespace SecondMonitor.Contracts.SimSettings
{
    using System;

    public class DriverCustomColorEnabledArgs : EventArgs
    {
        public DriverCustomColorEnabledArgs(string driverLongName)
        {
            DriverLongName = driverLongName;
        }

        public string DriverLongName { get; }
    }
}