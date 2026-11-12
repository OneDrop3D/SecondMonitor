namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.MainWindow.GraphPanel
{
    using AggregatedCharts;
    using Contracts.NInject;
    using Ninject.Syntax;

    public class AggregatedChartProviderFactory : AbstractBindingMetadataFactory<IAggregatedChartProvider>
    {
        public AggregatedChartProviderFactory(IResolutionRoot resolutionRoot) : base(resolutionRoot)
        {
        }

        protected override string MetadataName => BindingMetadataIds.AggregatedChartProviderName;
        protected override IAggregatedChartProvider CreateEmpty()
        {
            return null;
        }
    }
}