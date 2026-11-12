namespace SecondMonitor.ViewModels.Layouts
{
    using Settings.Model.Layout;

    public class ExpanderWrapper : AbstractViewModel
    {
        private IViewModel _viewModel;
        private ExpandDirection _expandDirection;
        private bool _isExpanded;

        public ExpanderWrapper()
        {
        }

        public ExpanderWrapper(IViewModel viewModel, ExpandDirection expandDirection, bool isExpanded)
        {
            _viewModel = viewModel;
            _expandDirection = expandDirection;
            _isExpanded = isExpanded;
        }

        public IViewModel ViewModel
        {
            get => _viewModel;
            set => SetProperty(ref _viewModel, value);
        }

        public ExpandDirection ExpandDirection
        {
            get => _expandDirection;
            set => SetProperty(ref _expandDirection, value);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }
    }
}