namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.MapView
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Media;
    using Contracts.SelectionCounter;
    using Contracts.Session;
    using Controllers.Synchronization;
    using DataModel.BasicProperties;
    using DataModel.Snapshot.Drivers;
    using DataModel.Telemetry;
    using DataModel.TrackMap;
    using LoadedLapCache;
    using NLog;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Colors;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Shapes;
    using SimdataManagement;
    using TelemetryManagement.DTO;

    public class MapViewViewModel : AbstractViewModel, IMapViewViewModel, ISessionInformationProvider
    {
        private const int PedalStep = 25;
        private const double SelectionLength = 5;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IViewModelFactory _viewModelFactory;
        private readonly ILoadedLapsCache _loadedLapsCache;
        private readonly IDataPointSelectionSynchronization _dataPointSelectionSynchronization;
        private readonly ILapColorSynchronization _lapColorSynchronization;
        private readonly Dictionary<string, ILapCustomPathsCollection> _lapsPaths;
        private readonly Dictionary<int, SelectionItemCounter<AbstractShapeViewModel>> _selectionPaths;

        private readonly ResourceDictionary _commonResources;
        private TrackMapWithCustomPathsViewModel _trackMapViewModel;

        private bool _showBrakeOverlay;
        private bool _showThrottleOverlay;
        private bool _showClutchOverlay;
        private bool _showShiftPoints;
        private bool _showColoredSectors;

        private ITrackMap _trackMapDto;

        public MapViewViewModel(IViewModelFactory viewModelFactory, ILoadedLapsCache loadedLapsCache, IDataPointSelectionSynchronization dataPointSelectionSynchronization, ILapColorSynchronization lapColorSynchronization)
        {
            _showColoredSectors = true;
            _viewModelFactory = viewModelFactory;
            _loadedLapsCache = loadedLapsCache;
            _dataPointSelectionSynchronization = dataPointSelectionSynchronization;
            _lapColorSynchronization = lapColorSynchronization;
            _lapsPaths = new Dictionary<string, ILapCustomPathsCollection>();
            _selectionPaths = new Dictionary<int, SelectionItemCounter<AbstractShapeViewModel>>();
            _commonResources = new ResourceDictionary
            {
                Source = new Uri(
                    @"pack://application:,,,/WindowsControls;component/WPF/CommonResources.xaml",
                    UriKind.RelativeOrAbsolute)
            };
            Subscribe();
        }

        public TrackMapWithCustomPathsViewModel TrackMapViewModel
        {
            get => _trackMapViewModel;
            set => SetProperty(ref _trackMapViewModel, value);
        }

        public bool? ShowAllOverlays
        {
            get
            {
                if (ShowBrakeOverlay && ShowThrottleOverlay && ShowClutchOverlay && ShowShiftPoints)
                {
                    return true;
                }

                if (!ShowBrakeOverlay && !ShowThrottleOverlay && !ShowClutchOverlay && !ShowShiftPoints)
                {
                    return false;
                }

                return null;
            }
            set
            {
                if (value == null)
                {
                    return;
                }

                _showBrakeOverlay = value.Value;
                _showThrottleOverlay = value.Value;
                _showShiftPoints = value.Value;
                _showClutchOverlay = value.Value;

                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(ShowBrakeOverlay));
                NotifyPropertyChanged(nameof(ShowThrottleOverlay));
                NotifyPropertyChanged(nameof(ShowClutchOverlay));
                NotifyPropertyChanged(nameof(ShowShiftPoints));
                RefreshOverlays();
            }
        }

        public bool ShowColoredSectors
        {
            get => _showColoredSectors;
            set
            {
                SetProperty(ref _showColoredSectors, value);
                if (_trackMapViewModel != null)
                {
                    _trackMapViewModel.ShowSectors = value;
                }
            }
        }

        public bool ShowBrakeOverlay
        {
            get => _showBrakeOverlay;
            set
            {
                _showBrakeOverlay = value;
                RefreshOverlays();
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(ShowAllOverlays));
            }
        }

        public bool ShowThrottleOverlay
        {
            get => _showThrottleOverlay;
            set
            {
                _showThrottleOverlay = value;
                RefreshOverlays();
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(ShowAllOverlays));
            }
        }

        public bool ShowClutchOverlay
        {
            get => _showClutchOverlay;
            set
            {
                _showClutchOverlay = value;
                RefreshOverlays();
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(ShowAllOverlays));
            }
        }

        public bool ShowShiftPoints
        {
            get => _showShiftPoints;
            set
            {
                _showShiftPoints = value;
                RefreshOverlays();
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(ShowAllOverlays));
            }
        }

        public void AddDriver(IDriverInfo driver)
        {
            if (TryGetCustomOutline(driver, out ColorDto outlineBrush))
            {
                TrackMapViewModel.AddDriver(driver, outlineBrush);
            }
            else
            {
                TrackMapViewModel.AddDriver(driver);
            }
        }

        public void RemoveDriver(IDriverInfo driverInfo)
        {
            _trackMapViewModel.RemoveDriver(driverInfo);
        }

        public void UpdateDriver(IDriverInfo driverInfo)
        {
            _trackMapViewModel.UpdateDriver(driverInfo);
        }

        public void LoadTrack(ITrackMap trackMapDto)
        {
            _trackMapDto = trackMapDto;
            TrackMapViewModel = InitializeFullMap(trackMapDto);
        }

        public async Task AddPathsForLap(LapTelemetryDto lapTelemetry)
        {
            ILapCustomPathsCollection geometryCollection = GetOrCreateLapPathCollection(lapTelemetry.LapSummary.Id);
            if (!geometryCollection.FullyInitialized)
            {
                await InitializeGeometryCollection(geometryCollection, lapTelemetry);
            }

            RefreshOverlays();
            geometryCollection.GetAllPaths().ForEach(TrackMapViewModel.AddPath);
        }

        public void DeselectPoints(IEnumerable<TimedTelemetrySnapshot> points)
        {
            var toDeselect = points.Where(x => _selectionPaths.ContainsKey(GetSelectionKey(x))).Select(x => new KeyValuePair<int, SelectionItemCounter<AbstractShapeViewModel>>(GetSelectionKey(x), _selectionPaths[GetSelectionKey(x)]));
            var toRemove = toDeselect.Where(x => x.Value.RemoveSelection()).ToList();
            toRemove.ForEach(x =>
            {
                _trackMapViewModel.RemovePath(x.Value.Item);
                _selectionPaths.Remove(x.Key);
            });
        }

        private static int GetSelectionKey(TimedTelemetrySnapshot timedTelemetrySnapshot)
        {
            return (int)(timedTelemetrySnapshot.PlayerData.LapDistance / SelectionLength);
        }

        public void SelectTelemetryPointsInArea(Point pt1, Point pt2)
        {
            _dataPointSelectionSynchronization.SelectPoints(GetTelemetryPointsInArea(pt1, pt2));
        }

        public void DeselectTelemetryPointsInArea(Point pt1, Point pt2)
        {
            _dataPointSelectionSynchronization.DeSelectPoints(GetTelemetryPointsInArea(pt1, pt2));
        }

        public void SelectPoints(IEnumerable<TimedTelemetrySnapshot> points)
        {
            foreach (TimedTelemetrySnapshot timedTelemetrySnapshot in points)
            {
                int key = GetSelectionKey(timedTelemetrySnapshot);
                if (_selectionPaths.TryGetValue(key, out SelectionItemCounter<AbstractShapeViewModel> itemCounter))
                {
                    itemCounter.AddSelection();
                    continue;
                }

                var markerPoint = TrackMapFromTelemetryFactory.ExtractWorldPoint(timedTelemetrySnapshot, _trackMapDto.TrackGeometry.XCoef, _trackMapDto.TrackGeometry.YCoef, _trackMapDto.TrackGeometry.IsSwappedAxis);
                var newPath = new GeometryViewModel(ColorDto.GreenYellowColor, ColorDto.GreenYellowColor, new EllipseGeometry(markerPoint, 5, 5), 0);
                itemCounter = new SelectionItemCounter<AbstractShapeViewModel>(newPath);
                _selectionPaths.Add(key, itemCounter);
                _trackMapViewModel.AddPath(newPath);
            }
        }

        private async Task InitializeGeometryCollection(ILapCustomPathsCollection geometryCollection, LapTelemetryDto lapTelemetry)
        {
            string baseGeometry = TrackMapFromTelemetryFactory.GetGeometry(lapTelemetry.GetReducedSet(TimeSpan.FromMilliseconds(300)), false, _trackMapDto.TrackGeometry.XCoef, _trackMapDto.TrackGeometry.YCoef, _trackMapDto.TrackGeometry.IsSwappedAxis);
            if (!_lapColorSynchronization.TryGetColorForLap(lapTelemetry.LapSummary.Id, out ColorDto color))
            {
                color = ColorDto.DarkBlueColor;
            }

            var geometryViewModel = new GeometryViewModel(color, Geometry.Parse(baseGeometry), 0.5);

            geometryCollection.BaseLapPath = geometryViewModel;

            string[] brakeGeometries = new string[0];
            string[] throttleGeometries = new string[0];
            string[] clutchGeometries = new string[0];
            List<ShiftPointViewModel> shiftPoints = new List<ShiftPointViewModel>();

            Task brakeTask = Task.Run(() => brakeGeometries = GetGeometriesPathForBrakes(lapTelemetry));
            Task throttleTask = Task.Run(() => throttleGeometries = GetGeometriesPathPedalInput(lapTelemetry, x => x.ThrottlePedalPosition));
            Task clutchTask = Task.Run(() => clutchGeometries = GetGeometriesPathPedalInput(lapTelemetry, x => x.ClutchPedalPosition));
            Task shiftPointsTask = Task.Run(() => shiftPoints = GetShiftPoints(lapTelemetry, color));

            await Task.WhenAll(brakeTask, throttleTask, clutchTask, shiftPointsTask);
            //start from one, no reason to include "no brakes" geometry
            for (int i = 1; i < brakeGeometries.Length; i++)
            {
                string brakeGeometry = brakeGeometries[i];
                if (string.IsNullOrEmpty(brakeGeometry))
                {
                    continue;
                }

                GeometryViewModel brakeGeometryViewModel = new GeometryViewModel((i * PedalStep) > 99 ? ColorDto.RedColor : ColorDto.OrangeColor, Geometry.Parse(brakeGeometry), 5.0)
                {
                    Opacity = (i * PedalStep) / 100.0,
                };
                geometryCollection.AddBrakingPath(brakeGeometryViewModel, brakeGeometryViewModel.Opacity);
            }

            for (int i = 1; i < throttleGeometries.Length; i++)
            {
                string throttleGeometry = throttleGeometries[i];
                if (string.IsNullOrEmpty(throttleGeometry))
                {
                    continue;
                }

                GeometryViewModel throttleGeometryViewModel = new GeometryViewModel((i * PedalStep) > 99 ? ColorDto.LimeGreenColor : ColorDto.GreenColor, Geometry.Parse(throttleGeometry), 4.0)
                {
                    Opacity = (i * PedalStep) / 100.0,
                };
                geometryCollection.AddThrottlePath(throttleGeometryViewModel, throttleGeometryViewModel.Opacity);
            }

            for (int i = 1; i < clutchGeometries.Length; i++)
            {
                string clutchGeometry = clutchGeometries[i];
                if (string.IsNullOrEmpty(clutchGeometry))
                {
                    continue;
                }

                GeometryViewModel clutchGeometryViewModel = new GeometryViewModel(ColorDto.YellowColor, Geometry.Parse(clutchGeometry), 3.0)
                {
                    Opacity = (i * PedalStep) / 100.0
                };
                geometryCollection.AddClutchPath(clutchGeometryViewModel, clutchGeometryViewModel.Opacity);
            }

            geometryCollection.ShiftPoints.AddRange(shiftPoints);
            geometryCollection.FullyInitialized = true;
        }

        private string[] GetGeometriesPathForBrakes(LapTelemetryDto lapTelemetryDto)
        {
            return GetGeometriesPathPedalInput(lapTelemetryDto, x => x.BrakePedalPosition);
        }

        private string[] GetGeometriesPathPedalInput(LapTelemetryDto lapTelemetryDto, Func<InputInfo, double> pedalValueFunc)
        {
            string[] geometries = new string[(100 / PedalStep) + 1];
            for (int i = 0; i < geometries.Length; i++)
            {
                geometries[i] = string.Empty;
            }

            int previousPedalBand = -1;
            List<TimedTelemetrySnapshot> currentSnapShot = new List<TimedTelemetrySnapshot>();
            foreach (TimedTelemetrySnapshot timedTelemetrySnapshot in lapTelemetryDto.GetReducedSet(TimeSpan.FromMilliseconds(200)))
            {
                int currentBrakeBand = ((int)(pedalValueFunc(timedTelemetrySnapshot.InputInfo) * 100)) / PedalStep;
                if (previousPedalBand >= 0 && previousPedalBand != currentBrakeBand)
                {
                    currentSnapShot.Add(timedTelemetrySnapshot);
                    geometries[previousPedalBand] += $" {TrackMapFromTelemetryFactory.GetGeometry(currentSnapShot, false, _trackMapDto.TrackGeometry.XCoef, _trackMapDto.TrackGeometry.YCoef, _trackMapDto.TrackGeometry.IsSwappedAxis)}";
                    currentSnapShot.Clear();
                }

                currentSnapShot.Add(timedTelemetrySnapshot);
                previousPedalBand = currentBrakeBand;
            }

            if (previousPedalBand < 0 || geometries.Length == 0)
            {
                return geometries;
            }

            geometries[previousPedalBand] += $" {TrackMapFromTelemetryFactory.GetGeometry(currentSnapShot, false, _trackMapDto.TrackGeometry.XCoef, _trackMapDto.TrackGeometry.YCoef, _trackMapDto.TrackGeometry.IsSwappedAxis)}";
            currentSnapShot.Clear();

            return geometries;
        }

        private List<ShiftPointViewModel> GetShiftPoints(LapTelemetryDto lapTelemetryDto, ColorDto color)
        {
            List<ShiftPointViewModel> shiftPoints = new List<ShiftPointViewModel>();
            StringBuilder sb = new StringBuilder();
            List<TimedTelemetrySnapshot> shiftTelemetryPoints = new List<TimedTelemetrySnapshot>();
            string currentGear = string.Empty;
            foreach (TimedTelemetrySnapshot timedTelemetrySnapshot in lapTelemetryDto.DataPoints.Where(x => x.PlayerData.CarInfo.CurrentGear != "N"))
            {
                if (!string.IsNullOrEmpty(currentGear) && currentGear != "N" && currentGear != timedTelemetrySnapshot.PlayerData.CarInfo.CurrentGear)
                {
                    shiftTelemetryPoints.Add(timedTelemetrySnapshot);
                }

                currentGear = timedTelemetrySnapshot.PlayerData.CarInfo.CurrentGear;
            }

            foreach (TimedTelemetrySnapshot timedTelemetrySnapshot in shiftTelemetryPoints)
            {
                Point mapPoint = TrackMapFromTelemetryFactory.ExtractWorldPoint(timedTelemetrySnapshot, _trackMapDto.TrackGeometry.XCoef, _trackMapDto.TrackGeometry.YCoef, _trackMapDto.TrackGeometry.IsSwappedAxis);
                ShiftPointViewModel shiftPoint = new ShiftPointViewModel(mapPoint.X, mapPoint.Y, timedTelemetrySnapshot.PlayerData.CarInfo.CurrentGear, color);
                shiftPoints.Add(shiftPoint);
            }

            return shiftPoints;
        }

        private void RefreshOverlays()
        {
            foreach (ILapCustomPathsCollection lapCustomPathsCollection in _lapsPaths.Values)
            {
                if (!lapCustomPathsCollection.FullyInitialized)
                {
                    continue;
                }

                lapCustomPathsCollection.ShiftPoints.ForEach(x => x.IsVisible = _showShiftPoints);
                lapCustomPathsCollection.GetAllBrakingPaths().ForEach(x => x.IsVisible = _showBrakeOverlay);
                lapCustomPathsCollection.GetAllClutchPaths().ForEach(x => x.IsVisible = _showClutchOverlay);
                lapCustomPathsCollection.GetAllThrottlePaths().ForEach(x => x.IsVisible = _showThrottleOverlay);
            }
        }

        public void RemovePathsForLap(LapSummaryDto lapSummaryDto)
        {
            ILapCustomPathsCollection geometryCollection = GetOrCreateLapPathCollection(lapSummaryDto.Id);
            geometryCollection.GetAllPaths().ForEach(TrackMapViewModel.RemovePath);
        }

        protected ILapCustomPathsCollection GetOrCreateLapPathCollection(string id)
        {
            if (_lapsPaths.TryGetValue(id, out ILapCustomPathsCollection lapCustomPathsCollection))
            {
                return lapCustomPathsCollection;
            }

            ILapCustomPathsCollection newLapCustomPathsCollection = new LapCustomPathsCollection();
            _lapsPaths[id] = newLapCustomPathsCollection;
            return newLapCustomPathsCollection;
        }

        private void Subscribe()
        {
            WeakEventManager<ILapColorSynchronization, LapColorArgs>.AddHandler(_lapColorSynchronization, nameof(ILapColorSynchronization.LapColorChanged), LapColorSynchronizationOnLapColorChanged);
            WeakEventManager<IDataPointSelectionSynchronization, TimedTelemetryArgs>.AddHandler(_dataPointSelectionSynchronization, nameof(IDataPointSelectionSynchronization.OnPointsSelected), DataPointSelectionSynchronizationOnOnPointsSelected);
            WeakEventManager<IDataPointSelectionSynchronization, TimedTelemetryArgs>.AddHandler(_dataPointSelectionSynchronization, nameof(IDataPointSelectionSynchronization.OnPointsDeselected), DataPointSelectionSynchronizationOnOnPointsDeselected);
        }

        private void DataPointSelectionSynchronizationOnOnPointsDeselected(object sender, TimedTelemetryArgs e)
        {
            DeselectPoints(e.TelemetrySnapshots);
        }

        private void DataPointSelectionSynchronizationOnOnPointsSelected(object sender, TimedTelemetryArgs e)
        {
            SelectPoints(e.TelemetrySnapshots);
        }

        private void LapColorSynchronizationOnLapColorChanged(object sender, LapColorArgs e)
        {
            if (TrackMapViewModel == null)
            {
                return;
            }

            ILapCustomPathsCollection lapPaths = GetOrCreateLapPathCollection(e.LapId);
            if (lapPaths.BaseLapPath != null)
            {
                lapPaths.BaseLapPath.Color = e.Color;
            }

            lapPaths.ShiftPoints.ForEach(x => x.Color = e.Color);

            _trackMapViewModel?.UpdateCustomOutline(e.LapId, e.Color);
        }

        private IEnumerable<TimedTelemetrySnapshot> GetTelemetryPointsInArea(Point pt1, Point pt2)
        {
            Point realWorldPoint1 = TrackMapFromTelemetryFactory.InverseExtractWorldPoint(pt1, _trackMapDto.TrackGeometry.XCoef, _trackMapDto.TrackGeometry.YCoef, _trackMapDto.TrackGeometry.IsSwappedAxis);
            Point realWorldPoint2 = TrackMapFromTelemetryFactory.InverseExtractWorldPoint(pt2, _trackMapDto.TrackGeometry.XCoef, _trackMapDto.TrackGeometry.YCoef, _trackMapDto.TrackGeometry.IsSwappedAxis);

            double minX = Math.Min(realWorldPoint1.X, realWorldPoint2.X);
            double maxX = Math.Max(realWorldPoint1.X, realWorldPoint2.X);
            double minY = Math.Min(realWorldPoint1.Y, realWorldPoint2.Y);
            double maxY = Math.Max(realWorldPoint1.Y, realWorldPoint2.Y);

            var pointsToSelect = _loadedLapsCache.LoadedLaps.SelectMany(x => x.DataPoints).Where(x =>
            {
                var worldPos = x.PlayerData.WorldPosition;
                double xInMeters = worldPos.X.InMeters;
                double yInMeters = worldPos.Z.InMeters;
                return minX <= xInMeters && xInMeters <= maxX && minY <= yInMeters && yInMeters <= maxY;
            });
            return pointsToSelect;
        }

        private TrackMapWithCustomPathsViewModel InitializeFullMap(ITrackMap trackMapDto)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                return Application.Current.Dispatcher.Invoke(() => InitializeFullMap(trackMapDto));
            }

            if (TrackMapViewModel != null)
            {
                TrackMapViewModel.PointsSelectionRequested -= TrackMapViewModelOnPointsSelectionRequested;
                TrackMapViewModel.PointsDeSelectionRequested -= TrackMapViewModelOnPointsDeSelectionRequested;
            }

            TrackMapWithCustomPathsViewModel newViewModel = new TrackMapWithCustomPathsViewModel(_viewModelFactory, new ClassColorProvider(new BasicColorPaletteProvider()), 0, false);
            newViewModel.ApplyTrackGeometry(trackMapDto.TrackGeometry);
            newViewModel.AnimateDrivers = false;
            newViewModel.AutoScaleDrivers = false;
            newViewModel.ShowSectors = ShowColoredSectors;
            newViewModel.PointsSelectionRequested += TrackMapViewModelOnPointsSelectionRequested;
            newViewModel.PointsDeSelectionRequested += TrackMapViewModelOnPointsDeSelectionRequested;

            Logger.Info("Initializing Full Map Control");
            _lapsPaths.Clear();

            return newViewModel;
        }

        private void TrackMapViewModelOnPointsDeSelectionRequested(object sender, PointsAreaArgs e)
        {
            DeselectTelemetryPointsInArea(e.Point1, e.Point2);
        }

        private void TrackMapViewModelOnPointsSelectionRequested(object sender, PointsAreaArgs e)
        {
            SelectTelemetryPointsInArea(e.Point1, e.Point2);
        }

        public bool IsDriverOnValidLap(IDriverInfo driver) => true;

        public bool IsDriverLastSectorGreen(IDriverInfo driver, int sectorNumber) => false;

        public bool IsDriverLastSectorPurple(IDriverInfo driver, int sectorNumber) => false;
        public bool IsDriverLastClassSectorPurple(IDriverInfo dataSetPlayerInfo, int i) => false;
        public bool HasDriverCompletedPitWindowStop(IDriverInfo driverInfo) => false;

        public int? GetPlayerPitStopCount() => 0;

        public int GetDriverPitStopCount(IDriverInfo driverInfo) => 0;

        public TimeSpan GetDriverCurrentLapTime(IDriverInfo driverInfo) => TimeSpan.Zero;

        public bool TryGetCustomOutline(IDriverInfo driverInfo, out ColorDto outlineBrush)
        {
            if (_lapColorSynchronization == null)
            {
                outlineBrush = null;
                return false;
            }

            if (_lapColorSynchronization.TryGetColorForLap(driverInfo.DriverLongName, out ColorDto lapColor))
            {
                outlineBrush = lapColor;
                return true;
            }

            outlineBrush = null;
            return false;
        }
    }
}