namespace SecondMonitor.ViewModels.DataGrid
{
    using System.Xml.Serialization;

    public class TemplatedColumnDescriptor : ColumnDescriptor
    {
        [XmlAttribute]
        public string CellTemplateName { get; set; }
    }
}