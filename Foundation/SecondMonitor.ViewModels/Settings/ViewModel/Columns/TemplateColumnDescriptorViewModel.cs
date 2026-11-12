namespace SecondMonitor.ViewModels.Settings.ViewModel.Columns
{
    using DataGrid;

    public class TemplateColumnDescriptorViewModel : ColumnDescriptorViewModel
    {
        public override ColumnDescriptor SaveToNewModel()
        {
            ApplyToModel(OriginalModel);
            return OriginalModel;
        }
    }
}