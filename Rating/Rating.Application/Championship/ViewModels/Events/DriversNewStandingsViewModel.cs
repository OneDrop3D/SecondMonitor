namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.DataModel.Championship.Events;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    public class DriversNewStandingsViewModel : AbstractViewModel<SessionResultDto>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private bool _isCarNameVisible;

        public DriversNewStandingsViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            DriversNewStandings = new List<DriverNewStandingViewModel>();
            EventTitleViewModel = _viewModelFactory.Create<EventTitleViewModel>();
        }

        public EventTitleViewModel EventTitleViewModel { get; }

        public List<DriverNewStandingViewModel> DriversNewStandings { get; }

        public bool IsCarNameVisible
        {
            get => _isCarNameVisible;
            set => SetProperty(ref _isCarNameVisible, value);
        }

        protected override void ApplyModel(SessionResultDto model)
        {
            int totalGap = 0;
            int previousPoints = 0;
            bool isFirst = true;
            IsCarNameVisible = model.DriverSessionResult.Count > 1 && model.DriverSessionResult.Any(x => x.CarName != model.DriverSessionResult[0].CarName);
            foreach (DriverSessionResultDto driverSessionResultDto in model.DriverSessionResult.OrderBy(x => x.AfterEventPosition))
            {
                var newDriverNewStanding = _viewModelFactory.Create<DriverNewStandingViewModel>();

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

        public override SessionResultDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}