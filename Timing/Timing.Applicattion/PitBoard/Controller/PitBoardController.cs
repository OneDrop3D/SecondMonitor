namespace SecondMonitor.Timing.Application.PitBoard.Controller
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.SessionTiming;
    using Common.SessionTiming.Drivers;
    using Common.SessionTiming.Drivers.Lap;
    using DataModel.Snapshot;
    using DataProviders;
    using ViewModel;
    using ViewModels;
    using ViewModels.Factory;
    using ViewModels.PitBoard;
    using ViewModels.PitBoard.Controller;
    using ViewModels.SessionEvents;

    public class PitBoardController : IPitBoardController
    {
        private readonly List<IAutonomousPitBoardDataProvider> _pitBoardDataProviders;
        private readonly TimingApplicationViewModel _timingApplicationViewModel;
        private readonly ILapEventProvider _lapEventProvider;
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly List<PitBoardEntry> _pitBoards;
        private Timer _pitBoardTimer;
        private Timer _pitProvidersDataTimer;

        public PitBoardController(IViewModelFactory viewModelFactory, List<IAutonomousPitBoardDataProvider> pitBoardDataProviders, TimingApplicationViewModel timingApplicationViewModel, ILapEventProvider lapEventProvider, ISessionEventProvider sessionEventProvider)
        {
            _pitBoardDataProviders = pitBoardDataProviders;
            _timingApplicationViewModel = timingApplicationViewModel;
            _lapEventProvider = lapEventProvider;
            _sessionEventProvider = sessionEventProvider;
            PitBoardViewModel = viewModelFactory.Create<PitBoardViewModel>();
            timingApplicationViewModel.PitBoardViewModel = PitBoardViewModel;
            _pitBoards = new List<PitBoardEntry>();
        }

        public PitBoardViewModel PitBoardViewModel { get; }

        public Task StartControllerAsync()
        {
            _lapEventProvider.LapCompleted += LapEventProviderOnLapCompleted;
            _pitBoardDataProviders.ForEach(x => x.StartDataProvider(this));
            _pitBoardTimer = new Timer((_) => RefreshPitBoard(), null, 50, 50);
            _pitProvidersDataTimer = new Timer((_) => OnPitBoardProvidersDataRefresh(), null, 2000, 1000);
            return Task.CompletedTask;
        }

        public Task StopControllerAsync()
        {
            _lapEventProvider.LapCompleted -= LapEventProviderOnLapCompleted;
            _pitBoardTimer?.Dispose();
            _pitProvidersDataTimer?.Dispose();
            return Task.CompletedTask;
        }

        private void LapEventProviderOnLapCompleted(object sender, LapEventArgs e)
        {
            if (!e.Lap.Driver.IsPlayer)
            {
                return;
            }

            var drivers = _timingApplicationViewModel.SessionTiming.Drivers.Values.ToList();
            var dateSet = _sessionEventProvider.LastDataSet;
            _pitBoardDataProviders.ForEach(x => x.OnPlayerCompletedLap(dateSet, drivers));
        }

        public void OnPlayerCompletedLap(SimulatorDataSet dataSet, IReadOnlyCollection<DriverTiming> driverTimingsModels)
        {
            _pitBoardDataProviders.ForEach(x => x.OnPlayerCompletedLap(dataSet, driverTimingsModels));
        }

        public void Reset()
        {
            _pitBoardDataProviders.ForEach(x => x.Reset());
        }

        public void RequestToShowPitBoard(IViewModel viewModel, int priority)
        {
            RequestToShowPitBoard(viewModel, priority, () => true);
        }

        public void RequestToShowPitBoard(IViewModel viewModel, int priority, Func<bool> keepVisibleFunc)
        {
            if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => RequestToShowPitBoard(viewModel, priority, keepVisibleFunc));
                return;
            }

            if (_pitBoards.Any(x => x.ViewModel == viewModel))
            {
                return;
            }

            PitBoardEntry newPitBoard = new PitBoardEntry(viewModel, keepVisibleFunc);
            _pitBoards.Add(newPitBoard);
            PitBoardViewModel.AddPitBoard(newPitBoard.ViewModel);
            RefreshPitBoard();
        }

        public void RequestToShowPitBoard(IViewModel viewModel, int priority, TimeSpan displayTime)
        {
            Stopwatch sw = Stopwatch.StartNew();
            RequestToShowPitBoard(viewModel, priority, () => sw.Elapsed < displayTime);
        }

        public void HidePitBoard(IViewModel viewModel)
        {
            if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => HidePitBoard(viewModel));
                return;
            }

            PitBoardViewModel.RemovePitBoard(viewModel);
            _pitBoards.RemoveAll(x => x.ViewModel == viewModel);
        }

        private void RefreshPitBoard()
        {
            if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                System.Windows.Application.Current.Dispatcher.Invoke(RefreshPitBoard);
                return;
            }

            if (_pitBoards.Count == 0)
            {
                return;
            }

            _pitBoards.Where(x => !x.KeepVisibleFunc()).ToList().ForEach(x =>
            {
                PitBoardViewModel.RemovePitBoard(x.ViewModel);
                _pitBoards.Remove(x);
            });
        }

        private void OnPitBoardProvidersDataRefresh()
        {
            SimulatorDataSet lastDataSet = _sessionEventProvider.LastDataSet;
            if (lastDataSet == null)
            {
                return;
            }

            _pitBoardDataProviders.ForEach(x => x.OnNextData(lastDataSet));
        }

        private class PitBoardEntry
        {
            public PitBoardEntry(IViewModel viewModel, Func<bool> keepVisibleFunc)
            {
                ViewModel = viewModel;
                KeepVisibleFunc = keepVisibleFunc;
            }

            public IViewModel ViewModel { get; }
            public Func<bool> KeepVisibleFunc { get; }
        }
    }
}