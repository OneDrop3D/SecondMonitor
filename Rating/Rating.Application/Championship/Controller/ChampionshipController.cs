namespace SecondMonitor.Rating.Application.Championship.Controller
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Common.DataModel.Championship;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using Filters;
    using NLog;
    using Pool;

    using SecondMonitor.ViewModels.Controllers;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.SessionEvents;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    using ViewModels.IconState;

    public class ChampionshipController : IChampionshipController, IChampionshipCurrentEventPointsProvider
    {
        private static readonly Logger logger = LogManager.CreateNullLogger();
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly IChampionshipsPool _championshipsPool;
        private readonly IChampionshipEligibilityEvaluator _championshipEligibilityEvaluator;
        private readonly IChampionshipOverviewController _championshipOverviewController;
        private readonly IChampionshipEventController _championshipEventController;
        private readonly IChampionshipSelectionController _championshipSelectionController;
        private readonly NextEventToolbarController _nextEventToolbarController;
        private readonly DisplaySettingsViewModel _displaySettingsViewModel;
        private List<ChampionshipDto> _championshipCandidates;

        public ChampionshipController(IViewModelFactory viewModelFactory, IChildControllerFactory childControllerFactory, ISessionEventProvider sessionEventProvider, ISettingsProvider settingsProvider,
            IChampionshipsPool championshipsPool, IChampionshipEligibilityEvaluator championshipEligibilityEvaluator)
        {
            _displaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
            _championshipCandidates = new List<ChampionshipDto>();
            _sessionEventProvider = sessionEventProvider;
            _championshipsPool = championshipsPool;
            _championshipEligibilityEvaluator = championshipEligibilityEvaluator;
            ChampionshipIconStateViewModel = viewModelFactory.Create<ChampionshipIconStateViewModel>();
            _championshipOverviewController = childControllerFactory.Create<IChampionshipOverviewController, IChampionshipController>(this);
            _championshipEventController = childControllerFactory.Create<IChampionshipEventController, IChampionshipController>(this);
            _championshipSelectionController = childControllerFactory.Create<IChampionshipSelectionController, IChampionshipController>(this);
            _nextEventToolbarController = childControllerFactory.Create<NextEventToolbarController, IChampionshipController>(this);
            _nextEventToolbarController.ChampionshipEventController = _championshipEventController;
            SetChampionshipIconToNone();
        }

        public ChampionshipIconStateViewModel ChampionshipIconStateViewModel { get; }

        public async Task StartControllerAsync()
        {
            _sessionEventProvider.DriversAdded += ReEvaluateChampionships;
            _sessionEventProvider.DriversRemoved += ReEvaluateChampionships;
            _sessionEventProvider.PlayerFinishStateChanged += ReEvaluateChampionships;
            _sessionEventProvider.PlayerPropertiesChanged += ReEvaluateChampionships;
            _sessionEventProvider.TrackChanged += ReEvaluateChampionships;
            await StartChildControllersAsync();
        }

        public async Task StopControllerAsync()
        {
            _sessionEventProvider.DriversAdded -= ReEvaluateChampionships;
            _sessionEventProvider.DriversRemoved -= ReEvaluateChampionships;
            _sessionEventProvider.PlayerFinishStateChanged -= ReEvaluateChampionships;
            _sessionEventProvider.PlayerPropertiesChanged -= ReEvaluateChampionships;
            _sessionEventProvider.TrackChanged -= ReEvaluateChampionships;
            await StopChildControllersAsync();
        }

        public bool TryGetPointsAndPositionForDriver(string driverName, out int points, out int position)
        {
            if (!_championshipEventController.IsChampionshipActive)
            {
                points = 0;
                position = 0;
                return false;
            }

            DriverDto driver = _championshipEventController.CurrentChampionship.Drivers.FirstOrDefault(x => x.LastUsedName == driverName);
            points = driver?.TotalPoints ?? 0;
            position = driver?.Position ?? 0;
            return driver != null;
        }

        public void OpenChampionshipWindow()
        {
            if (_championshipEventController.IsChampionshipActive && ChampionshipIconStateViewModel.ChampionshipIconState == ChampionshipIconState.ChampionshipInProgress)
            {
                OpenRunningChampionshipDetail();
                return;
            }

            if (_championshipCandidates.Count > 0)
            {
                OpenCandidatesSelector();
                return;
            }

            _championshipOverviewController.OpenChampionshipOverviewWindow();
        }

        private void OpenRunningChampionshipDetail()
        {
            _championshipOverviewController.OpenChampionshipDetailsWindow(_championshipEventController.CurrentChampionship);
        }

        public void StartNextEvent(ChampionshipDto championship)
        {
            _championshipCandidates.Clear();
            _championshipEventController.StartNextEvent(championship);
            SetIconToRunning();
        }

        public void EventFinished(ChampionshipDto championship)
        {
            if (championship != null)
            {
                _championshipsPool.UpdateChampionship(championship);
            }

            ReEvaluateChampionships(_sessionEventProvider.LastDataSet);
        }

        protected async Task StartChildControllersAsync()
        {
            await _championshipOverviewController.StartControllerAsync();
            await _championshipEventController.StartControllerAsync();
            await _nextEventToolbarController.StartControllerAsync();
        }

        protected async Task StopChildControllersAsync()
        {
            await _championshipOverviewController.StopControllerAsync();
            await _championshipOverviewController.StopControllerAsync();
            await _nextEventToolbarController.StopControllerAsync();
        }

        private void ReEvaluateChampionships(object sender, DataSetArgs e)
        {
            ReEvaluateChampionships(e.DataSet);
        }

        private void ReEvaluateChampionships(object sender, DriversArgs e)
        {
            ReEvaluateChampionships(e.DataSet);
        }

        private void SetChampionshipIconToNone()
        {
            logger.Info("Icon set to none");
            _championshipCandidates.Clear();
            ChampionshipIconStateViewModel.ChampionshipIconState = ChampionshipIconState.None;
            ChampionshipIconStateViewModel.TooltipText = "Opens all championships overview.";
            _nextEventToolbarController.RefreshToolbar();
        }

        private void ReEvaluateChampionships(SimulatorDataSet dataSet)
        {
            if (dataSet.SessionInfo.SessionType == SessionType.Na || string.IsNullOrEmpty(dataSet.SessionInfo.TrackInfo.TrackFullName))
            {
                SetChampionshipIconToNone();
                return;
            }

            if (string.IsNullOrEmpty(dataSet.PlayerInfo?.DriverSessionId) || dataSet.PlayerInfo?.FinishStatus == DriverFinishStatus.Finished || dataSet.PlayerInfo?.FinishStatus == DriverFinishStatus.Dnf || dataSet.PlayerInfo?.FinishStatus == DriverFinishStatus.Dq)
            {
                SetChampionshipIconToNone();
                return;
            }

            if (_championshipEventController.IsChampionshipActive)
            {
                SetIconToRunning();
                return;
            }

            if (_championshipEventController.TryResumePreviousChampionship(dataSet))
            {
                SetIconToRunning();
                return;
            }

            List<ChampionshipDto> perfectlyMatchingChampionships = new();
            List<ChampionshipDto> matchingChampionships = new();

            foreach (ChampionshipDto championship in _championshipsPool.GetAllChampionshipDtos().Where(x => x.ChampionshipState != ChampionshipState.Finished))
            {
                RequirementResultKind evaluationResult = _championshipEligibilityEvaluator.EvaluateChampionship(championship, dataSet);
                switch (evaluationResult)
                {
                    case RequirementResultKind.DoesNotMatch:
                        continue;
                    case RequirementResultKind.PerfectMatch:
                        perfectlyMatchingChampionships.Add(championship);
                        matchingChampionships.Add(championship);
                        break;
                    case RequirementResultKind.CanMatch:
                        matchingChampionships.Add(championship);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (perfectlyMatchingChampionships.Count == 1)
            {
                StartNextEvent(perfectlyMatchingChampionships[0]);
                return;
            }

            _championshipCandidates = matchingChampionships;
            if (_championshipCandidates.Count == 0)
            {
                SetChampionshipIconToNone();
                return;
            }

            logger.Info("Icon set to pending");
            ChampionshipIconStateViewModel.ChampionshipIconState = ChampionshipIconState.PotentialChampionship;
            ChampionshipIconStateViewModel.TooltipText = "Opens championships eligible for current session";
        }

        private void SetIconToRunning()
        {
            logger.Info("Icon set to running");
            ChampionshipIconStateViewModel.ChampionshipIconState = ChampionshipIconState.ChampionshipInProgress;
            ChampionshipIconStateViewModel.TooltipText = "Opens the overview of the currently running championship";
            _nextEventToolbarController.RefreshToolbar();
        }

        private void OpenCandidatesSelector()
        {
            _championshipSelectionController.ShowOrFocusSelectionDialog(_championshipCandidates);
        }
    }
}