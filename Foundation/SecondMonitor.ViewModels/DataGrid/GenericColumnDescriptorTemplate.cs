namespace SecondMonitor.ViewModels.DataGrid
{
    using System;

    public class GenericColumnDescriptorTemplate : IColumnDescriptorTemplate
    {
        private readonly Func<ColumnDescriptor> _createFunction;

        public GenericColumnDescriptorTemplate(Func<ColumnDescriptor> createFunction, string name)
        {
            Name = name;
            _createFunction = createFunction;
        }

        public string Name { get; }
        public ColumnDescriptor CreateColumnDescriptor()
        {
            return _createFunction();
        }
    }
}