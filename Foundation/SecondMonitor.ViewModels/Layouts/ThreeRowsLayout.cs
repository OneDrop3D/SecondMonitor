namespace SecondMonitor.ViewModels.Layouts
{
    using Settings.Model.Layout;

    public class ThreeRowsLayout : AbstractViewModel
    {
        private IViewModel _topRowViewModel;
        private IViewModel _middleRowViewModel;
        private IViewModel _bottomRowViewModel;
        private LengthDefinitionSetting _topRowSize;
        private LengthDefinitionSetting _middleRowSize;
        private LengthDefinitionSetting _bottomRowSize;

        public ThreeRowsLayout()
        {
            _topRowSize = new LengthDefinitionSetting();
            _bottomRowSize = new LengthDefinitionSetting();
        }

        public IViewModel TopRowViewModel
        {
            get => _topRowViewModel;
            set => SetProperty(ref _topRowViewModel, value);
        }

        public IViewModel BottomRowViewModel
        {
            get => _bottomRowViewModel;
            set => SetProperty(ref _bottomRowViewModel, value);
        }

        public LengthDefinitionSetting TopRowSize
        {
            get => _topRowSize;
            set => SetProperty(ref _topRowSize, value);
        }

        public LengthDefinitionSetting BottomRowSize
        {
            get => _bottomRowSize;
            set => SetProperty(ref _bottomRowSize, value);
        }

        public IViewModel MiddleRowViewModel
        {
            get => _middleRowViewModel;
            set => SetProperty(ref _middleRowViewModel, value);
        }

        public LengthDefinitionSetting MiddleRowSize
        {
            get => _middleRowSize;
            set => SetProperty(ref _middleRowSize, value);
        }
    }
}