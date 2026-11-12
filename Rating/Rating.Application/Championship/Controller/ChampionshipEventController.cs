namespace SecondMonitor.Rating.Application.Championship.Controller
{
    using System;
    using System.Threading.Tasks;

    using Common.DataModel.Championship;
    using Common.DataModel.Championship.Events;

    using Contracts.Commands;

    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;

    using Filters;

    using NLog;

    using Operations;

    using SecondMonitor.ViewModels.Controllers;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.PitBoard.Controller;
    using SecondMonitor.ViewModels.SessionEvents;

    using Timing.Common.SessionTiming;
    using Timing.Common.SessionTiming.Drivers.Lap;

    using ViewModels;

    public class ChampionshipEventController : AbstractChildController<IChampionshipController>, IChampionshipEventController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IChampionshipManipulator _championshipManipulator;
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly IChampionshipEligibilityEvaluator _championshipEligibilityEvaluator;
        private readonly IChampionshipDialogProvider _championshipDialogProvider;
        private readonly IPitBoardController _pitBoardController;
        private readonly ILapEventProvider _lapEventProvider;
        private readonly IChampionshipDriverPresentationProvider _championshipDriverPresentationProvider;
        private readonly OpenLastResultPitBoardViewModel _openLastResultPitBoardViewModel;
        private SessionDto _currentSession;
        private string _lastTrack;
        private bool _isSessionRace;
        private bool _hasPlayerFinished;

        private ILapInfo _bestLap;

        public ChampionshipEventController(IViewModelFactory viewModelFactory, IChampionshipManipulator championshipManipulator, ISessionEventProvider sessionEventProvider,
            IChampionshipEligibilityEvaluator championshipEligibilityEvaluator,
            IChampionshipDialogProvider championshipDialogProvider, IPitBoardController pitBoardController, ILapEventProvider lapEventProvider, IChampionshipDriverPresentationProvider championshipDriverPresentationProvider) : base(viewModelFactory)
        {
            _championshipManipulator = championshipManipulator;
            _sessionEventProvider = sessionEventProvider;
            _championshipEligibilityEvaluator = championshipEligibilityEvaluator;
            _championshipDialogProvider = championshipDialogProvider;
            _pitBoardController = pitBoardController;
            _lapEventProvider = lapEventProvider;
            _championshipDriverPresentationProvider = championshipDriverPresentationProvider;
            _openLastResultPitBoardViewModel = new OpenLastResultPitBoardViewModel
            {
                OpenCommand = new RelayCommand(OpenLastEventResults)
            };
        }

        public bool IsChampionshipActive { get; private set; }

        public ChampionshipDto CurrentChampionship { get; private set; }

        public void StartNextEvent(ChampionshipDto championship)
        {
            logger.Info($"Starting new Event {championship.ChampionshipName}");
            CurrentChampionship = championship;
            if (CurrentChampionship.ChampionshipState == ChampionshipState.NotStarted)
            {
                _championshipManipulator.StartChampionship(championship, _sessionEventProvider.LastDataSet);
            }
            else
            {
                _championshipManipulator.UpdateDriversProperties(championship, _sessionEventProvider.LastDataSet);
            }

            _championshipManipulator.StartNextEvent(championship, _sessionEventProvider.LastDataSet);
            InitializePropertiesOnSessionStart(_sessionEventProvider.LastDataSet);

            IsChampionshipActive = true;
            _championshipDriverPresentationProvider.UpdateDriversOutLines(championship);
            ShowWelcomeScreen(_sessionEventProvider.LastDataSet, championship);
        }

        private static bool ShouldShowWelcomeScreen(ChampionshipDto championship)
        {
            SessionDto lastResults = championship.GetLastSessionWithResults().SessionDto;
            return lastResults == null || (DateTime.UtcNow - lastResults.SessionResult.ResultCreationTime).TotalMinutes > 30;
        }

        public bool TryResumePreviousChampionship(SimulatorDataSet dataSet)
        {
            IsChampionshipActive = dataSet.PlayerInfo.FinishStatus != DriverFinishStatus.Finished && CurrentChampionship != null &&
                                   _championshipEligibilityEvaluator.EvaluateChampionship(CurrentChampionship, dataSet) != RequirementResultKind.DoesNotMatch;
            logger.Info($"TryResumePreviousChampionship result is {IsChampionshipActive}");

            if (IsChampionshipActive)
            {
                InitializePropertiesOnSessionStart(_sessionEventProvider.LastDataSet);
                _championshipManipulator.UpdateDriversProperties(CurrentChampionship, dataSet);
                _championshipDriverPresentationProvider.UpdateDriversOutLines(CurrentChampionship);
            }

            if (IsChampionshipActive && dataSet.SessionInfo.TrackInfo.TrackFullName != _lastTrack)
            {
                ShowWelcomeScreen(dataSet, CurrentChampionship);
            }

            return IsChampionshipActive;
        }

        public override Task StartControllerAsync()
        {
            _sessionEventProvider.PlayerFinishStateChanged += SessionEventProviderOnPlayerFinishStateChanged;
            _sessionEventProvider.SessionTypeChange += SessionEventProviderOnSessionTypeChange;
            _sessionEventProvider.DriversAdded += SessionEventProviderOnDriversChanged;
            _sessionEventProvider.DriversRemoved += SessionEventProviderOnDriversRemoved;
            _lapEventProvider.BestClassLapChanged += LapEventProviderOnClassLapChanged;
            _lapEventProvider.BestLapChanged += LapEventProviderOnBestLapChanged;
            _sessionEventProvider.OnNewDataSet += SessionEventProviderOnOnNewDataSet;
            _sessionEventProvider.PlayerLeftSession += SessionEventProviderOnPlayerLeftSession;
            return Task.CompletedTask;
        }

        private void SessionEventProviderOnOnNewDataSet(object sender, DataSetArgs e)
        {
            FillActualSessionLength(e.DataSet);
        }

        private void FillActualSessionLength(SimulatorDataSet dataSet)
        {
            if (!_isSessionRace || dataSet.SessionInfo.SessionLengthType == SessionLengthType.Na)
            {
                return;
            }

            SessionInfo sessionInfo = dataSet.SessionInfo;
            double currentSessionLength = sessionInfo.SessionLengthType == SessionLengthType.Laps ? sessionInfo.TotalNumberOfLaps : Math.Ceiling(sessionInfo.SessionTimeRemaining);

            if (!_currentSession.IsSessionLengthFilled || _currentSession.ActualLengthType != sessionInfo.SessionLengthType ||
                currentSessionLength > _currentSession.ActualSessionLength)
            {
                _currentSession.FillSessionLength(sessionInfo.SessionLengthType, currentSessionLength);
            }
        }

        public override Task StopControllerAsync()
        {
            _sessionEventProvider.PlayerFinishStateChanged -= SessionEventProviderOnPlayerFinishStateChanged;
            _sessionEventProvider.SessionTypeChange -= SessionEventProviderOnSessionTypeChange;
            _sessionEventProvider.DriversAdded -= SessionEventProviderOnDriversChanged;
            _sessionEventProvider.DriversRemoved -= SessionEventProviderOnDriversRemoved;
            _lapEventProvider.BestClassLapChanged -= LapEventProviderOnClassLapChanged;
            _lapEventProvider.BestLapChanged -= LapEventProviderOnBestLapChanged;
            _sessionEventProvider.PlayerLeftSession -= SessionEventProviderOnPlayerLeftSession;
            return Task.CompletedTask;
        }

        private void SessionEventProviderOnDriversRemoved(object sender, DriversArgs e)
        {
            SessionEventProviderOnDriversChanged(sender, e);
        }

        private void SessionEventProviderOnDriversChanged(object sender, DriversArgs e)
        {
            SimulatorDataSet dataSet = e.PreviousDataSet;
            if (!IsChampionshipActive || dataSet == null)
            {
                return;
            }

            //Drivers count changed, but not for driver car
            if (dataSet.GetDriversInfoByMultiClass().Count == CurrentChampionship.TotalDrivers)
            {
                return;
            }

            logger.Info("Drivers changed - ending championship");

            if (dataSet.SessionInfo.SessionType == SessionType.Race)
            {
                logger.Info("Drivers chagned in race");
                if (dataSet.GetDriversInfoByMultiClass().Count != CurrentChampionship.TotalDrivers)
                {
                    logger.Warn(
                        $"Drivers count missmatch, unable to update. In DataSet {dataSet.GetDriversInfoByMultiClass().Count}, in Championship: {CurrentChampionship.TotalDrivers}");
                    return;
                }

                double completionPercentage = GetCompletionPercentage(dataSet);

                if (!double.IsNaN(completionPercentage) && !double.IsInfinity(completionPercentage) && completionPercentage != 0)
                {
                    _championshipManipulator.AddResultsForCurrentSession(CurrentChampionship, dataSet, _bestLap, completionPercentage);
                    CurrentChampionship.ChampionshipState = ChampionshipState.LastSessionCanceled;
                }

                //Finish with actual dataset - so the actual dataset is used for championship reeavluation
                FinishCurrentEvent();
            }
            else
            {
                FinishCurrentEvent();
            }
        }

        private void SessionEventProviderOnPlayerLeftSession(object sender, DataSetArgs e)
        {
            logger.Info("Player left session");
            SessionPropertiesChanged(e);
        }

        private void SessionEventProviderOnSessionTypeChange(object sender, DataSetArgs e)
        {
            logger.Info("Session Type Changed");
            SessionPropertiesChanged(e);
        }

        private void SessionPropertiesChanged(DataSetArgs e)
        {
            if (_openLastResultPitBoardViewModel.OriginalModel != null && !_openLastResultPitBoardViewModel.AlreadyOpened)
            {
                OpenLastEventResults();
            }

            if (!IsChampionshipActive || _sessionEventProvider.BeforeLastDataSet == null)
            {
                return;
            }

            if (_sessionEventProvider.BeforeLastDataSet.SessionInfo.SessionType == SessionType.Race)
            {
                logger.Info("Session Properties Chagned, and is in race");
                double completionPercentage = GetCompletionPercentage(_sessionEventProvider.BeforeLastDataSet);

                if (!double.IsNaN(completionPercentage) && !double.IsInfinity(completionPercentage) && completionPercentage != 0)
                {
                    _championshipManipulator.AddResultsForCurrentSession(CurrentChampionship, _sessionEventProvider.BeforeLastDataSet, _bestLap, completionPercentage);
                    CurrentChampionship.ChampionshipState = ChampionshipState.LastSessionCanceled;
                }

                FinishCurrentEvent();
                return;
            }

            if (e.DataSet.SessionInfo.SessionType != SessionType.Na &&
                _championshipEligibilityEvaluator.EvaluateChampionship(CurrentChampionship, e.DataSet) == RequirementResultKind.DoesNotMatch)
            {
                logger.Info("Session properties changed, and current championship is not elligible");
                FinishCurrentEvent();
                return;
            }

            InitializePropertiesOnSessionStart(e.DataSet);
        }

        private void SessionEventProviderOnPlayerFinishStateChanged(object sender, DataSetArgs e)
        {
            if (!IsChampionshipActive)
            {
                return;
            }

            if (e.DataSet.PlayerInfo.FinishStatus is DriverFinishStatus.Finished && _sessionEventProvider.BeforeLastDataSet?.SessionInfo?.SessionType == SessionType.Race &&
                _isSessionRace && !_hasPlayerFinished)
            {
                logger.Info("Player finished in race");
                _hasPlayerFinished = true;
                _championshipManipulator.AddResultsForCurrentSession(CurrentChampionship, e.DataSet, _bestLap, 1);
                _championshipManipulator.CommitLastSessionResultsAndAdvance(CurrentChampionship);
                _openLastResultPitBoardViewModel.FromModel(CurrentChampionship);
                _pitBoardController.RequestToShowPitBoard(_openLastResultPitBoardViewModel, 10);
                FinishCurrentEvent();
                CurrentChampionship = null;
            }
            else if (e.DataSet.PlayerInfo.FinishStatus is DriverFinishStatus.Dnq or DriverFinishStatus.Dnf or DriverFinishStatus.Dq)
            {
                logger.Info("Player DQ, and is in race");
                double completionPercentage = GetCompletionPercentage(_sessionEventProvider.BeforeLastDataSet);
                _championshipManipulator.AddResultsForCurrentSession(CurrentChampionship, _sessionEventProvider.BeforeLastDataSet, _bestLap, completionPercentage);
                CurrentChampionship.ChampionshipState = ChampionshipState.LastSessionCanceled;
                FinishCurrentEvent();
            }

            _hasPlayerFinished = e.DataSet.PlayerInfo.FinishStatus == DriverFinishStatus.Finished;
        }

        private void OpenLastEventResults()
        {
            _pitBoardController.HidePitBoard(_openLastResultPitBoardViewModel);
            if (_openLastResultPitBoardViewModel.OriginalModel == null)
            {
                return;
            }

            _openLastResultPitBoardViewModel.AlreadyOpened = true;
            _championshipDialogProvider.ShowLastEvenResultWindow(_openLastResultPitBoardViewModel.OriginalModel);
        }

        private void FinishCurrentEvent()
        {
            logger.Info($"Finishing event for {CurrentChampionship.ChampionshipName}");
            IsChampionshipActive = false;
            ParentController.EventFinished(CurrentChampionship);
        }

        private void ShowWelcomeScreen(SimulatorDataSet dataSet, ChampionshipDto championshipDto)
        {
            _lastTrack = dataSet.SessionInfo.TrackInfo.TrackFullName;
            if (ShouldShowWelcomeScreen(championshipDto))
            {
                _championshipDialogProvider.ShowWelcomeScreen(dataSet, CurrentChampionship);
            }
        }

        private void InitializePropertiesOnSessionStart(SimulatorDataSet dataSet)
        {
            _isSessionRace = dataSet.SessionInfo.SessionType == SessionType.Race;
            _hasPlayerFinished = dataSet.PlayerInfo?.FinishStatus == DriverFinishStatus.Finished;
            var currentEvent = CurrentChampionship.GetCurrentOrLastEvent();
            _currentSession = currentEvent.Sessions[CurrentChampionship.CurrentSessionIndex];
            FillActualSessionLength(dataSet);
        }

        private void LapEventProviderOnBestLapChanged(object sender, BestLapChangedArgs e)
        {
            if (_sessionEventProvider.LastDataSet.SessionInfo.IsMultiClass)
            {
                return;
            }

            _bestLap = e.NewLap;
        }

        private void LapEventProviderOnClassLapChanged(object sender, BestLapChangedArgs e)
        {
            SimulatorDataSet dataSet = _sessionEventProvider.LastDataSet;
            if (dataSet?.SessionInfo.IsMultiClass == false || e.NewLap == null || e.NewLap.Driver.CarClassId != _sessionEventProvider.LastDataSet.PlayerInfo.CarClassId)
            {
                return;
            }

            _bestLap = e.NewLap;
        }

        private double GetCompletionPercentage(SimulatorDataSet dataSet)
        {
            return dataSet.SessionInfo.SessionLengthType != SessionLengthType.Laps
                ? Math.Min(1, (1 - (dataSet.SessionInfo.SessionTimeRemaining / _currentSession.ActualSessionLength)))
                : Math.Min(1, ((double)(dataSet.LeaderInfo.CompletedLaps) / dataSet.SessionInfo.TotalNumberOfLaps));
        }
    }
}
