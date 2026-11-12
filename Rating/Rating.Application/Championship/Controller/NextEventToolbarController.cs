namespace SecondMonitor.Rating.Application.Championship.Controller
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.Rating.Application.Championship.Pool;
    using SecondMonitor.Rating.Application.Championship.ViewModels.Events;
    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.ViewModels.Controllers;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Layouts.Factory;
    using SecondMonitor.ViewModels.SessionEvents;

    public class NextEventToolbarController : AbstractChildController<IChampionshipController>
    {
        private readonly IChampionshipsPool _championshipsPool;
        private readonly CachedViewModelsFactory _cachedViewModelsFactory;
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly NextEventToolbarViewModel _nextEventToolbarViewModel;
        private List<ChampionshipDto> _eligibleChampionships;

        public NextEventToolbarController(IViewModelFactory viewModelFactory, IChampionshipsPool championshipsPool, CachedViewModelsFactory cachedViewModelsFactory,
            ISessionEventProvider sessionEventProvider)
            : base(viewModelFactory)
        {
            Bind<NextEventToolbarViewModel>().Command(x => x.NextChampionshipCommand)
                .To(ShowNextChampionshipOnToolbar);
            Bind<NextEventToolbarViewModel>().Command(x => x.RandomChampionshipCommand)
                .To(ShowRandomChampionshipCommand);
            _eligibleChampionships = [];
            _nextEventToolbarViewModel = ViewModelFactory.Create<NextEventToolbarViewModel>();
            _championshipsPool = championshipsPool;
            _cachedViewModelsFactory = cachedViewModelsFactory;
            _sessionEventProvider = sessionEventProvider;
        }

        public IChampionshipEventController ChampionshipEventController { get; set; }

        public void RefreshToolbar()
        {
            UpdateNextChampionshipViewModel(_sessionEventProvider.LastDataSet);
        }

        public override Task StartControllerAsync()
        {
            _championshipsPool.ChampionshipAdded += ChampionshipsPoolOnChampionshipAddedOrUpdated;
            _championshipsPool.ChampionshipUpdated += ChampionshipsPoolOnChampionshipAddedOrUpdated;
            _sessionEventProvider.SimulatorChanged += SessionEventProviderOnSimulatorChanged;
            _cachedViewModelsFactory.RegisterViewModel(NextEventToolbarViewModel.ViewModelLayoutName, _nextEventToolbarViewModel);
            UpdateNextChampionshipViewModel(_sessionEventProvider.LastDataSet);
            return Task.CompletedTask;
        }

        public override Task StopControllerAsync()
        {
            _championshipsPool.ChampionshipAdded -= ChampionshipsPoolOnChampionshipAddedOrUpdated;
            _championshipsPool.ChampionshipUpdated -= ChampionshipsPoolOnChampionshipAddedOrUpdated;
            _sessionEventProvider.SimulatorChanged -= SessionEventProviderOnSimulatorChanged;
            return Task.CompletedTask;
        }

        private void UpdateNextChampionshipViewModel(SimulatorDataSet dataSet)
        {
            IReadOnlyCollection<ChampionshipDto> championships = _championshipsPool.GetAllChampionshipDtos();
            if (ChampionshipEventController.IsChampionshipActive ||
                championships.All(x => x.ChampionshipState == ChampionshipState.Finished))
            {
                _nextEventToolbarViewModel.IsVisible = false;
                return;
            }

            List<ChampionshipDto> championshipDtos = championships.Where(x => x.ChampionshipState is not ChampionshipState.Finished).OrderByDescending(x => x.LastRunDateTime)
                .ThenBy(x => x.ChampionshipName).ToList();
            _eligibleChampionships = championshipDtos.Where(x => x.SimulatorName == dataSet?.Source).ToList();

            if (_eligibleChampionships.Count == 0)
            {
                _eligibleChampionships = championshipDtos;
            }

            ChampionshipDto championshipDto = _eligibleChampionships.FirstOrDefault();
            if (championshipDto == null)
            {
                _nextEventToolbarViewModel.IsVisible = false;
                return;
            }

            if (dataSet == null ||
                (dataSet.SessionInfo.SessionType == SessionType.Race && dataSet.PlayerInfo.FinishStatus is DriverFinishStatus.Finished or DriverFinishStatus.Na) ||
                dataSet.SessionInfo.SessionType == SessionType.Na)
            {
                _nextEventToolbarViewModel.IsVisible = true;
                _nextEventToolbarViewModel.FromModel(championshipDto);
                return;
            }

            _nextEventToolbarViewModel.IsVisible = false;
        }

        private void ShowNextChampionshipOnToolbar()
        {
            if (_eligibleChampionships.Count < 2)
            {
                return;
            }

            int index = _eligibleChampionships.IndexOf(_nextEventToolbarViewModel.OriginalModel);
            int indexOfNextChampionships = (index + 1) % _eligibleChampionships.Count;
            ChampionshipDto nextChampionships = _eligibleChampionships[indexOfNextChampionships];

            _nextEventToolbarViewModel.IsVisible = true;
            _nextEventToolbarViewModel.FromModel(nextChampionships);
        }

        private void ShowRandomChampionshipCommand()
        {
            if (_eligibleChampionships.Count < 2)
            {
                return;
            }

            int index = _eligibleChampionships.IndexOf(_nextEventToolbarViewModel.OriginalModel);
            int indexOfNextChampionships = indexOfNextChampionships = Random.Shared.Next(_eligibleChampionships.Count);
            if (indexOfNextChampionships == index)
            {
                ShowNextChampionshipOnToolbar();
                return;
            }

            ChampionshipDto nextChampionships = _eligibleChampionships[indexOfNextChampionships];

            _nextEventToolbarViewModel.IsVisible = true;
            _nextEventToolbarViewModel.FromModel(nextChampionships);
        }

        private void ChampionshipsPoolOnChampionshipAddedOrUpdated(object sender, ChampionshipEventArgs e)
        {
            RefreshToolbar();
        }

        private void SessionEventProviderOnSimulatorChanged(object sender, DataSetArgs e)
        {
            UpdateNextChampionshipViewModel(e.DataSet);
        }
    }
}
