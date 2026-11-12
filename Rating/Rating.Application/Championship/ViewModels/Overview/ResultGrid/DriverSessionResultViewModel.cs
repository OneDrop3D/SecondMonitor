namespace SecondMonitor.Rating.Application.Championship.ViewModels.Overview.ResultGrid
{
    using SecondMonitor.Rating.Common.DataModel.Championship.Events;
    using SecondMonitor.ViewModels;

    public class DriverSessionResultViewModel : AbstractViewModel<DriverSessionResultDto>
    {
        private string _pointsGained = "-";
        private int _finishPosition = 0;
        private bool _wasFastestLap;
        public string PointsGained
        {
            get => _pointsGained;
            set => SetProperty(ref _pointsGained, value);
        }

        public int FinishPosition
        {
            get => _finishPosition;
            set => SetProperty(ref _finishPosition, value);
        }

        public bool WasFastestLap
        {
            get => _wasFastestLap;
            set => SetProperty(ref _wasFastestLap, value);
        }

        protected override void ApplyModel(DriverSessionResultDto model)
        {
            FinishPosition = model.FinishPosition;
            PointsGained = model.PointsGain > 0 ? model.PointsGain.ToString() : "-";
            WasFastestLap = model.WasFastestLap;
        }

        public override DriverSessionResultDto SaveToNewModel()
        {
            return OriginalModel;
        }
    }
}
