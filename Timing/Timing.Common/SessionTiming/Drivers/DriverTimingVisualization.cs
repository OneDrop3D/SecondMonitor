namespace SecondMonitor.Timing.Common.SessionTiming.Drivers
{
    using System;

    public class DriverTimingVisualization
    {
        public bool IsMarkerBeforeEnabled { get; private set;  }
        public int MarkerBeforeSize { get; private set; }
        public bool IsMarkerAfterEnabled { get; private set; }
        public int MarkerAfterSize { get; private set; }
        public bool ForceRelativeGapTime { get; set; }
        public DriverPositionChange DriverPositionChange { get; set; }
        public TimeSpan DriverPositionResetSessionTime { get; set; }

        public void ClearMarkers()
        {
            IsMarkerBeforeEnabled = false;
            IsMarkerAfterEnabled = false;
        }

        public void SetMarkerBefore(int size)
        {
            IsMarkerBeforeEnabled = true;
            MarkerBeforeSize = size;
        }

        public void SetMarkerAfter(int size)
        {
            IsMarkerAfterEnabled = true;
            MarkerAfterSize = size;
        }

        public void Reset()
        {
            ClearMarkers();
        }
    }
}