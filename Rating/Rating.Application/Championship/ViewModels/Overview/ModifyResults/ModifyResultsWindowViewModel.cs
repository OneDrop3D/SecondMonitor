namespace SecondMonitor.Rating.Application.Championship.ViewModels.Overview.ModifyResults
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Input;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.Rating.Common.DataModel.Championship.Events;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    public class ModifyResultsWindowViewModel : AbstractViewModel<ModifyResultsWindowModel>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private string _header;
        private bool _isCarNameVisible;
        private bool _isTeamNameVisible;
        private bool _suppressModificationEvents;
        private ScoringDto _scoringDto = new ScoringDto();
        private ICommand _okCommand;
        private ICommand _cancelCommand;

        public ModifyResultsWindowViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }

        public ObservableCollection<DriverResultViewModel> DriverResultViewModels { get; } = [];

        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        public ICommand OkCommand
        {
            get => _okCommand;
            set => SetProperty(ref _okCommand, value);
        }

        public ICommand CancelCommand
        {
            get => _cancelCommand;
            set => SetProperty(ref _cancelCommand, value);
        }

        public AmendedSessionResult CreateAmendedSessionResult()
        {
            return new AmendedSessionResult(OriginalModel.SessionDto.SessionResult, DriverResultViewModels.Select(x => x.CreateAmendedDriverResult()).ToList());
        }

        protected override void ApplyModel(ModifyResultsWindowModel model)
        {
            _scoringDto = model.ScoringDto;
            DriverResultViewModels.ForEach(x => x.PropertyChanged -= DriverResultViewModelOnPropertyChanged);
            DriverResultViewModels.Clear();

            Header = $"{model.EventDto.EventName} at {model.EventDto.TrackName} ({model.SessionDto.Name})";
            _isCarNameVisible = model.SessionDto.SessionResult.DriverSessionResult.Count > 1 && model.SessionDto.SessionResult.DriverSessionResult.Any(x => x.CarName != model.SessionDto.SessionResult.DriverSessionResult[0].CarName);
            _isTeamNameVisible = model.SessionDto.SessionResult.DriverSessionResult.Any(x => !string.IsNullOrWhiteSpace(x.TeamName));
            foreach (DriverSessionResultDto driverSessionResultDto in model.SessionDto.SessionResult.DriverSessionResult.OrderBy(x => x.FinishPosition))
            {
                DriverResultViewModel driverResultViewModel = _viewModelFactory.Create<DriverResultViewModel>();
                driverResultViewModel.FromModel(driverSessionResultDto);
                driverResultViewModel.IsCarNameVisible = _isCarNameVisible;
                driverResultViewModel.IsTeamNameVisible = _isTeamNameVisible;
                driverResultViewModel.PropertyChanged += DriverResultViewModelOnPropertyChanged;
                DriverResultViewModels.Add(driverResultViewModel);
            }
        }

        private void DriverResultViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_suppressModificationEvents || e.PropertyName != nameof(DriverResultViewModel.FinishPosition) || sender is not DriverResultViewModel driverResultViewModel)
            {
                return;
            }

            _suppressModificationEvents = true;
            driverResultViewModel.FinishPosition = Math.Clamp(driverResultViewModel.FinishPosition, 1, DriverResultViewModels.Count);
            DriverResultViewModels.Move(DriverResultViewModels.IndexOf(driverResultViewModel), driverResultViewModel.FinishPosition - 1);

            Dictionary<string, List<DriverResultViewModel>> driversGroupedByClass = DriverResultViewModels
                .GroupBy(x => x.ClassId)
                .ToDictionary(x => x.Key, x => x.ToList());

            for (int i = 0; i < DriverResultViewModels.Count; i++)
            {
                DriverResultViewModel modifiedDriverResult = DriverResultViewModels[i];
                modifiedDriverResult.FinishPosition = i + 1;
                modifiedDriverResult.ClassFinishPosition = driversGroupedByClass[modifiedDriverResult.ClassId].IndexOf(modifiedDriverResult) + 1;
                modifiedDriverResult.PointsGain = modifiedDriverResult.WasFastestLap && modifiedDriverResult.FinishPosition <= _scoringDto.MaxPositionForFastestLap ?
                    _scoringDto.GetPointsForPosition(modifiedDriverResult.FinishPosition) + _scoringDto.PointsForFastestLap
                    : _scoringDto.GetPointsForPosition(modifiedDriverResult.FinishPosition);
            }

            _suppressModificationEvents = false;
        }

        public override ModifyResultsWindowModel SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}
