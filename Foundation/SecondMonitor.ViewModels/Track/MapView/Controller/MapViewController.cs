namespace SecondMonitor.ViewModels.Track.MapView.Controller
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Media;
    using Contracts.Commands;
    using Contracts.Session;
    using Contracts.SimSettings;
    using Contracts.TrackMap;
    using Controllers;
    using DataModel.BasicProperties;
    using DataModel.DriversPresentation;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using DataModel.Snapshot.Systems;
    using DataModel.TrackMap;
    using NLog;

    using SecondMonitor.DataModel.Extensions;

    using SessionEvents;
    using Settings;
    using Settings.Model;
    using Settings.ViewModel;

    public class MapViewController : IController, INotifyPropertyChanged
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly SituationOverviewViewModelFactory _situationOverviewViewModelFactory;
        private readonly IDriverPresentationsManager _driverPresentationsManager;
        private readonly DisplaySettingsViewModel _displaySettingsViewModel;
        private readonly IMapManagementController _mapManagementController;
        private readonly ISessionInformationProvider _sessionInformationProvider;
        private readonly IPlayerPitEstimationProvider _playerPitEstimationProvider;
        private ISituationOverviewViewModel _situationOverviewViewModel;

        private (string FullTrackName, string SimName) _currentTrackTuple;

        private bool _usePositionInClass;
        private bool _isWorldPositionInvalid;
        private Window _popUpWindow;

        public MapViewController(ISettingsProvider settingsProvider, ISessionEventProvider sessionEventProvider, SituationOverviewViewModelFactory situationOverviewViewModelFactory,
            IDriverPresentationsManager driverPresentationsManager, IMapManagementController mapManagementController, ISessionInformationProvider sessionInformationProvider, IPlayerPitEstimationProvider playerPitEstimationProvider)
        {
            _sessionEventProvider = sessionEventProvider;
            _situationOverviewViewModelFactory = situationOverviewViewModelFactory;
            _driverPresentationsManager = driverPresentationsManager;
            _mapManagementController = mapManagementController;
            _sessionInformationProvider = sessionInformationProvider;
            _playerPitEstimationProvider = playerPitEstimationProvider;
            SubscribeMapManager();
            driverPresentationsManager.DriverCustomColorChanged += DriverPresentationsManagerOnDriverCustomColorChanged;
            _displaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
            _sessionEventProvider.TrackChanged += SessionEventProviderOnTrackChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ISituationOverviewViewModel SituationOverviewViewModel
        {
            get => _situationOverviewViewModel;
            private set
            {
                _situationOverviewViewModel = value;
                NotifyPropertyChanged();
            }
        }

        public Task StartControllerAsync()
        {
            SubscribeDisplaySettings();
            SubscribeMapManager();
            SubscribeDisplaySettings();
            ApplyDisplaySettings();
            if (_displaySettingsViewModel.MapDisplaySettingsViewModel.RenderMapInSeparateWindow && _popUpWindow == null)
            {
                OpenOrFocusPopupWindow();
            }

            _displaySettingsViewModel.MapDisplaySettingsViewModel.PropertyChanged += MapDisplaySettingsViewModelOnPropertyChanged;
            return Task.CompletedTask;
        }

        public Task StopControllerAsync()
        {
            _displaySettingsViewModel.MapDisplaySettingsViewModel.PropertyChanged -= MapDisplaySettingsViewModelOnPropertyChanged;
            UnSubscribeDisplaySettings();
            UnsubscribeMapManager();
            UnSubscribeDisplaySettings();
            _popUpWindow?.Close();
            return Task.CompletedTask;
        }

        private void ApplyDisplaySettings()
        {
            if (_displaySettingsViewModel == null)
            {
                return;
            }

            _usePositionInClass = _displaySettingsViewModel.MultiClassDisplayKind is MultiClassDisplayKind.ClassFirst or MultiClassDisplayKind.OnlyClass;

            if (SituationOverviewViewModel != null)
            {
                SituationOverviewViewModel.AnimateDrivers = _displaySettingsViewModel.AnimateDriversPosition;
                SituationOverviewViewModel.DriversUpdatedPerTick = _displaySettingsViewModel.DriversUpdatedPerTick;
            }

            if (_mapManagementController != null)
            {
                _mapManagementController.MapPointsInterval = TimeSpan.FromMilliseconds(_displaySettingsViewModel.MapDisplaySettingsViewModel.MapPointsInterval);
            }
        }

        public void ApplyDateSet(SimulatorDataSet dataSet)
        {
            if (dataSet == null || SituationOverviewViewModel == null || dataSet.SessionInfo.SpectatingState == SpectatingState.Replay)
            {
                return;
            }

            if (_isWorldPositionInvalid != dataSet.SimulatorSourceInfo.WorldPositionInvalid)
            {
                _isWorldPositionInvalid = dataSet.SimulatorSourceInfo.WorldPositionInvalid;
                LoadCurrentMap(dataSet);
            }

            List<DriverInfo> unknownDrivers = SituationOverviewViewModel.Update(dataSet, _sessionInformationProvider, _usePositionInClass);
            unknownDrivers.ForEach(AddDriver);

            RefreshPitEstimation(dataSet);
        }

        private void RefreshPitEstimation(SimulatorDataSet dataSet)
        {
            if (!_playerPitEstimationProvider.IsPredictedReturnPositionFilled ||
                !_playerPitEstimationProvider.IsPlayerExpectedToPit ||
                !_displaySettingsViewModel.PitEstimationSettingsViewModel.IsReasonEnabled(_playerPitEstimationProvider.PitPredictionReason, x => x.IsMapEnabled)
                || dataSet.PlayerInfo.InPits)
            {
                SituationOverviewViewModel.IsPitReturnPredictionVisible = false;
                return;
            }

            SituationOverviewViewModel.IsPitReturnPredictionVisible = true;
            SituationOverviewViewModel.UpdatePitStopReturnPrediction(_playerPitEstimationProvider.CurrentPitReturnPrediction, _usePositionInClass);
        }

        public void Reset()
        {
            _situationOverviewViewModel?.RemoveAllDrivers();
        }

        public void RemoveDriver(DriverInfo driver)
        {
            SituationOverviewViewModel?.RemoveDriver(driver);
        }

        public void AddDriver(DriverInfo driver)
        {
            if (_driverPresentationsManager.TryGetDriverPresentation(driver.DriverLongName, out DriverPresentationDto driverPresentationDto) && driverPresentationDto.CustomOutLineEnabled)
            {
                SituationOverviewViewModel?.AddDriver(driver, driverPresentationDto.OutLineColor);
            }
            else
            {
                SituationOverviewViewModel?.AddDriver(driver);
            }
        }

        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SubscribeMapManager()
        {
            if (_mapManagementController == null)
            {
                return;
            }

            _mapManagementController.NewMapAvailable += OnNewMapAvailable;
            _mapManagementController.MapRemoved += OnMapRemoved;
        }

        private void UnsubscribeMapManager()
        {
            if (_mapManagementController == null)
            {
                return;
            }

            _mapManagementController.NewMapAvailable -= OnNewMapAvailable;
            _mapManagementController.MapRemoved -= OnMapRemoved;
        }

        private void SubscribeDisplaySettings()
        {
            if (_displaySettingsViewModel == null)
            {
                return;
            }

            _displaySettingsViewModel.PropertyChanged += OnDisplaySettingsChanged;
            _displaySettingsViewModel.MapDisplaySettingsViewModel.PropertyChanged += OnDisplaySettingsChanged;
        }

        private void UnSubscribeDisplaySettings()
        {
            if (_displaySettingsViewModel == null)
            {
                return;
            }

            _displaySettingsViewModel.PropertyChanged -= OnDisplaySettingsChanged;
            _displaySettingsViewModel.MapDisplaySettingsViewModel.PropertyChanged -= OnDisplaySettingsChanged;
        }

        private void OnDisplaySettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            ApplyDisplaySettings();
        }

        private void LoadCurrentMap(SimulatorDataSet dataSet)
        {
            if (dataSet == null)
            {
                return;
            }

            _currentTrackTuple = (dataSet.SessionInfo.TrackInfo.TrackFullName, dataSet.Source);
            if (string.IsNullOrEmpty(_currentTrackTuple.FullTrackName) || _mapManagementController == null)
            {
                return;
            }

            Logger.Info("Loading New Map");
            if (_isWorldPositionInvalid ||
                _displaySettingsViewModel.MapDisplaySettingsViewModel.AlwaysUseCirce ||
                !_mapManagementController.TryGetMap(_currentTrackTuple.SimName, _currentTrackTuple.FullTrackName, out TrackMapDto trackMapDto))
            {
                SituationOverviewViewModel = _situationOverviewViewModelFactory.CreateDefault(dataSet);
            }
            else
            {
                SituationOverviewViewModel = _situationOverviewViewModelFactory.Create(dataSet, trackMapDto);
            }

            dataSet.DriversInfo.ForEach(AddDriver);

            SituationOverviewViewModel.MapSidePanelViewModel.DeleteMapCommand = new RelayCommand(RemoveCurrentMap);
            SituationOverviewViewModel.MapSidePanelViewModel.RotateMapLeftCommand = new RelayCommand(RotateCurrentMapLeft);
            SituationOverviewViewModel.MapSidePanelViewModel.RotateMapRightCommand = new RelayCommand(RotateCurrentMapRight);
            SituationOverviewViewModel.MapSidePanelViewModel.PopUpCommand = new RelayCommand(OpenOrFocusPopupWindow);

            UpdatePopUpWindowContent();
        }

        private void UpdatePopUpWindowContent()
        {
            if (_popUpWindow == null)
            {
                return;
            }

            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(UpdatePopUpWindowContent);
                return;
            }

            _popUpWindow.Content = SituationOverviewViewModel;
        }

        private void SessionEventProviderOnTrackChanged(object sender, DataSetArgs e)
        {
            LoadCurrentMap(e.DataSet);
        }

        private void OnNewMapAvailable(object sender, MapEventArgs e)
        {
            if (_currentTrackTuple.SimName == e.TrackMapDto.SimulatorSource && _currentTrackTuple.FullTrackName == e.TrackMapDto.TrackFullName)
            {
                Logger.Info($"New Map is Available: {_currentTrackTuple.SimName}, {_currentTrackTuple.FullTrackName}");
                LoadCurrentMap(_sessionEventProvider.LastDataSet);
            }
        }

        private void OnMapRemoved(object sender, MapEventArgs e)
        {
            if (_currentTrackTuple.SimName == e.TrackMapDto.SimulatorSource && _currentTrackTuple.FullTrackName == e.TrackMapDto.TrackFullName)
            {
                Logger.Info($" Map Removed: {_currentTrackTuple.SimName}, {_currentTrackTuple.FullTrackName}");
                LoadCurrentMap(_sessionEventProvider.LastDataSet);
            }
        }

        private void RemoveCurrentMap()
        {
            _mapManagementController.RemoveMap(_currentTrackTuple.SimName, _currentTrackTuple.FullTrackName);
        }

        private void RotateCurrentMapLeft()
        {
            _mapManagementController.RotateMapLeft(_currentTrackTuple.SimName, _currentTrackTuple.FullTrackName);
        }

        private void RotateCurrentMapRight()
        {
            _mapManagementController.RotateMapRight(_currentTrackTuple.SimName, _currentTrackTuple.FullTrackName);
        }

        private void DriverPresentationsManagerOnDriverCustomColorChanged(object sender, DriverCustomColorEnabledArgs e)
        {
            if (_driverPresentationsManager.TryGetDriverPresentation(e.DriverLongName, out DriverPresentationDto driverPresentationDto) && driverPresentationDto.CustomOutLineEnabled)
            {
                SituationOverviewViewModel?.UpdateCustomOutline(e.DriverLongName, driverPresentationDto.OutLineColor);
            }
            else
            {
                SituationOverviewViewModel?.UpdateCustomOutline(e.DriverLongName, null);
            }
        }

        private void OpenOrFocusPopupWindow()
        {
            if (_popUpWindow != null)
            {
                _popUpWindow.Focus();
                return;
            }

            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(OpenOrFocusPopupWindow);
                return;
            }

            _popUpWindow = new Window() { WindowState = WindowState.Normal, Title = "Map", Content = SituationOverviewViewModel, Background = Brushes.Black };
            _popUpWindow.Show();
            _popUpWindow.Closed += OnPopupWindowClosed;

            WindowLocationSetting windowLocationSetting = _displaySettingsViewModel.MapWindowLocationSettings;
            if (windowLocationSetting == null)
            {
                return;
            }

            _popUpWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            _popUpWindow.Left = windowLocationSetting.Left;
            _popUpWindow.Top = windowLocationSetting.Top;
            _popUpWindow.WindowState = WindowState.Normal;
            _popUpWindow.WindowState = (WindowState)windowLocationSetting.WindowState;
            //_popUpWindow.WindowStyle = WindowStyle.ToolWindow;
            if (windowLocationSetting.Height > 0 && windowLocationSetting.Width > 0)
            {
                _popUpWindow.Width = windowLocationSetting.Width;
                _popUpWindow.Height = windowLocationSetting.Height;
            }
        }

        private void OnPopupWindowClosed(object sender, EventArgs e)
        {
            _displaySettingsViewModel.MapWindowLocationSettings = new WindowLocationSetting()
            {
                Height = _popUpWindow.Height,
                Left = _popUpWindow.Left,
                Top = _popUpWindow.Top,
                Width = _popUpWindow.Width,
                WindowState = (int)_popUpWindow.WindowState,
            };

            _popUpWindow = null;
        }

        private void MapDisplaySettingsViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            LoadCurrentMap(_sessionEventProvider.LastDataSet);
        }
    }
}