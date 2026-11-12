namespace SecondMonitor.ViewModels.Settings.ViewModel.Migrations
{
    using System.Linq;

    using Model;

    using SecondMonitor.DataModel.Extensions;

    public class Version02AutoHideColumnsMigration : ISettingMigration
    {
        private static readonly string[] columnsToMigrate = new[]
        {
            "Class Ribbon",
            "Class",
            "Tyre Kind Ribbon",
            "Pit Information",
            "Rating",
            "Points",
        };

        public int VersionAfterMigration => 2;

        public DisplaySettings ApplyMigration(DisplaySettings displaySettings)
        {
            displaySettings.PracticeOptions.ColumnsSettings.Columns.Where(x => columnsToMigrate.Contains(x.Name) && x.IsAutoHideCapable == false).ForEach(x =>
            {
                x.IsAutoHideCapable = true;
                x.IsAutoHideEnabled = true;
            });
            displaySettings.QualificationOptions.ColumnsSettings.Columns.Where(x => columnsToMigrate.Contains(x.Name) && x.IsAutoHideCapable == false).ForEach(x =>
            {
                x.IsAutoHideCapable = true;
                x.IsAutoHideEnabled = true;
            });
            displaySettings.RaceOptions.ColumnsSettings.Columns.Where(x => columnsToMigrate.Contains(x.Name) && x.IsAutoHideCapable == false).ForEach(x =>
            {
                x.IsAutoHideCapable = true;
                x.IsAutoHideEnabled = true;
            });

            displaySettings.Version = VersionAfterMigration;
            return displaySettings;
        } 
    }
}