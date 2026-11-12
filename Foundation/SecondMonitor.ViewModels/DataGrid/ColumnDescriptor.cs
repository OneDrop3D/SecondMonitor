namespace SecondMonitor.ViewModels.DataGrid
{
    using System;
    using System.Xml.Serialization;
    using Settings.Model.Layout;

    [Serializable]
    [XmlInclude(typeof(TextColumnDescriptor))]
    [XmlInclude(typeof(TemplatedColumnDescriptor))]
    [XmlInclude(typeof(TemplateTextColumnDescriptor))]
    [XmlInclude(typeof(BlankColumnDescriptor))]
    [XmlInclude(typeof(BindableDataColumnDescriptor))]
    public abstract class ColumnDescriptor
    {
        [XmlAttribute]
        public bool CanResize { get; set; }

        [XmlAttribute]
        public string Title { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public int MinWidth { get; set; }

        [XmlAttribute]
        public bool IsAutoHideCapable { get; set; }

        [XmlAttribute]
        public bool IsAutoHideEnabled { get; set; }

        [XmlAttribute]
        public HorizontalAlignmentKind HorizontalAlignment { get; set; }

        [XmlAttribute]
        public VerticalAlignmentKind VerticalAlignment { get; set; }

        public LengthDefinitionSetting ColumnLength { get; set; }
    }
}