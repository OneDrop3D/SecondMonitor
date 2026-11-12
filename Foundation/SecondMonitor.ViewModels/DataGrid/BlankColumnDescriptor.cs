namespace SecondMonitor.ViewModels.DataGrid
{
    using System.Xml.Serialization;
    using DataModel.BasicProperties;

    public class BlankColumnDescriptor : ColumnDescriptor
    {
        [XmlAttribute]
        public ColorDto BackgroundColor { get; set; }
    }
}