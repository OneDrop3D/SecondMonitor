namespace SecondMonitor.ViewModels.Shapes
{
    using DataModel.BasicProperties;

    public abstract class AbstractShapeViewModel : AbstractViewModel
    {
        private ColorDto _color;
        private double _opacity;
        private bool _isVisible;

        protected AbstractShapeViewModel(ColorDto color)
        {
            _opacity = 1;
            _isVisible = true;
            _color = color;
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public double Opacity
        {
            get => _opacity;
            set => SetProperty(ref _opacity, value);
        }

        public ColorDto Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }
    }
}