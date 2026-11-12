namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;

    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.ViewModels;

    public class DriverClassFinishViewModel : AbstractViewModel<ClassDriverResultDto>
    {
        private bool _isCarNameVisible;
        public string DriverName { get; private set; }
        public string CarName { get; private set; }
        public int FinishPosition { get; private set; }
        public int PointsGain { get; private set; }
        public bool IsPlayer { get; private set; }

        public bool IsCarNameVisible
        {
            get => _isCarNameVisible;
            set => SetProperty(ref _isCarNameVisible, value);
        }

        public bool WasFastestLap { get; private set; }
        public bool IsTeamNameVisible { get; set; }
        public string TeamName { get; set; }

        protected override void ApplyModel(ClassDriverResultDto model)
        {
            DriverName = model.DriverName;
            CarName = model.CarName;
            FinishPosition = model.FinishPosition;
            PointsGain = model.PointsGain;
            IsPlayer = model.IsPlayer;
            WasFastestLap = model.WasFastestLap;
        }

        public override ClassDriverResultDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}