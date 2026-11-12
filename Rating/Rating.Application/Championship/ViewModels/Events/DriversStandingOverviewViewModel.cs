namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.DataModel.Championship;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    public class DriversStandingOverviewViewModel : AbstractViewModel<IEnumerable<DriverDto>>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private bool _isCarNameVisible;
        private bool _isTeamNameVisible;
        private bool _isClassNameVisible;

        public DriversStandingOverviewViewModel(IViewModelFactory viewModelFactory)
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

        public bool IsClassNameVisible
        {
            get => _isClassNameVisible;
            set => SetProperty(ref _isClassNameVisible, value);
        }

        public IReadOnlyCollection<DriverStandingViewModel> DriversStanding { get; private set; }

        protected override void ApplyModel(IEnumerable<DriverDto> model)
        {
            int totalGap = 0;
            int previousPoints = 0;
            bool isFirst = true;
            List<DriverDto> eligibleDrivers = model.Where(x => !x.IsInactive).ToList();
            IsClassNameVisible = eligibleDrivers.Count > 2 && eligibleDrivers.Any(x => x.LastClassName != eligibleDrivers[0].LastClassName);
            IsCarNameVisible = eligibleDrivers.Count > 2 && eligibleDrivers.Any(x => x.LastCarName != eligibleDrivers[0].LastCarName);
            IsTeamNameVisible = eligibleDrivers.Any(x => !string.IsNullOrWhiteSpace(x.TeamName));
            DriversStanding = eligibleDrivers.OrderBy(x => x.Position).Select(x =>
            {
                var newViewModel = _viewModelFactory.Create<DriverStandingViewModel>();
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
                newViewModel.IsClassNameVisible = IsClassNameVisible;
                previousPoints = x.TotalPoints;
                isFirst = false;
                return newViewModel;
            }).ToList();
        }

        public override IEnumerable<DriverDto> SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}