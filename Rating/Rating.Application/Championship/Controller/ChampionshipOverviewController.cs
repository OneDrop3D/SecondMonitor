namespace SecondMonitor.Rating.Application.Championship.Controller
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using Common.Championship;
    using Common.DataModel.Championship;
    using Common.DataModel.Championship.Calendar;

    using NLog;

    using Operations;
    using Pool;

    using SecondMonitor.Contracts.Commands;
    using SecondMonitor.Rating.Application.Championship.ViewModels.Events;
    using SecondMonitor.Rating.Application.Championship.ViewModels.Overview.ModifyResults;
    using SecondMonitor.Rating.Common.DataModel.Championship.Events;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Controllers;
    using SecondMonitor.ViewModels.Factory;

    using ViewModels.Overview;
    using Application = System.Windows.Application;

    public class ChampionshipOverviewController : AbstractChildController<IChampionshipController>, IChampionshipOverviewController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly IChildControllerFactory _childControllerFactory;
        private readonly IChampionshipsPool _championshipsPool;
        private readonly IStoredCalendarsAccessor _storedCalendarsAccessor;
        private readonly IWindowService _windowService;
        private readonly IDialogService _dialogService;
        private readonly IChampionshipManipulator _championshipManipulator;
        private readonly IChampionshipDialogProvider _championshipDialogProvider;
        private readonly CalendarDtoFactory _calendarDtoFactory;
        private IChampionshipCreationController _championshipCreationController;
        private Window _overviewWindow;
        private ChampionshipsOverviewViewModel _championshipOverviewViewModel;

        public ChampionshipOverviewController(IViewModelFactory viewModelFactory, IChildControllerFactory childControllerFactory, IChampionshipsPool championshipsPool, IStoredCalendarsAccessor storedCalendarsAccessor,
            IWindowService windowService, IDialogService dialogService, IChampionshipManipulator championshipManipulator, IChampionshipDialogProvider championshipDialogProvider, CalendarDtoFactory calendarDtoFactory)
            : base(viewModelFactory)
        {
            _childControllerFactory = childControllerFactory;
            _championshipsPool = championshipsPool;
            _storedCalendarsAccessor = storedCalendarsAccessor;
            _windowService = windowService;
            _dialogService = dialogService;
            _championshipManipulator = championshipManipulator;
            _championshipDialogProvider = championshipDialogProvider;
            _calendarDtoFactory = calendarDtoFactory;
        }

        public override Task StartControllerAsync()
        {
            _championshipsPool.ChampionshipAdded += ChampionshipsPoolOnChampionshipAdded;
            _championshipsPool.ChampionshipRemoved += ChampionshipsPoolOnChampionshipRemoved;
            _championshipsPool.ChampionshipUpdated += ChampionshipsPoolOnChampionshipUpdated;

            Bind<ChampionshipsOverviewViewModel>().Command(x => x.CreateNewCommand).ToAsync(CreateNewChampionship);
            Bind<ChampionshipsOverviewViewModel>().Command(x => x.RemoveSelectedCommand).To(RemoveSelectedChampionship);
            Bind<NextRaceOverviewViewModel>().Command(x => x.DnfSessionCommand).To(DnfSelectedChampionshipSession);
            Bind<NextRaceOverviewViewModel>().Command(x => x.RedFlagSessionCommand).To(RedFlagSelectedChampionship);
            Bind<ChampionshipsOverviewViewModel>().Command(x => x.OpenSelectedCommand).To(OpenSelectedChampionship);
            Bind<ChampionshipsOverviewViewModel>().Command(x => x.ResetSelectedCommand).To(ResetSelectedChampionship);
            Bind<ChampionshipsOverviewViewModel>().Command(x => x.ReRollMysteryPropertiesCommand).To(ReRollSelectedChampionship);
            Bind<ChampionshipsOverviewViewModel>().Command(x => x.SaveSelectedChampionshipCalendarCommand).To(SaveSelectedChampionship);
            Bind<ChampionshipsOverviewViewModel>().Command(x => x.ModifyLastResultCommand).To(ModifyLastResult);
            return Task.CompletedTask;
        }

        public override Task StopControllerAsync()
        {
            _championshipsPool.ChampionshipAdded -= ChampionshipsPoolOnChampionshipAdded;
            _championshipsPool.ChampionshipRemoved -= ChampionshipsPoolOnChampionshipRemoved;
            _championshipsPool.ChampionshipUpdated -= ChampionshipsPoolOnChampionshipUpdated;
            return Task.CompletedTask;
        }

        public void OpenChampionshipOverviewWindow()
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(OpenChampionshipOverviewWindow);
                return;
            }

            if (_overviewWindow != null)
            {
                _overviewWindow.Focus();
                return;
            }

            _championshipOverviewViewModel = ViewModelFactory.Create<ChampionshipsOverviewViewModel>();
            _championshipOverviewViewModel.FromModel(_championshipsPool.GetAllChampionshipDtos());
            _overviewWindow = _windowService.OpenWindow(_championshipOverviewViewModel, "All Championships", WindowState.Normal, SizeToContent.WidthAndHeight, WindowStartupLocation.CenterOwner, _ => WindowClosed());
        }

        private void OpenSelectedChampionship()
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(OpenSelectedChampionship);
                return;
            }

            if (_championshipOverviewViewModel.SelectedChampionship == null)
            {
                return;
            }

            OpenChampionshipDetailsWindow(_championshipOverviewViewModel.SelectedChampionship.OriginalModel);
        }

        private void ReRollSelectedChampionship()
        {
            if (_championshipOverviewViewModel.SelectedChampionship == null)
            {
                return;
            }

            ChampionshipDto championshipDto = _championshipOverviewViewModel.SelectedChampionship.OriginalModel;
            _championshipManipulator.ReRollMysteryProperties(championshipDto);
            _championshipsPool.UpdateChampionship(championshipDto);
        }

        private void SaveSelectedChampionship()
        {
            if (_championshipOverviewViewModel.SelectedChampionship == null)
            {
                return;
            }

            ChampionshipOverviewViewModel selectedChampionship = _championshipOverviewViewModel.SelectedChampionship;
            CalendarDto calendarDto = _calendarDtoFactory.Create(selectedChampionship.OriginalModel);
            _storedCalendarsAccessor.TrySaveCalendar(calendarDto);
        }

        public void OpenChampionshipDetailsWindow(ChampionshipDto championship)
        {
            ChampionshipDetailViewModel detailViewModel = _championshipDialogProvider.OpenChampionshipDetailsWindow(championship);
            if (detailViewModel.ChampionshipSessionsResults.ResultsHistory.Count == 0)
            {
                return;
            }

            detailViewModel.ChampionshipSessionsResults.ResultsHistory
                .OfType<SessionResultWithTitleViewModel>()
                .Last().AmendResults = new RelayCommand(() => ModifyLastResult(championship, true, false));
        }

        private void RemoveSelectedChampionship()
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(RemoveSelectedChampionship);
                return;
            }

            if (_championshipOverviewViewModel.SelectedChampionship == null)
            {
                return;
            }

            if (!_dialogService.ShowYesNoDialog("Confirmation", "Remove Selected Championship?"))
            {
                return;
            }

            _championshipsPool.RemoveChampionship(_championshipOverviewViewModel.SelectedChampionship.OriginalModel);
        }

        private void ResetSelectedChampionship()
        {
            if (_championshipOverviewViewModel.SelectedChampionship == null)
            {
                return;
            }

            RestartChampionshipDialogViewModel dialogViewModel = ViewModelFactory.Create<RestartChampionshipDialogViewModel>();
            _windowService.OpenDialog(dialogViewModel, "Reset Championship", WindowState.Normal, SizeToContent.WidthAndHeight, WindowStartupLocation.CenterScreen, _overviewWindow);

            if (dialogViewModel.ResetChampionshipDialogResult == RestartChampionshipDialogViewModel.DialogResultKind.Cancel)
            {
                return;
            }

            ChampionshipDto originalChampionship = _championshipOverviewViewModel.SelectedChampionship.OriginalModel;
            _championshipManipulator.ResetChampionship(originalChampionship, dialogViewModel.ResetChampionshipDialogResult == RestartChampionshipDialogViewModel.DialogResultKind.Shuffle);
            _championshipsPool.UpdateChampionship(originalChampionship);
        }

        private void ChampionshipsPoolOnChampionshipRemoved(object sender, ChampionshipEventArgs e)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => ChampionshipsPoolOnChampionshipRemoved(sender, e));
                return;
            }

            _championshipOverviewViewModel.RemoveChampionship(e.ChampionshipDto);
        }

        private void ChampionshipsPoolOnChampionshipAdded(object sender, ChampionshipEventArgs e)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => ChampionshipsPoolOnChampionshipAdded(sender, e));
                return;
            }

            _championshipOverviewViewModel?.InsertChampionshipFirst(e.ChampionshipDto);
        }

        private void ChampionshipsPoolOnChampionshipUpdated(object sender, ChampionshipEventArgs e)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => ChampionshipsPoolOnChampionshipUpdated(sender, e));
                return;
            }

            if (_championshipOverviewViewModel == null)
            {
                return;
            }

            Guid selectedGuid = _championshipOverviewViewModel.SelectedChampionship?.OriginalModel.ChampionshipGlobalId ?? Guid.Empty;

            _championshipOverviewViewModel?.RemoveChampionship(e.ChampionshipDto);
            _championshipOverviewViewModel?.InsertChampionshipFirst(e.ChampionshipDto);
            if (selectedGuid == e.ChampionshipDto.ChampionshipGlobalId)
            {
                _championshipOverviewViewModel.SelectedChampionship = _championshipOverviewViewModel.AllChampionships.FirstOrDefault(x => x.OriginalModel.ChampionshipGlobalId == selectedGuid);
            }
        }

        private void DnfSelectedChampionshipSession()
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(DnfSelectedChampionshipSession);
                return;
            }

            if (_championshipOverviewViewModel.SelectedChampionship == null)
            {
                return;
            }

            ChampionshipDto selectedChampionship = _championshipOverviewViewModel.SelectedChampionship.OriginalModel;

            _championshipManipulator.DnfPlayerAndCommitLastSessionResults(selectedChampionship);
            _championshipsPool.UpdateChampionship(selectedChampionship);

            ShowLastEvenResultWindow(selectedChampionship);
        }

        private void RedFlagSelectedChampionship()
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(DnfSelectedChampionshipSession);
                return;
            }

            if (_championshipOverviewViewModel.SelectedChampionship == null)
            {
                return;
            }

            ChampionshipDto selectedChampionship = _championshipOverviewViewModel.SelectedChampionship.OriginalModel;

            _championshipManipulator.CommitLastSessionResultsAndAdvance(selectedChampionship);
            _championshipsPool.UpdateChampionship(selectedChampionship);

            ShowLastEvenResultWindow(selectedChampionship);
        }

        private void ShowLastEvenResultWindow(ChampionshipDto championship)
        {
            _championshipDialogProvider.ShowLastEvenResultWindow(championship);
        }

        private async Task CreateNewChampionship()
        {
            if (_championshipCreationController != null)
            {
                _championshipCreationController.TryFocusCreationWindow();
                return;
            }

            _championshipCreationController = _childControllerFactory.Create<IChampionshipCreationController, IChampionshipOverviewController>(this);
            await _championshipCreationController.StartControllerAsync();
            _championshipCreationController.OpenChampionshipCreationDialog(NewChampionshipCreated, ChampionshipCreationCancelled);
        }

        private void ChampionshipCreationCancelled()
        {
            _championshipCreationController.StopControllerAsync();
            _championshipCreationController = null;
        }

        private void NewChampionshipCreated(ChampionshipDto newChampionshipDto)
        {
            _championshipsPool.AddNewChampionship(newChampionshipDto);
            _championshipCreationController.StopControllerAsync();
            _championshipCreationController = null;
        }

        private void ModifyLastResult()
        {
            ChampionshipDto championshipDto = _championshipOverviewViewModel.SelectedChampionship.OriginalModel;
            ModifyLastResult(championshipDto, false, true);
        }

        private void ModifyLastResult(ChampionshipDto championshipDto, bool openDetailsWindow, bool openResultWindow)
        {
            _championshipDialogProvider.CloseChampionshipDetailsWindow(championshipDto);
            (EventDto EventDto, SessionDto SessionDto) lastSessionWithResults = championshipDto.GetLastSessionWithResults();
            ScoringDto scoring = championshipDto.Scoring[lastSessionWithResults.EventDto.Sessions.IndexOf(lastSessionWithResults.SessionDto)];

            ModifyResultsWindowModel modifyResultsWindowModel =
                new ModifyResultsWindowModel(championshipDto, lastSessionWithResults.EventDto, lastSessionWithResults.SessionDto, scoring);
            ModifyResultsWindowViewModel modifyResultsWindowViewModel = ViewModelFactory.Create<ModifyResultsWindowViewModel>();
            modifyResultsWindowViewModel.FromModel(modifyResultsWindowModel);
            Window window = _windowService.OpenWindow(modifyResultsWindowViewModel, "Modify Results", WindowState.Normal, SizeToContent.WidthAndHeight, WindowStartupLocation.CenterOwner,
                openDetailsWindow ? _ => OpenChampionshipDetailsWindow(championshipDto) : _ => { });

            modifyResultsWindowViewModel.OkCommand = new RelayCommand(() => ConfirmResultModification(modifyResultsWindowViewModel, window, openResultWindow));
            modifyResultsWindowViewModel.CancelCommand = new RelayCommand(() => window.Close());
        }

        private void ConfirmResultModification(ModifyResultsWindowViewModel modifyResultsWindowModel, Window window, bool openResultWindow)
        {
            ChampionshipDto championship = modifyResultsWindowModel.OriginalModel.Championship;
            AmendedSessionResult amendedResult = modifyResultsWindowModel.CreateAmendedSessionResult();
            _championshipManipulator.AmendLastResult(championship, amendedResult);
            if (openResultWindow)
            {
                _championshipDialogProvider.ShowLastEvenResultWindow(championship);
            }
            
            _championshipsPool.UpdateChampionship(championship);

            window.Close();
        }

        private async void WindowClosed()
        {
            try
            {
                _championshipOverviewViewModel.CreateNewCommand = null;
                _championshipOverviewViewModel.OpenSelectedCommand = null;
                _championshipOverviewViewModel.DeleteSelectedCommand = null;

                _championshipOverviewViewModel = null;

                if (_championshipCreationController != null)
                {
                    await _championshipCreationController.StopControllerAsync();
                }

                _overviewWindow = null;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error While Closing window");
            }
        }
    }
}