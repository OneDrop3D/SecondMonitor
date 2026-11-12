namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.MainWindow.LapPicker
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Forms;
    using Checks;
    using Contracts.Commands;
    using Contracts.UserInput;

    using OpenWindow;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.Telemetry.TelemetryApplication.Controllers.LapsCharts;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Colors;
    using SecondMonitor.ViewModels.Controllers;
    using SecondMonitor.ViewModels.Factory;
    using SettingsWindow;
    using Synchronization;
    using Synchronization.Graphs;
    using TelemetryLoad;
    using TelemetryManagement.DTO;
    using ViewModels;
    using ViewModels.LapPicker;
    using ViewModels.LoadedLapCache;
    using MessageBox = System.Windows.MessageBox;

    public class LapPickerController : AbstractController, ILapPickerController
    {
        private readonly ITelemetryViewsSynchronization _telemetryViewsSynchronization;
        private readonly ITelemetryLoadController _telemetryLoadController;
        private readonly IColorPaletteProvider _colorPaletteProvider;
        private readonly IOpenWindowController _openWindowController;
        private readonly ISettingsWindowController _settingsWindowController;
        private readonly IUserInputProvider _userInputProvider;
        private readonly IDialogService _dialogService;
        private readonly ILoadedLapsCache _loadedLapsCache;
        private readonly ITelemetryCheckController _telemetryCheckController;
        private readonly ILapsChartsController _lapsChartsController;
        private readonly LapSelectionViewModel _lapSelectionViewModel;
        private readonly List<LapSummaryDto> _allAvailableLaps;
        private readonly List<LapSummaryDto> _loadedLaps;
        private SessionInfoDto _mainSession;

        public LapPickerController(ITelemetryViewsSynchronization telemetryViewsSynchronization, ITelemetryLoadController telemetryLoadController, IMainWindowViewModel mainWindowViewModel, IViewModelFactory viewModelFactory,
             IColorPaletteProvider colorPaletteProvider, IOpenWindowController openWindowController, ISettingsWindowController settingsWindowController, IUserInputProvider userInputProvider, IDialogService dialogService,
             ILoadedLapsCache loadedLapsCache, ITelemetryCheckController telemetryCheckController, IChildControllerFactory childControllerFactory) : base(viewModelFactory)
        {
            _allAvailableLaps = new List<LapSummaryDto>();
            _loadedLaps = new List<LapSummaryDto>();
            _telemetryViewsSynchronization = telemetryViewsSynchronization;
            _telemetryLoadController = telemetryLoadController;
            _lapSelectionViewModel = mainWindowViewModel.LapSelectionViewModel;
            _colorPaletteProvider = colorPaletteProvider;
            _openWindowController = openWindowController;
            _settingsWindowController = settingsWindowController;
            _userInputProvider = userInputProvider;
            _dialogService = dialogService;
            _loadedLapsCache = loadedLapsCache;
            _telemetryCheckController = telemetryCheckController;
            _lapsChartsController = childControllerFactory.Create<ILapsChartsController, ILapPickerController>(this);
        }

        public override async Task StartControllerAsync()
        {
            Subscribe();
            _lapSelectionViewModel.AddCustomLapCommand = new AsyncCommand(AddCustomLap);
            _lapSelectionViewModel.LoadAllLapsCommand = new AsyncCommand(LoadAllLaps);
            _lapSelectionViewModel.UnloadAllLapsCommand = new AsyncCommand(UnLoadAllLaps);
            _lapSelectionViewModel.SaveSessionCommand = new AsyncCommand(SaveMainSession);
            _lapSelectionViewModel.SaveAsNewSessionCommand = new AsyncCommand(SaveAsNewSession);
            _lapSelectionViewModel.ImportSessionCommand = new AsyncCommand(ImportSession);
            _lapSelectionViewModel.ExportSessionCommand = new AsyncCommand(ExportSession);
            _lapSelectionViewModel.OpenLapChartCommand = new RelayCommand(OpenLapChart);
            _lapSelectionViewModel.AvailableStints = new[] { "All" }.Concat(Enumerable.Range(0, 20).Select(x => x.ToString())).ToList();
            _lapSelectionViewModel.SelectedStint = "All";
            await StartChildControllersAsync();
        }

        private Task UnLoadAllLaps()
        {
            var lapsToUnloadLoad = _lapSelectionViewModel.LapSummaries.Where(x => x.Selected).Select(x => x.OriginalModel);
            if (int.TryParse(_lapSelectionViewModel.SelectedStint, out int stintToLoad))
            {
                lapsToUnloadLoad = lapsToUnloadLoad.Where(x => x.Stint == stintToLoad);
            }

            return _telemetryLoadController.UnloadLaps(lapsToUnloadLoad);
        }

        private Task LoadAllLaps()
        {
            var lapsToLoad = _lapSelectionViewModel.LapSummaries.Where(x => !x.Selected).Select(x => x.OriginalModel);
            if (int.TryParse(_lapSelectionViewModel.SelectedStint, out int stintToLoad))
            {
                lapsToLoad = lapsToLoad.Where(x => x.Stint == stintToLoad);
            }

            return _telemetryLoadController.LoadLaps(lapsToLoad);
        }

        public override async Task StopControllerAsync()
        {
            UnSubscribe();
            await StopChildControllersAsync();
        }

        private async Task StartChildControllersAsync()
        {
            await _telemetryCheckController.StartControllerAsync();
            await _openWindowController.StartControllerAsync();
            await _settingsWindowController.StartControllerAsync();
            await _lapsChartsController.StartControllerAsync();
        }

        private async Task StopChildControllersAsync()
        {
            IEnumerable<Task> unloadTask = _allAvailableLaps.Select(x => _telemetryLoadController.UnloadLap(x));
            await Task.WhenAll(unloadTask);
            await _lapsChartsController.StopControllerAsync();
            await _telemetryCheckController.StopControllerAsync();
            await _openWindowController.StopControllerAsync();
            await _settingsWindowController.StopControllerAsync();
        }

        private void Subscribe()
        {
            _lapSelectionViewModel.PropertyChanged += LapSelectionViewModelOnPropertyChanged;
            _telemetryViewsSynchronization.NewSessionLoaded += OnSessionStarted;
            _telemetryViewsSynchronization.SessionAdded += OnSessionAdded;
            _telemetryViewsSynchronization.LapAddedToSession += OnLapAddedToSession;
            _lapSelectionViewModel.LapSelected += LapSelectionViewModelOnLapSelected;
            _lapSelectionViewModel.LapUnselected += LapSelectionViewModelOnLapUnselected;
            _lapSelectionViewModel.ReferenceLapChanged += LapSelectionViewModelOnReferenceLapChanged;
            _telemetryViewsSynchronization.LapLoaded += TelemetryViewsSynchronizationOnLapLoaded;
            _telemetryViewsSynchronization.LapUnloaded += TelemetryViewsSynchronizationOnLapUnloaded;
            _telemetryViewsSynchronization.SessionModified += TelemetryViewsSynchronizationOnSessionModified;
            _telemetryViewsSynchronization.LapRemovedFromSession += TelemetryViewsSynchronizationOnLapRemovedFromSession;
            _telemetryViewsSynchronization.ReferenceLapSelected += TelemetryViewsSynchronizationOnReferenceLapSelected;
        }

        private void UnSubscribe()
        {
            _lapSelectionViewModel.PropertyChanged -= LapSelectionViewModelOnPropertyChanged;
            _telemetryViewsSynchronization.NewSessionLoaded -= OnSessionStarted;
            _telemetryViewsSynchronization.SessionAdded -= OnSessionAdded;
            _telemetryViewsSynchronization.LapAddedToSession -= OnLapAddedToSession;
            _lapSelectionViewModel.LapSelected -= LapSelectionViewModelOnLapSelected;
            _lapSelectionViewModel.LapUnselected -= LapSelectionViewModelOnLapUnselected;
            _lapSelectionViewModel.ReferenceLapChanged -= LapSelectionViewModelOnReferenceLapChanged;
            _telemetryViewsSynchronization.LapLoaded -= TelemetryViewsSynchronizationOnLapLoaded;
            _telemetryViewsSynchronization.LapUnloaded -= TelemetryViewsSynchronizationOnLapUnloaded;
            _telemetryViewsSynchronization.LapRemovedFromSession -= TelemetryViewsSynchronizationOnLapRemovedFromSession;
        }

        private void TelemetryViewsSynchronizationOnLapRemovedFromSession(object sender, LapsSummaryArgs e)
        {
            _allAvailableLaps.RemoveAll(x => e.LapsSummaries.Any(y => y.Id == x.Id));
            _lapSelectionViewModel.LapSummaries.Where(x => e.LapsSummaries.Any(y => y.Id == x.OriginalModel.Id)).ToList().ForEach(x => _lapSelectionViewModel.LapSummaries.Remove(x));
            RefreshBestLaps();
        }

        private void TelemetryViewsSynchronizationOnSessionModified(object sender, SessionModifiedArgs e)
        {
            _lapSelectionViewModel.IsSaveButtonEnabled = e.SessionInfoDto.SessionTransientData.IsModified;
        }

        private void TelemetryViewsSynchronizationOnReferenceLapSelected(object sender, LapSummaryArgs e)
        {
            _lapSelectionViewModel.ReferenceLapDto = e.LapSummary;
        }

        private void TelemetryViewsSynchronizationOnLapUnloaded(object sender, LapsSummaryArgs lapsSummaryArgs)
        {
            _loadedLaps.RemoveAll(x => lapsSummaryArgs.LapsSummaries.Contains(x));
            var lapViewModels = _lapSelectionViewModel.LapSummaries.Where(x => lapsSummaryArgs.LapsSummaries.Contains(x.OriginalModel));
            lapViewModels.ForEach(x => x.Selected = false);
        }

        private void TelemetryViewsSynchronizationOnLapLoaded(object sender, LapsTelemetryArgs lapsTelemetryArgs)
        {
            var lapSummaries = lapsTelemetryArgs.LapsTelemetries.Select(x => x.LapSummary).ToList();
            _loadedLaps.AddRange(lapSummaries);
            var lapViewModels = _lapSelectionViewModel.LapSummaries.Where(x => lapSummaries.Contains(x.OriginalModel));
            lapViewModels.ForEach(x => x.Selected = true);
        }

        private void LapSelectionViewModelOnReferenceLapChanged(object sender, LapSummaryArgs e)
        {
            if (_loadedLapsCache.ReferenceLap == e.LapSummary)
            {
                return;
            }

            _telemetryViewsSynchronization.NotifyReferenceLapSelected(e.LapSummary);
        }

        private async void LapSelectionViewModelOnLapUnselected(object sender, LapSummaryArgs e)
        {
            if (!_loadedLaps.Exists(x => x.Id == e.LapSummary.Id))
            {
                return;
            }

            if (_mainSession.SessionTransientData.ModifiedLaps.Contains(e.LapSummary) && _dialogService.ShowYesNoDialog("Lap Was Modified", "Lap Was Modified\nDo you want to save the lap before unloading the lap?"))
            {
                await _telemetryLoadController.SaveModifiedLap(e.LapSummary);
            }

            await _telemetryLoadController.UnloadLap(e.LapSummary);
        }

        private void LapSelectionViewModelOnLapSelected(object sender, LapSummaryArgs e)
        {
            if (_loadedLaps.Exists(x => x.Id == e.LapSummary.Id))
            {
                return;
            }

            _telemetryLoadController.LoadLap(e.LapSummary);
        }

        private void OnLapAddedToSession(object sender, LapsSummaryArgs lapsSummaryArgs)
        {
            AddLaps(lapsSummaryArgs.LapsSummaries);
        }

        private void AddLaps(IReadOnlyCollection<LapSummaryDto> lapsSummary)
        {
            var playerLaps = lapsSummary.Where(x => x.IsPlayer).OrderBy(x => x.LapNumber);
            var nonPlayerLaps = lapsSummary.Where(x => !x.IsPlayer).OrderBy(x => x.CustomDisplayName);
            foreach (LapSummaryDto lapSummaryDto in playerLaps.Concat(nonPlayerLaps))
            {
                ILapSummaryViewModel newViewModel = ViewModelFactory.Create<ILapSummaryViewModel>();
                newViewModel.FromModel(lapSummaryDto);
                newViewModel.LapColor = _colorPaletteProvider.GetNext();
                newViewModel.RemoveLapCommand = new AsyncCommand(() => RemoveLap(lapSummaryDto));
                newViewModel.ShowTelemetryChecklist = new AsyncCommand(() => OpenTelemetryChecklist(lapSummaryDto));
                newViewModel.IsVisible = true;
                newViewModel.IsDriverNameVisible = !lapSummaryDto.IsPlayer && !string.IsNullOrWhiteSpace(lapSummaryDto.DriverName);
                _lapSelectionViewModel.AddLapSummaryViewModel(newViewModel);
                _allAvailableLaps.Add(lapSummaryDto);
            }

            RefreshBestLaps();
            _lapSelectionViewModel.RefreshLapSummariesView();
        }

        private async Task OpenTelemetryChecklist(LapSummaryDto lapSummaryDto)
        {
            if (!_loadedLapsCache.TryGetLapTelemetry(lapSummaryDto, out LapTelemetryDto lapTelemetryDto))
            {
                lapTelemetryDto = await _telemetryLoadController.LoadLap(lapSummaryDto);
            }

            _telemetryCheckController.OpenCheckWindow(lapTelemetryDto);
        }

        private void RefreshBestLaps()
        {
            LapSummaryDto bestLap = _allAvailableLaps.Where(x => x.IsPlayer && x.LapTime != TimeSpan.Zero).OrderBy(x => x.LapTime).FirstOrDefault();
            if (bestLap != null && _loadedLaps.Count == 0)
            {
                _lapSelectionViewModel.BestLap = $"{bestLap.CustomDisplayName} - {bestLap.LapTime.FormatToDefault()}";
                _telemetryLoadController.LoadLap(bestLap);
            }

            LapSummaryDto bestSector1Lap = _allAvailableLaps.Where(x => x.IsPlayer && x.Sector1Time > TimeSpan.Zero).OrderBy(x => x.Sector1Time).FirstOrDefault();
            _lapSelectionViewModel.BestSector1 = bestSector1Lap?.Sector1Time > TimeSpan.Zero ? $"{bestSector1Lap.CustomDisplayName} - {bestSector1Lap.Sector1Time.FormatToDefault()}" : string.Empty;

            LapSummaryDto bestSector2Lap = _allAvailableLaps.Where(x => x.IsPlayer && x.Sector2Time > TimeSpan.Zero).OrderBy(x => x.Sector2Time).FirstOrDefault();
            _lapSelectionViewModel.BestSector2 = bestSector2Lap?.Sector2Time > TimeSpan.Zero ? $"{bestSector2Lap.CustomDisplayName} - {bestSector2Lap.Sector2Time.FormatToDefault()}" : string.Empty;

            LapSummaryDto bestSector3Lap = _allAvailableLaps.Where(x => x.IsPlayer && x.Sector3Time > TimeSpan.Zero).OrderBy(x => x.Sector3Time).FirstOrDefault();
            _lapSelectionViewModel.BestSector3 = bestSector3Lap?.Sector3Time > TimeSpan.Zero ? $"{bestSector3Lap.CustomDisplayName} - {bestSector3Lap.Sector3Time.FormatToDefault()}" : string.Empty;
        }

        private async Task RemoveLap(LapSummaryDto lapSummary)
        {
            await _telemetryLoadController.RemoveLap(lapSummary);
        }

        private void AddLapsFromSession(SessionInfoDto sessionInfoDto)
        {
            AddLaps(sessionInfoDto.LapsSummary.Where(x => _allAvailableLaps.All(y => y.Id != x.Id)).ToList());
        }

        private void ReinitializeViewMode(SessionInfoDto sessionInfoDto)
        {
            _lapSelectionViewModel.Clear();
            _allAvailableLaps.Clear();
            _loadedLaps.Clear();
            _lapSelectionViewModel.TrackName = string.IsNullOrEmpty(sessionInfoDto.LayoutName) ? sessionInfoDto.TrackName : $"{sessionInfoDto.TrackName} - {sessionInfoDto.LayoutName}";
            _lapSelectionViewModel.CarName = sessionInfoDto.CarName;
            _lapSelectionViewModel.SessionTime = sessionInfoDto.SessionRunDateTime;
            _lapSelectionViewModel.SimulatorName = sessionInfoDto.Simulator;
            _lapSelectionViewModel.SessionName = sessionInfoDto.SessionCustomName;
            AddLapsFromSession(sessionInfoDto);
        }

        private void OnSessionAdded(object sender, TelemetrySessionArgs e)
        {
            AddLapsFromSession(e.SessionInfoDto);
        }

        private void LapSelectionViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(LapSelectionViewModel.SessionName))
            {
                return;
            }

            _mainSession.SessionCustomName = _lapSelectionViewModel.SessionName;
            _mainSession.SessionTransientData.IsModified = true;
            _telemetryViewsSynchronization.NotifySessionModified(_mainSession);
        }

        private async Task AddCustomLap()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".lap; .plap", Filter = "Lap Files (*.lap, *.plap)|*.lap; *.plap",
                Multiselect = true
            };
            bool? result = dlg.ShowDialog();
            if (result == false)
            {
                return;
            }

            List<Task> loadTask = new List<Task>();
            foreach (string fileName in dlg.FileNames)
            {
                string fileCustomName = await _userInputProvider.GetUserInput("Enter Lap Name:", $"Ex-{Path.GetFileNameWithoutExtension(fileName)}");
                loadTask.Add(_telemetryLoadController.LoadLap(new FileInfo(fileName), fileCustomName));
            }

            await Task.WhenAll(loadTask);
        }

        private void OnSessionStarted(object sender, TelemetrySessionArgs e)
        {
            _mainSession = e.SessionInfoDto;
            ReinitializeViewMode(e.SessionInfoDto);
        }

        private async Task SaveMainSession()
        {
            await _telemetryLoadController.SaveMainSession();
        }

        private async Task SaveAsNewSession()
        {
            await _telemetryLoadController.ArchiveSession(_mainSession);
        }

        private async Task ExportSession()
        {
            if (_mainSession.SessionTransientData.IsModified)
            {
                if (!_dialogService.ShowYesNoDialog("Session Modified", "Session is modified, and needs to be saved before it can be exported. To you want to save it?"))
                {
                    return;
                }

                await _telemetryLoadController.SaveMainSession();
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.DefaultExt = ".tzip";
                saveFileDialog.Filter = @"Telemetry Archive (*.tzip)|*.tzip";
                DialogResult result = saveFileDialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(saveFileDialog.FileName))
                {
                    await _telemetryLoadController.ExportTelemetry(_mainSession, saveFileDialog.FileName);
                    MessageBox.Show("Telemetry exported successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void OpenLapChart()
        {
            _lapsChartsController.OpenOrFocusLapChartsWindow(_allAvailableLaps, _lapSelectionViewModel.LapSummaries.Where(x => x.Selected).Select(x => x.OriginalModel.Id).ToList());
        }

        private async Task ImportSession()
        {
            try
            {
                SessionInfoDto newSession = null;
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.DefaultExt = ".tzip";
                    openFileDialog.Filter = @"Telemetry Archive (*.tzip)|*.tzip";
                    DialogResult result = openFileDialog.ShowDialog();

                    if (result != DialogResult.OK || string.IsNullOrWhiteSpace(openFileDialog.FileName))
                    {
                        return;
                    }

                    newSession = await _telemetryLoadController.ImportTelemetry(openFileDialog.FileName);
                }

                if (newSession == null)
                {
                    MessageBox.Show("Unable To Import Session", "Failed to Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_dialogService.ShowYesNoDialog("Import Successful", "Session was imported successfully into archived, Do you want to open it?"))
                {
                    await _telemetryLoadController.LoadRecentSessionAsync(newSession);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while importing Session\n" + ex.Message, "Failed to Import", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}