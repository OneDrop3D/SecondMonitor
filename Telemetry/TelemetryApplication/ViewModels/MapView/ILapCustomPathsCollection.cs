namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.MapView
{
    using System.Collections.Generic;
    using SecondMonitor.ViewModels.Shapes;

    public interface ILapCustomPathsCollection
    {
        string LapId { get; set; }
        bool FullyInitialized { get; set; }

        List<ShiftPointViewModel> ShiftPoints { get; }

        AbstractShapeViewModel BaseLapPath { get; set; }
        void AddBrakingPath(AbstractShapeViewModel path, double intensity);
        IEnumerable<AbstractShapeViewModel> GetAllBrakingPaths();

        void AddClutchPath(AbstractShapeViewModel path, double intensity);
        IEnumerable<AbstractShapeViewModel> GetAllClutchPaths();

        void AddThrottlePath(AbstractShapeViewModel path, double intensity);
        IEnumerable<AbstractShapeViewModel> GetAllThrottlePaths();

        AbstractShapeViewModel[] GetAllPaths();
    }
}