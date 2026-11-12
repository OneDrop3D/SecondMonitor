namespace SecondMonitor.ViewModels.Layouts
{
    public class ViewModelContainer : AbstractViewModel
    {
        private IViewModel _viewModel;

        public ViewModelContainer()
        {
        }

        public ViewModelContainer(IViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public IViewModel ViewModel
        {
            get => _viewModel;
            set => SetProperty(ref _viewModel, value);
        }
    }
}