namespace SecondMonitor.ViewModels.Settings.ViewModel.Columns
{
    using DataGrid;
    using DataModel.BasicProperties;

    public class BlankColumnDescriptorViewModel : ColumnDescriptorViewModel
    {
        private ColorDto _backgroundColor;

        public ColorDto BackgroundColor
        {
            get => _backgroundColor;
            set => SetProperty(ref _backgroundColor, value);
        }

        protected override void ApplyModel(ColumnDescriptor model)
        {
            base.ApplyModel(model);
            if (model is BlankColumnDescriptor blankColumnDescriptor)
            {
                BackgroundColor = blankColumnDescriptor.BackgroundColor;
            }
        }

        public override ColumnDescriptor SaveToNewModel()
        {
            if (OriginalModel is BlankColumnDescriptor blankColumnDescriptor)
            {
                blankColumnDescriptor.BackgroundColor = _backgroundColor;
            }

            ApplyToModel(OriginalModel);
            return OriginalModel;
        }
    }
}