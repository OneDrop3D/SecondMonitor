namespace SecondMonitor.ViewModels.Dialogs
{
    public class BasicDialogViewModel : AbstractDialogViewModel
    {
        private IViewModel _content;

        public BasicDialogViewModel(IViewModel content)
        {
            _content = content;
        }

        public IViewModel Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }
    }
}