namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Controllers.Settings;
    using Settings.Workspace;

    public class ChartSetProvider : IChartSetProvider
    {
        private readonly List<IBuildInChartSetProvider> _buildInChartSets;
        private readonly ISettingsController _settingsController;

        public ChartSetProvider(List<IBuildInChartSetProvider> buildInChartSets, ISettingsController settingsController)
        {
            _buildInChartSets = buildInChartSets;
            _settingsController = settingsController;
        }

        public bool TryGetChartSet(Guid chartSetGuid, out ChartSet chartSet)
        {
            var customChartList = _settingsController.TelemetrySettings.CustomChartSets.FirstOrDefault(x => x.Guid == chartSetGuid);
            if (customChartList != null)
            {
                chartSet = customChartList;
                return true;
            }

            var buildInChartSet = _buildInChartSets.FirstOrDefault(x => x.ChartSetGuid == chartSetGuid);
            if (buildInChartSet == null)
            {
                chartSet = null;
                return false;
            }

            chartSet = buildInChartSet.GetChartSet();
            return true;
        }

        public IEnumerable<ChartSet> GetAllChartSets()
        {
            return _buildInChartSets.Select(x => x.GetChartSet()).Concat(_settingsController.TelemetrySettings.CustomChartSets);
        }
    }
}