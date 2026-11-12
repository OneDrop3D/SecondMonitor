namespace SecondMonitor.ViewModels.Settings.ViewModel.Migrations
{
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.Settings.Model;
    using SecondMonitor.ViewModels.Settings.Model.Layout;

    public class Version06DeltaTypo : ISettingMigration
    {
        public int VersionAfterMigration => 6;

        public DisplaySettings ApplyMigration(DisplaySettings oldVersion)
        {
            List<NamedContentSetting> allNamedContent = oldVersion.LayoutSettings.Description.RootElement.GetDescendants().OfType<NamedContentSetting>().ToList();
            allNamedContent.Where(x => x.ContentName == "Lap Detla").ForEach(x => x.ContentName = "Lap Delta");
            return oldVersion;
        }
    }
}
