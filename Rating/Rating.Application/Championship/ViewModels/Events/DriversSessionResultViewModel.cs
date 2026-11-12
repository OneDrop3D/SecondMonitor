namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.DataModel.Championship.Events;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    public class DriversSessionResultViewModel : AbstractViewModel<SessionResultDto>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private bool _isCarNameVisible;
        private bool _isTeamNameVisible;
        private bool _isClassNameVisible;

        public DriversSessionResultViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            DriversFinish = new List<DriverFinishViewModel>();
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

        public List<DriverFinishViewModel> DriversFinish { get; private set; }

        protected override void ApplyModel(SessionResultDto model)
        {
            Header = "Driver Results";
            DriversFinish.Clear();
            IsCarNameVisible = model.DriverSessionResult.Count > 1 && model.DriverSessionResult.Any(x => x.CarName != model.DriverSessionResult[0].CarName);
            IsClassNameVisible = model.ClassSessionResults.Count > 1;
            IsTeamNameVisible = model.DriverSessionResult.Any(x => !string.IsNullOrWhiteSpace(x.TeamName));
            foreach (DriverSessionResultDto driverSessionResultDto in model.DriverSessionResult.Where(x => !x.SkippedEvent).OrderBy(x => x.FinishPosition))
            {
                DriverFinishViewModel newViewModel = _viewModelFactory.Create<DriverFinishViewModel>();
                newViewModel.FromModel(driverSessionResultDto);
                newViewModel.IsCarNameVisible = IsCarNameVisible;
                newViewModel.IsTeamNameVisible = IsTeamNameVisible;
                newViewModel.IsClassNameVisible = IsClassNameVisible;
                DriversFinish.Add(newViewModel);
            }
        }

        public override SessionResultDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}