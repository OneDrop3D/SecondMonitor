namespace SecondMonitor.Timing.Application.SessionTiming.Drivers.Presentation.ViewModel
{
    using Common.SessionTiming.Drivers;
    using ViewModels;

    public class DriverTimingAdditionalVisualizationViewModel : AbstractViewModel<DriverTimingVisualization>
    {
        private bool _isMarkerBeforeEnabled;
        private int _markerBeforeSize;
        private bool _isMarkerAfterEnabled;
        private int _markerAfterSize;
        private bool _forceRelativeGapTime;
        private bool _showPositionGainedArrow;
        private bool _showPositionLostArrow;

        public bool IsMarkerBeforeEnabled
        {
            get => _isMarkerBeforeEnabled;
            private set => SetProperty(ref _isMarkerBeforeEnabled, value);
        }

        public int MarkerBeforeSize
        {
            get => _markerBeforeSize;
            private set => SetProperty(ref _markerBeforeSize, value);
        }

        public bool IsMarkerAfterEnabled
        {
            get => _isMarkerAfterEnabled;
            private set => SetProperty(ref _isMarkerAfterEnabled, value);
        }

        public int MarkerAfterSize
        {
            get => _markerAfterSize;
            private set => SetProperty(ref _markerAfterSize, value);
        }

        public bool ForceRelativeGapTime
        {
            get => _forceRelativeGapTime;
            set => SetProperty(ref _forceRelativeGapTime, value);
        }

        public bool ShowPositionGainedArrow
        {
            get => _showPositionGainedArrow;
            set => SetProperty(ref _showPositionGainedArrow, value);
        }

        public bool ShowPositionLostArrow
        {
            get => _showPositionLostArrow;
            set => SetProperty(ref _showPositionLostArrow, value);
        }

        protected override void ApplyModel(DriverTimingVisualization model)
        {
            IsMarkerAfterEnabled = model.IsMarkerAfterEnabled;
            IsMarkerBeforeEnabled = model.IsMarkerBeforeEnabled;
            MarkerAfterSize = model.MarkerAfterSize;
            MarkerBeforeSize = model.MarkerBeforeSize;
            ForceRelativeGapTime = model.ForceRelativeGapTime;
            ShowPositionGainedArrow = model.DriverPositionChange == DriverPositionChange.Gained;
            ShowPositionLostArrow = model.DriverPositionChange == DriverPositionChange.Lost;
        }

        public override DriverTimingVisualization SaveToNewModel()
        {
            return OriginalModel;
        }
    }
}