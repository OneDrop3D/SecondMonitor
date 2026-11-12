namespace SecondMonitor.ViewModels.Layouts.Editor
{
    using System.Windows.Input;
    using Contracts.Commands;

    public class ColumnDefinitionSettingViewModel : AbstractViewModel, ILayoutConfigurationViewModel, ILayoutContainer
    {
        private ILayoutConfigurationViewModel _columnContent;
        private int _columnNumber;
        private bool _isSelected;
        private ICommand _selectCommand;

        public ColumnDefinitionSettingViewModel()
        {
            SelectCommand = new RelayCommand(Select);
            ColumnPropertiesViewModel = new ColumnPropertiesViewModel
            {
                MoveLeftCommand = new RelayCommand(MoveThisColumnLeft),
                MoveRightCommand = new RelayCommand(MoveThisColumnRight),
                RemoveColumnCommand = new RelayCommand(RemoveThisColumn)
            };
        }

        public ColumnPropertiesViewModel ColumnPropertiesViewModel { get; }
        public ColumnsDefinitionSettingViewModel ParentViewModel { get; set; }

        public int ColumnNumber
        {
            get => _columnNumber;
            set => SetProperty(ref _columnNumber, value);
        }

        public ILayoutConfigurationViewModel ColumnContent
        {
            get => _columnContent;
            set => SetProperty(ref _columnContent, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public IViewModel PropertiesViewModel => ColumnPropertiesViewModel;

        public ICommand SelectCommand
        {
            get => _selectCommand;
            set => SetProperty(ref _selectCommand, value);
        }

        public ILayoutConfigurationViewModelFactory LayoutConfigurationViewModelFactory { get; set; }
        public ILayoutEditorManipulator LayoutEditorManipulator { get; set; }
        public ILayoutConfigurationViewModel LayoutElement => ColumnContent;

        private void Select()
        {
            LayoutEditorManipulator?.Select(this);
        }

        public void MoveThisColumnLeft()
        {
            ParentViewModel.MoveColumnLeft(this);
        }

        public void MoveThisColumnRight()
        {
            ParentViewModel.MoveColumnRight(this);
        }

        private void RemoveThisColumn()
        {
            ParentViewModel.RemoveColumn(this);
        }

        public void SetLayout(ILayoutConfigurationViewModel layoutElement)
        {
            ColumnContent = layoutElement;
        }
    }
}