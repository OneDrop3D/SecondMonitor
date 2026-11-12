namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.DataModel.Championship.Events;

    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    public class TeamsSessionResultViewModel : AbstractViewModel<IHaveTeamResults>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private string _header;
        private bool _hasAnyResults;

        public TeamsSessionResultViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            TeamsFinish = new List<TeamsFinishViewModel>();
        }

        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        public bool HasAnyResults
        {
            get => _hasAnyResults;
            set => SetProperty(ref _hasAnyResults, value);
        }

        public List<TeamsFinishViewModel> TeamsFinish { get; private set; }

        protected override void ApplyModel(IHaveTeamResults model)
        {
            TeamsFinish.Clear();
            foreach (TeamSessionResultDto teamSessionResultDto in model.TeamSessionResult.Where(x => !x.SkippedEvent).OrderBy(x => x.FinishPosition))
            {
                var newViewModel = _viewModelFactory.Create<TeamsFinishViewModel>();
                newViewModel.FromModel(teamSessionResultDto);
                TeamsFinish.Add(newViewModel);
            }

            HasAnyResults = TeamsFinish.Count > 0;
        }

        public override SessionResultDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}