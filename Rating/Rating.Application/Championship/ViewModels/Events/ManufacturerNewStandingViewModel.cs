namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;

    using Common.DataModel.Championship.Events;

    using SecondMonitor.ViewModels;

    public class ManufacturerNewStandingViewModel : AbstractViewModel<ManufacturerSessionResultDto>
    {
        public int Position { get; private set; }
        public string Name { get; private set; }
        public int TotalPoints { get; private set; }
        public int PositionsGained { get; private set; }
        public bool IsPlayer { get; private set; }

        public bool IsFirst { get; private set; }
        public int GapToPrevious { get; set; }
        public int GapToLeader { get; set; }

        protected override void ApplyModel(ManufacturerSessionResultDto model)
        {
            IsFirst = model.AfterEventPosition == 1;
            Position = model.AfterEventPosition;
            Name = model.ManufacturerName;
            TotalPoints = model.TotalPoints;
            PositionsGained = model.PositionGained;
            IsPlayer = model.IsPlayer;
        }

        public override ManufacturerSessionResultDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}