namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.MainWindow.GraphPanel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AggregatedCharts;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Layouts;
    using SecondMonitor.ViewModels.Layouts.Editor;
    using Settings;
    using Settings.Car;
    using Synchronization;
    using TelemetryApplication.Settings.DTO;
    using ViewModels.LoadedLapCache;

    public class WorkspaceAggregatedChartsController : IWorkspaceAggregatedChartsController
    {
        private readonly ITelemetryViewsSynchronization _telemetryViewsSynchronization;
        private readonly ISettingsController _settingsController;
        private readonly AggregatedChartProviderFactory _aggregatedChartProviderFactory;
        private readonly ILoadedLapsCache _loadedLapsCache;
        private readonly Dictionary<string, IAggregatedChartProvider> _providersCache;
        private readonly List<ChartWithProvider> _createdCharts;

        public WorkspaceAggregatedChartsController(ITelemetryViewsSynchronization telemetryViewsSynchronization, ISettingsController settingsController, AggregatedChartProviderFactory aggregatedChartProviderFactory, ILoadedLapsCache loadedLapsCache)
        {
            _providersCache = new Dictionary<string, IAggregatedChartProvider>();
            _createdCharts = new List<ChartWithProvider>();
            _telemetryViewsSynchronization = telemetryViewsSynchronization;
            _settingsController = settingsController;
            _aggregatedChartProviderFactory = aggregatedChartProviderFactory;
            _loadedLapsCache = loadedLapsCache;
        }

        public Task StartControllerAsync()
        {
            _telemetryViewsSynchronization.LapLoadingFinished += TelemetryViewsSynchronizationOnLapLoadedFinished;
            _settingsController.CarSettingsController.CarPropertiesChanges += CarSettingsControllerOnCarPropertiesChanges;
            return Task.CompletedTask;
        }

        public Task StopControllerAsync()
        {
            _telemetryViewsSynchronization.LapLoadingFinished -= TelemetryViewsSynchronizationOnLapLoadedFinished;
            _settingsController.CarSettingsController.CarPropertiesChanges -= CarSettingsControllerOnCarPropertiesChanges;
            return Task.CompletedTask;
        }

        public IViewModel GetChartByName(string name)
        {
            (IViewModel contentViewModel, IAggregatedChartProvider aggregatedChartProvider) = GetAggregatedChartByName(name);
            ViewModelContainer wrapper = new ViewModelContainer(contentViewModel);
            _createdCharts.Add(new ChartWithProvider(name, wrapper, aggregatedChartProvider?.IsUsingCarProperties ?? false));
            return wrapper;
        }

        private (IViewModel ViewModel, IAggregatedChartProvider AggregatedChartProvider) GetAggregatedChartByName(string name)
        {
            IViewModel contentViewModel;
            if (!TryGetProviderForChart(name, out IAggregatedChartProvider chartProvider))
            {
                contentViewModel = new EmptyViewModel();
            }
            else
            {
                contentViewModel = chartProvider.CreateAggregatedChartViewModels(GetChartSettings()).FirstOrDefault();
            }

            return (contentViewModel, chartProvider);
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

        private bool TryGetProviderForChart(string chartName, out IAggregatedChartProvider aggregatedChartProvider)
        {
            if (_providersCache.TryGetValue(chartName, out aggregatedChartProvider))
            {
                return true;
            }

            aggregatedChartProvider = _aggregatedChartProviderFactory.Create(chartName);
            if (aggregatedChartProvider != null)
            {
                _providersCache[chartName] = aggregatedChartProvider;
            }

            return aggregatedChartProvider != null;
        }

        private void TelemetryViewsSynchronizationOnLapLoadedFinished(object sender, EventArgs eventArgs)
        {
            RefreshAllAggregatedCharts();
        }

        private void RefreshAllAggregatedCharts()
        {
            foreach (var createdChart in _createdCharts)
            {
                createdChart.WrapperViewModel.ViewModel = GetAggregatedChartByName(createdChart.ProviderName).ViewModel;
            }
        }

        private void CarSettingsControllerOnCarPropertiesChanges(object sender, CarPropertiesArgs e)
        {
            foreach (var createdChart in _createdCharts.Where(x => x.RefreshOnCarSettingsChange))
            {
                createdChart.WrapperViewModel.ViewModel = GetAggregatedChartByName(createdChart.ProviderName).ViewModel;
            }
        }
    }
}