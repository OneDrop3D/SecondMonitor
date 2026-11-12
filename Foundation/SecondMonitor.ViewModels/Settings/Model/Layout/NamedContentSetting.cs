namespace SecondMonitor.ViewModels.Settings.Model.Layout
{
    using System;
    using System.Xml.Serialization;

    [Serializable]
    public class NamedContentSetting : GenericContentSetting
    {
        public NamedContentSetting(string contentName)
        {
            ContentName = contentName;
        }

        public NamedContentSetting()
        {
        }

        [XmlAttribute]
        public string ContentName { get; set; }
    }
}