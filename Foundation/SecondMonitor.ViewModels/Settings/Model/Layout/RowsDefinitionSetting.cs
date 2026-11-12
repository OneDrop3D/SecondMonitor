namespace SecondMonitor.ViewModels.Settings.Model.Layout
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    [Serializable]
    public class RowsDefinitionSetting : GenericContentSetting
    {
        public RowsDefinitionSetting()
        {
        }

        public RowsDefinitionSetting(int rowCount, LengthDefinitionSetting[] rowsSize, GenericContentSetting[] rowsContent, bool addGridSplitters)
        {
            RowCount = rowCount;
            RowsSize = rowsSize;
            RowsContent = rowsContent;
            AddGridSplitters = addGridSplitters;
        }

        [XmlAttribute]
        public bool AddGridSplitters { get; set; }

        [XmlAttribute]
        public int RowCount { get; set; }

        public LengthDefinitionSetting[] RowsSize { get; set; }
        public GenericContentSetting[] RowsContent { get; set; }

        public override IEnumerable<GenericContentSetting> GetDescendants() => RowsContent.Concat(RowsContent.SelectMany(x => x.GetDescendants()));
    }
}