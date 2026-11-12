namespace SecondMonitor.SimdataManagement.SimSettings
{
    using System;

    public class SimSettingsException : Exception
    {
        public SimSettingsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}