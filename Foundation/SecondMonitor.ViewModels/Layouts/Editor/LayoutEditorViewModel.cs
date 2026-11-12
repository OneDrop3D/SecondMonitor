namespace SecondMonitor.ViewModels.Layouts.Editor
{
    using System.Windows.Input;

    public class LayoutEditorViewModel : AbstractViewModel
    {
        private LayoutEditorRootViewModel _layoutViewModel;
        private string _layoutName;
        private IViewModel _selectedElementPropertiesViewModel;
        private ICommand _applyLayoutCommand;
        private ICommand _revertToDefaultLayoutCommand;
        private ICommand _createEmptyLayoutCommand;
        private ICommand _saveLayoutCommand;
        private ICommand _loadLayoutCommand;
        private bool _isBottomBarVisible;
        private bool _isLayoutNameVisible;

        public LayoutEditorViewModel()
        {
            IsBottomBarVisible = true;
            LayoutViewModel = new LayoutEditorRootViewModel();
        }

        public bool IsBottomBarVisible
        {
            get => _isBottomBarVisible;
            set => SetProperty(ref _isBottomBarVisible, value);
        }

        public string LayoutName
        {
            get => _layoutName;
            set => SetProperty(ref _layoutName, value);
        }

        public bool IsLayoutNameVisible
        {
            get => _isLayoutNameVisible;
            set => SetProperty(ref _isLayoutNameVisible, value);
        }

        public LayoutEditorRootViewModel LayoutViewModel
        {
            get => _layoutViewModel;
            set => SetProperty(ref _layoutViewModel, value);
        }

        public IViewModel SelectedElementPropertiesViewModel
        {
            get => _selectedElementPropertiesViewModel;
            set => SetProperty(ref _selectedElementPropertiesViewModel, value);
        }

        public ICommand ApplyLayoutCommand
        {
            get => _applyLayoutCommand;
            set => SetProperty(ref _applyLayoutCommand, value);
        }

        public ICommand RevertToDefaultLayoutCommand
        {
            get => _revertToDefaultLayoutCommand;
            set => SetProperty(ref _revertToDefaultLayoutCommand, value);
        }

        public ICommand CreateEmptyLayoutCommand
        {
            get => _createEmptyLayoutCommand;
            set => SetProperty(ref _createEmptyLayoutCommand, value);
        }

        public ICommand SaveLayoutCommand
        {
            get => _saveLayoutCommand;
            set => SetProperty(ref _saveLayoutCommand, value);
        }

        public ICommand LoadLayoutCommand
        {
            get => _loadLayoutCommand;
            set => SetProperty(ref _loadLayoutCommand, value);
        }
    }
}