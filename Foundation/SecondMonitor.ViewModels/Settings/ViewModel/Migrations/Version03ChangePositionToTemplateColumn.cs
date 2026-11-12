namespace SecondMonitor.ViewModels.Settings.ViewModel.Migrations
{
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.DataGrid;
    using SecondMonitor.ViewModels.Settings.Model;

    public class Version03ChangePositionToTemplateColumn : ISettingMigration
    {
        public int VersionAfterMigration => 3;

        public DisplaySettings ApplyMigration(DisplaySettings displaySettings)
        {
            MigrateUp(displaySettings.PracticeOptions.ColumnsSettings);
            MigrateUp(displaySettings.QualificationOptions.ColumnsSettings);
            MigrateUp(displaySettings.RaceOptions.ColumnsSettings);
            return displaySettings;
        }

        private void MigrateUp(ColumnsSettings columnsSettings)
        {
            List<TextColumnDescriptor> positionColumns = columnsSettings.Columns.OfType<TextColumnDescriptor>()
                .Where(x => x.Name == "Position" && x.GetType() == typeof(TextColumnDescriptor)).ToList();
            foreach (TextColumnDescriptor positionColumn in positionColumns)
            {
                int index = columnsSettings.Columns.IndexOf(x => x == positionColumn);
                TemplateTextColumnDescriptor newDescriptor = new TemplateTextColumnDescriptor()
                {
                    Title = positionColumn.Title,
                    BindingPropertyName = positionColumn.BindingPropertyName,
                    CanResize = positionColumn.CanResize,
                    ColumnLength = positionColumn.ColumnLength,
                    CustomElementStyle = positionColumn.CustomElementStyle,
                    IsAutoHideCapable = positionColumn.IsAutoHideCapable,
                    IsAutoHideEnabled = positionColumn.IsAutoHideEnabled,
                    CellTemplateName = "PositionColumnTemplate",
                    Name = positionColumn.Name,
                    IsBold = positionColumn.IsBold,
                    FontSize = positionColumn.FontSize,
                    HorizontalAlignment = positionColumn.HorizontalAlignment,
                    IsItalic = positionColumn.IsItalic,
                    MinWidth = positionColumn.MinWidth,
                    UseCustomFontSize = positionColumn.UseCustomFontSize,
                    VerticalAlignment = positionColumn.VerticalAlignment
                };

                columnsSettings.Columns[index] = newDescriptor;
            }
        }
    }
}
