namespace SecondMonitor.ViewModels.Settings.Model
{
    using System;
    using System.Xml.Serialization;

    [Serializable]
    public class AutoDeleteSettings
    {
        public AutoDeleteSettings()
        {
            Directory = string.Empty;
        }

        public AutoDeleteSettings(bool isDeleteBySizeEnabled, bool isDeleteByDaysEnabled, int maxMegabytes, int maxDays)
        {
            IsDeleteBySizeEnabled = isDeleteBySizeEnabled;
            IsDeleteByDaysEnabled = isDeleteByDaysEnabled;
            MaxMegabytes = maxMegabytes;
            MaxDays = maxDays;
        }

        [XmlAttribute]
        public string Directory { get; set; }

        [XmlAttribute]
        public bool IsDeleteBySizeEnabled { get; set; }

        [XmlAttribute]
        public bool IsDeleteByDaysEnabled { get; set; }

        [XmlAttribute]
        public int MaxMegabytes { get; set; }

        [XmlAttribute]
        public int MaxDays { get; set; }
    }
}
