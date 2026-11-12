namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.DataModel.Championship;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    public class DriversClassStandingOverviewViewModel : AbstractViewModel<IEnumerable<ClassDriverDto>>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private bool _isCarNameVisible;
        private bool _isTeamNameVisible;

        public DriversClassStandingOverviewViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            Header = "Current Driver Standings";
        }

        public string Header { get; set; }

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

        public IReadOnlyCollection<DriverClassStandingViewModel> DriversStanding { get; private set; }

        protected override void ApplyModel(IEnumerable<ClassDriverDto> model)
        {
            int totalGap = 0;
            int previousPoints = 0;
            bool isFirst = true;
            List<ClassDriverDto> eligibleDrivers = model.ToList();
            IsCarNameVisible = eligibleDrivers.Count > 2 && eligibleDrivers.Any(x => x.CarName != eligibleDrivers[0].CarName);
            IsTeamNameVisible = eligibleDrivers.Any(x => !string.IsNullOrWhiteSpace(x.TeamName));
            DriversStanding = eligibleDrivers.OrderBy(x => x.Position).Select(x =>
            {
                var newViewModel = _viewModelFactory.Create<DriverClassStandingViewModel>();
                newViewModel.FromModel(x);
                if (!isFirst)
                {
                    int gap = previousPoints - x.TotalPoints;
                    totalGap = gap + totalGap;
                    newViewModel.GapToPrevious = gap;
                    newViewModel.GapToLeader = totalGap;
                }

                newViewModel.IsCarNameVisible = IsCarNameVisible;
                newViewModel.IsTeamNameVisible = IsTeamNameVisible;
                previousPoints = x.TotalPoints;
                isFirst = false;
                return newViewModel;
            }).ToList();
        }

        public override IEnumerable<ClassDriverDto> SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}