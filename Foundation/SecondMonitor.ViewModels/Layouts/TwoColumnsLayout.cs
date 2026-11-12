namespace SecondMonitor.ViewModels.Layouts
{
    using Settings.Model.Layout;

    public class TwoColumnsLayout : AbstractViewModel
    {
        private IViewModel _leftColumnViewModel;
        private IViewModel _rightColumnViewModel;
        private LengthDefinitionSetting _leftColumnSize;
        private LengthDefinitionSetting _rightColumnSize;

        public TwoColumnsLayout()
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
    }
}