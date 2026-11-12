namespace SecondMonitor.ViewModels.Layouts
{
    using Settings.Model.Layout;

    public class ThreeColumnsLayout : AbstractViewModel
    {
        private IViewModel _leftColumnViewModel;
        private IViewModel _middleColumnViewModel;
        private IViewModel _rightColumnViewModel;
        private LengthDefinitionSetting _leftColumnSize;
        private LengthDefinitionSetting _middleColumnSize;
        private LengthDefinitionSetting _rightColumnSize;

        public ThreeColumnsLayout()
        {
            _leftColumnSize = new LengthDefinitionSetting();
            _rightColumnSize = new LengthDefinitionSetting();
        }

        public IViewModel LeftColumnViewModel
        {
            get => _leftColumnViewModel;
            set => SetProperty(ref _leftColumnViewModel, value);
        }

        public IViewModel RightColumnViewModel
        {
            get => _rightColumnViewModel;
            set => SetProperty(ref _rightColumnViewModel, value);
        }

        public LengthDefinitionSetting LeftColumnSize
        {
            get => _leftColumnSize;
            set => SetProperty(ref _leftColumnSize, value);
        }

        public LengthDefinitionSetting RightColumnSize
        {
            get => _rightColumnSize;
            set => SetProperty(ref _rightColumnSize, value);
        }

        public IViewModel MiddleColumnViewModel
        {
            get => _middleColumnViewModel;
            set => SetProperty(ref _middleColumnViewModel, value);
        }

        public LengthDefinitionSetting MiddleColumnSize
        {
            get => _middleColumnSize;
            set => SetProperty(ref _middleColumnSize, value);
        }
    }
}