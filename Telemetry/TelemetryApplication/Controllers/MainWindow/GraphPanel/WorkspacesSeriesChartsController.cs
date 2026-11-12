namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.MainWindow.GraphPanel
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Settings;
    using Settings;
    using Settings.Car;
    using Synchronization;
    using Synchronization.Graphs;
    using TelemetryManagement.DTO;
    using ViewModels.GraphPanel;
    using CollectionExtension = SecondMonitor.DataModel.Extensions.CollectionExtension;

    public class WorkspacesSeriesChartsController : IWorkspacesSeriesChartsController
    {
        private readonly ITelemetryViewsSynchronization _telemetryViewsSynchronization;
        private readonly ILapColorSynchronization _lapColorSynchronization;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IGraphViewSynchronization _graphViewSynchronization;
        private readonly ISettingsController _telemetrySettings;
        private readonly SeriesChartFactory _seriesChartFactory;
        private readonly List<LapTelemetryDto> _loadedLaps;
        private readonly List<IGraphViewModel> _charts;

        public WorkspacesSeriesChartsController(ITelemetryViewsSynchronization telemetryViewsSynchronization, ILapColorSynchronization lapColorSynchronization, ISettingsProvider settingsProvider,
            IGraphViewSynchronization graphViewSynchronization, ISettingsController telemetrySettings, SeriesChartFactory seriesChartFactory)
        {
            _charts = new List<IGraphViewModel>();
            _telemetryViewsSynchronization = telemetryViewsSynchronization;
            _lapColorSynchronization = lapColorSynchronization;
            _settingsProvider = settingsProvider;
            _graphViewSynchronization = graphViewSynchronization;
            _telemetrySettings = telemetrySettings;
            _seriesChartFactory = seriesChartFactory;
            _loadedLaps = new List<LapTelemetryDto>();
        }

        public Task StartControllerAsync()
        {
            Subscribe();
            return Task.CompletedTask;
        }

        public Task StopControllerAsync()
        {
            Unsubscribe();
            _charts.ForEach(x => x.Dispose());
            return Task.CompletedTask;
        }

        public IViewModel GetChartByName(string name)
        {
            IGraphViewModel newChart = _seriesChartFactory.Create(name);
            if (newChart == null)
            {
                return null;
            }

            InitializeViewModel(newChart);
            _charts.Add(newChart);

            return new ChartWrapperViewModel(newChart);
        }

        public void DisposeAllPreviouslyCreatedCharts()
        {
            _charts.ForEach(x => x.Dispose());
            _charts.Clear();
        }

        private void InitializeViewModel(IGraphViewModel graphViewModel)
        {
            graphViewModel.XAxisKind = _telemetrySettings.TelemetrySettings.XAxisKind;
            graphViewModel.GraphViewSynchronization = _graphViewSynchronization;
            graphViewModel.LapColorSynchronization = _lapColorSynchronization;
            graphViewModel.UnitsCollection = _settingsProvider.DisplaySettingsViewModel.GetUnitsCollection();
            CollectionExtension.ForEach(_telemetrySettings.CarSettingsController.FillCarProperties(_loadedLaps), x => graphViewModel.AddLapTelemetry(x.LapTelemetryDto, x.CarPropertiesDto));
        }

        private void Subscribe()
        {
            _telemetryViewsSynchronization.SyncTelemetryView += TelemetryViewsSynchronizationOnSyncTelemetryView;
            _telemetryViewsSynchronization.LapLoaded += TelemetryViewsSynchronizationOnLapLoaded;
            _telemetryViewsSynchronization.LapUnloaded += TelemetryViewsSynchronizationOnLapUnloaded;
            _telemetrySettings.CarSettingsController.CarPropertiesChanges += CarSettingsControllerOnCarPropertiesChanges;
        }

        private void Unsubscribe()
        {
            _telemetryViewsSynchronization.SyncTelemetryView -= TelemetryViewsSynchronizationOnSyncTelemetryView;
            _telemetryViewsSynchronization.LapLoaded -= TelemetryViewsSynchronizationOnLapLoaded;
            _telemetryViewsSynchronization.LapUnloaded -= TelemetryViewsSynchronizationOnLapUnloaded;
            _telemetrySettings.CarSettingsController.CarPropertiesChanges -= CarSettingsControllerOnCarPropertiesChanges;
        }

        private void TelemetryViewsSynchronizationOnSyncTelemetryView(object sender, TelemetrySnapshotArgs e)
        {
            if (e.LapSummaryDto == null || e.TelemetrySnapshot == null)
            {
                return;
            }

            _charts.ForEach(x => x.UpdateXSelection(e.LapSummaryDto, e.TelemetrySnapshot));
        }

        private void TelemetryViewsSynchronizationOnLapUnloaded(object sender, LapsSummaryArgs lapsSummaryArgs)
        {
            _loadedLaps.RemoveAll(x => lapsSummaryArgs.LapsSummaries.Contains(x.LapSummary));
            foreach (LapSummaryDto lapSummaryDto in lapsSummaryArgs.LapsSummaries)
            {
                _charts.ForEach(x => x.RemoveLapTelemetry(lapSummaryDto));
            }
        }

        private void TelemetryViewsSynchronizationOnLapLoaded(object sender, LapsTelemetryArgs lapsTelemetryArgs)
        {
            _loadedLaps.AddRange(lapsTelemetryArgs.LapsTelemetries);
            foreach (LapTelemetryDto lapTelemetryDto in lapsTelemetryArgs.LapsTelemetries)
            {
                var carProperties = _telemetrySettings.CarSettingsController.GetCarProperties(lapTelemetryDto);
                _charts.ForEach(x => x.AddLapTelemetry(lapTelemetryDto, carProperties));
            }
        }

        private void CarSettingsControllerOnCarPropertiesChanges(object sender, CarPropertiesArgs e)
        {
            var chartsToRefresh = _charts.Where(x => x.IsCarSettingsDependant).ToList();
            foreach (IGraphViewModel graphViewModel in chartsToRefresh)
            {
                graphViewModel.RemoveAllLapsTelemetry();
                graphViewModel.AddLapTelemetries(_telemetrySettings.CarSettingsController.FillCarProperties(_loadedLaps));
            }
        }
    }
}