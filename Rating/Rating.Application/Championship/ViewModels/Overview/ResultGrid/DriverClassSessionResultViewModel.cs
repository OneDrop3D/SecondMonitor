namespace SecondMonitor.Rating.Application.Championship.ViewModels.Overview.ResultGrid
{
    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.ViewModels;

    public class DriverClassSessionResultViewModel : AbstractViewModel<ClassDriverResultDto>
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

        protected override void ApplyModel(ClassDriverResultDto model)
        {
            FinishPosition = model.FinishPosition;
            PointsGained = model.PointsGain > 0 ? model.PointsGain.ToString() : "-";
            WasFastestLap = model.WasFastestLap;
        }

        public override ClassDriverResultDto SaveToNewModel()
        {
            return OriginalModel;
        }
    }
}
