namespace SecondMonitor.ViewModels.DataGrid
{
    using System.Xml.Serialization;

    public class TextColumnDescriptor : ColumnDescriptor
    {
        [XmlAttribute]
        public bool UseCustomFontSize { get; set; }

        [XmlAttribute]
        public int FontSize { get; set; }

        [XmlAttribute]
        public bool IsBold { get; set; }

        [XmlAttribute]
        public bool IsItalic { get; set; }

        [XmlAttribute]
        public string BindingPropertyName { get; set; }

        [XmlAttribute]
        public string CustomElementStyle { get; set; }
    }
}