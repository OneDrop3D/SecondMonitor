namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;

    using Common.DataModel.Championship;
    using SecondMonitor.ViewModels;

    public class DriverClassStandingViewModel : AbstractViewModel<ClassDriverDto>
    {
        private bool _isCarNameVisible;
        private bool _isTeamNameVisible;
        private bool _isClassNameVisible;
        private string _className;
        public string DriverName { get; private set; }
        public string CarName { get; private set; }
        public int TotalPoints { get; set; }

        public bool IsFirst => Position == 1;
        public int Position { get; private set; }
        public bool IsPlayer { get; private set; }
        public int GapToPrevious { get; set; }
        public int GapToLeader { get; set; }

        public string TeamName { get; set; }

        public bool IsCarNameVisible
        {
            get => _isCarNameVisible;
            set => SetProperty(ref _isCarNameVisible, value);
        }

        public bool IsTeamNameVisible
        {
            get => _isTeamNameVisible;
            set => SetProperty(ref _isTeamNameVisible, value);
        }

        protected override void ApplyModel(ClassDriverDto model)
        {
            DriverName = model.DriverName;
            CarName = model.CarName;
            TotalPoints = model.TotalPoints;
            Position = model.Position;
            IsPlayer = model.IsPlayer;
            TeamName = model.TeamName;
        }

        public override ClassDriverDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}