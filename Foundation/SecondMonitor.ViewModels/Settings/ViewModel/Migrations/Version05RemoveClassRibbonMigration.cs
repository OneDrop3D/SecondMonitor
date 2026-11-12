namespace SecondMonitor.ViewModels.Settings.ViewModel.Migrations
{
    using System.Linq;

    using SecondMonitor.ViewModels.Settings.Model;

    public class Version05RemoveClassRibbonMigration : ISettingMigration
    {
        public int VersionAfterMigration => 5;

        public DisplaySettings ApplyMigration(DisplaySettings oldVersion)
        {
            ApplyMigration(oldVersion.PracticeOptions.ColumnsSettings);
            ApplyMigration(oldVersion.QualificationOptions.ColumnsSettings);
            ApplyMigration(oldVersion.RaceOptions.ColumnsSettings);
            return oldVersion;
        }

        private void ApplyMigration(ColumnsSettings columnsSettings)
        {
            columnsSettings.Columns = columnsSettings.Columns.ToList().Where(x => x.Name != "Class Ribbon").ToArray();
        }
    }
}
