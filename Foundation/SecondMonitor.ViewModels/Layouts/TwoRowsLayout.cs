namespace SecondMonitor.ViewModels.Layouts
{
    using Settings.Model.Layout;

    public class TwoRowsLayout : AbstractViewModel
    {
        private IViewModel _topRowViewModel;
        private IViewModel _bottomRowViewModel;
        private LengthDefinitionSetting _topRowSize;
        private LengthDefinitionSetting _bottomRowSize;

        public TwoRowsLayout()
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
    }
}