namespace SecondMonitor.ViewModels.FuelConsumption
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Summary.FuelStrategy;
    using SecondMonitor.ViewModels.CarStatus.FuelStatus;
    using SecondMonitor.ViewModels.FuelConsumption.FuelStrategy;
    using SecondMonitor.ViewModels.SessionEvents;

    public class FuelStrategyController : IFuelStrategyController
    {
        private readonly IFuelOverviewViewModel _fuelOverviewViewModel;
        private readonly FuelStrategiesViewModel _fuelStrategiesViewModel;
        private readonly IList<IFuelStrategy> _fuelStrategies;
        private readonly FuelStrategiesRepository _fuelStrategiesRepository;
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly Stopwatch _fuelStateUpdateStopwatch;
        private FuelStrategies _storedFuelStrategiesData;
        private ClassFuelStrategy _currentStrategyData;

        public FuelStrategyController(IFuelOverviewViewModel fuelOverviewViewModel, IList<IFuelStrategy> fuelStrategies,
            FuelStrategiesRepository fuelStrategiesRepository, ISessionEventProvider sessionEventProvider)
        {
            _fuelOverviewViewModel = fuelOverviewViewModel;
            _fuelStrategiesViewModel = _fuelOverviewViewModel.FuelStrategiesViewModel;
            _fuelStrategies = fuelStrategies.OrderByDescending(x => x.Priority).ToList();
            _fuelStrategiesRepository = fuelStrategiesRepository;
            _sessionEventProvider = sessionEventProvider;
            _fuelStateUpdateStopwatch = Stopwatch.StartNew();
            _fuelStrategiesViewModel.SetSelectableStrategies(_fuelStrategies);
        }

        public Task StartControllerAsync()
        {
            _storedFuelStrategiesData = _fuelStrategiesRepository.LoadOrCreateNew();
            _fuelOverviewViewModel.FuelPlannerViewModel.CalculatorForSelectedSession.PropertyChanged += CalculatedPreSessionFuelChanged;
            _fuelOverviewViewModel.FuelPlannerViewModel.PropertyChanged += FuelPlannerViewModelOnPropertyChanged;
            _fuelStrategies.ForEach(x => x.UpdateConsumptionStatistics(_fuelOverviewViewModel.FuelPlannerViewModel?.SelectedSession?.OriginalModel));
            _sessionEventProvider.PlayerPropertiesChanged += RefreshFuelStrategiesOnChanges;
            _sessionEventProvider.SessionTypeChange += RefreshFuelStrategiesOnChanges;
            _fuelStrategiesViewModel.PropertyChanged += SelectedStrategyChanged;

            return Task.CompletedTask;
        }

        public Task StopControllerAsync()
        {
            _fuelStrategiesRepository.Save(_storedFuelStrategiesData);
            _fuelOverviewViewModel.FuelPlannerViewModel.CalculatorForSelectedSession.PropertyChanged -= CalculatedPreSessionFuelChanged;
            _fuelOverviewViewModel.FuelPlannerViewModel.PropertyChanged -= FuelPlannerViewModelOnPropertyChanged;
            _sessionEventProvider.PlayerPropertiesChanged -= RefreshFuelStrategiesOnChanges;
            _sessionEventProvider.SessionTypeChange -= RefreshFuelStrategiesOnChanges;
            _fuelStrategiesViewModel.PropertyChanged -= SelectedStrategyChanged;
            return Task.CompletedTask;
        }

        public FuelConsumptionController ParentController { get; set; }

        public void ApplyDataSet(SimulatorDataSet dataSet)
        {
            UpdateCurrentFuelState(dataSet);
        }

        private void UpdateCurrentFuelState(SimulatorDataSet dataSet)
        {
            if (_fuelStateUpdateStopwatch.Elapsed.TotalSeconds < 1)
            {
                return;
            }

            _fuelStateUpdateStopwatch.Restart();

            UpdatePitStopEstimations(dataSet);
            UpdateNextPitStopInfo(dataSet);
        }

        private void UpdatePitStopEstimations(SimulatorDataSet dataSet)
        {
            if (ParentController == null)
            {
                return;
            }

            if ((dataSet.SessionInfo.SessionType != SessionType.Race || _fuelOverviewViewModel.FuelDelta?.InLiters >= 0 || _fuelOverviewViewModel.TimeDelta.TotalSeconds > 0 ||
                _fuelOverviewViewModel.LapsDelta > 0) && _fuelOverviewViewModel.PitStopsViewModel != null)
            {
                _fuelOverviewViewModel.PitStopsViewModel.IsFuelToAddVisible = false;
                _fuelOverviewViewModel.PitStopsViewModel.IsPitStopCountVisible = false;
                _fuelOverviewViewModel.PitStopsViewModel.RefuelingWindowState = RefuelingWindowState.Closed;
                return;
            }

            _fuelStrategiesViewModel?.SelectedStrategy?.Value?.UpdateInRaceFuelPrediction(dataSet, _fuelOverviewViewModel, ParentController.ConsumptionMonitors);
        }

        private void UpdateNextPitStopInfo(SimulatorDataSet dataSet)
        {
            if (ParentController == null)
            {
                return;
            }

            _fuelStrategiesViewModel?.SelectedStrategy?.Value?.UpdateNextPitStopInfo(dataSet, _fuelOverviewViewModel, ParentController.ConsumptionMonitors);
        }

        private void CalculatedPreSessionFuelChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(FuelCalculatorViewModel.RequiredFuel))
            {
                return;
            }

            _fuelStrategies.ForEach(x =>
            {
                x.FuelMultiplier = _fuelOverviewViewModel.FuelMultiplier;
                x.UpdateRequiredFuel(_fuelOverviewViewModel.FuelPlannerViewModel.CalculatorForSelectedSession.RequiredFuel);
            });
        }

        private void FuelPlannerViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FuelPlannerViewModel.IsVisible))
            {
                if (_fuelOverviewViewModel.FuelPlannerViewModel.IsVisible)
                {
                    OnFuelCalculatorOpened();
                }
                else
                {
                    OnFuelCalculatorClosed();
                }

                return;
            }

            if (e.PropertyName != nameof(FuelPlannerViewModel.SelectedSession) || _fuelOverviewViewModel.FuelPlannerViewModel.SelectedSession == null)
            {
                return;
            }

            _fuelStrategies.ForEach(x => x.UpdateConsumptionStatistics(_fuelOverviewViewModel.FuelPlannerViewModel.SelectedSession.OriginalModel));
        }

        private void OnFuelCalculatorClosed()
        {
            SimulatorDataSet lastDataSet = _sessionEventProvider.LastDataSetWithDrivers;
            if (lastDataSet?.PlayerInfo == null || string.IsNullOrEmpty(lastDataSet.PlayerInfo.CarClassId))
            {
                return;
            }

            SaveCurrentStrategy();
            _currentStrategyData = null;
        }

        private void OnFuelCalculatorOpened()
        {
            SimulatorDataSet lastDataSet = _sessionEventProvider.LastDataSetWithDrivers;
            if (lastDataSet?.PlayerInfo == null || string.IsNullOrEmpty(lastDataSet.PlayerInfo.CarClassId))
            {
                return;
            }

            RefreshFuelStrategies(lastDataSet);
        }

        private void SaveCurrentStrategy()
        {
            if (_currentStrategyData == null)
            {
                return;
            }

            _fuelStrategies.ForEach(x => x.SaveToSettings(_currentStrategyData));
            _currentStrategyData.LastSelectedStrategy = _fuelStrategiesViewModel.SelectedStrategy.Value.StrategyName;
            _currentStrategyData.FuelMultiplier = _fuelOverviewViewModel.FuelMultiplier;
            _storedFuelStrategiesData.UpdateFuelStrategy(_currentStrategyData);
        }

        private void RefreshFuelStrategiesOnChanges(object sender, DataSetArgs e)
        {
            RefreshFuelStrategies(e.DataSet.DriversInfo.Length > 0 ? e.DataSet : _sessionEventProvider.LastDataSetWithDrivers);
        }

        private void RefreshFuelStrategies(SimulatorDataSet lastDataSet)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => RefreshFuelStrategies(lastDataSet));
                return;
            }

            if (lastDataSet == null)
            {
                return;
            }

            SaveCurrentStrategy();

            _currentStrategyData = _storedFuelStrategiesData.GetOrCreateFuelStrategy(lastDataSet.Source, lastDataSet.PlayerInfo.CarClassId);
            _fuelStrategies.ForEach(x => x.LoadSettings(_currentStrategyData));
            _fuelOverviewViewModel.FuelMultiplier = _currentStrategyData.FuelMultiplier;

            _fuelStrategiesViewModel.SetSelectableStrategies(_fuelStrategies.Where(x => x.IsEligible(lastDataSet)));
            if (_fuelStrategiesViewModel.TrySelectStrategy(_currentStrategyData.LastSelectedStrategy))
            {
                return;
            }

            _fuelStrategiesViewModel.TrySelectStrategy(_fuelStrategiesViewModel.SelectableStrategies.Select(x => x.Value)
                .OrderByDescending(x => x.Priority).FirstOrDefault()?.StrategyName ?? string.Empty);
        }

        private void SelectedStrategyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(FuelStrategiesViewModel.SelectedStrategy) || _fuelStrategiesViewModel.SelectedStrategy == null)
            {
                return;
            }

            _fuelOverviewViewModel.PitStopsViewModel = _fuelStrategiesViewModel.SelectedStrategy.Value.PitStopsViewModel;
        }
    }
}
