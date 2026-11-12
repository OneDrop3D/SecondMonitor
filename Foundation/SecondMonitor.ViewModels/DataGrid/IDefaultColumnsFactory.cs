namespace SecondMonitor.ViewModels.DataGrid
{
    using System.Collections.Generic;
    using DataModel.BasicProperties;

    public interface IDefaultColumnsFactory
    {
        IEnumerable<ColumnDescriptor> GetColumnDescriptors(SessionType sessionType);
    }
}