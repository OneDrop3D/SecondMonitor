namespace SecondMonitor.ViewModels.Track
{
    using System;
    using System.Windows.Media;
    using DataModel.TrackMap;

    public class TrackGeometryViewModel : AbstractViewModel<TrackGeometryDto>
    {
        private double _width;
        private double _height;
        private double _leftOffset;
        private double _topOffset;
        private Geometry _trackGeometry;
        private bool _isGeometryFilled;
        private bool _isAxisSwapped;
        private double _xCoef;
        private double _yCoef;

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

        public double LeftOffset
        {
            get => _leftOffset;
            set => SetProperty(ref _leftOffset, value);
        }

        public double TopOffset
        {
            get => _topOffset;
            set => SetProperty(ref _topOffset, value);
        }

        public Geometry TrackGeometry
        {
            get => _trackGeometry;
            set => SetProperty(ref _trackGeometry, value);
        }

        public bool IsGeometryFilled
        {
            get => _isGeometryFilled;
            set => SetProperty(ref _isGeometryFilled, value);
        }

        public bool IsAxisSwapped
        {
            get => _isAxisSwapped;
            set => SetProperty(ref _isAxisSwapped, value);
        }

        public double XCoef
        {
            get => _xCoef;
            set => SetProperty(ref _xCoef, value);
        }

        public double YCoef
        {
            get => _yCoef;
            set => SetProperty(ref _yCoef, value);
        }

        protected override void ApplyModel(TrackGeometryDto model)
        {
            if (model == null)
            {
                IsGeometryFilled = false;
                return;
            }

            Width = model.Width;
            Height = model.Height;
            LeftOffset = -model.LeftOffset;
            TopOffset = -model.TopOffset;
            TrackGeometry = Geometry.Parse(model.FullMapGeometry);
            IsAxisSwapped = model.IsSwappedAxis;
            XCoef = model.XCoef;
            YCoef = model.YCoef;
            IsGeometryFilled = true;
        }

        public override TrackGeometryDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}