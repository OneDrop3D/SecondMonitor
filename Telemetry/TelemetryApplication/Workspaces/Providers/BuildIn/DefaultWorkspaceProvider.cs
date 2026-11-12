namespace SecondMonitor.Telemetry.TelemetryApplication.Workspaces.Providers.BuildIn
{
    using System;
    using System.Collections.Generic;
    using ChartSet;

    public class DefaultWorkspaceProvider : AbstractWorkspaceProvider
    {
        public static readonly Guid Guid = Guid.Parse("b8feb0e0-7557-4288-aa85-8dd2e4905bca");
        public override Guid WorkspaceGuid => Guid;
        public override string Name => "Default";
        protected override List<string> GetChartSet()
        {
            return new List<string>()
            {
                CompareChartSetProvider.Guid.ToString(),
                DriverChartSetProvider.Guid.ToString(),
                BrakesChartSetProvider.Guid.ToString(),
                SteeringChartSetProvider.Guid.ToString(),
                SuspensionChartSetProvider.Guid.ToString(),
                RideHeightChartSetProvider.Guid.ToString(),
                EngineChartSetProvider.Guid.ToString(),
                TyresChartSetProvider.Guid.ToString(),
                AerodynamicsChartSetProvider.Guid.ToString(),
                DifferentialChartSetProvider.Guid.ToString(),
            };
        }
    }
}