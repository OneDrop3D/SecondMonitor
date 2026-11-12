namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Input;

    using Common.DataModel.Championship;
    using Common.DataModel.Championship.Events;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    public class SessionResultWithTitleViewModel : AbstractViewModel<(ChampionshipDto Championship, EventDto EventDto, SessionDto SessionDto)>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private bool _showManufacturerResults;
        private string _remark;
        private bool _showTeamsResults;
        private ICommand _amendResults;
        private bool _showClassResults;

        public SessionResultWithTitleViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            EventTitleViewModel = viewModelFactory.Create<EventTitleViewModel>();
            DriversSessionResultViewModel = viewModelFactory.Create<DriversSessionResultViewModel>();
            TeamsSessionResultViewModels = [];
            ManufacturersSessionResultViewModels = [];
            DriversClassSessionResultViewModels = new List<DriversClassSessionResultViewModel>();
        }

        public EventTitleViewModel EventTitleViewModel { get; }
        public DriversSessionResultViewModel DriversSessionResultViewModel { get; }
        public List<DriversClassSessionResultViewModel> DriversClassSessionResultViewModels { get; }
        public List<TeamsSessionResultViewModel> TeamsSessionResultViewModels { get; }
        public List<ManufacturersSessionResultViewModel> ManufacturersSessionResultViewModels { get; }

        public ICommand AmendResults
        {
            get => _amendResults;
            set => SetProperty(ref _amendResults, value);
        }

        public string Remark
        {
            get => _remark;
            private set => SetProperty(ref _remark, value);
        }

        public bool ShowManufacturerResults
        {
            get => _showManufacturerResults;
            private set => SetProperty(ref _showManufacturerResults, value);
        }

        public bool ShowTeamsResults
        {
            get => _showTeamsResults;
            private set => SetProperty(ref _showTeamsResults, value);
        }

        public bool ShowClassResults
        {
            get => _showClassResults;
            set => SetProperty(ref _showClassResults, value);
        }

        protected override void ApplyModel((ChampionshipDto Championship, EventDto EventDto, SessionDto SessionDto) model)
        {
            (ChampionshipDto championship, EventDto eventDto, SessionDto sessionDto) = model;
            EventTitleViewModel.FromModel(model);
            CreateDriversResults(sessionDto, championship);
            CreateTeamsResults(sessionDto, championship);
            CreateManufacturersResults(championship, sessionDto);

            Remark = sessionDto.SessionResult.Remarks;
            if (Remark.Length > 200)
            {
                Remark = $"{Remark.AsSpan(0, 200)}...";
            }
        }

        private void CreateManufacturersResults(ChampionshipDto championship, SessionDto sessionDto)
        {
            ClassIdentifier classIdentifier = championship.GetOverviewClassIdentifiers();
            string classTitle = string.IsNullOrWhiteSpace(classIdentifier.ClassName) ? "Overall" : classIdentifier.ClassName;
            ManufacturersSessionResultViewModel manufacturersSessionResultViewModel = _viewModelFactory.Create<ManufacturersSessionResultViewModel>();
            manufacturersSessionResultViewModel.FromModel(sessionDto.SessionResult);
            manufacturersSessionResultViewModel.Header = $"Manufacturer Results ({classTitle})";
            ShowManufacturerResults = championship.IsManufacturerScoringEnabled && manufacturersSessionResultViewModel.ManufacturerFinish.Count > 1;
            ManufacturersSessionResultViewModels.Add(manufacturersSessionResultViewModel);

            if (!championship.IsTrackingMultipleClasses)
            {
                return;
            }

            foreach (ClassSessionResultDto classSessionResult in sessionDto.SessionResult.ClassSessionResults)
            {
                manufacturersSessionResultViewModel = _viewModelFactory.Create<ManufacturersSessionResultViewModel>();
                manufacturersSessionResultViewModel.FromModel(classSessionResult);
                manufacturersSessionResultViewModel.Header = $"Manufacturer Results ({classSessionResult.ClassName})";
                ManufacturersSessionResultViewModels.Add(manufacturersSessionResultViewModel);
            }
        }

        private void CreateTeamsResults(SessionDto sessionDto, ChampionshipDto championship)
        {
            TeamsSessionResultViewModel teamsSessionResultViewModel = _viewModelFactory.Create<TeamsSessionResultViewModel>();
            teamsSessionResultViewModel.FromModel(sessionDto.SessionResult);
            ClassIdentifier classIdentifier = championship.GetOverviewClassIdentifiers();
            string classTitle = string.IsNullOrWhiteSpace(classIdentifier.ClassName) ? "Overall" : classIdentifier.ClassName;
            teamsSessionResultViewModel.Header = $"Team Results ({classTitle})";
            TeamsSessionResultViewModels.Add(teamsSessionResultViewModel);
            ShowTeamsResults = championship.IsTeamScoringEnabled && teamsSessionResultViewModel.TeamsFinish.Count > 1;

            if (!championship.IsTrackingMultipleClasses)
            {
                return;
            }

            foreach (ClassSessionResultDto classSessionResult in sessionDto.SessionResult.ClassSessionResults)
            {
                teamsSessionResultViewModel = _viewModelFactory.Create<TeamsSessionResultViewModel>();
                teamsSessionResultViewModel.FromModel(classSessionResult);
                teamsSessionResultViewModel.Header = $"Team Results ({classSessionResult.ClassName})";
                TeamsSessionResultViewModels.Add(teamsSessionResultViewModel);
            }
        }

        private void CreateDriversResults(SessionDto sessionDto, ChampionshipDto championship)
        {
            DriversSessionResultViewModel.FromModel(sessionDto.SessionResult);

            ShowClassResults = championship.IsTrackingMultipleClasses;
            if (!championship.IsTrackingMultipleClasses)
            {
                return;
            }

            foreach (ClassSessionResultDto sessionResultClassSessionResult in sessionDto.SessionResult.ClassSessionResults)
            {
                DriversClassSessionResultViewModel driversClassSessionResultViewModel = _viewModelFactory.Create<DriversClassSessionResultViewModel>();
                driversClassSessionResultViewModel.FromModel(sessionResultClassSessionResult);
                driversClassSessionResultViewModel.Header = $"Driver Results ({sessionResultClassSessionResult.ClassName})";
                DriversClassSessionResultViewModels.Add(driversClassSessionResultViewModel);
            }
        }

        public override (ChampionshipDto Championship, EventDto EventDto, SessionDto SessionDto) SaveToNewModel()
        {
            throw new System.NotSupportedException();
        }
    }
}