namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.MapView
{
    using System;
    using System.Windows;

    public class PointsAreaArgs : EventArgs
    {
        public PointsAreaArgs(Point point1, Point point2)
        {
            Point1 = point1;
            Point2 = point2;
        }

        public Point Point1 { get; }

        public Point Point2 { get; }
    }
}