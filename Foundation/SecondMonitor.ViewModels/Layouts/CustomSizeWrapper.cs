namespace SecondMonitor.ViewModels.Layouts
{
    public class CustomSizeWrapper : AbstractViewModel
    {
        private double _width;
        private double _height;
        private IViewModel _content;

        public CustomSizeWrapper(double width, double height, IViewModel content)
        {
            _width = width;
            _height = height;
            _content = content;
        }

        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        public IViewModel Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }
    }
}