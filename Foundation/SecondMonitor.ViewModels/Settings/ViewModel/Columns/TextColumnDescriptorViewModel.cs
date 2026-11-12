namespace SecondMonitor.ViewModels.Settings.ViewModel.Columns
{
    using DataGrid;

    public class TextColumnDescriptorViewModel : ColumnDescriptorViewModel
    {
        private int _fontSize;
        private bool _isBold;
        private bool _isItalic;

        public int FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }

        public bool IsBold
        {
            get => _isBold;
            set => SetProperty(ref _isBold, value);
        }

        public bool IsItalic
        {
            get => _isItalic;
            set => SetProperty(ref _isItalic, value);
        }

        protected override void ApplyModel(ColumnDescriptor model)
        {
            base.ApplyModel(model);
            if (!(model is TextColumnDescriptor textColumnDescriptor))
            {
                return;
            }

            IsBold = textColumnDescriptor.IsBold;
            IsItalic = textColumnDescriptor.IsItalic;
            FontSize = textColumnDescriptor.FontSize;
        }

        public override ColumnDescriptor SaveToNewModel()
        {
            if (OriginalModel is TextColumnDescriptor textColumnDescriptor)
            {
                textColumnDescriptor.FontSize = FontSize;
                textColumnDescriptor.UseCustomFontSize = FontSize != 0;
                textColumnDescriptor.IsBold = IsBold;
                textColumnDescriptor.IsItalic = IsItalic;
            }

            ApplyToModel(OriginalModel);
            return OriginalModel;
        }
    }
}