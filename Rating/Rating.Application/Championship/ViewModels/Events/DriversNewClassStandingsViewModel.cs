namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.DataModel.Championship.Events;

    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    public class DriversNewClassStandingsViewModel : AbstractViewModel<ClassSessionResultDto>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private bool _isCarNameVisible;

        public DriversNewClassStandingsViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            DriversNewStandings = new List<DriverNewClassStandingViewModel>();
            EventTitleViewModel = _viewModelFactory.Create<EventTitleViewModel>();
        }

        public EventTitleViewModel EventTitleViewModel { get; }

        public List<DriverNewClassStandingViewModel> DriversNewStandings { get; }

        public bool IsCarNameVisible
        {
            get => _isCarNameVisible;
            set => SetProperty(ref _isCarNameVisible, value);
        }

        protected override void ApplyModel(ClassSessionResultDto model)
        {
            int totalGap = 0;
            int previousPoints = 0;
            bool isFirst = true;
            IsCarNameVisible = model.ClassSessionResult.Count > 1 && model.ClassSessionResult.Any(x => x.CarName != model.ClassSessionResult[0].CarName);
            foreach (ClassDriverResultDto driverSessionResultDto in model.ClassSessionResult.OrderBy(x => x.AfterEventPosition))
            {
                DriverNewClassStandingViewModel newDriverNewStanding = _viewModelFactory.Create<DriverNewClassStandingViewModel>();

                if (!isFirst)
                {
                    int gap = driverSessionResultDto.TotalPoints - previousPoints;
                    totalGap = gap + totalGap;
                    newDriverNewStanding.GapToPrevious = gap;
                    newDriverNewStanding.GapToLeader = totalGap;
                }

                newDriverNewStanding.IsCarNameVisible = IsCarNameVisible;
                previousPoints = driverSessionResultDto.TotalPoints;
                isFirst = false;

                newDriverNewStanding.FromModel(driverSessionResultDto);
                DriversNewStandings.Add(newDriverNewStanding);
            }
        }

        public override ClassSessionResultDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}