namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.DataModel.Championship;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    public class ManufacturersStandingOverviewViewModel : AbstractViewModel<ManufacturersChampionshipDto>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private string _header;

        public ManufacturersStandingOverviewViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            Header = "Current Manufacturers Standings";
        }

        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        public IReadOnlyCollection<ManufacturerStandingViewModel> ManufacturerStandings { get; private set; }

        protected override void ApplyModel(ManufacturersChampionshipDto model)
        {
            int totalGap = 0;
            int previousPoints = 0;
            bool isFirst = true;
            string className = string.IsNullOrWhiteSpace(model.ClassName) ? "Overall" : model.ClassName;
            Header = $"Current Manufacturers Standings ({className})";
            ManufacturerStandings = model.Manufacturers.OrderBy(x => x.Position).Select(x =>
            {
                ManufacturerStandingViewModel newViewModel = _viewModelFactory.Create<ManufacturerStandingViewModel>();
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

        public override ManufacturersChampionshipDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}