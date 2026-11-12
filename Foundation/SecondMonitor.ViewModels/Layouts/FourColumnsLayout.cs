namespace SecondMonitor.ViewModels.Layouts
{
    using Settings.Model.Layout;

    public class FourColumnsLayout : AbstractViewModel
    {
        private IViewModel _firstColumnViewModel;
        private IViewModel _secondColumnViewModel;
        private IViewModel _thirdColumnViewModel;
        private IViewModel _fourthColumnViewModel;
        private LengthDefinitionSetting _firstColumnSize;
        private LengthDefinitionSetting _secondColumnSize;
        private LengthDefinitionSetting _thirdColumnSize;
        private LengthDefinitionSetting _fourthColumnSize;

        public FourColumnsLayout()
        {
            _firstColumnSize = new LengthDefinitionSetting();
            _secondColumnSize = new LengthDefinitionSetting();
            _thirdColumnSize = new LengthDefinitionSetting();
            _fourthColumnSize = new LengthDefinitionSetting();
        }

        public IViewModel FirstColumnViewModel
        {
            get => _firstColumnViewModel;
            set => SetProperty(ref _firstColumnViewModel, value);
        }

        public IViewModel SecondColumnViewModel
        {
            get => _secondColumnViewModel;
            set => SetProperty(ref _secondColumnViewModel, value);
        }

        public LengthDefinitionSetting FirstColumnSize
        {
            get => _firstColumnSize;
            set => SetProperty(ref _firstColumnSize, value);
        }

        public LengthDefinitionSetting SecondColumnSize
        {
            get => _secondColumnSize;
            set => SetProperty(ref _secondColumnSize, value);
        }

        public IViewModel ThirdColumnViewModel
        {
            get => _thirdColumnViewModel;
            set => SetProperty(ref _thirdColumnViewModel, value);
        }

        public IViewModel FourthColumnViewModel
        {
            get => _fourthColumnViewModel;
            set => SetProperty(ref _fourthColumnViewModel, value);
        }

        public LengthDefinitionSetting ThirdColumnSize
        {
            get => _thirdColumnSize;
            set => SetProperty(ref _thirdColumnSize, value);
        }

        public LengthDefinitionSetting FourthColumnSize
        {
            get => _fourthColumnSize;
            set => SetProperty(ref _fourthColumnSize, value);
        }
    }
}