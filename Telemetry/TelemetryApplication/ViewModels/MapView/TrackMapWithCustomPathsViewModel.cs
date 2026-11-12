namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.MapView
{
    using System;
    using System.Collections.ObjectModel;
    using System.Windows;
    using DataModel.Snapshot.Drivers;
    using SecondMonitor.ViewModels.Colors;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Shapes;
    using SecondMonitor.ViewModels.Track;
    using SecondMonitor.ViewModels.Track.MapView;

    public class TrackMapWithCustomPathsViewModel : TrackMapSituationOverviewViewModel
    {
        private bool _showSectors;
        public TrackMapWithCustomPathsViewModel(IViewModelFactory viewModelFactory, IClassColorProvider classColorProvider, double layoutLength, bool animateDrivers) : base(viewModelFactory, classColorProvider, layoutLength, animateDrivers, 99)
        {
            CustomPaths = new ObservableCollection<AbstractShapeViewModel>();
        }

        public event EventHandler<PointsAreaArgs> PointsSelectionRequested;
        public event EventHandler<PointsAreaArgs> PointsDeSelectionRequested;

        public ObservableCollection<AbstractShapeViewModel> CustomPaths { get; }

        public bool ShowSectors
        {
            get => _showSectors;
            set => SetProperty(ref _showSectors, value);
        }

        public virtual void UpdateDriver(IDriverInfo driverInfo)
        {
            if (!DriversDictionary.TryGetValue(driverInfo.DriverSessionId, out DriverPositionViewModel driverPositionViewModel))
            {
                AddDriver(driverInfo);
                return;
            }

            driverPositionViewModel.IsPlayer = driverInfo.IsPlayer;
            driverPositionViewModel.Position = driverInfo.PositionInClass;
            driverPositionViewModel.X = driverInfo.WorldPosition.X.InMeters;
            driverPositionViewModel.Y = driverInfo.WorldPosition.Z.InMeters;
            driverPositionViewModel.LapCompletedPercentage = driverInfo.LapDistance / LayoutLength;
        }

        public void AddPath(AbstractShapeViewModel abstractShape)
        {
            CustomPaths.Add(abstractShape);
        }

        public void RemovePath(AbstractShapeViewModel abstractShape)
        {
            CustomPaths.Remove(abstractShape);
        }

        public void DeselectTelemetryPointsInArea(Point mapPoint1, Point mapPoint2)
        {
            PointsDeSelectionRequested?.Invoke(this, new PointsAreaArgs(mapPoint1, mapPoint2));
        }

        public void SelectTelemetryPointsInArea(Point mapPoint1, Point mapPoint2)
        {
            PointsSelectionRequested?.Invoke(this, new PointsAreaArgs(mapPoint1, mapPoint2));
        }
    }
}