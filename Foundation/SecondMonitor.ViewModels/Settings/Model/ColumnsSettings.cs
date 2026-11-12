namespace SecondMonitor.ViewModels.Settings.Model
{
    using System;
    using DataGrid;

    [Serializable]
    public class ColumnsSettings
    {
        public ColumnDescriptor[] Columns { get; set; } = Array.Empty<ColumnDescriptor>();
    }
}
