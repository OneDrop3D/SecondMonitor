namespace SecondMonitor.ViewModels.Layouts.Editor
{
    using System.Windows.Input;
    using Contracts.Commands;

    public class EmptyLayoutElementViewModel : AbstractViewModel, ILayoutConfigurationViewModel
    {
        private bool _isSelected;
        private ICommand _selectCommand;

        public EmptyLayoutElementViewModel()
        {
            SelectCommand = new RelayCommand(Select);
            PropertiesViewModel = new EmptyLayoutPropertiesViewModel()
            {
                ReplaceForContentCommand = new RelayCommand(ReplaceForContent),
                ReplaceForColumnsCommand = new RelayCommand(ReplaceForColumns),
                ReplaceForRowsCommand = new RelayCommand(ReplaceForRows),
            };
        }

        public ILayoutConfigurationViewModelFactory LayoutConfigurationViewModelFactory { get; set; }
        public ILayoutEditorManipulator LayoutEditorManipulator { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public ICommand SelectCommand
        {
            get => _selectCommand;
            set => SetProperty(ref _selectCommand, value);
        }

        public IViewModel PropertiesViewModel { get; set; }

        private void Select()
        {
            LayoutEditorManipulator?.Select(this);
        }

        private void ReplaceForContent()
        {
            LayoutEditorManipulator.ReplaceForNamedContent(this);
        }

        private void ReplaceForRows()
        {
            LayoutEditorManipulator.ReplaceForRows(this);
        }

        private void ReplaceForColumns()
        {
            LayoutEditorManipulator.ReplaceForColumns(this);
        }
    }
}