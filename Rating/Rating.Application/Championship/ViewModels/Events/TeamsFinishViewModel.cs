namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;

    using Common.DataModel.Championship.Events;
    using SecondMonitor.ViewModels;

    public class TeamsFinishViewModel : AbstractViewModel<TeamSessionResultDto>
    {
        public string TeamName { get; private set; }
        public int FinishPosition { get; private set; }
        public int PointsGain { get; private set; }
        public bool IsPlayer { get; private set; }

        protected override void ApplyModel(TeamSessionResultDto model)
        {
            TeamName = model.TeamName;
            FinishPosition = model.FinishPosition;
            PointsGain = model.PointsGain;
            IsPlayer = model.IsPlayer;
        }

        public override TeamSessionResultDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}