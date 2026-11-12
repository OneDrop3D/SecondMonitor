namespace SecondMonitor.ViewModels.DataGrid
{
    using System.Collections.Generic;

    public interface IColumnDescriptorTemplatesFactory
    {
        IEnumerable<IColumnDescriptorTemplate> Create();
    }
}