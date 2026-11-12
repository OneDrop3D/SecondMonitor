namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;

    using Common.DataModel.Championship;
    using SecondMonitor.ViewModels;

    public class DriverStandingViewModel : AbstractViewModel<DriverDto>
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

        public bool IsClassNameVisible
        {
            get => _isClassNameVisible;
            set => SetProperty(ref _isClassNameVisible, value);
        }

        public string ClassName
        {
            get => _className;
            set => SetProperty(ref _className, value);
        }

        protected override void ApplyModel(DriverDto model)
        {
            DriverName = model.LastUsedName;
            CarName = model.LastCarName;
            TotalPoints = model.TotalPoints;
            Position = model.Position;
            IsPlayer = model.IsPlayer;
            TeamName = model.TeamName;
            ClassName = model.LastClassName;
        }

        public override DriverDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}