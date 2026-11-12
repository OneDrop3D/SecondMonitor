namespace SecondMonitor.ViewModels.DataGrid
{
    using System.Xml.Serialization;

    public class TemplateTextColumnDescriptor : TextColumnDescriptor
    {
        [XmlAttribute]
        public string CellTemplateName { get; set; }
    }
}