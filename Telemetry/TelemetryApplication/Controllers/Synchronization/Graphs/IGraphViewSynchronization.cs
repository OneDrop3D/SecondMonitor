namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Synchronization.Graphs
{
    using System;

    public interface IGraphViewSynchronization
    {
        event EventHandler<PanEventArgs> PanChanged;
        event EventHandler XAxisReset;
        event EventHandler<WheelVisibilityArgs> WheelVisibilityChanged;
        event EventHandler<TyreTempVisibilityArgs> TyreTempVisibilityChanged;
        event EventHandler<EventArgs> GraphSettingsChanged;
        event EventHandler<DistanceEventArgs> OnDistanceSelectionRequested;
        event EventHandler<TimeEventArgs> OnTimeSelectionRequested;

        void NotifyXAxisReset(object sender);
        void NotifyPanChanged(object sender, double minimum, double maximum);
        void NotifyWheelVisibilityChanged(object sender, bool frontLeftVisible, bool frontRightVisible, bool rearLeftVisible, bool rearRightVisible);
        void NotifyTyreTempVisibilityChanged(object sender, bool leftTempVisible, bool middleTempVisible, bool rightTempVisible, bool coreTempVisible);
        void NotifyGraphSettingsChanged(object sender);

        void RequestDistanceSelection(double distance);

        void RequestDistanceSelectionForAll(double distance);

        void RequestTimeSelectionForAll(TimeSpan time);
    }
}