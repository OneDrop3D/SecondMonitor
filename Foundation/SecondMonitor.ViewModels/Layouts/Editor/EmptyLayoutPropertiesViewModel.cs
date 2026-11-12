namespace SecondMonitor.ViewModels.Layouts.Editor
{
    using System.Windows.Input;

    public class EmptyLayoutPropertiesViewModel : AbstractViewModel
    {
        private ICommand _replaceForContentCommand;
        private ICommand _replaceForRowsCommand;
        private ICommand _replaceForColumnsCommand;

        public ICommand ReplaceForContentCommand
        {
            get => _replaceForContentCommand;
            set => SetProperty(ref _replaceForContentCommand, value);
        }

        public ICommand ReplaceForRowsCommand
        {
            get => _replaceForRowsCommand;
            set => SetProperty(ref _replaceForRowsCommand, value);
        }

        public ICommand ReplaceForColumnsCommand
        {
            get => _replaceForColumnsCommand;
            set => SetProperty(ref _replaceForColumnsCommand, value);
        }
    }
}