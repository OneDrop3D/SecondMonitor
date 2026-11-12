namespace SecondMonitor.ViewModels.DataGrid
{
    public interface IColumnDescriptorTemplate
    {
        string Name { get; }
        ColumnDescriptor CreateColumnDescriptor();
    }
}