namespace SecondMonitor.Timing.Application.TimingGrid.Columns
{
    using Contracts.NInject;
    using Ninject.Syntax;
    using BindingMetadataIds = Application.BindingMetadataIds;

    public class ColumnVisibilityAdapterFactory : AbstractBindingMetadataFactory<IColumnVisibilityAdapter>
    {
        public ColumnVisibilityAdapterFactory(IResolutionRoot resolutionRoot) : base(resolutionRoot)
        {
        }

        protected override string MetadataName => BindingMetadataIds.AdapterColumnName;
        protected override IColumnVisibilityAdapter CreateEmpty()
        {
            return null;
        }
    }
}