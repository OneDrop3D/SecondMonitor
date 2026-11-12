namespace SecondMonitor.ViewModels.Settings.Model.Layout
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    [Serializable]
    public class ColumnsDefinitionSetting : GenericContentSetting
    {
        public ColumnsDefinitionSetting(int columnsCount, LengthDefinitionSetting[] columnsSize, GenericContentSetting[] columnsContent, bool addGridSplitter)
        {
            ColumnsCount = columnsCount;
            ColumnsSize = columnsSize;
            ColumnsContent = columnsContent;
            AddGridSplitter = addGridSplitter;
        }

        public ColumnsDefinitionSetting()
        {
        }

        [XmlAttribute]
        public int ColumnsCount { get; set; }

        public LengthDefinitionSetting[] ColumnsSize { get; set; }

        public GenericContentSetting[] ColumnsContent { get; set; }

        [XmlAttribute]
        public bool AddGridSplitter { get; set; }

        public override IEnumerable<GenericContentSetting> GetDescendants() => ColumnsContent.Concat(ColumnsContent.SelectMany(x => x.GetDescendants()));
    }
}