namespace SecondMonitor.ViewModels.Settings.ViewModel.Migrations
{
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.DataGrid;
    using SecondMonitor.ViewModels.Settings.Model;
    using SecondMonitor.ViewModels.Settings.Model.Layout;

    public class Version07VeColumn : ISettingMigration
    {
        private const string TyreRibbonColumn = "Tyre Kind Ribbon";
        private const string GapColumn = "GapColumn";

        public int VersionAfterMigration => 7;

        public DisplaySettings ApplyMigration(DisplaySettings oldVersion)
        {
            AddVirtualEnergyColumn(oldVersion.PracticeOptions.ColumnsSettings);
            AddVirtualEnergyColumn(oldVersion.RaceOptions.ColumnsSettings);
            return oldVersion;
        }

        public void AddVirtualEnergyColumn(ColumnsSettings columnsSettings)
        {
            if (columnsSettings.Columns.Length == 0)
            {
                return;
            }

            List<ColumnDescriptor> columns = columnsSettings.Columns.ToList();

            int index = columns.IndexOf(x => x.Name == TyreRibbonColumn);

            if (index == -1)
            {
                index = columns.IndexOf(x => x.Name == GapColumn);
            }

            if (index == -1)
            {
                index = columns.Count - 1;
            }

            TemplateTextColumnDescriptor newColumn = new TemplateTextColumnDescriptor()
            {
                Title = "VE",
                Name = "Virtual Energy",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 40),
                CanResize = true,
                IsBold = true,
                CellTemplateName = "VirtualEnergyTemplate",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            columns.Insert(index, newColumn);
            columnsSettings.Columns = columns.ToArray();
        }
    }
}
