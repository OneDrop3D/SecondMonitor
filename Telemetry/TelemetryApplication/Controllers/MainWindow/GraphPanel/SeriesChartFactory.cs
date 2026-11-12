namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.MainWindow.GraphPanel
{
    using Contracts.NInject;
    using Ninject.Syntax;
    using ViewModels.GraphPanel;

    public class SeriesChartFactory : AbstractBindingMetadataFactory<IGraphViewModel>
    {
        public SeriesChartFactory(IResolutionRoot resolutionRoot) : base(resolutionRoot)
        {
        }

        protected override string MetadataName => BindingMetadataIds.SeriesChartsNameBinding;
        protected override IGraphViewModel CreateEmpty()
        {
            return null;
        }
    }
}