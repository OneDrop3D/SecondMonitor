namespace SecondMonitor.ViewModels.Settings.Model.Layout
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [Serializable]
    [XmlInclude(typeof(ColumnsDefinitionSetting))]
    [XmlInclude(typeof(RowsDefinitionSetting))]
    [XmlInclude(typeof(NamedContentSetting))]
    public class GenericContentSetting
    {
        [XmlAttribute]
        public bool IsCustomWidth { get; set; }

        [XmlAttribute]
        public bool IsCustomHeight { get; set; }

        [XmlAttribute]
        public int CustomWidth { get; set; }

        [XmlAttribute]
        public int CustomHeight { get; set; }

        [XmlAttribute]
        public bool IsExpanderEnabled { get; set; }

        [XmlAttribute]
        public ExpandDirection ExpandDirection { get; set; }

        [XmlAttribute]
        public bool IsExpanderExpanded { get; set; }

        public virtual IEnumerable<GenericContentSetting> GetDescendants() => new List<GenericContentSetting>();
    }
}