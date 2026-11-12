namespace SecondMonitor.Rating.Application.Championship.ViewModels.Overview
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using Common.DataModel.Championship;

    using Events;

    using SecondMonitor.Rating.Application.Championship.ViewModels.Overview.ProgressChart;
    using SecondMonitor.Rating.Application.Championship.ViewModels.Overview.ResultGrid;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    public class ChampionshipDetailViewModel : AbstractViewModel<ChampionshipDto>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly DriversStandingOverviewViewModel _driversStandingOverviewViewModel;
        private ICommand _closeCommand;

        public ChampionshipDetailViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            StandingsViewModels = new List<IViewModel>();
            _driversStandingOverviewViewModel = viewModelFactory.Create<DriversStandingOverviewViewModel>();
            CalendarResultsOverview = viewModelFactory.Create<CalendarResultsOverviewViewModel>();
            ChampionshipSessionsResults = viewModelFactory.Create<ChampionshipSessionsResultsViewModel>();
            DriversResultGridViewModels = new List<IViewModel>();
            ChampionshipProgressChartViewModels = new List<IViewModel>();
        }

        public string ChampionshipName { get; private set; }

        public List<IViewModel> StandingsViewModels { get; }

        public CalendarResultsOverviewViewModel CalendarResultsOverview { get; }

        public List<IViewModel> DriversResultGridViewModels { get; }

        public ChampionshipSessionsResultsViewModel ChampionshipSessionsResults { get; }

        public List<IViewModel> ChampionshipProgressChartViewModels { get; }

        public ICommand CloseCommand
        {
            get => _closeCommand;
            set => SetProperty(ref _closeCommand, value);
        }

        protected override void ApplyModel(ChampionshipDto model)
        {
            if (model == null)
            {
                return;
            }

            StandingsViewModels.Clear();

            ChampionshipName = model.ChampionshipName;
            _driversStandingOverviewViewModel.FromModel(model.Drivers);
            CalendarResultsOverview.FromModel(model);
            ChampionshipSessionsResults.FromModel(model);

            CreateResultGrids(model);
            CreateProgressCharts(model);

            StandingsViewModels.Add(_driversStandingOverviewViewModel);

            if (model.IsTrackingMultipleClasses)
            {
                foreach (ClassChampionshipDto championshipClassChampionshipDto in model.ClassChampionshipDtos)
                {
                    DriversClassStandingOverviewViewModel driverClassStandingViewModel = _viewModelFactory.Create<DriversClassStandingOverviewViewModel>();
                    driverClassStandingViewModel.FromModel(championshipClassChampionshipDto.Drivers);
                    driverClassStandingViewModel.Header = $"Current Driver Standings ({championshipClassChampionshipDto.ClassName})";
                    StandingsViewModels.Add(driverClassStandingViewModel);
                }
            }

            if (model.IsTrackingMultipleClasses)
            {
                AddTeamAndManufacturerStandingsAllClasses(model);
            }
            else
            {
                AddTeamAndManufacturerStandingsOverview(model);
            }
        }

        private void CreateProgressCharts(ChampionshipDto model)
        {
            ChampionshipProgressChartViewModel championshipProgressChartViewModel = _viewModelFactory.Create<ChampionshipProgressChartViewModel>();
            championshipProgressChartViewModel.Header = "Overall";
            championshipProgressChartViewModel.FromModel(model);
            ChampionshipProgressChartViewModels.Add(championshipProgressChartViewModel);

            if (!model.IsTrackingMultipleClasses)
            {
                return;
            }

            foreach (ClassChampionshipDto classChampionshipDto in model.ClassChampionshipDtos.Where(x => !string.IsNullOrWhiteSpace(x.ClassId)))
            {
                ChampionshipClassProgressChartViewModel newModel = _viewModelFactory.Create<ChampionshipClassProgressChartViewModel>();
                newModel.Header = classChampionshipDto.ClassName;
                newModel.FromModel((model, classChampionshipDto.GetClassIdentifier));
                ChampionshipProgressChartViewModels.Add(newModel);
            }
        }

        private void CreateResultGrids(ChampionshipDto model)
        {
            DriversResultGridViewModel driversResultGridViewModel = _viewModelFactory.Create<DriversResultGridViewModel>();
            driversResultGridViewModel.Header = "Overall";
            driversResultGridViewModel.FromModel(model);
            DriversResultGridViewModels.Add(driversResultGridViewModel);

            if (!model.IsTrackingMultipleClasses)
            {
                return;
            }

            foreach (ClassChampionshipDto classChampionshipDto in model.ClassChampionshipDtos.Where(x => !string.IsNullOrWhiteSpace(x.ClassId)))
            {
                DriversClassResultGridViewModel newModel = _viewModelFactory.Create<DriversClassResultGridViewModel>();
                newModel.Header = classChampionshipDto.ClassName;
                newModel.FromModel((model, classChampionshipDto.GetClassIdentifier));
                DriversResultGridViewModels.Add(newModel);
            }
        }

        private void AddTeamAndManufacturerStandingsAllClasses(ChampionshipDto model)
        {
            foreach (TeamsChampionshipDto championshipDto in model.TeamsChampionshipDtos.Where(x => x.TeamDtos.Count > 1))
            {
                TeamsStandingOverviewViewModel resultViewModel = _viewModelFactory.Create<TeamsStandingOverviewViewModel>();
                resultViewModel.FromModel(championshipDto);
                StandingsViewModels.Add(resultViewModel);
            }

            foreach (ManufacturersChampionshipDto championshipDto in model.ManufacturersChampionshipDtos.Where(x => x.Manufacturers.Count > 1))
            {
                ManufacturersStandingOverviewViewModel resultViewModel = _viewModelFactory.Create<ManufacturersStandingOverviewViewModel>();
                resultViewModel.FromModel(championshipDto);
                StandingsViewModels.Add(resultViewModel);
            }
        }

        private void AddTeamAndManufacturerStandingsOverview(ChampionshipDto model)
        {
            if (model.Drivers.Count == 0)
            {
                return;
            }

            TeamsChampionshipDto teamChampionshipDto = model.GetOrCreateTeamChampionship(model.GetOverviewClassIdentifiers());
            if (teamChampionshipDto.TeamDtos.Count > 1)
            {
                TeamsStandingOverviewViewModel resultViewModel = _viewModelFactory.Create<TeamsStandingOverviewViewModel>();
                resultViewModel.FromModel(teamChampionshipDto);
                StandingsViewModels.Add(resultViewModel);
            }

            ManufacturersChampionshipDto manufacturerChampionshipDto = model.GetOrCreateManufacturerChampionship(model.GetOverviewClassIdentifiers());
            if (manufacturerChampionshipDto.Manufacturers.Count > 1)
            {
                ManufacturersStandingOverviewViewModel resultViewModel = _viewModelFactory.Create<ManufacturersStandingOverviewViewModel>();
                resultViewModel.FromModel(manufacturerChampionshipDto);
                StandingsViewModels.Add(resultViewModel);
            }
        }

        public override ChampionshipDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}