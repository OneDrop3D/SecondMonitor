namespace SecondMonitor.ViewModels.FuelConsumption.FuelStrategy
{
    using System;
    using System.ComponentModel;

    using SecondMonitor.Contracts.PitStatistics;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Summary;
    using SecondMonitor.DataModel.Summary.FuelConsumption;
    using SecondMonitor.DataModel.Summary.FuelStrategy;
    using SecondMonitor.ViewModels.CarStatus.FuelStatus;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.SessionEvents;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;
    using SecondMonitor.ViewModels.Text;

    public abstract class AbstractFuelStrategy<T, TPitStopViewModel> : IFuelStrategy where T : IFuelStrategyViewModel
        where TPitStopViewModel : class, IPitStopsViewModel
    {
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly IPitStopStatisticProvider _pitStopStatisticProvider;
        private bool _isUpdating;

        protected AbstractFuelStrategy(ISessionEventProvider sessionEventProvider, ISettingsProvider settingsProvider,
            IPitStopStatisticProvider pitStopStatisticProvider, IViewModelFactory viewModelFactory)
        {
            FuelMultiplier = 1;
            _sessionEventProvider = sessionEventProvider;
            _pitStopStatisticProvider = pitStopStatisticProvider;
            DisplaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
            StrategyViewModelConcrete = viewModelFactory.Create<T>();
            PitStopsViewModelConcrete = viewModelFactory.Create<TPitStopViewModel>();
            StrategyViewModelConcrete.PropertyChanged += StrategyViewModelConcreteOnPropertyChanged;
        }

        public int FuelMultiplier { get; set; }

        public abstract string StrategyName { get; }
        public abstract int Priority { get; }
        public IFuelStrategyViewModel StrategyViewModel => StrategyViewModelConcrete;

        public IPitStopsViewModel PitStopsViewModel => PitStopsViewModelConcrete;

        protected TPitStopViewModel PitStopsViewModelConcrete { get; }

        protected T StrategyViewModelConcrete { get; }

        protected bool IsPitStopStatisticsValid => _pitStopStatisticProvider.GetCurrentStatisticsPlayerClass().EntriesCount >= 5;

        protected PitStatistics PitStopStatistics => _pitStopStatisticProvider.GetCurrentStatisticsPlayerClass();

        protected FuelConsumptionInfo FuelConsumptionInfo { get; private set; }

        protected SessionFuelConsumptionDto FuelConsumptionDto { get; private set; }

        protected Volume RequiredFuel { get; private set; }

        protected Volume FuelTankSize => _sessionEventProvider.LastDataSet?.PlayerInfo?.CarInfo.FuelSystemInfo.FuelCapacity ?? Volume.FromLiters(0);

        protected Volume FuelPerLap => FuelConsumptionInfo.GetAveragePerDistance(TrackInfo.LayoutLength);

        protected TrackInfo TrackInfo => _sessionEventProvider.LastDataSet.SessionInfo.TrackInfo;

        protected DisplaySettingsViewModel DisplaySettingsViewModel { get; }

        protected bool CanRecalculatePreRaceFuel => FuelConsumptionInfo != null && RequiredFuel != null;

        public abstract bool IsEligible(SimulatorDataSet dataSet);

        public virtual void UpdateConsumptionStatistics(SessionFuelConsumptionDto fuelConsumptionDto)
        {
            FuelConsumptionDto = fuelConsumptionDto;
            if (fuelConsumptionDto == null)
            {
                FuelConsumptionInfo = null;
                return;
            }

            FuelConsumptionInfo = new FuelConsumptionInfo(fuelConsumptionDto.ConsumedFuel, TimeSpan.FromSeconds(fuelConsumptionDto.ElapsedSeconds), Distance.FromMeters(fuelConsumptionDto.TraveledDistanceMeters));
            if (!CanRecalculatePreRaceFuel)
            {
                return;
            }

            _isUpdating = true;
            UpdatePreRaceCalculation(_sessionEventProvider.LastDataSet);
            _isUpdating = false;
        }

        public void UpdateRequiredFuel(Volume requiredFuel)
        {
            RequiredFuel = requiredFuel;
            if (!CanRecalculatePreRaceFuel)
            {
                return;
            }

            _isUpdating = true;
            UpdatePreRaceCalculation(_sessionEventProvider.LastDataSet);
            _isUpdating = false;
        }

        public abstract void LoadSettings(ClassFuelStrategy classFuelStrategy);

        public abstract void SaveToSettings(ClassFuelStrategy classFuelStrategy);

        public abstract void UpdateInRaceFuelPrediction(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel, ConsumptionMonitors consumptionMonitors);

        protected void UpdateInRaceFuelPrediction(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel, StintLengthKind stintLength, FuelConsumptionMonitor fuelConsumptionMonitor)
        {
            //It is impossible to use the full tank during a stint, as you need some fuel left to get the pit box
            double fuelToAddWithoutContingency = -fuelOverviewViewModel.FuelDelta?.InLiters ?? 0.0;
            double stintFuelCapacity = FuelTankSize.InLiters - fuelConsumptionMonitor.TotalPerLap.InLiters;
            double fuelDelta = fuelToAddWithoutContingency;
            fuelDelta += fuelConsumptionMonitor.TotalPerLap.InLiters * DisplaySettingsViewModel.ExtraRaceLaps;
            fuelDelta += fuelConsumptionMonitor.TotalPerMinute.InLiters * DisplaySettingsViewModel.ExtraRaceMinutes;
            int pitStopCount = (int)Math.Ceiling(fuelDelta / stintFuelCapacity);

            double fuelForLastPitStopInLiters = (fuelDelta) - ((pitStopCount - 1) * stintFuelCapacity);

            fuelForLastPitStopInLiters *= 1 + (DisplaySettingsViewModel.ExtraContingencyFuel / 100.0);
            fuelForLastPitStopInLiters = Math.Min(fuelForLastPitStopInLiters, stintFuelCapacity);

            PitStopsViewModelConcrete.IsFuelToAddVisible = true;
            PitStopsViewModelConcrete.PitStopCount = pitStopCount;
            PitStopsViewModelConcrete.IsPitStopCountVisible = pitStopCount > 1;

            if (dataSet.SessionInfo.PitWindow.PitWindowState != PitWindowState.None)
            {
                PitStopsViewModelConcrete.IsLastStopFuelToAddVisible = false;
                PitStopsViewModelConcrete.FuelForNextPitStopLabel = "Add: ";
                PitStopsViewModelConcrete.FuelForNextPitStop = Volume.FromLiters(Math.Ceiling(fuelForLastPitStopInLiters));
                if (dataSet.SessionInfo.PitWindow.PitWindowState == PitWindowState.InPitWindow)
                {
                    UpdateViewModelRefuelingState(dataSet, fuelOverviewViewModel,  fuelToAddWithoutContingency, fuelConsumptionMonitor);
                }
                else
                {
                    PitStopsViewModelConcrete.RefuelingWindowState = RefuelingWindowState.Closed;
                }

                UpdateViewModelRefuelingState(dataSet, fuelOverviewViewModel, fuelToAddWithoutContingency, fuelConsumptionMonitor);
                return;
            }

            if (PitStopsViewModelConcrete.PitStopCount <= 1)
            {
                PitStopsViewModelConcrete.IsLastStopFuelToAddVisible = false;
                PitStopsViewModelConcrete.FuelForNextPitStopLabel = "Add: ";
                PitStopsViewModelConcrete.FuelForNextPitStop = Volume.FromLiters(Math.Ceiling(fuelForLastPitStopInLiters));
                UpdateViewModelRefuelingState(dataSet, fuelOverviewViewModel, fuelToAddWithoutContingency, fuelConsumptionMonitor);
                return;
            }

            double fuelToAdd;
            double fuelToAddLastStop;

            switch (stintLength)
            {
                case StintLengthKind.Equal:
                    fuelToAdd = fuelDelta / pitStopCount;
                    fuelToAddLastStop = fuelToAdd * (1 + (DisplaySettingsViewModel.ExtraContingencyFuel / 100.0));
                    break;
                case StintLengthKind.LastShort:
                    fuelToAdd = FuelTankSize.InLiters;
                    fuelToAddLastStop = fuelForLastPitStopInLiters;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stintLength), stintLength, null);
            }

            PitStopsViewModelConcrete.IsLastStopFuelToAddVisible = true;
            PitStopsViewModelConcrete.FuelToAddLastStopLabel = "Last Stop: ";
            PitStopsViewModelConcrete.FuelForNextPitStop = Volume.FromLiters(fuelToAdd);
            PitStopsViewModelConcrete.FuelToAddLastStop = Volume.FromLiters(fuelToAddLastStop);
            UpdateViewModelRefuelingState(dataSet, fuelOverviewViewModel, fuelToAddWithoutContingency, fuelConsumptionMonitor);
        }

        public virtual void UpdateNextPitStopInfo(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel, ConsumptionMonitors consumptionMonitors)
        {
            FuelConsumptionMonitor fuelConsumptionMonitor = consumptionMonitors.FuelConsumptionMonitor;
            if (dataSet.SessionInfo.SessionType != SessionType.Race
                || fuelConsumptionMonitor.LapStartQuantityStatus == null
                || fuelOverviewViewModel.FuelDelta.InLiters >= 0
                || fuelOverviewViewModel.TimeDelta.TotalSeconds > 0
                || fuelOverviewViewModel.LapsDelta > 0
                || dataSet.PlayerInfo == null
                || fuelConsumptionMonitor.LapStartQuantityStatus.FuelLevel.InLiters < dataSet.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining.InLiters)
            {
                PitStopsViewModelConcrete.IsLastStopFuelToAddVisible = false;
                PitStopsViewModelConcrete.IsNextPitStopInfoVisible = false;
                PitStopsViewModelConcrete.NextPitStopState = RefuelingWindowState.Closed;
                PitStopsViewModelConcrete.NextPitStopInfo = string.Empty;
                return;
            }

            int lapsOfFuelLeft = fuelConsumptionMonitor.GetLapsOfFuelLeftAtNextLapStart();
            UpdateNextPitStopInfo(lapsOfFuelLeft, dataSet, fuelOverviewViewModel);
            PitStopsViewModelConcrete.FuelForNextPitStopLabel = "Next Stop: ";
            PitStopsViewModelConcrete.FuelToAddLastStopLabel = "Last Stop: ";
            PitStopsViewModelConcrete.IsNextPitStopInfoVisible = true;
        }

        protected void UpdateNextPitStopInfo(int lapsOfRunningLeft, SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel)
        {
            switch (lapsOfRunningLeft)
            {
                case <=0:
                    PitStopsViewModelConcrete.NextPitStopInfo = "PIT THIS LAP";
                    PitStopsViewModelConcrete.NextPitStopState = dataSet.PlayerInfo.PitStopRequested || dataSet.PlayerInfo.InPits ? RefuelingWindowState.WithContingency
                        : RefuelingWindowState.NoContingency;
                    PitStopsViewModelConcrete.NextPitStopFontStyle = FontStyle.Bold;
                    break;
                case 1:
                    PitStopsViewModelConcrete.NextPitStopInfo = "Pit Next Lap";
                    PitStopsViewModelConcrete.NextPitStopState = RefuelingWindowState.WithContingency;
                    PitStopsViewModelConcrete.NextPitStopFontStyle = FontStyle.Bold;
                    break;
                default:
                    PitStopsViewModelConcrete.NextPitStopInfo = $"Pit in {lapsOfRunningLeft + 1} laps";
                    PitStopsViewModelConcrete.NextPitStopState = RefuelingWindowState.Closed;
                    PitStopsViewModelConcrete.NextPitStopFontStyle = FontStyle.Normal;
                    break;
            }

            PitStopsViewModelConcrete.IsNextPitStopInfoVisible = true;
        }

        protected void UpdateViewModelRefuelingState(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel, double fuelToAddWithoutContingency, FuelConsumptionMonitor fuelConsumptionMonitor)
        {
            double fuelRemainingInLiters = dataSet.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining.InLiters;

            if ((fuelRemainingInLiters + PitStopsViewModelConcrete.FuelForNextPitStop.InLiters < FuelTankSize.InLiters
                || fuelRemainingInLiters <= fuelConsumptionMonitor.TotalPerLap.InLiters) && dataSet.SessionInfo.PitWindow.PitWindowState is PitWindowState.None or PitWindowState.InPitWindow)
            {
                PitStopsViewModelConcrete.RefuelingWindowState = RefuelingWindowState.WithContingency;
            }
            else if ((fuelRemainingInLiters + fuelToAddWithoutContingency < FuelTankSize.InLiters && PitStopsViewModelConcrete.PitStopCount == 1) && dataSet.SessionInfo.PitWindow.PitWindowState is PitWindowState.None or PitWindowState.InPitWindow)
            {
                PitStopsViewModelConcrete.RefuelingWindowState = RefuelingWindowState.NoContingency;
            }
            else
            {
                PitStopsViewModelConcrete.RefuelingWindowState = RefuelingWindowState.Closed;
            }
        }

        protected int GetLaps(Volume fuelVolume)
        {
            double fuelPerOneLap = FuelConsumptionInfo.GetAveragePerDistance(TrackInfo.LayoutLength).InLiters;
            return (int)(fuelVolume.InLiters / fuelPerOneLap) / FuelMultiplier;
        }

        protected int GetMinutes(Volume fuelVolume)
        {
            return (int)(fuelVolume.InLiters / FuelConsumptionInfo.GetAveragePerMinute().InLiters) / FuelMultiplier;
        }

        private void StrategyViewModelConcreteOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isUpdating || !CanRecalculatePreRaceFuel)
            {
                return;
            }

            _isUpdating = true;
            UpdatePreRaceCalculation(_sessionEventProvider.LastDataSet);
            _isUpdating = false;
        }

        protected abstract void UpdatePreRaceCalculation(SimulatorDataSet dataSet);
    }
}
