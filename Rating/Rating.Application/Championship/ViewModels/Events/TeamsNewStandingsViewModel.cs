namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.DataModel.Championship.Events;

    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    public class TeamsNewStandingsViewModel : AbstractViewModel<IHaveTeamResults>
    {
        private readonly IViewModelFactory _viewModelFactory;

        public TeamsNewStandingsViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            TeamNewStandings = new List<TeamsNewStandingViewModel>();
            EventTitleViewModel = _viewModelFactory.Create<EventTitleViewModel>();
        }

        public EventTitleViewModel EventTitleViewModel { get; }

        public List<TeamsNewStandingViewModel> TeamNewStandings { get; }

        protected override void ApplyModel(IHaveTeamResults model)
        {
            int totalGap = 0;
            int previousPoints = 0;
            bool isFirst = true;
            foreach (TeamSessionResultDto teamSessionResultDto in model.TeamSessionResult.OrderBy(x => x.AfterEventPosition))
            {
                var newTeamStanding = _viewModelFactory.Create<TeamsNewStandingViewModel>();

                if (!isFirst)
                {
                    int gap = teamSessionResultDto.TotalPoints - previousPoints;
                    totalGap = gap + totalGap;
                    newTeamStanding.GapToPrevious = gap;
                    newTeamStanding.GapToLeader = totalGap;
                }

                previousPoints = teamSessionResultDto.TotalPoints;
                isFirst = false;

                newTeamStanding.FromModel(teamSessionResultDto);
                TeamNewStandings.Add(newTeamStanding);
            }
        }

        public override SessionResultDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}