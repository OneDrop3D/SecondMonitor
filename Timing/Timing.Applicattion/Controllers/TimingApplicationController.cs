namespace SecondMonitor.Timing.Application.Controllers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using Contracts.Commands;
    using Contracts.PerfMon;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using Foundation.Connectors;
    using LapTimings;
    using NLog;
    using PitBoard.Controller;
    using PitStatistics;
    using PluginManager.Core;
    using Presentation.View;
    using Rating.Application.Championship.Controller;
    using Rating.Application.Rating.Controller;
    using Rating.Application.Rating.RatingProvider.FieldRatingProvider.ReferenceRatingProviders;
    using ReportCreation.ViewModel;

    using SecondMonitor.Timing.Application.AutoDelete;
    using SecondMonitor.Timing.Common.TimingPlugin;

    using SimdataManagement.DriverPresentation;
    using SimdataManagement.RaceSuggestion;
    using SimdataManagement.SimSettings;
    using TrackRecords.Controller;
    using ViewModel;
    using ViewModels.FuelConsumption;
    using ViewModels.Settings;
    using ViewModels.Settings.Model;
    using ViewModels.Settings.ViewModel;
    using ViewModels.SimulatorContent;
    using ViewModels.SplashScreen;
    using ViewModels.Track.MapView.Controller;

    public class TimingApplicationController : ISecondMonitorPlugin
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SecondMonitor\\settings.json");
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly TimingApplicationViewModel _timingApplicationViewModel;
        private readonly DriverPresentationsManager _driverPresentationsManager;
        private readonly DisplaySettingsLoader _displaySettingsLoader;
        private readonly IRatingApplicationController _ratingApplicationController;
        private readonly ISettingsProvider _settingsProvider;
        private readonly ISimulatorContentController _simulatorContentController;
        private readonly ITrackRecordsController _trackRecordsController;
        private readonly IChampionshipController _championshipController;
        private readonly ISessionEventsController _sessionEventsController;
        private readonly IRaceSuggestionController _raceSuggestionController;
        private readonly FuelConsumptionController _fuelConsumptionController;
        private readonly MapViewController _mapViewController;
        private readonly DriverDetailsController _driverDetailsController;
        private readonly SettingsWindowController _settingsWindowController;
        private readonly ForceSingleClassVisitor _forceSingleClassVisitor;
        private readonly PitBoardController _pitBoardController;
        private readonly SplashScreenViewModel _splashScreenViewModel;
        private readonly ISimSettingControllerFactory _simSettingControllerFactory;
        private readonly IReferenceRatingProviderFactory _referenceRatingProviderFactory;
        private readonly IOpenTelemetryChecklistController _openTelemetryChecklistController;
        private readonly PitStopStatisticsController _pitStopStatisticsController;
        private readonly PitPredictionController _pitPredictionController;
        private readonly AutoDeleteController _autoDeleteController;
        private readonly IList<ITimingPlugin> _timingPlugins;
        private readonly PeriodicPerformanceMonitor _periodicPerformanceMonitor;
        private readonly SemaphoreSlim _updateSemaphore;

        private SimSettingController _simSettingController;
        private TimingGui _timingGui;
        private DisplaySettingsViewModel _displaySettingsViewModel;
        private ReportsController _reportsController;

        private Window _splashScreen;

        public TimingApplicationController(DisplaySettingsLoader displaySettingsLoader, IRatingApplicationController ratingApplicationController, ISettingsProvider settingsProvider,
            ISimulatorContentController simulatorContentController, ITrackRecordsController trackRecordsController, IChampionshipController championshipController,
            ISessionEventsController sessionEventsController, MapViewController mapViewController, PitBoardController pitBoardController,
            DriverPresentationsManager driverPresentationsManager, FuelConsumptionController fuelConsumptionController, DriverDetailsController driverDetailsController, SettingsWindowController settingsWindowController,
            ForceSingleClassVisitor forceSingleClassVisitor, IRaceSuggestionController raceSuggestionController, TimingApplicationViewModel timingApplicationViewModel, SplashScreenViewModel splashScreenViewModel,
            ISimSettingControllerFactory simSettingControllerFactory, IReferenceRatingProviderFactory referenceRatingProviderFactory, IOpenTelemetryChecklistController openTelemetryChecklistController, PitStopStatisticsController pitStopStatisticsController,
            PitPredictionController pitPredictionController, AutoDeleteController autoDeleteController, IList<ITimingPlugin> timingPlugins)
        {
            _updateSemaphore = new SemaphoreSlim(1);
            _displaySettingsLoader = displaySettingsLoader;
            _ratingApplicationController = ratingApplicationController;
            _settingsProvider = settingsProvider;
            _simulatorContentController = simulatorContentController;
            _trackRecordsController = trackRecordsController;
            _championshipController = championshipController;
            _sessionEventsController = sessionEventsController;
            _mapViewController = mapViewController;
            _pitBoardController = pitBoardController;
            _driverPresentationsManager = driverPresentationsManager;
            _fuelConsumptionController = fuelConsumptionController;
            _driverDetailsController = driverDetailsController;
            _settingsWindowController = settingsWindowController;
            _forceSingleClassVisitor = forceSingleClassVisitor;
            _raceSuggestionController = raceSuggestionController;
            _timingApplicationViewModel = timingApplicationViewModel;
            _splashScreenViewModel = splashScreenViewModel;
            _simSettingControllerFactory = simSettingControllerFactory;
            _referenceRatingProviderFactory = referenceRatingProviderFactory;
            _openTelemetryChecklistController = openTelemetryChecklistController;
            _pitStopStatisticsController = pitStopStatisticsController;
            _pitPredictionController = pitPredictionController;
            _autoDeleteController = autoDeleteController;
            _timingPlugins = timingPlugins;
            _periodicPerformanceMonitor = new PeriodicPerformanceMonitor(TimeSpan.FromMinutes(1));
        }

        public PluginsManager PluginManager { get; set; }

        public bool IsDaemon => false;

        public string PluginName => "Timing UI";

        public bool IsEnabledByDefault => true;

        public async Task RunPlugin()
        {
            try
            {
                _periodicPerformanceMonitor.Start();
                CreateDisplaySettingsViewModel();
                ApplyCustomColors();
                ApplyResources();
                ShowSplashScreen();
                CreateReportsController();
                CreateSimSettingsController();
                await StartControllers();
                _timingApplicationViewModel.PlayerFinished += TimingApplicationViewModelOnPlayerFinished;
                _timingApplicationViewModel.SessionCompleted += TimingApplicationViewModelOnSessionCompleted;
                _timingApplicationViewModel.RatingApplicationViewModel = _ratingApplicationController.RatingApplicationViewModel;
                _timingApplicationViewModel.SidebarViewModel.ChampionshipIconStateViewModel = _championshipController.ChampionshipIconStateViewModel;
                _timingApplicationViewModel.FuelOverviewViewModel = _fuelConsumptionController.FuelOverviewViewModel;
                _timingApplicationViewModel.SuggestionsViewModel = _raceSuggestionController.SuggestionsViewModel;
                BindCommands();
                CreateGui();
                _timingApplicationViewModel?.Initialize();
                await _simulatorContentController.StartControllerAsync();
                await _trackRecordsController.StartControllerAsync();
                await Task.Run(() => _timingPlugins.Select(x => x.StartControllerAsync()).ToList());

                HideSplashScreen();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                await PluginManager.DeletePlugin(this, new List<Exception>());
            }
        }

        private static string GetAssemblyDirectory()
        {
            string path = Assembly.GetEntryAssembly()?.Location;
            return Path.GetDirectoryName(path);
        }

        private static async Task OpenCurrentTelemetrySession()
        {
            await Task.Run(StartTelemetryApplication);
        }

        private static void StartTelemetryApplication()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            if (string.IsNullOrWhiteSpace(path))
            {
                Logger.Warn($"Unable to Start Telemetry, AppDomain.CurrentDomain.BaseDirectory was empty.");
                return;
            }

            string executablePath = path + @"TelemetryViewer.exe";
            if (!File.Exists(executablePath))
            {
                Logger.Warn($"Unable to Start Telemetry, File {executablePath} was not found.");
                return;
            }

            var startInfo = new ProcessStartInfo(executablePath)
            {
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }

        private void HideSplashScreen()
        {
            _splashScreen.Close();
        }

        private void ShowSplashScreen()
        {
            _splashScreenViewModel.PrimaryInformation = "Loading...";
            _splashScreen = new Window
            {
                Content = _splashScreenViewModel,
                Title = "Starting",
                SizeToContent = SizeToContent.Manual,
                WindowStartupLocation = WindowStartupLocation.Manual,
                ShowInTaskbar = false,
            };

            _splashScreen.Show();

            if (_displaySettingsViewModel?.WindowLocationSetting == null)
            {
                return;
            }

            _splashScreen.WindowStartupLocation = WindowStartupLocation.Manual;
            _splashScreen.Left = _displaySettingsViewModel.WindowLocationSetting.Left;
            _splashScreen.Top = _displaySettingsViewModel.WindowLocationSetting.Top;
            _splashScreen.WindowState = WindowState.Normal;
            _splashScreen.WindowState = (WindowState)_displaySettingsViewModel.WindowLocationSetting.WindowState;
            if (_splashScreen.WindowState == WindowState.Maximized)
            {
                _splashScreen.WindowStyle = WindowStyle.None;
            }
        }

        private async Task StartControllers()
        {
            await _mapViewController.StartControllerAsync();
            await _ratingApplicationController.StartControllerAsync();
            await _championshipController.StartControllerAsync();
            await _sessionEventsController.StartControllerAsync();
            await _pitBoardController.StartControllerAsync();
            await _fuelConsumptionController.StartControllerAsync();
            await _settingsWindowController.StartControllerAsync();
            await _raceSuggestionController.StartControllerAsync();
            await _driverDetailsController.StartControllerAsync();
            await _openTelemetryChecklistController.StartControllerAsync();
            await _pitStopStatisticsController.StartControllerAsync();
            await _pitPredictionController.StartControllerAsync();
            await _autoDeleteController.StartControllerAsync();
        }

        private Task StopChildControllers()
        {
            return Task.WhenAll(new[]
            {
                _periodicPerformanceMonitor.Finish(),
                _ratingApplicationController.StopControllerAsync(),
                _championshipController.StopControllerAsync(),
                _sessionEventsController.StopControllerAsync(),
                _pitBoardController.StopControllerAsync(),
                _mapViewController.StopControllerAsync(),
                _fuelConsumptionController.StopControllerAsync(),
                _settingsWindowController.StopControllerAsync(),
                _raceSuggestionController.StopControllerAsync(),
                _driverDetailsController.StopControllerAsync(),
                _openTelemetryChecklistController.StopControllerAsync(),
                _pitStopStatisticsController.StopControllerAsync(),
                _pitPredictionController.StopControllerAsync(),
                _autoDeleteController.StopControllerAsync(),
            });
        }

        private void CreateReportsController()
        {
            _reportsController = new ReportsController(_displaySettingsViewModel);
        }

        public Task OnDisplayMessage(object sender, MessageArgs e)
        {
            _timingApplicationViewModel?.DisplayMessage(e);
            return Task.CompletedTask;
        }

        public async Task OnSessionStarted(SimulatorDataSet dataSet)
        {
            await _updateSemaphore.WaitAsync();
            try
            {
                _periodicPerformanceMonitor.ResetHighestCpuUsage();
                _sessionEventsController.UpdateLastDataSet(dataSet);
                _sessionEventsController.Reset();
                _pitBoardController.Reset();
                _forceSingleClassVisitor.Reset();
                _pitStopStatisticsController.Reset();
                _forceSingleClassVisitor.Visit(dataSet);
                _sessionEventsController.Visit(dataSet);
                await _ratingApplicationController.NotifyDataLoaded(dataSet);
                _trackRecordsController.OnSessionStarted(dataSet);
                if (_timingApplicationViewModel != null)
                {
                    await _timingApplicationViewModel.StartNewSession(dataSet);
                }

                _fuelConsumptionController.Reset();
                _periodicPerformanceMonitor.ResetHighestCpuUsage();
            }
            finally
            {
                _updateSemaphore.Release();
            }
        }

        public async Task OnDataLoaded(SimulatorDataSet dataSet)
        {
            await _updateSemaphore.WaitAsync();
            try
            {
                _sessionEventsController.UpdateLastDataSet(dataSet);
                _forceSingleClassVisitor.Visit(dataSet);
                _simSettingController?.ApplySimSettings(dataSet);
                _simulatorContentController.Visit(dataSet);
                _sessionEventsController.Visit(dataSet);
                await _timingApplicationViewModel.ApplyDateSet(dataSet);
                _fuelConsumptionController.ApplyDataSet(dataSet);
                await _ratingApplicationController.NotifyDataLoaded(dataSet);
            }
            catch (SimSettingsException ex)
            {
                _timingApplicationViewModel.DisplayMessage(new MessageArgs(ex.Message));
                _simSettingController?.ApplySimSettings(dataSet);
            }
            finally
            {
                _updateSemaphore.Release();
            }
        }

        private void CreateSimSettingsController()
        {
            _simSettingController = _simSettingControllerFactory.CreateSimSettingController();
        }

        private void CreateGui()
        {
            _timingGui = new TimingGui(_displaySettingsViewModel.IsHwAccelerationEnabled);
            _timingGui.Show();
            _timingGui.Closing += OnGuiClosed;
            _timingGui.StateChanged += TimingGuiOnStateChanged;

            if (_displaySettingsViewModel?.WindowLocationSetting != null)
            {
                _timingGui.WindowStartupLocation = WindowStartupLocation.Manual;
                _timingGui.Left = _displaySettingsViewModel.WindowLocationSetting.Left;
                _timingGui.Top = _displaySettingsViewModel.WindowLocationSetting.Top;
                _timingGui.WindowState = WindowState.Normal;
                _timingGui.WindowState = (WindowState)_displaySettingsViewModel.WindowLocationSetting.WindowState;
                if (_timingGui.WindowState == WindowState.Maximized)
                {
                    _timingGui.WindowStyle = WindowStyle.None;
                }
                else if (_displaySettingsViewModel.WindowLocationSetting.Height > 0 && _displaySettingsViewModel.WindowLocationSetting.Width > 0)
                {
                    _timingGui.Width = _displaySettingsViewModel.WindowLocationSetting.Width;
                    _timingGui.Height = _displaySettingsViewModel.WindowLocationSetting.Height;
                }
            }

            _timingGui.DataContext = _timingApplicationViewModel;
            Application.Current.MainWindow = _timingGui;
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        private void TimingGuiOnStateChanged(object sender, EventArgs e)
        {
            if (_timingApplicationViewModel == null)
            {
                return;
            }

            switch (_timingGui.WindowState)
            {
                case WindowState.Normal:
                    _timingApplicationViewModel.IsPaused = false;
                    break;
                case WindowState.Minimized:
                    _timingApplicationViewModel.IsPaused = true;
                    break;
                case WindowState.Maximized:
                    _timingApplicationViewModel.IsPaused = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async void OnGuiClosed(object sender, CancelEventArgs e)
        {
            try
            {
                await Task.WhenAll(_timingPlugins.Select(x => x.StopControllerAsync()).ToList());
                Task stopTask = StopChildControllers();
                _displaySettingsViewModel.WindowLocationSetting = new WindowLocationSetting()
                {
                    Left = _timingGui.Left,
                    Top = _timingGui.Top,
                    Width = _timingGui.Width,
                    Height = _timingGui.Height,
                    WindowState = (int)_timingGui.WindowState
                };
                List<Exception> exceptions = new List<Exception>();
                _driverPresentationsManager.SavePresentations();
                _timingApplicationViewModel.SessionCompleted -= TimingApplicationViewModelOnSessionCompleted;
                _timingApplicationViewModel?.TerminatePeriodicTask(exceptions);
                _displaySettingsLoader.TrySaveDisplaySettings(_displaySettingsViewModel.SaveToNewModel(), SettingsPath);
                await Task.WhenAll(stopTask,
                    _trackRecordsController.StopControllerAsync(),
                    _simulatorContentController.StopControllerAsync());
                await PluginManager.DeletePlugin(this, exceptions);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void BindCommands()
        {
            _timingApplicationViewModel.SidebarViewModel.OpenSettingsCommand = new RelayCommand(OpenSettingsWindow);
            _timingApplicationViewModel.SidebarViewModel.OpenCarSettingsCommand = new RelayCommand(OpenCarSettingsWindow);
            _timingApplicationViewModel.SidebarViewModel.OpenCurrentTelemetrySession = new AsyncCommand(OpenCurrentTelemetrySession);
            _timingApplicationViewModel.SidebarViewModel.OpenLastReportCommand = new RelayCommand(_reportsController.OpenLastReport);
            _timingApplicationViewModel.SidebarViewModel.OpenReportFolderCommand = new RelayCommand(_reportsController.OpenReportsFolder);
            _timingApplicationViewModel.SidebarViewModel.OpenChampionshipWindowCommand = new RelayCommand(_championshipController.OpenChampionshipWindow);
            _timingApplicationViewModel.SidebarViewModel.OpenRandomSuggestionsCommand = new RelayCommand(_raceSuggestionController.ToggleRaceSuggestionViewVisibility);
            _timingApplicationViewModel.TimingDataGridViewModel.OpenSelectedDriversLapsCommand = new RelayCommand(OpenSelectedDriversLaps);
            _timingApplicationViewModel.SidebarViewModel.OpenTelemetryCheckCommand = new RelayCommand(OpenTelemetryChecklist);
        }

        private void OpenTelemetryChecklist()
        {
            if (_timingApplicationViewModel.SessionTiming?.Player == null)
            {
                return;
            }

            _openTelemetryChecklistController.OpenLapSelection(_timingApplicationViewModel.SessionTiming.Player);
        }

        private void OpenSelectedDriversLaps()
        {
            if (_driverDetailsController == null || _timingApplicationViewModel?.TimingDataGridViewModel?.SelectedDriverTimingViewModel?.DriverTiming == null)
            {
                return;
            }

            _driverDetailsController.OpenWindowDefault(_timingApplicationViewModel.TimingDataGridViewModel.SelectedDriverTimingViewModel.DriverTiming);
        }

        private void CreateDisplaySettingsViewModel()
        {
            _displaySettingsViewModel = _settingsProvider.DisplaySettingsViewModel;
            FillDisplaySettingsOptions();
        }

        private void FillDisplaySettingsOptions()
        {
            _displaySettingsViewModel.RatingSettingsViewModel.AvailableReferenceRatingProviders = _referenceRatingProviderFactory.GetAvailableReferenceRatingsProviders();
        }

        private void OpenSettingsWindow()
        {
            _settingsWindowController.OpenSettingWindow();
        }

        private void OpenCarSettingsWindow()
        {
            _simSettingController.OpenCarSettingsControl(_timingGui);
        }

        private void TimingApplicationViewModelOnPlayerFinished(object sender, SessionSummaryEventArgs e)
        {
            if (e.Summary.SessionType == SessionType.Race && e.Summary.Drivers.Any(x => x.IsPlayer && x.Finished))
            {
                _reportsController?.CreateReport(e.Summary, "_Line");
            }

            if (e.Summary.SessionType == SessionType.Race)
            {
                _ratingApplicationController.OnPlayerFinished(e.Summary);
            }
        }

        private void ApplyResources()
        {
            var applicationResourcesUri = new Uri("pack://application:,,,/WindowsControls;component/WPF/CommonResources.xaml", UriKind.RelativeOrAbsolute);
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = applicationResourcesUri });

            applicationResourcesUri = new Uri("pack://application:,,,/WindowsControls;component/ControlsPresentationTemplates.xaml", UriKind.RelativeOrAbsolute);
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = applicationResourcesUri });

            applicationResourcesUri = new Uri("pack://application:,,,/Rating.Presentation;component/RatingPresentation.xaml", UriKind.RelativeOrAbsolute);
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = applicationResourcesUri });

            applicationResourcesUri = new Uri("pack://application:,,,/Rating.Presentation;component/RatingPresentationTemplates.xaml", UriKind.RelativeOrAbsolute);
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = applicationResourcesUri });

            applicationResourcesUri = new Uri("pack://application:,,,/TelemetryPresentation;component/TelemetryPresentationTemplates.xaml", UriKind.RelativeOrAbsolute);
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = applicationResourcesUri });

            applicationResourcesUri = new Uri("/Timing.Application;component/Presentation/TimingApplicationTemplates.xaml", UriKind.RelativeOrAbsolute);
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = applicationResourcesUri });
        }

        private void ApplyCustomColors()
        {
            try
            {
                string sourcePath = Path.Combine(GetAssemblyDirectory(), "Colors.xaml");
                string destinationPath = Path.Combine(_displaySettingsViewModel.ReportingSettingsViewModel.ExportDirectoryReplacedSpecialDirs, "colors.xaml");
                File.Copy(sourcePath, destinationPath, true);

                if (string.IsNullOrWhiteSpace(_displaySettingsViewModel.CustomResourcesPath) || !File.Exists(_displaySettingsViewModel.CustomResourcesPath))
                {
                    return;
                }

                var uri = new Uri(_displaySettingsViewModel.CustomResourcesPath);
                ResourceDictionary replacementColors = new ResourceDictionary() { Source = uri };
                foreach (DictionaryEntry replacementColor in replacementColors)
                {
                    if (Application.Current.Resources.Contains(replacementColor.Key))
                    {
                        Application.Current.Resources.Remove(replacementColor.Key);
                        Application.Current.Resources.Add(replacementColor.Key, replacementColor.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private async void TimingApplicationViewModelOnSessionCompleted(object sender, SessionSummaryEventArgs e)
        {
            await _ratingApplicationController.NotifySessionCompletion(e.Summary);
            _reportsController?.CreateReport(e.Summary, e.Summary.SessionType == SessionType.Race ? "_End" : string.Empty);
        }
    }
}