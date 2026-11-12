namespace SecondMonitor.ViewModels.Shapes
{
    using System.Windows.Media;
    using DataModel.BasicProperties;

    public class GeometryViewModel : AbstractShapeViewModel
    {
        private Geometry _geometry;
        private double _thickness;
        private ColorDto _fillColor;

        public GeometryViewModel(ColorDto fillColor, ColorDto color, Geometry geometry, double thickness) : this(color, geometry, thickness)
        {
            _fillColor = fillColor;
        }

        public GeometryViewModel(ColorDto color, Geometry geometry, double thickness) : base(color)
        {
            _geometry = geometry;
            _thickness = thickness;
        }

        public ColorDto FillColor
        {
            get => _fillColor;
            set => SetProperty(ref _fillColor, value);
        }

        public Geometry Geometry
        {
            get => _geometry;
            set => SetProperty(ref _geometry, value);
        }

        public double Thickness
        {
            get => _thickness;
            set => SetProperty(ref _thickness, value);
        }
    }
}