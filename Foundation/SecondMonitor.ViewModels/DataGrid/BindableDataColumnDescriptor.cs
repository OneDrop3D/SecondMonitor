namespace SecondMonitor.ViewModels.DataGrid
{
    using System.Xml.Serialization;

    public class BindableDataColumnDescriptor : ColumnDescriptor
    {
        [XmlAttribute]
        public string BindingPropertyName { get; set; }
    }
}
