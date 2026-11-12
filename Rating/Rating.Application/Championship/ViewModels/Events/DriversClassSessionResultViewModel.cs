namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.DataModel.Championship.Events;

    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    public class DriversClassSessionResultViewModel : AbstractViewModel<ClassSessionResultDto>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private bool _isCarNameVisible;
        private bool _isTeamNameVisible;
        private bool _isClassNameVisible;

        public DriversClassSessionResultViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            DriversFinish = new List<DriverClassFinishViewModel>();
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

        public List<DriverClassFinishViewModel> DriversFinish { get; private set; }

        protected override void ApplyModel(ClassSessionResultDto model)
        {
            Header = "Driver Results";
            DriversFinish.Clear();
            IsCarNameVisible = model.ClassSessionResult.Count > 1 && model.ClassSessionResult.Any(x => x.CarName != model.ClassSessionResult[0].CarName);
            IsTeamNameVisible = model.ClassSessionResult.Any(x => !string.IsNullOrWhiteSpace(x.TeamName));
            foreach (ClassDriverResultDto driverSessionResultDto in model.ClassSessionResult.Where(x => !x.SkippedEvent).OrderBy(x => x.FinishPosition))
            {
                DriverClassFinishViewModel newViewModel = _viewModelFactory.Create<DriverClassFinishViewModel>();
                newViewModel.FromModel(driverSessionResultDto);
                newViewModel.IsCarNameVisible = IsCarNameVisible;
                newViewModel.IsTeamNameVisible = IsTeamNameVisible;
                DriversFinish.Add(newViewModel);
            }
        }

        public override ClassSessionResultDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}