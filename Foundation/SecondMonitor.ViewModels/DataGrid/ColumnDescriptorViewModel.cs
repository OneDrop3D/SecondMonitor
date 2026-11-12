namespace SecondMonitor.ViewModels.DataGrid
{
    using System;

    public class ColumnDescriptorViewModel : AbstractViewModel<ColumnDescriptor>
    {
        private bool _isVisible;

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public ColumnDescriptor ColumnDescriptor => OriginalModel;

        protected override void ApplyModel(ColumnDescriptor model)
        {
            IsVisible = true;
        }

        public override ColumnDescriptor SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}