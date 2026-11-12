namespace SecondMonitor.Rating.Application.Championship.ViewModels.Overview
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows.Input;
    using Common.DataModel.Championship;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.SessionEvents;

    public class ChampionshipsOverviewViewModel : AbstractViewModel<IEnumerable<ChampionshipDto>>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly ISessionEventProvider _sessionEventProvider;
        private ICommand _createNewCommand;
        private ICommand _deleteSelectedCommand;
        private ICommand _openSelectedCommand;
        private ICommand _resetSelectedCommand;
        private ChampionshipOverviewViewModel _selectedChampionship;
        private ObservableCollection<ChampionshipOverviewViewModel> _allChampionships;
        private ICommand _reRollMysteryPropertiesCommand;
        private ICommand _saveSelectedChampionshipCalendarCommand;
        private bool _isReRollButtonVisible;
        private ICommand _modifyLastResultCommand;
        private bool _isModifyLastResultCommandVisible;

        public ChampionshipsOverviewViewModel(IViewModelFactory viewModelFactory, ISessionEventProvider sessionEventProvider)
        {
            _viewModelFactory = viewModelFactory;
            _sessionEventProvider = sessionEventProvider;
            AllChampionships = new ObservableCollection<ChampionshipOverviewViewModel>();
            NextRaceOverviewViewModel = viewModelFactory.Create<NextRaceOverviewViewModel>();
        }

        public NextRaceOverviewViewModel NextRaceOverviewViewModel { get; }

        public ICommand CreateNewCommand
        {
            get => _createNewCommand;
            set => SetProperty(ref _createNewCommand, value);
        }

        public ICommand DeleteSelectedCommand
        {
            get => _deleteSelectedCommand;
            set => SetProperty(ref _deleteSelectedCommand, value);
        }

        public ICommand OpenSelectedCommand
        {
            get => _openSelectedCommand;
            set => SetProperty(ref _openSelectedCommand, value);
        }

        public ICommand ResetSelectedCommand
        {
            get => _resetSelectedCommand;
            set => SetProperty(ref _resetSelectedCommand, value);
        }

        public ICommand ReRollMysteryPropertiesCommand
        {
            get => _reRollMysteryPropertiesCommand;
            set => SetProperty(ref _reRollMysteryPropertiesCommand, value);
        }

        public ICommand SaveSelectedChampionshipCalendarCommand
        {
            get => _saveSelectedChampionshipCalendarCommand;
            set => SetProperty(ref _saveSelectedChampionshipCalendarCommand, value);
        }

        public bool IsReRollButtonVisible
        {
            get => _isReRollButtonVisible;
            set => SetProperty(ref _isReRollButtonVisible, value);
        }

        public ICommand ModifyLastResultCommand
        {
            get => _modifyLastResultCommand;
            set => SetProperty(ref _modifyLastResultCommand, value);
        }

        public bool IsModifyLastResultCommandVisible
        {
            get => _isModifyLastResultCommandVisible;
            set => SetProperty(ref _isModifyLastResultCommandVisible, value);
        }

        public ChampionshipOverviewViewModel SelectedChampionship
        {
            get => _selectedChampionship;
            set
            {
                SetProperty(ref _selectedChampionship, value);
                OnSelectedChampionshipChange();
            }
        }

        public ObservableCollection<ChampionshipOverviewViewModel> AllChampionships
        {
            get => _allChampionships;
            private set => SetProperty(ref _allChampionships, value);
        }

        public ICommand RemoveSelectedCommand { get; set; }

        private void OnSelectedChampionshipChange()
        {
            if (_selectedChampionship != null)
            {
                NextRaceOverviewViewModel.FromModel(_selectedChampionship.OriginalModel);
                if (_sessionEventProvider.LastDataSet?.PlayerInfo != null)
                {
                    NextRaceOverviewViewModel.Evaluate(_sessionEventProvider.LastDataSet);
                }
            }

            IsReRollButtonVisible = _selectedChampionship?.OriginalModel.CurrentSessionIndex == 0 && _selectedChampionship.OriginalModel.ChampionshipState != ChampionshipState.LastSessionCanceled
                                                                                                  && _selectedChampionship.OriginalModel.ChampionshipState != ChampionshipState.Finished
                                                                                                  && (_selectedChampionship.OriginalModel.IsMysteryClass || _selectedChampionship.OriginalModel.GetCurrentOrLastEvent().IsMysteryTrack);
            IsModifyLastResultCommandVisible = _selectedChampionship?.OriginalModel.GetAllResults().Any() == true;
        }

        protected override void ApplyModel(IEnumerable<ChampionshipDto> model)
        {
            AllChampionships.Clear();
            model.OrderByDescending(x => x.LastRunDateTime).ThenBy(x => x.ChampionshipName).ForEach(AddChampionship);
            SelectedChampionship = AllChampionships.FirstOrDefault();
        }

        public override IEnumerable<ChampionshipDto> SaveToNewModel()
        {
            throw new NotSupportedException();
        }

        public void InsertChampionshipFirst(ChampionshipDto championshipDto)
        {
            var newViewModel = _viewModelFactory.Create<ChampionshipOverviewViewModel>();
            newViewModel.FromModel(championshipDto);
            AllChampionships.Insert(0, newViewModel);
        }

        private void AddChampionship(ChampionshipDto championshipDto)
        {
            var newViewModel = _viewModelFactory.Create<ChampionshipOverviewViewModel>();
            newViewModel.FromModel(championshipDto);
            AllChampionships.Add(newViewModel);
        }

        public void RemoveChampionship(ChampionshipDto championshipDto)
        {
            List<ChampionshipOverviewViewModel> toRemove = AllChampionships.Where(x => x.OriginalModel.ChampionshipGlobalId == championshipDto.ChampionshipGlobalId).ToList();
            toRemove.ForEach(x => AllChampionships.Remove(x));
        }
    }
}