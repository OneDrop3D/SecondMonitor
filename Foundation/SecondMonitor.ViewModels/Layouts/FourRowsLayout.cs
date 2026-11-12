namespace SecondMonitor.ViewModels.Layouts
{
    using Settings.Model.Layout;

    public class FourRowsLayout : AbstractViewModel
    {
        private IViewModel _firstRowViewModel;
        private IViewModel _secondRowViewModel;
        private IViewModel _thirdRowViewModel;
        private IViewModel _fourthRowViewModel;
        private LengthDefinitionSetting _firstRowSize;
        private LengthDefinitionSetting _secondRowSize;
        private LengthDefinitionSetting _thirdRowSize;
        private LengthDefinitionSetting _fourthRowSize;

        public FourRowsLayout()
        {
            _firstRowSize = new LengthDefinitionSetting();
            _secondRowSize = new LengthDefinitionSetting();
            _thirdRowSize = new LengthDefinitionSetting();
            _fourthRowSize = new LengthDefinitionSetting();
        }

        public IViewModel FirstRowViewModel
        {
            get => _firstRowViewModel;
            set => SetProperty(ref _firstRowViewModel, value);
        }

        public IViewModel SecondRowViewModel
        {
            get => _secondRowViewModel;
            set => SetProperty(ref _secondRowViewModel, value);
        }

        public LengthDefinitionSetting FirstRowSize
        {
            get => _firstRowSize;
            set => SetProperty(ref _firstRowSize, value);
        }

        public LengthDefinitionSetting SecondRowSize
        {
            get => _secondRowSize;
            set => SetProperty(ref _secondRowSize, value);
        }

        public IViewModel ThirdRowViewModel
        {
            get => _thirdRowViewModel;
            set => SetProperty(ref _thirdRowViewModel, value);
        }

        public IViewModel FourthRowViewModel
        {
            get => _fourthRowViewModel;
            set => SetProperty(ref _fourthRowViewModel, value);
        }

        public LengthDefinitionSetting ThirdRowSize
        {
            get => _thirdRowSize;
            set => SetProperty(ref _thirdRowSize, value);
        }

        public LengthDefinitionSetting FourthRowSize
        {
            get => _fourthRowSize;
            set => SetProperty(ref _fourthRowSize, value);
        }
    }
}