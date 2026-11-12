namespace SecondMonitor.Timing.Api.Data
{
    using SecondMonitor.DataModel.BasicProperties;

    public class Point3DData
    {
        public Point3DData(Point3D point3D)
        {
            XinM = point3D.X.InMeters;
            YinM = point3D.Y.InMeters;
            ZinM = point3D.Z.InMeters;
        }

        public double XinM { get; }

        public double YinM { get; }

        public double ZinM { get; }
    }
}
