namespace SecondMonitor.ViewModels.Settings.ViewModel.Migrations
{
    using System.Linq;
    using DataGrid;

    using Model;

    using SecondMonitor.DataModel.Extensions;

    public class Version04ClassColumnNameBugMigration : ISettingMigration
    {
        public int VersionAfterMigration => 4;

        public DisplaySettings ApplyMigration(DisplaySettings displaySettings)
        {
            displaySettings.PracticeOptions.ColumnsSettings.Columns.OfType<TextColumnDescriptor>().Where(x => x.Name == "Class").ForEach(x => x.BindingPropertyName = "CarClassName");
            displaySettings.QualificationOptions.ColumnsSettings.Columns.OfType<TextColumnDescriptor>().Where(x => x.Name == "Class").ForEach(x => x.BindingPropertyName = "CarClassName");
            displaySettings.RaceOptions.ColumnsSettings.Columns.OfType<TextColumnDescriptor>().Where(x => x.Name == "Class").ForEach(x => x.BindingPropertyName = "CarClassName");
            return displaySettings;
        }
    }
}