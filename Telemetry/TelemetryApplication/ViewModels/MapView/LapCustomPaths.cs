namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.MapView
{
    using System.Collections.Generic;
    using System.Linq;
    using SecondMonitor.ViewModels.Shapes;

    public class LapCustomPathsCollection : ILapCustomPathsCollection
    {
        private readonly Dictionary<double, AbstractShapeViewModel> _brakingPaths;
        private readonly Dictionary<double, AbstractShapeViewModel> _throttlePaths;
        private readonly Dictionary<double, AbstractShapeViewModel> _clutchPaths;

        public LapCustomPathsCollection()
        {
            _brakingPaths = new Dictionary<double, AbstractShapeViewModel>();
            _throttlePaths = new Dictionary<double, AbstractShapeViewModel>();
            _clutchPaths = new Dictionary<double, AbstractShapeViewModel>();
            ShiftPoints = new List<ShiftPointViewModel>();
        }

        public string LapId { get; set; }

        public bool FullyInitialized { get; set; }

        public List<ShiftPointViewModel> ShiftPoints { get; }

        public AbstractShapeViewModel BaseLapPath { get; set; }

        public void AddBrakingPath(AbstractShapeViewModel path, double intensity)
        {
            _brakingPaths[intensity] = path;
        }

        public IEnumerable<AbstractShapeViewModel> GetAllBrakingPaths() => _brakingPaths.Values;

        public void AddClutchPath(AbstractShapeViewModel path, double intensity)
        {
            _clutchPaths[intensity] = path;
        }

        public IEnumerable<AbstractShapeViewModel> GetAllClutchPaths() => _clutchPaths.Values;

        public void AddThrottlePath(AbstractShapeViewModel path, double intensity)
        {
            _throttlePaths[intensity] = path;
        }

        public IEnumerable<AbstractShapeViewModel> GetAllThrottlePaths() => _throttlePaths.Values;

        public AbstractShapeViewModel[] GetAllPaths()
        {
            return _brakingPaths.Values.Concat(_throttlePaths.Values.Concat(_clutchPaths.Values).Append(BaseLapPath)).Concat(ShiftPoints).ToArray();
        }
    }
}