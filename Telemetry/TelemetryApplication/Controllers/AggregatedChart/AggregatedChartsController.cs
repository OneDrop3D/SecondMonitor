namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.AggregatedChart
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using AggregatedCharts;
    using Contracts.Commands;
    using MainWindow.GraphPanel;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Layouts;
    using Settings;
    using Settings.Car;
    using Synchronization;
    using TelemetryApplication.Settings.DTO;
    using ViewModels;
    using ViewModels.AggregatedCharts;
    using ViewModels.LoadedLapCache;
    using ViewModels.SettingsWindow;

    public class AggregatedChartsController : IAggregatedChartsController
    {
        private readonly IMainWindowViewModel _mainWindowViewModel;
        private readonly ILoadedLapsCache _loadedLapsCache;
        private readonly IWindowService _windowService;
        private readonly IViewModelFactory _viewModelFactory;
        private readonly ITelemetryViewsSynchronization _telemetryViewsSynchronization;
        private readonly ISettingsController _settingsController;
        private readonly List<IAggregatedChartProvider> _aggregatedChartProviders;
        private readonly List<ChartWithProviderAndWindow> _chartWithProviderAndWindows;
        private Window _chartSelectionWindow;

        public AggregatedChartsController(IEnumerable<IAggregatedChartProvider> aggregatedChartProviders, IMainWindowViewModel mainWindowViewModel, ILoadedLapsCache loadedLapsCache, IWindowService windowService, IViewModelFactory viewModelFactory,
            ITelemetryViewsSynchronization telemetryViewsSynchronization, ISettingsController settingsController)
        {
            _loadedLapsCache = loadedLapsCache;
            _windowService = windowService;
            _viewModelFactory = viewModelFactory;
            _telemetryViewsSynchronization = telemetryViewsSynchronization;
            _settingsController = settingsController;
            _mainWindowViewModel = mainWindowViewModel;
            _loadedLapsCache = loadedLapsCache;
            _aggregatedChartProviders = aggregatedChartProviders.ToList();
            _chartWithProviderAndWindows = new List<ChartWithProviderAndWindow>();
        }

        public Task StartControllerAsync()
        {
            BindCommands();
            Subscribe();
            return Task.CompletedTask;
        }

        private void Subscribe()
        {
            _telemetryViewsSynchronization.LapLoaded += TelemetryViewsSynchronizationOnLapLoaded;
            _telemetryViewsSynchronization.LapUnloaded += TelemetryViewsSynchronizationOnLapUnloaded;
            _settingsController.CarSettingsController.CarPropertiesChanges += CarSettingsControllerOnCarPropertiesChanges;
        }

        private void UnSubscribe()
        {
            _telemetryViewsSynchronization.LapLoaded -= TelemetryViewsSynchronizationOnLapLoaded;
            _telemetryViewsSynchronization.LapUnloaded -= TelemetryViewsSynchronizationOnLapUnloaded;
            _settingsController.CarSettingsController.CarPropertiesChanges -= CarSettingsControllerOnCarPropertiesChanges;
        }

        private void TelemetryViewsSynchronizationOnLapLoaded(object sender, LapsTelemetryArgs lapsTelemetryArgs)
        {
            RefreshOpenSelectorButton();
        }

        private void TelemetryViewsSynchronizationOnLapUnloaded(object sender, LapsSummaryArgs lapsSummaryArgs)
        {
            RefreshOpenSelectorButton();
        }

        private void RefreshOpenSelectorButton()
        {
            _mainWindowViewModel.LapSelectionViewModel.IsOpenAggregatedChartSelectorEnabled = _loadedLapsCache.LoadedLaps.Count > 0;
        }

        private void BindCommands()
        {
            _mainWindowViewModel.LapSelectionViewModel.OpenAggregatedChartSelectorCommand = new RelayCommand(OpenAggregatedChartSelector);
        }

        private void OpenAggregatedChartSelector()
        {
            if (_loadedLapsCache.LoadedLaps.Count == 0)
            {
                return;
            }

            if (_chartSelectionWindow?.IsLoaded == true)
            {
                _chartSelectionWindow.Focus();
                return;
            }

            IAggregatedChartSelectorViewModel viewModel = CreateAggregatedChartSelectionViewModel();
            _chartSelectionWindow = _windowService.OpenWindow(viewModel, "Select Aggregated Chart");
        }

        private IAggregatedChartSelectorViewModel CreateAggregatedChartSelectionViewModel()
        {
            IAggregatedChartSelectorViewModel viewModel = _viewModelFactory.Create<IAggregatedChartSelectorViewModel>();
            viewModel.HistogramChartNames = _aggregatedChartProviders.Where(x => x.Kind == AggregatedChartKind.Histogram).Select(x => x.ChartName).OrderBy(x => x).ToList();
            viewModel.ScatterPlotChartNames = _aggregatedChartProviders.Where(x => x.Kind == AggregatedChartKind.ScatterPlot).Select(x => x.ChartName).OrderBy(x => x).ToList();
            viewModel.CancelAndCloseWindowCommand = new RelayCommand(CancelAndCloseSelectionWindow);
            viewModel.OpenSelectedChartCommand = new RelayCommand(OpenSelectedChart);

            IAggregatedChartSettingsViewModel aggregatedChartSettingsViewModel = _viewModelFactory.Create<IAggregatedChartSettingsViewModel>();
            aggregatedChartSettingsViewModel.FromModel(_settingsController.TelemetrySettings.AggregatedChartSettings);
            viewModel.AggregatedChartSettingsViewModel = aggregatedChartSettingsViewModel;
            return viewModel;
        }

        private void OpenSelectedChart()
        {
            if (!(_chartSelectionWindow?.Content is IAggregatedChartSelectorViewModel viewModel))
            {
                return;
            }

            CancelAndCloseSelectionWindow();

            string providerName = viewModel.SelectedTabIndex == 0 ? viewModel.SelectedHistogramChartName : viewModel.SelectedScatterPlotChartName;
            AggregatedChartSettingsDto aggregatedChartSettingsDto = viewModel.AggregatedChartSettingsViewModel.SaveToNewModel();
            _settingsController.TelemetrySettings.AggregatedChartSettings = aggregatedChartSettingsDto;

            if (string.IsNullOrWhiteSpace(providerName))
            {
                return;
            }

            IAggregatedChartProvider selectedProvider = _aggregatedChartProviders.FirstOrDefault(x => x.ChartName == providerName);

            if (selectedProvider == null)
            {
                return;
            }

            List<ViewModelContainer> chartViewModels = selectedProvider.CreateAggregatedChartViewModels(aggregatedChartSettingsDto).Select(x => new ViewModelContainer(x)).ToList();
            List<Window> windows = OpenAggregatedCharts(chartViewModels);
            if (windows.Count != chartViewModels.Count)
            {
                return;
            }

            Guid groupIdentifier = Guid.NewGuid();
            for (int i = 0; i < chartViewModels.Count; i++)
            {
                Window window = windows[i];
                window.Closed += AggregatedChartWindowOnClosed;
                _chartWithProviderAndWindows.Add(new ChartWithProviderAndWindow(providerName, chartViewModels[i], selectedProvider.IsUsingCarProperties, window, groupIdentifier));
            }
        }

        private void AggregatedChartWindowOnClosed(object sender, EventArgs e)
        {
            _chartWithProviderAndWindows.RemoveAll(x => x.Window == sender);
        }

        private void CarSettingsControllerOnCarPropertiesChanges(object sender, CarPropertiesArgs e)
        {
            AggregatedChartSettingsDto settingsDto = GetChartSettings();
            var affectedGroups = _chartWithProviderAndWindows.Where(x => x.RefreshOnCarSettingsChange).GroupBy(x => x.GroupIdentifier);
            foreach (IGrouping<Guid, ChartWithProviderAndWindow> chartWithProviderAndWindowsGroup in affectedGroups)
            {
                var chartWithProviderAndWindows = chartWithProviderAndWindowsGroup.ToList();
                IAggregatedChartProvider selectedProvider = _aggregatedChartProviders.First(x => x.ChartName == chartWithProviderAndWindows[0].ProviderName);
                List<IAggregatedChartViewModel> chartViewModels = selectedProvider.CreateAggregatedChartViewModels(settingsDto).ToList();
                if (chartViewModels.Count != chartWithProviderAndWindows.Count)
                {
                    return;
                }

                for (int i = 0; i < chartViewModels.Count; i++)
                {
                    chartWithProviderAndWindows[i].WrapperViewModel.ViewModel = chartViewModels[i];
                }
            }
        }

        private AggregatedChartSettingsDto GetChartSettings()
        {
            StintRenderingKind preferredChartsKind = _settingsController.TelemetrySettings.AggregatedChartSettings.StintRenderingKind;
            int firstLapStint = _loadedLapsCache.LoadedLaps.FirstOrDefault()?.LapSummary.Stint ?? 0;
            if (preferredChartsKind == StintRenderingKind.None || _loadedLapsCache.LoadedLaps.All(x => x.LapSummary.Stint == firstLapStint))
            {
                return new AggregatedChartSettingsDto()
                {
                    StintRenderingKind = StintRenderingKind.None
                };
            }

            return new AggregatedChartSettingsDto()
            {
                StintRenderingKind = StintRenderingKind.SingleChart
            };
        }

        private List<Window> OpenAggregatedCharts(IEnumerable<ViewModelContainer> aggregatedChartViewModels)
        {
            return aggregatedChartViewModels.Select(x => _windowService.OpenWindow(x, ((IAggregatedChartViewModel)x.ViewModel).Title, WindowState.Maximized, SizeToContent.Manual, WindowStartupLocation.CenterScreen)).ToList();
        }

        private void CancelAndCloseSelectionWindow()
        {
            if (_chartSelectionWindow?.IsLoaded != true)
            {
                return;
            }

            _chartSelectionWindow.Close();
        }

        public Task StopControllerAsync()
        {
            UnSubscribe();
            return Task.CompletedTask;
        }
    }
}