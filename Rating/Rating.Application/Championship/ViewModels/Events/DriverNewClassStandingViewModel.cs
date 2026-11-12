namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;

    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.ViewModels;

    public class DriverNewClassStandingViewModel : AbstractViewModel<ClassDriverResultDto>
    {
        private bool _isCarNameVisible;
        public int Position { get; private set; }
        public string Name { get; private set; }
        public string CarName { get; private set; }
        public int TotalPoints { get; private set; }
        public int PositionsGained { get; private set; }
        public bool IsPlayer { get; private set; }

        public bool IsFirst { get; private set; }
        public int GapToPrevious { get; set; }
        public int GapToLeader { get; set; }
        public bool WasFastestLap { get; set; }

        public bool IsCarNameVisible
        {
            get => _isCarNameVisible;
            set => SetProperty(ref _isCarNameVisible, value);
        }

        protected override void ApplyModel(ClassDriverResultDto model)
        {
            IsFirst = model.AfterEventPosition == 1;
            Position = model.AfterEventPosition;
            Name = model.DriverName;
            CarName = model.CarName;
            TotalPoints = model.TotalPoints;
            PositionsGained = model.PositionGained;
            IsPlayer = model.IsPlayer;
            WasFastestLap = model.WasFastestLap;
        }

        public override ClassDriverResultDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}