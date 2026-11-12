namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces.Providers.BuildIn
{
    using System;
    using System.Collections.Generic;
    using ChartSet;

    public class DefaultWorkspaceProviderTemp : AbstractWorkspaceProvider
    {
        public static readonly Guid Guid = Guid.Parse("6f7865e3-4ce8-45ad-a403-0d4d5fb1c77a");
        public override Guid WorkspaceGuid => Guid;
        public override string Name => "Default Another";
        protected override List<string> GetChartSet()
        {
            return new List<string>()
            {
                AerodynamicsChartSetProvider.Guid.ToString(),
                CompareChartSetProvider.Guid.ToString(),
                DriverChartSetProvider.Guid.ToString(),
                BrakesChartSetProvider.Guid.ToString(),
                SuspensionChartSetProvider.Guid.ToString(),
                RideHeightChartSetProvider.Guid.ToString(),
                EngineChartSetProvider.Guid.ToString(),
                TyresChartSetProvider.Guid.ToString(),
            };
        }
    }
}