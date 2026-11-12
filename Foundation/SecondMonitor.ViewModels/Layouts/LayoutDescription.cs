namespace SecondMonitor.ViewModels.Layouts
{
    using System;
    using Settings.Model.Layout;

    [Serializable]
    public class LayoutDescription
    {
        public string Name { get; set; }
        public GenericContentSetting RootElement { get; set; }
    }
}