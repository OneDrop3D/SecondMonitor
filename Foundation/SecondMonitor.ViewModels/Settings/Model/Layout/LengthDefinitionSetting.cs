namespace SecondMonitor.ViewModels.Settings.Model.Layout
{
    using System;
    using System.Xml.Serialization;

    [Serializable]
    public class LengthDefinitionSetting
    {
        public LengthDefinitionSetting()
        {
            SizeKind = SizeKind.Remaining;
            ManualSize = 100;
        }

        public LengthDefinitionSetting(SizeKind sizeKind, int manualSize)
        {
            SizeKind = sizeKind;
            ManualSize = manualSize;
        }

        [XmlAttribute]
        public SizeKind SizeKind { get; set; }

        [XmlAttribute]
        public int ManualSize { get; set; }
    }
}