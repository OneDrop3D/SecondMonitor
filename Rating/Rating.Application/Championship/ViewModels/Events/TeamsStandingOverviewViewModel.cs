namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.DataModel.Championship;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    public class TeamsStandingOverviewViewModel : AbstractViewModel<TeamsChampionshipDto>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private string _header;

        public TeamsStandingOverviewViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            Header = "Current Teams Standings";
        }

        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        public IReadOnlyCollection<TeamStandingViewModel> TeamsStandings { get; private set; }

        protected override void ApplyModel(TeamsChampionshipDto model)
        {
            int totalGap = 0;
            int previousPoints = 0;
            bool isFirst = true;
            string className = string.IsNullOrWhiteSpace(model.ClassName) ? "Overall" : model.ClassName;
            Header = $"Current Teams Standings ({className})";
            TeamsStandings = model.TeamDtos.OrderBy(x => x.Position).Select(x =>
            {
                TeamStandingViewModel newViewModel = _viewModelFactory.Create<TeamStandingViewModel>();
                newViewModel.FromModel(x);
                if (!isFirst)
                {
                    int gap = previousPoints - x.TotalPoints;
                    totalGap = gap + totalGap;
                    newViewModel.GapToPrevious = gap;
                    newViewModel.GapToLeader = totalGap;
                }

                previousPoints = x.TotalPoints;
                isFirst = false;
                return newViewModel;
            }).ToList();
        }

        public override TeamsChampionshipDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}