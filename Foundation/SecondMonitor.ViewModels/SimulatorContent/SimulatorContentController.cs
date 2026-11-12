namespace SecondMonitor.ViewModels.SimulatorContent
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using DataModel;
    using DataModel.SimulatorContent;
    using DataModel.Snapshot;

    public class SimulatorContentController : ISimulatorContentController, ISimulatorContentProvider
    {
        private readonly ISimulatorContentRepository _simulatorContentRepository;
        private readonly Random _random;
        private SimulatorsContent _simulatorsContent;
        private Stopwatch _stopwatch;

        private SimulatorContent _currentSimulatorContent;
        private string _lastCar;
        private string _lastTrack;

        public SimulatorContentController(ISimulatorContentRepository simulatorContentRepository)
        {
            _random = new Random();
            _simulatorContentRepository = simulatorContentRepository;
            _lastCar = string.Empty;
            _lastTrack = string.Empty;
        }

        public Task StartControllerAsync()
        {
            _simulatorsContent = _simulatorContentRepository.LoadOrCreateSimulatorsContent();
            _stopwatch = Stopwatch.StartNew();
            return Task.CompletedTask;
        }

        public Task StopControllerAsync()
        {
            _simulatorContentRepository.SaveSimulatorContent(_simulatorsContent);
            return Task.CompletedTask;
        }

        public void Visit(SimulatorDataSet simulatorDataSet)
        {
            if (_stopwatch == null || _stopwatch.ElapsedMilliseconds < 10000 || _simulatorsContent == null)
            {
                return;
            }

            _stopwatch.Restart();

            if (simulatorDataSet.PlayerInfo?.CarInfo == null || string.IsNullOrEmpty(simulatorDataSet.Source) || SimulatorsNameMap.IsNotConnected(simulatorDataSet.Source))
            {
                return;
            }

            if (_currentSimulatorContent == null || _currentSimulatorContent.SimulatorName != simulatorDataSet.Source)
            {
                SwitchSimulatorContent(simulatorDataSet.Source);
            }

            if (_currentSimulatorContent == null)
            {
                return;
            }

            if (_lastCar != simulatorDataSet.PlayerInfo.CarName && !string.IsNullOrEmpty(simulatorDataSet.PlayerInfo.CarName))
            {
                _currentSimulatorContent.AddCar(simulatorDataSet.PlayerInfo.CarName, simulatorDataSet.PlayerInfo.CarClassName);
                _lastCar = simulatorDataSet.PlayerInfo.CarName;
            }

            if (_lastTrack != simulatorDataSet.SessionInfo.TrackInfo.TrackFullName && !string.IsNullOrEmpty(simulatorDataSet.SessionInfo.TrackInfo.TrackFullName) && simulatorDataSet.SessionInfo.TrackInfo.LayoutLength.InMeters > 0)
            {
                _currentSimulatorContent.AddTrack(simulatorDataSet.SessionInfo.TrackInfo.TrackFullName, simulatorDataSet.SessionInfo.TrackInfo.LayoutLength.InMeters);
                _lastTrack = simulatorDataSet.SessionInfo.TrackInfo.TrackFullName;
            }
        }

        private void SwitchSimulatorContent(string simulatorName)
        {
            _currentSimulatorContent = _simulatorsContent.GetOrCreateSimulatorContent(simulatorName);
            _currentSimulatorContent.Tracks.RemoveAll(x => x.LapDistance == 0);
            _lastCar = string.Empty;
            _lastTrack = string.Empty;
        }

        public void Reset()
        {
        }

        public IReadOnlyList<Track> GetAllTracksForSimulator(string simulatorName)
        {
            return _simulatorsContent == null ? Enumerable.Empty<Track>().ToList() : _simulatorsContent.GetOrCreateSimulatorContent(simulatorName).Tracks;
        }

        public IReadOnlyList<Car> GetAllCarsForSimulator(string simulatorName)
        {
            return _simulatorsContent == null ? new List<Car>() : _simulatorsContent.GetOrCreateSimulatorContent(simulatorName).Classes.SelectMany(x => x.Cars).ToList();
        }

        public IReadOnlyList<Car> GetAllCarsForSimulator(string simulatorName, string carClass)
        {
            return _simulatorsContent == null ? new List<Car>() : _simulatorsContent.GetOrCreateSimulatorContent(simulatorName).GetCarClass(carClass).Cars.ToList();
        }

        public Track GetRandomTrack(string simulatorName) => GetRandomTrack(simulatorName, []);

        public Track GetRandomTrack(string simulatorName, List<string> tracksToIgnore)
        {
            if (_simulatorsContent == null)
            {
                return null;
            }

            List<Track> simulatorTracks = _simulatorsContent.GetOrCreateSimulatorContent(simulatorName).Tracks.Where(x => !tracksToIgnore.Contains(x.Name)).ToList();
            if (simulatorTracks.Count == 0)
            {
                simulatorTracks = _simulatorsContent.GetOrCreateSimulatorContent(simulatorName).Tracks;
            }

            return simulatorTracks.Count > 0 ? simulatorTracks[_random.Next(simulatorTracks.Count)] : null;
        }

        public CarClass GetRandomClass(string simulatorName)
        {
            if (_simulatorsContent == null)
            {
                return null;
            }

            var simulatorClasses = _simulatorsContent.GetOrCreateSimulatorContent(simulatorName).Classes;
            return simulatorClasses.Count > 0 ? simulatorClasses[_random.Next(simulatorClasses.Count)] : null;
        }

        public bool TryGetTrack(string simulator, string trackName, out Track track)
        {
            if (_simulatorsContent == null)
            {
                track = null;
                return false;
            }

            return _simulatorsContent.GetOrCreateSimulatorContent(simulator).TryGetTrack(trackName, out track);
        }

        public IReadOnlyList<CarClass> GetAllCarClassesForSimulator(string simulatorName)
        {
            return _simulatorsContent == null ? Enumerable.Empty<CarClass>().ToList() : _simulatorsContent.GetOrCreateSimulatorContent(simulatorName).Classes;
        }

        public IReadOnlyList<string> GetSimulatorsWithContent()
        {
            return _simulatorsContent.SimulatorContents.Where(x => x.Tracks.Count > 0 && x.Classes.Count > 0).Select(x => x.SimulatorName).ToList();
        }
    }
}