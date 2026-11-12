namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.DataModel.Championship.Events;

    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    public class ManufacturerNewStandingsViewModel : AbstractViewModel<IHaveManufacturersResults>
    {
        private readonly IViewModelFactory _viewModelFactory;

        public ManufacturerNewStandingsViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            ManufacturerNewStandings = new List<ManufacturerNewStandingViewModel>();
            EventTitleViewModel = _viewModelFactory.Create<EventTitleViewModel>();
        }

        public EventTitleViewModel EventTitleViewModel { get; }

        public List<ManufacturerNewStandingViewModel> ManufacturerNewStandings { get; }

        protected override void ApplyModel(IHaveManufacturersResults model)
        {
            int totalGap = 0;
            int previousPoints = 0;
            bool isFirst = true;
            foreach (ManufacturerSessionResultDto driverSessionResultDto in model.ManufacturersSessionResult.OrderBy(x => x.AfterEventPosition))
            {
                var newManufacturerStanding = _viewModelFactory.Create<ManufacturerNewStandingViewModel>();

                if (!isFirst)
                {
                    int gap = driverSessionResultDto.TotalPoints - previousPoints;
                    totalGap = gap + totalGap;
                    newManufacturerStanding.GapToPrevious = gap;
                    newManufacturerStanding.GapToLeader = totalGap;
                }

                previousPoints = driverSessionResultDto.TotalPoints;
                isFirst = false;

                newManufacturerStanding.FromModel(driverSessionResultDto);
                ManufacturerNewStandings.Add(newManufacturerStanding);
            }
        }

        public override SessionResultDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}