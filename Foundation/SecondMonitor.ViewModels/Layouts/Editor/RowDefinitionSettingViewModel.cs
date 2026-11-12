namespace SecondMonitor.ViewModels.Layouts.Editor
{
    using System.Windows.Input;
    using Contracts.Commands;

    public class RowDefinitionSettingViewModel : AbstractViewModel, ILayoutConfigurationViewModel, ILayoutContainer
    {
        private ILayoutConfigurationViewModel _rowContent;

        private int _rowNumber;
        private bool _isSelected;
        private ICommand _selectCommand;

        public RowDefinitionSettingViewModel()
        {
            SelectCommand = new RelayCommand(Select);
            RowPropertiesViewModel = new RowPropertiesViewModel()
            {
                MoveUpCommand = new RelayCommand(MoveThisRowUp),
                MoveDownCommand = new RelayCommand(MoveThisRowDown),
                RemoveRowCommand = new RelayCommand(RemoveThisRow),
            };
        }

        public RowsDefinitionSettingViewModel ParentViewModel { get; set; }

        public int RowNumber
        {
            get => _rowNumber;
            set => SetProperty(ref _rowNumber, value);
        }

        public ILayoutConfigurationViewModel RowContent
        {
            get => _rowContent;
            set => SetProperty(ref _rowContent, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public IViewModel PropertiesViewModel => RowPropertiesViewModel;

        public ICommand SelectCommand
        {
            get => _selectCommand;
            set => SetProperty(ref _selectCommand, value);
        }

        public ILayoutConfigurationViewModel LayoutElement => RowContent;

        public RowPropertiesViewModel RowPropertiesViewModel { get; }
        public ILayoutConfigurationViewModelFactory LayoutConfigurationViewModelFactory { get; set; }
        public ILayoutEditorManipulator LayoutEditorManipulator { get; set; }

        public void MoveThisRowUp()
        {
            ParentViewModel.MoveRowUp(this);
        }

        public void MoveThisRowDown()
        {
            ParentViewModel.MoveRowDown(this);
        }

        private void RemoveThisRow()
        {
            ParentViewModel.RemoveRow(this);
        }

        private void Select()
        {
            LayoutEditorManipulator?.Select(this);
        }

        public void SetLayout(ILayoutConfigurationViewModel layoutElement)
        {
            RowContent = layoutElement;
        }
    }
}