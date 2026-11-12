namespace SecondMonitor.ViewModels.FuelConsumption
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using CarStatus.FuelStatus;
    using Controllers;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using DataModel.Summary.FuelConsumption;
    using Factory;
    using NLog;

    using SecondMonitor.Contracts.NInject;
    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.FuelConsumption.Repository;

    using SessionEvents;

    public class FuelConsumptionController : AbstractController, IController, IFuelPredictionProvider
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly TimeSpan minimumSessionLength = TimeSpan.FromMinutes(2);
        private readonly FuelConsumptionRepository _fuelConsumptionRepository;
        private readonly Lazy<OverallFuelConsumptionHistory> _overallFuelHistoryLazy;
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly IList<ISessionFuelCalculatorConfiguration> _sessionConfigurations;
        private readonly IFuelStrategyController _fuelStrategyController;
        private readonly HybridOverviewController _hybridOverviewController;
        private bool _isFuelCalculatorShown;
        private SimulatorDataSet _lastDataSet;
        private string _loadedEntriesCarName;
        private ISessionFuelCalculatorConfiguration _activeConfiguration;
        private IReadOnlyCollection<SessionFuelConsumptionDto> _currentFuelConsumptionEntries;

        public FuelConsumptionController(FuelConsumptionRepository fuelConsumptionRepository, ISessionEventProvider sessionEventProvider, 
            IList<ISessionFuelCalculatorConfiguration> sessionConfigurations, IViewModelFactory viewModelFactory, ConsumptionMonitors consumptionMonitors, IChildControllerFactory controllerFactory) : base(viewModelFactory)
        {
            ConsumptionMonitors = consumptionMonitors;
            _currentFuelConsumptionEntries = new List<SessionFuelConsumptionDto>();
            _fuelConsumptionRepository = fuelConsumptionRepository;
            _sessionEventProvider = sessionEventProvider;
            _sessionConfigurations = sessionConfigurations;
            _overallFuelHistoryLazy = new Lazy<OverallFuelConsumptionHistory>(LoadOverallFuelConsumptionHistory);
            _activeConfiguration = new SessionFuelConfigurationNull();
            Bind<FuelOverviewViewModel>().Command(x => x.ShowFuelCalculatorCommand).To(() => ShowFuelCalculator(false));
            Bind<FuelOverviewViewModel>().Command(x => x.HideFuelCalculatorCommand).To(HideFuelCalculatorByUser);
            FuelOverviewViewModel = ViewModelFactory.Create<FuelOverviewViewModel>();
            FuelOverviewViewModel.IsVisible = true;

            TypeMatchingConstructorArgument<IFuelOverviewViewModel> constructorArgument = new((_, _) => FuelOverviewViewModel);
            _fuelStrategyController = controllerFactory.Create<IFuelStrategyController, FuelConsumptionController>(this, constructorArgument);

            TypeMatchingConstructorArgument<HybridOverviewViewModel> hybridViewModelConstructorArgument = new((_, _) => FuelOverviewViewModel.HybridOverviewViewModel);
            _hybridOverviewController = controllerFactory.Create<HybridOverviewController, FuelConsumptionController>(this, hybridViewModelConstructorArgument);
        }

        public ConsumptionMonitors ConsumptionMonitors { get; }

        public FuelOverviewViewModel FuelOverviewViewModel { get; }

        private OverallFuelConsumptionHistory OverallFuelConsumptionHistory => _overallFuelHistoryLazy.Value;

        private bool IsFuelCalculatorShown
        {
            get => _isFuelCalculatorShown;
            set
            {
                if (_isFuelCalculatorShown == value)
                {
                    return;
                }

                logger.Info($"FuelCalculator visibility switched to {value}");
                _isFuelCalculatorShown = value;
                FuelOverviewViewModel.FuelPlannerViewModel.IsVisible = value;
            }
        }

        public SessionFuelConsumptionDto LastFinishedConsumptionDto { get; private set; }

        public override async Task StartControllerAsync()
        {
            _sessionEventProvider.PlayerPropertiesChanged += SessionEventProviderOnPlayerPropertiesChanged;
            _sessionEventProvider.TrackChanged += SessionEventProviderOnSessionTypeChange;
            _sessionEventProvider.SessionTypeChange += SessionEventProviderOnSessionTypeChange;
            await _fuelStrategyController.StartControllerAsync();
            await _hybridOverviewController.StartControllerAsync();
        }

        public override async Task StopControllerAsync()
        {
            _sessionEventProvider.PlayerPropertiesChanged -= SessionEventProviderOnPlayerPropertiesChanged;
            _sessionEventProvider.TrackChanged -= SessionEventProviderOnSessionTypeChange;
            _sessionEventProvider.SessionTypeChange -= SessionEventProviderOnSessionTypeChange;
            StoreCurrentSessionConsumption();
            if (_overallFuelHistoryLazy.IsValueCreated)
            {
                _fuelConsumptionRepository.Save(OverallFuelConsumptionHistory);
            }

            await _fuelStrategyController.StopControllerAsync();
            await _hybridOverviewController.StopControllerAsync();
        }

        public void ApplyDataSet(SimulatorDataSet dataSet)
        {
            if (dataSet.SessionInfo.SpectatingState != SpectatingState.Live || dataSet.PlayerInfo == null)
            {
                return;
            }

            try
            {
                _lastDataSet = dataSet;
                _hybridOverviewController.ApplyDataSet(dataSet);
                CheckFuelConsumptionEntries(dataSet);
                ConsumptionMonitors.UpdateQuantityConsumption(dataSet);
                FuelOverviewViewModel.ApplyDataSet(dataSet, ConsumptionMonitors);
                _fuelStrategyController.ApplyDataSet(dataSet);
                UpdateIsFuelCalculatorButtonEnabled(dataSet);
                if (ShouldOpenFuelCalculator(dataSet))
                {
                    AutoOpenFuelCalculator(dataSet);
                }

                if (ShouldCloseFuelCalculator(dataSet))
                {
                    AutoHideFuelCalculator();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while applying data set");
                throw;
            }
        }

        private void CheckFuelConsumptionEntries(SimulatorDataSet dataSet)
        {
            if (dataSet.PlayerInfo == null || string.IsNullOrEmpty(dataSet.PlayerInfo.CarName))
            {
                return;
            }

            // Wrong car is load, and we're 100% that the dataset car name is the correct one
            if (dataSet.PlayerInfo.CarName != _loadedEntriesCarName && dataSet.PlayerInfo.CarName == _sessionEventProvider.CurrentCarName)
            {
                logger.Info("Fuel Consumption: Car Name mismatch. Reseting.");
                Reset();
                FillCurrentConsumptionEntries(dataSet);
            }
        }

        private void UpdateIsFuelCalculatorButtonEnabled(SimulatorDataSet dataSet)
        {
            if (!ConsumptionMonitors.TryGet(out FuelConsumptionMonitor fuelConsumptionMonitor))
            {
                return;
            }

            FuelOverviewViewModel.IsFuelCalculatorButtonEnabled = dataSet.PlayerInfo != null && (_currentFuelConsumptionEntries.Count > 0 ||
                                                                                                 fuelConsumptionMonitor.TotalQuantityConsumptionInfo.ElapsedTime >=
                                                                                                 minimumSessionLength);
        }

        public void Reset()
        {
            try
            {
                StoreCurrentSessionConsumption();
                _hybridOverviewController.Reset();

                if (!string.IsNullOrWhiteSpace(_sessionEventProvider.CurrentCarName))
                {
                    AutoHideFuelCalculator();
                    _sessionConfigurations.ForEach(x => x.Reset());
                    _activeConfiguration = _sessionConfigurations.FirstOrDefault(x => x.SessionType == _sessionEventProvider.LastDataSet.SessionInfo.SessionType) ?? new SessionFuelConfigurationNull();
                }

                if (_overallFuelHistoryLazy.IsValueCreated)
                {
                    _fuelConsumptionRepository.Save(OverallFuelConsumptionHistory);
                }

                _lastDataSet = null;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while reseting FuelConsumption Controller");
            }

            FuelOverviewViewModel.Reset();
            ConsumptionMonitors.Reset();
        }

        private void AutoOpenFuelCalculator(SimulatorDataSet dataSet)
        {
            (int laps, int minutes, int extraFuel) = _activeConfiguration.GetRequiredRunTime(dataSet, OverallFuelConsumptionHistory);

            FuelOverviewViewModel.FuelPlannerViewModel.CalculatorForSelectedSession.UpdateDefaultSettingsOnValueChanged = dataSet.SessionInfo.SessionType == SessionType.Race && !_sessionEventProvider.CurrentSimulatorSettings.IsSessionLengthAvailableBeforeStart;
            FuelOverviewViewModel.FuelPlannerViewModel.CalculatorForSelectedSession.SetRequiredRunTime(laps, minutes, extraFuel);

            ShowFuelCalculator(true);
        }

        private void ShowFuelCalculator(bool isAutoOpen)
        {
            try
            {
                if (!Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(() => ShowFuelCalculator(isAutoOpen));
                    return;
                }

                logger.Info("Opening Fuel Calculator");

                if (_lastDataSet?.PlayerInfo == null)
                {
                    logger.Info("Unable to open Fuel Calculator - No Player");
                    return;
                }

                IReadOnlyCollection<SessionFuelConsumptionDto> fuelConsumptionEntries = _currentFuelConsumptionEntries;
                SessionFuelConsumptionDto currentSessionEntry = CreateCurrentSessionFuelConsumptionDto();
                if (currentSessionEntry != null)
                {
                    fuelConsumptionEntries = new[] { currentSessionEntry }.Concat(_currentFuelConsumptionEntries).ToList();
                }

                if (isAutoOpen && fuelConsumptionEntries.Count == 0)
                {
                    logger.Info(" Calculator Not Auto Opened - No Entries");
                    return;
                }

                FuelOverviewViewModel.FuelPlannerViewModel.Sessions.CollectionChanged -= SessionsOnCollectionChanged;
                FuelOverviewViewModel.FuelPlannerViewModel.FromModel(fuelConsumptionEntries.ToList());
                IsFuelCalculatorShown = true;

                FuelOverviewViewModel.FuelPlannerViewModel.Sessions.CollectionChanged += SessionsOnCollectionChanged;
                logger.Info("Fuel Calculator Opened");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unable to open Fuel Calculator, Exception");
            }
        }

        private void SessionsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Remove)
            {
                return;
            }

            IEnumerable<SessionFuelConsumptionDto> removedInfo = e.OldItems.OfType<ISessionFuelConsumptionViewModel>().Select(x => x.OriginalModel);
            removedInfo.ForEach(x => OverallFuelConsumptionHistory.RemoveTrackConsumptionHistoryEntry(x));
        }

        private void AutoHideFuelCalculator()
        {
            _activeConfiguration.OnAutoClose();
            IsFuelCalculatorShown = false;
        }

        private void HideFuelCalculatorByUser()
        {
            _activeConfiguration.OnUserClosed();
            IsFuelCalculatorShown = false;
        }

        private void StoreCurrentSessionConsumption()
        {
            if (!ConsumptionMonitors.TryGet(out FuelConsumptionMonitor fuelConsumptionMonitor))
            {
                return;
            }

            if (_lastDataSet?.PlayerInfo == null || fuelConsumptionMonitor.TotalQuantityConsumptionInfo.ElapsedTime < minimumSessionLength || fuelConsumptionMonitor.TotalQuantityConsumptionInfo.ConsumedFuel.InLiters <= 0)
            {
                if (_lastDataSet?.PlayerInfo == null)
                {
                    logger.Info("Fuel Consumption - Current Session Discared. No dataSet / No player");
                }
                else
                {
                    logger.Info($"Fuel Consumption - Current Session Discared. Elapsed Time: {fuelConsumptionMonitor.TotalQuantityConsumptionInfo.ElapsedTime.FormatToMinutesSeconds()}. Total Fuel Consumed: {fuelConsumptionMonitor.TotalQuantityConsumptionInfo.ConsumedFuel.InLiters:F0}");
                }

                return;
            }

            logger.Info($"Fuel Consumtion - Stored Session Data. Elapsed Time: {fuelConsumptionMonitor.TotalQuantityConsumptionInfo.ElapsedTime.FormatToMinutesSeconds()}. Total Fuel Consumed: {fuelConsumptionMonitor.TotalQuantityConsumptionInfo.ConsumedFuel.InLiters:F0}");

            LastFinishedConsumptionDto = CreateCurrentSessionFuelConsumptionDto();

            OverallFuelConsumptionHistory.AddTrackConsumptionHistoryEntry(LastFinishedConsumptionDto);
        }

        public SessionFuelConsumptionDto CreateCurrentSessionFuelConsumptionDto()
        {
            if (!ConsumptionMonitors.TryGet(out FuelConsumptionMonitor fuelConsumptionMonitor) || _lastDataSet?.PlayerInfo == null || fuelConsumptionMonitor.TotalQuantityConsumptionInfo.ElapsedTime < minimumSessionLength)
            {
                return null;
            }

            bool hasVirtualTankConsumption = ConsumptionMonitors.TryGet(out VirtualEnergyConsumptionMonitor virtualEnergyConsumptionMonitor);
            double fuelConsumptionCoef = hasVirtualTankConsumption
                ? fuelConsumptionMonitor.TotalQuantityConsumptionInfo.ConsumedFuel.InLiters /
                  virtualEnergyConsumptionMonitor.TotalQuantityConsumptionInfo.ConsumedQuantity.InPercentage
                : 0;

            SessionFuelConsumptionDto currentSessionFuelConsumption = new SessionFuelConsumptionDto()
            {
                IsWetSession = FuelOverviewViewModel.IsWetSession,
                CarName = _sessionEventProvider.CurrentCarName,
                LapDistance = _lastDataSet.SessionInfo.TrackInfo.LayoutLength.InMeters,
                RecordDate = DateTime.Now,
                SessionKind = _lastDataSet.SessionInfo.SessionType,
                Simulator = _lastDataSet.Source,
                TrackFullName = _lastDataSet.SessionInfo.TrackInfo.TrackFullName,
                TraveledDistanceMeters = fuelConsumptionMonitor.TotalQuantityConsumptionInfo.TraveledDistance.InMeters,
                ConsumedFuel = fuelConsumptionMonitor.TotalQuantityConsumptionInfo.ConsumedFuel / FuelOverviewViewModel.FuelMultiplier,
                ElapsedSeconds = fuelConsumptionMonitor.TotalQuantityConsumptionInfo.ElapsedTime.TotalSeconds,
                HasVirtualEnergyFuelCoef = hasVirtualTankConsumption,
                VirtualEnergyFuelCoef = fuelConsumptionCoef,
            };

            return currentSessionFuelConsumption;
        }

        private void SessionEventProviderOnPlayerPropertiesChanged(object sender, DataSetArgs e)
        {
            if (e.DataSet.SessionInfo.SpectatingState != SpectatingState.Live)
            {
                return;
            }

            if (e.DataSet.PlayerInfo.CarName == _loadedEntriesCarName)
            {
                return;
            }

            Reset();
            FillCurrentConsumptionEntries(e.DataSet);
        }

        private OverallFuelConsumptionHistory LoadOverallFuelConsumptionHistory()
        {
            return _fuelConsumptionRepository.LoadOrCreateNew();
        }

        public TimeSpan GetRemainingFuelTime()
        {
            return FuelOverviewViewModel.TimeLeft;
        }

        public double GetLapsLeft()
        {
            return FuelOverviewViewModel.LapsLeft;
        }

        private bool ShouldCloseFuelCalculator(SimulatorDataSet dataSet)
        {
            return _activeConfiguration.ShouldAutoClose(dataSet);
        }

        private bool ShouldOpenFuelCalculator(SimulatorDataSet dataSet)
        {
            return FuelOverviewViewModel.IsVisible && _activeConfiguration.ShouldAutoOpen(dataSet);
        }

        private void SessionEventProviderOnSessionTypeChange(object sender, DataSetArgs e)
        {
            Reset();
            FillCurrentConsumptionEntries(e.DataSet);
        }

        private void FillCurrentConsumptionEntries(SimulatorDataSet dataSet)
        {
            _loadedEntriesCarName = _sessionEventProvider.CurrentCarName;
            if (dataSet.PlayerInfo == null)
            {
                _currentFuelConsumptionEntries = new List<SessionFuelConsumptionDto>();
            }
            else
            {
                ConsumptionMonitors.Initialize(dataSet);
                _currentFuelConsumptionEntries =
                    OverallFuelConsumptionHistory.GetTrackConsumptionHistoryEntries(dataSet.Source, _sessionEventProvider.LastTrackFullName, _sessionEventProvider.CurrentCarName);
            }
        }
    }
}