namespace SecondMonitor.ViewModels.Tabs
{
    public class TabItemViewModel : AbstractViewModel
    {
        private IViewModel _title;
        private IViewModel _tabContent;

        public TabItemViewModel()
        {
        }

        public TabItemViewModel(IViewModel title, IViewModel tabContent)
        {
            _title = title;
            _tabContent = tabContent;
        }

        public IViewModel Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public IViewModel TabContent
        {
            get => _tabContent;
            set => SetProperty(ref _tabContent, value);
        }
    }
}