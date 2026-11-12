namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using Model;
    using ViewModels;

    public class MapDisplaySettingsViewModel : AbstractViewModel<MapDisplaySettings>
    {
        private bool _autoScaleDrivers;
        private bool _keepMapRatio;
        private int _mapPointsInterval;
        private bool _alwaysUseCirce;
        private bool _renderMapInSeparateWindow;
        private int _driverScalePercentage;
        private bool _showPitsInformation;

        public MapDisplaySettingsViewModel()
        {
        }

        public bool AutoScaleDrivers
        {
            get => _autoScaleDrivers;
            set
            {
                _autoScaleDrivers = value;
                NotifyPropertyChanged();
            }
        }

        public bool KeepMapRatio
        {
            get => _keepMapRatio;
            set
            {
                _keepMapRatio = value;
                NotifyPropertyChanged();
            }
        }

        public bool AlwaysUseCirce
        {
            get => _alwaysUseCirce;
            set
            {
                _alwaysUseCirce = value;
                NotifyPropertyChanged();
            }
        }

        public int MapPointsInterval
        {
            get => _mapPointsInterval;
            set
            {
                _mapPointsInterval = value;
                NotifyPropertyChanged();
            }
        }

        public bool RenderMapInSeparateWindow
        {
            get => _renderMapInSeparateWindow;
            set => SetProperty(ref _renderMapInSeparateWindow, value);
        }

        public int DriverScalePercentage
        {
            get => _driverScalePercentage;
            set => SetProperty(ref _driverScalePercentage, value);
        }

        public bool ShowPitsInformation
        {
            get => _showPitsInformation;
            set => SetProperty(ref _showPitsInformation, value);
        }

        protected override void ApplyModel(MapDisplaySettings model)
        {
            KeepMapRatio = model.KeepMapRatio;
            AutoScaleDrivers = model.AutosScaleDrivers;
            MapPointsInterval = model.MapPointsInterval;
            AlwaysUseCirce = model.AlwaysUseCirce;
            RenderMapInSeparateWindow = model.RenderMapInSeparateWindow;
            DriverScalePercentage = model.DriverScalePercentage;
            ShowPitsInformation = model.ShowPitsInformation;
        }

        public override MapDisplaySettings SaveToNewModel()
        {
            return new MapDisplaySettings()
            {
                AutosScaleDrivers = AutoScaleDrivers,
                KeepMapRatio = KeepMapRatio,
                MapPointsInterval = MapPointsInterval,
                AlwaysUseCirce = AlwaysUseCirce,
                RenderMapInSeparateWindow = RenderMapInSeparateWindow,
                DriverScalePercentage = DriverScalePercentage,
                ShowPitsInformation = ShowPitsInformation,
            };
        }
    }
}