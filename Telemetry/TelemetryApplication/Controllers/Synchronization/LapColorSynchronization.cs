namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Synchronization
{
    using System;
    using System.Collections.Generic;

    using DataModel.BasicProperties;

    public class LapColorSynchronization : ILapColorSynchronization
    {
        private readonly Dictionary<string, ColorDto> _lapColors;

        public LapColorSynchronization()
        {
            _lapColors = new Dictionary<string, ColorDto>();
        }

        public event EventHandler<LapColorArgs> LapColorChanged;

        public bool TryGetColorForLap(string lapId, out ColorDto color)
        {
            return _lapColors.TryGetValue(lapId, out color);
        }

        public void SetColorForLap(string lapId, ColorDto color)
        {
            if (_lapColors.TryGetValue(lapId, out ColorDto value) && value.Equals(color))
            {
                return;
            }

            _lapColors[lapId] = color;
            LapColorChanged?.Invoke(this, new LapColorArgs(lapId, color));
        }
    }
}
