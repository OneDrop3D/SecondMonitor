namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.MapView
{
    using DataModel.BasicProperties;
    using SecondMonitor.ViewModels.Shapes;

    public class ShiftPointViewModel : AbstractShapeViewModel
    {
        public ShiftPointViewModel(double x, double y, string gear, ColorDto color) : base(color)
        {
            X = x;
            Y = y;
            Gear = gear;
        }

        public double X { get; }
        public double Y { get; }
        public string Gear { get; }
    }
}