namespace SecondMonitor.ViewModels.Settings.ViewModel.Columns
{
    using System.Windows.Input;
    using DataGrid;
    using Model.Layout;

    public abstract class ColumnDescriptorViewModel : AbstractViewModel<ColumnDescriptor>, IApplyToModel<ColumnDescriptor>
    {
        private string _name;
        private string _title;
        private int _columnWidth;
        private ICommand _removeColumnCommand;
        private bool _isAutoHideEnabled;
        private bool _isAutoHideVisible;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public int ColumnWidth
        {
            get => _columnWidth;
            set => SetProperty(ref _columnWidth, value);
        }

        public ICommand RemoveColumnCommand
        {
            get => _removeColumnCommand;
            set => SetProperty(ref _removeColumnCommand, value);
        }

        public bool IsAutoHideVisible
        {
            get => _isAutoHideVisible;
            set => SetProperty(ref _isAutoHideVisible, value);
        }

        public bool IsAutoHideEnabled
        {
            get => _isAutoHideEnabled;
            set => SetProperty(ref _isAutoHideEnabled, value);
        }

        protected override void ApplyModel(ColumnDescriptor model)
        {
            Name = model.Name;
            Title = model.Title;
            IsAutoHideVisible = model.IsAutoHideCapable;
            IsAutoHideEnabled = model.IsAutoHideEnabled;
            ColumnWidth = model.ColumnLength.SizeKind == SizeKind.Automatic ? 0 : model.ColumnLength.ManualSize;
        }

        public void ApplyToModel(ColumnDescriptor model)
        {
            model.Name = Name;
            model.Title = Title;
            model.IsAutoHideEnabled = IsAutoHideEnabled;
            model.IsAutoHideCapable = IsAutoHideVisible;
            model.ColumnLength = new LengthDefinitionSetting()
            {
                ManualSize = ColumnWidth,
                SizeKind = ColumnWidth == 0 ? SizeKind.Automatic : SizeKind.Manual,
            };
        }
    }
}