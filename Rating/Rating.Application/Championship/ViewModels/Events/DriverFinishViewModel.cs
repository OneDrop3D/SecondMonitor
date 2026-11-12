namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;

    using Common.DataModel.Championship.Events;
    using SecondMonitor.ViewModels;

    public class DriverFinishViewModel : AbstractViewModel<DriverSessionResultDto>
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

        public string ClassName { get; set; }
        public bool IsClassNameVisible { get; set; }

        protected override void ApplyModel(DriverSessionResultDto model)
        {
            DriverName = model.DriverName;
            CarName = model.CarName;
            FinishPosition = model.FinishPosition;
            PointsGain = model.PointsGain;
            IsPlayer = model.IsPlayer;
            WasFastestLap = model.WasFastestLap;
            TeamName = model.TeamName;
            ClassName = model.ClassName;
        }

        public override DriverSessionResultDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}