namespace ControlTestingApp.ViewModels
{
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.ViewModels;

    public class ColorTestingViewModel : AbstractViewModel
    {
        private int _r = 69;
        private int _g = 124;
        private int _b = 69;
        private ColorDto _fillColor;

        public ColorTestingViewModel()
        {
            RecalculateColor();
        }

        public int R
        {
            get => _r;
            set => SetProperty(ref _r, value, (_, __) => RecalculateColor());
        }

        public int G
        {
            get => _g;
            set => SetProperty(ref _g, value, (_, __) => RecalculateColor());
        }

        public int B
        {
            get => _b;
            set => SetProperty(ref _b, value, (_, __) => RecalculateColor());
        }

        public ColorDto FillColor
        {
            get => _fillColor;
            set => SetProperty(ref _fillColor, value);
        }

        private void RecalculateColor()
        {
            FillColor = new ColorDto(255, (byte)R, (byte)G, (byte)B);
        }
    }
}
