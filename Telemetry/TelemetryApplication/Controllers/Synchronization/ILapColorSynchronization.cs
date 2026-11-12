namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Synchronization
{
    using System;
    using DataModel.BasicProperties;

    public interface ILapColorSynchronization
    {
        event EventHandler<LapColorArgs> LapColorChanged;

        bool TryGetColorForLap(string lapId, out ColorDto color);
        void SetColorForLap(string lapId, ColorDto color);
    }
}