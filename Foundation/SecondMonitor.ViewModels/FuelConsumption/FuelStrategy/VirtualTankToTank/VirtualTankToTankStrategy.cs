namespace SecondMonitor.ViewModels.FuelConsumption.FuelStrategy.VirtualTankToTank
{
    using System;

    using SecondMonitor.Contracts.PitStatistics;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Summary;
    using SecondMonitor.DataModel.Summary.FuelConsumption;
    using SecondMonitor.DataModel.Summary.FuelStrategy;
    using SecondMonitor.DataModel.Summary.FuelStrategy.VirtualEnergyStrategy;
    using SecondMonitor.ViewModels.CarStatus;
    using SecondMonitor.ViewModels.CarStatus.FuelStatus;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Formatters;
    using SecondMonitor.ViewModels.SessionEvents;
    using SecondMonitor.ViewModels.Settings;

    public class VirtualTankToTankStrategy : AbstractFuelStrategy<VirtualTankToTankViewModel, VirtualTankToTankPitStopViewModel>, IFuelStrategy
    {
        private readonly SessionRemainingCalculator _sessionRemainingCalculator;
        private readonly VirtualEnergyCondition _virtualEnergyCondition;
        private readonly IQuantityFormatter _quantityFormatter;
        private VirtualTankConsumptionInfo _virtualTankConsumptionInfo;

        public VirtualTankToTankStrategy(SessionRemainingCalculator sessionRemainingCalculator, VirtualEnergyCondition virtualEnergyCondition,  IQuantityFormatter quantityFormatter, IViewModelFactory viewModelFactory,
            ISessionEventProvider sessionEventProvider, ISettingsProvider settingsProvider, IPitStopStatisticProvider pitStopStatisticProvider)
            : base(sessionEventProvider, settingsProvider, pitStopStatisticProvider, viewModelFactory)
        {
            _sessionRemainingCalculator = sessionRemainingCalculator;
            _virtualEnergyCondition = virtualEnergyCondition;
            _quantityFormatter = quantityFormatter;
        }

        private double? VirtualEnergyFuelCoef => FuelConsumptionDto?.VirtualEnergyFuelCoef;

        public override string StrategyName => "Tank to Tank (Virtual)";
        public override int Priority => 0;

        public override void UpdateConsumptionStatistics(SessionFuelConsumptionDto fuelConsumptionDto)
        {
            base.UpdateConsumptionStatistics(fuelConsumptionDto);
            _virtualTankConsumptionInfo = CanRecalculatePreRaceFuel && fuelConsumptionDto.HasVirtualEnergyFuelCoef ? new VirtualTankConsumptionInfo(
                Percentage.FromPercentage(fuelConsumptionDto.ConsumedFuel.InLiters / fuelConsumptionDto.VirtualEnergyFuelCoef),
                TimeSpan.FromSeconds(fuelConsumptionDto.ElapsedSeconds), Distance.FromMeters(fuelConsumptionDto.TraveledDistanceMeters)) : null;
        }

        public override bool IsEligible(SimulatorDataSet dataSet)
        {
            return _virtualEnergyCondition.HasVirtualEnergy(dataSet);
        }

        public override void LoadSettings(ClassFuelStrategy classFuelStrategy)
        {
            StrategyViewModelConcrete.FromModel(classFuelStrategy.VirtualTankToTankStrategySettings);
        }

        public override void SaveToSettings(ClassFuelStrategy classFuelStrategy)
        {
            classFuelStrategy.VirtualTankToTankStrategySettings = StrategyViewModelConcrete.SaveToNewModel();
        }

        public override void UpdateNextPitStopInfo(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel, ConsumptionMonitors consumptionMonitors)
        {
            if (!consumptionMonitors.TryGet(out VirtualEnergyConsumptionMonitor virtualEnergyConsumptionMonitor) || FuelConsumptionDto == null)
            {
                return;
            }

            FuelConsumptionMonitor fuelConsumptionMonitor = consumptionMonitors.FuelConsumptionMonitor;

            if (dataSet.SessionInfo.SessionType != SessionType.Race
                || fuelOverviewViewModel.FuelDelta == null
                || fuelConsumptionMonitor.LapStartQuantityStatus == null
                || fuelOverviewViewModel.TimeDelta.TotalSeconds >= 0
                || virtualEnergyConsumptionMonitor.LapStartQuantityStatus == null
                || fuelConsumptionMonitor.LapStartQuantityStatus.FuelLevel.InLiters < dataSet.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining.InLiters
                || dataSet.PlayerInfo == null)
            {
                ClearPitStopInformationFromViewModel();
                return;
            }

            if (dataSet.PlayerInfo.InPits)
            {
                return;
            }

            double lapsRemaining = _sessionRemainingCalculator.GetLapsRemaining(dataSet);
            Volume fuelDelta = fuelConsumptionMonitor.GetQuantityIn(lapsRemaining, dataSet);
            Percentage virtualEnergyDelta = virtualEnergyConsumptionMonitor.GetQuantityIn(lapsRemaining, dataSet);

            switch (fuelDelta.InLiters)
            {
                case > 0 when virtualEnergyDelta.InPercentage > 0:
                    ClearPitStopInformationFromViewModel();
                    return;
                case <= 0 when virtualEnergyDelta.InPercentage > 0:
                    UpdateLastPitStopInfoFuelOnly(dataSet, fuelOverviewViewModel, fuelConsumptionMonitor);
                    return;
            }

            int lapsOfFuelLeft = fuelConsumptionMonitor.GetLapsOfFuelLeftAtNextLapStart();
            int lapsOfVirtualEnergyLeft = virtualEnergyConsumptionMonitor.GetLapsOfFuelLeftAtNextLapStart();

            int lapsOfAnyFuelLeft = Math.Min(lapsOfFuelLeft, lapsOfVirtualEnergyLeft);
            UpdateNextPitStopInfo(lapsOfAnyFuelLeft, dataSet, fuelOverviewViewModel);

            double stintFuelCapacity = FuelTankSize.InLiters - fuelConsumptionMonitor.TotalPerLap.InLiters;
            double stintVirtualEnergyCapacity = GetStintVirtualEnergyCapacity(virtualEnergyConsumptionMonitor.TotalQuantityConsumptionInfo);
            int fuelPitStopCount = (int)Math.Ceiling(-fuelDelta.InLiters / stintFuelCapacity);
            int virtualEnergyPitStopCount = (int)Math.Ceiling(-virtualEnergyDelta.InPercentage / stintVirtualEnergyCapacity);

            if (virtualEnergyPitStopCount <= 1 && fuelPitStopCount <= 1)
            {
                UpdateNextPitStopInfoLastStop(dataSet, fuelOverviewViewModel, fuelConsumptionMonitor, virtualEnergyConsumptionMonitor, fuelDelta, virtualEnergyDelta,
                    stintFuelCapacity, stintVirtualEnergyCapacity, lapsOfAnyFuelLeft + 1);
                return;
            }

            if (fuelPitStopCount > virtualEnergyPitStopCount)
            {
                UpdateNextPitStopsByFuel(dataSet, fuelOverviewViewModel, consumptionMonitors);
                return;
            }

            UpdateNextPitStopsByVirtualEnergy(dataSet, fuelOverviewViewModel, fuelConsumptionMonitor, virtualEnergyConsumptionMonitor, fuelDelta, virtualEnergyDelta,
                stintVirtualEnergyCapacity, lapsOfAnyFuelLeft + 1);
        }

        private void UpdateNextPitStopsByFuel(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel, ConsumptionMonitors consumptionMonitors)
        {
            UpdateInRaceFuelPrediction(dataSet, fuelOverviewViewModel, StrategyViewModelConcrete.StintLength.Value, consumptionMonitors.FuelConsumptionMonitor);
            base.UpdateNextPitStopInfo(dataSet, fuelOverviewViewModel, consumptionMonitors);
        }

        private void UpdateNextPitStopsByVirtualEnergy(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel, FuelConsumptionMonitor fuelConsumptionMonitor,
            VirtualEnergyConsumptionMonitor virtualEnergyConsumptionMonitor,
            Volume fuelDelta, Percentage virtualEnergyDelta, double stintVirtualEnergyCapacity, int lapsOfFuelLeft)
        {
            double virtualEnergyCoef = fuelConsumptionMonitor.TotalQuantityConsumptionInfo.ConsumedFuel.InLiters /
                                       virtualEnergyConsumptionMonitor.TotalQuantityConsumptionInfo.ConsumedQuantity.InPercentage;

            double virtualEnergyWithoutContingency = -virtualEnergyDelta.InPercentage;
            double totalVirtualEnergyToAdd = virtualEnergyWithoutContingency;
            totalVirtualEnergyToAdd += virtualEnergyConsumptionMonitor.TotalPerLap.InPercentage * DisplaySettingsViewModel.ExtraRaceLaps;
            totalVirtualEnergyToAdd += virtualEnergyConsumptionMonitor.TotalPerMinute.InPercentage * DisplaySettingsViewModel.ExtraRaceMinutes;
            int pitStopCount = (int)Math.Ceiling(totalVirtualEnergyToAdd / stintVirtualEnergyCapacity);
            double fuelToVirtualEnergyDelta = virtualEnergyDelta.InPercentage * virtualEnergyCoef - fuelDelta.InLiters;

            double virtualEnergyForLastPitStop = (totalVirtualEnergyToAdd) - ((pitStopCount - 1) * stintVirtualEnergyCapacity);

            virtualEnergyForLastPitStop *= 1 + (DisplaySettingsViewModel.ExtraContingencyFuel / 100.0);
            virtualEnergyForLastPitStop = Math.Min(virtualEnergyForLastPitStop, stintVirtualEnergyCapacity);

            double virtualEnergyNextStop;
            double virtualEnergyLastStop;

            switch (StrategyViewModelConcrete.StintLength.Value)
            {
                case StintLengthKind.Equal:
                    virtualEnergyNextStop = totalVirtualEnergyToAdd / pitStopCount;
                    virtualEnergyLastStop = virtualEnergyNextStop * (1 + (DisplaySettingsViewModel.ExtraContingencyFuel / 100.0));
                    break;
                case StintLengthKind.LastShort:
                    virtualEnergyNextStop = 100;
                    virtualEnergyLastStop = virtualEnergyForLastPitStop;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(StrategyViewModelConcrete.StintLength.Value), StrategyViewModelConcrete.StintLength.Value, null);
            }

            double fuelNextStopInLiters = virtualEnergyNextStop * virtualEnergyCoef + fuelToVirtualEnergyDelta;
            Volume fuelLastStop = Volume.FromLiters(virtualEnergyLastStop * virtualEnergyCoef);

            PitStopsViewModelConcrete.PitStopCount = pitStopCount;
            PitStopsViewModelConcrete.IsPitStopCountVisible = pitStopCount > 1;
            PitStopsViewModelConcrete.IsLastStopFuelToAddVisible = true;
            PitStopsViewModelConcrete.IsFuelToAddVisible = true;
            CreateNextPitStopInfoLabel(virtualEnergyConsumptionMonitor, virtualEnergyNextStop, fuelNextStopInLiters, lapsOfFuelLeft);

            PitStopsViewModelConcrete.FuelToAddLastStopLabel = $"Last Stop: ";
            PitStopsViewModelConcrete.VirtualEnergyLastPitStop = Math.Ceiling(virtualEnergyLastStop).ToString("F0") + "%";
            PitStopsViewModelConcrete.FuelToAddLastStop = fuelLastStop;
            UpdateViewModelRefuelingState(dataSet, fuelOverviewViewModel, -virtualEnergyDelta.InPercentage, virtualEnergyNextStop, virtualEnergyConsumptionMonitor);
        }

        private void UpdateNextPitStopInfoLastStop(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel, FuelConsumptionMonitor fuelConsumptionMonitor, VirtualEnergyConsumptionMonitor virtualEnergyConsumptionMonitor,
            Volume fuelDelta, Percentage virtualEnergyDelta, double stintFuelCapacity, double stintVirtualEnergyCapacity, int lapsOfFuelLeft)
        {
            double fuelForLastPitStopInLiters = -fuelDelta.InLiters;
            fuelForLastPitStopInLiters += fuelConsumptionMonitor.TotalPerLap.InLiters * DisplaySettingsViewModel.ExtraRaceLaps;
            fuelForLastPitStopInLiters += fuelConsumptionMonitor.TotalPerMinute.InLiters * DisplaySettingsViewModel.ExtraRaceMinutes;
            fuelForLastPitStopInLiters *= 1 + (DisplaySettingsViewModel.ExtraContingencyFuel / 100.0);
            fuelForLastPitStopInLiters = Math.Min(fuelForLastPitStopInLiters, stintFuelCapacity);

            double virtualEnergyPitStop = -virtualEnergyDelta.InPercentage;
            virtualEnergyPitStop += virtualEnergyConsumptionMonitor.TotalPerLap.InPercentage * DisplaySettingsViewModel.ExtraRaceLaps;
            virtualEnergyPitStop += virtualEnergyConsumptionMonitor.TotalPerMinute.InPercentage * DisplaySettingsViewModel.ExtraRaceMinutes;
            virtualEnergyPitStop += DisplaySettingsViewModel.ExtraContingencyFuel;
            virtualEnergyPitStop = Math.Min(virtualEnergyPitStop, stintVirtualEnergyCapacity);

            PitStopsViewModelConcrete.IsLastStopFuelToAddVisible = false;
            PitStopsViewModelConcrete.IsPitStopCountVisible = false;
            PitStopsViewModelConcrete.PitStopCount = 1;
            CreateNextPitStopInfoLabel(virtualEnergyConsumptionMonitor, virtualEnergyPitStop, fuelForLastPitStopInLiters, lapsOfFuelLeft);
            PitStopsViewModelConcrete.IsFuelToAddVisible = true;
            UpdateViewModelRefuelingState(dataSet, fuelOverviewViewModel, -virtualEnergyDelta.InPercentage, virtualEnergyPitStop, virtualEnergyConsumptionMonitor);
        }

        private void CreateNextPitStopInfoLabel(VirtualEnergyConsumptionMonitor virtualEnergyConsumptionMonitor, double virtualEnergyPitStop, double fuelForPitStop, int lapsOfFuelLeft)
        {
            string fuelToAddLabel;
            bool isFuelForNextPitStopTotal = false;
            if (VirtualEnergyFuelCoef == null)
            {
                return;
            }

            double virtualEnergyCoef = VirtualEnergyFuelCoef.Value;

            switch (StrategyViewModelConcrete.SelectedPitStopInfo.Value)
            {
                case VirtualEnergyPitStopInfo.TotalFuelEndOfLap:
                    double veAtLapEnd = virtualEnergyConsumptionMonitor.LapStartQuantityStatus.VirtualTank.InPercentage - virtualEnergyConsumptionMonitor.TotalPerLap.InPercentage;
                    virtualEnergyPitStop += veAtLapEnd;
                    virtualEnergyPitStop = Math.Min(100, virtualEnergyPitStop);
                    fuelForPitStop = virtualEnergyPitStop * virtualEnergyCoef;
                    fuelToAddLabel = "Total (This Lap):";
                    isFuelForNextPitStopTotal = true;
                    break;
                case VirtualEnergyPitStopInfo.TotalFuelPitLap:
                    veAtLapEnd = virtualEnergyConsumptionMonitor.LapStartQuantityStatus.VirtualTank.InPercentage - virtualEnergyConsumptionMonitor.TotalPerLap.InPercentage * lapsOfFuelLeft;
                    virtualEnergyPitStop += veAtLapEnd;
                    virtualEnergyPitStop = Math.Min(100, virtualEnergyPitStop);
                    fuelForPitStop = virtualEnergyPitStop * virtualEnergyCoef;
                    fuelToAddLabel = "Total (Pit Lap):";
                    isFuelForNextPitStopTotal = true;
                    break;
                case VirtualEnergyPitStopInfo.FuelToAdd:
                    fuelToAddLabel = "Add:";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            PitStopsViewModelConcrete.IsFuelForNextPitStopTotal = isFuelForNextPitStopTotal;
            PitStopsViewModelConcrete.VirtualEnergyNextPitStop = Math.Ceiling(virtualEnergyPitStop).ToString("F0") + "%";
            PitStopsViewModelConcrete.FuelCoefNextPitStop = (fuelForPitStop / virtualEnergyPitStop).ToStringScalableDecimals();
            PitStopsViewModelConcrete.FuelForNextPitStopLabel = fuelToAddLabel;
            PitStopsViewModelConcrete.FuelForNextPitStop = Volume.FromLiters(Math.Ceiling(fuelForPitStop));
        }

        private void UpdateViewModelRefuelingState(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel, double virtualEnergyToAddWithoutContingency, double virtualEnergyToAdd, VirtualEnergyConsumptionMonitor virtualEnergyConsumptionMonitor)
        {
            double virtualEnergyRemaining = dataSet.PlayerInfo.CarInfo.FuelSystemInfo.VirtualTank.InPercentage;

            if (virtualEnergyRemaining + virtualEnergyToAdd < 100
                || virtualEnergyRemaining <= virtualEnergyConsumptionMonitor.TotalPerLap.InPercentage)
            {
                PitStopsViewModelConcrete.RefuelingWindowState = RefuelingWindowState.WithContingency;
            }
            else if (virtualEnergyRemaining + virtualEnergyToAddWithoutContingency < 100 && PitStopsViewModelConcrete.PitStopCount == 1)
            {
                PitStopsViewModelConcrete.RefuelingWindowState = RefuelingWindowState.NoContingency;
            }
            else
            {
                PitStopsViewModelConcrete.RefuelingWindowState = RefuelingWindowState.Closed;
            }
        }

        private void UpdateLastPitStopInfoFuelOnly(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel, FuelConsumptionMonitor fuelConsumptionMonitor)
        {
            UpdateInRaceFuelPrediction(dataSet, fuelOverviewViewModel, StrategyViewModelConcrete.StintLength.Value, fuelConsumptionMonitor);
            int lapsOfFuelLeft = fuelConsumptionMonitor.GetLapsOfFuelLeftAtNextLapStart();
            UpdateNextPitStopInfo(lapsOfFuelLeft, dataSet, fuelOverviewViewModel);

            PitStopsViewModelConcrete.IsNextPitStopInfoVisible = true;
        }

        public override void UpdateInRaceFuelPrediction(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel, ConsumptionMonitors consumptionMonitors)
        {
        }

        protected override void UpdatePreRaceCalculation(SimulatorDataSet dataSet)
        {
            if (_virtualTankConsumptionInfo == null)
            {
                return;
            }

            double requiredVirtualEnergy = RequiredFuel.InLiters / FuelConsumptionDto.VirtualEnergyFuelCoef;
            double stintVirtualEnergyCapacity = GetStintVirtualEnergyCapacity(_virtualTankConsumptionInfo);
            int optimalStopCount = (int)(requiredVirtualEnergy / stintVirtualEnergyCapacity);

            if (StrategyViewModelConcrete.StartFuel.Value != FuelLoadKind.Auto)
            {
                double startingVirtualFuel = StrategyViewModelConcrete.StartFuel.Value == FuelLoadKind.Custom ? StrategyViewModelConcrete.CustomStartingFuelAsVolume.InLiters : 100;
                PreRacePredictionWithSetStartingFuel(requiredVirtualEnergy, startingVirtualFuel);

                if (StrategyViewModelConcrete.IsPitStopRequired && StrategyViewModelConcrete.PitStopCount != optimalStopCount)
                {
                    StrategyViewModelConcrete.IsStrategyValid = false;
                    StrategyViewModelConcrete.StrategyRemark = $"Too many Stops, Optimal is {optimalStopCount}.";
                }

                return;
            }
            
            if (VirtualEnergyFuelCoef == null)
            {
                return;
            }

            double virtualEnergyCoef = VirtualEnergyFuelCoef.Value;

            if (requiredVirtualEnergy <= 100)
            {
                double requiredFuel = requiredVirtualEnergy * virtualEnergyCoef;
                StrategyViewModelConcrete.AutomaticFuelVolume = Volume.FromLiters(requiredFuel);
                StrategyViewModelConcrete.AutomaticVirtualEnergyPercentage = (int)Math.Ceiling(requiredVirtualEnergy);
                StrategyViewModelConcrete.StartFuelLaps = GetLaps(StrategyViewModelConcrete.AutomaticFuelVolume);
                StrategyViewModelConcrete.IsStrategyValid = true;
                StrategyViewModelConcrete.StrategyRemark = "No Refueling Required";
                StrategyViewModelConcrete.IsPitStopRequired = false;
                return;
            }

            StrategyViewModelConcrete.IsPitStopRequired = true;
            StrategyViewModelConcrete.IsStrategyValid = true;
            StrategyViewModelConcrete.StrategyRemark = string.Empty;
            double pitStopVirtualEnergy = UpdatePreRacePitCount(requiredVirtualEnergy, 0);

            StrategyViewModelConcrete.AutomaticVirtualEnergyPercentage = StrategyViewModelConcrete.StintLength.Value == StintLengthKind.LastShort ? 100 : (int)Math.Ceiling(pitStopVirtualEnergy);
            StrategyViewModelConcrete.AutomaticFuelVolume = StrategyViewModelConcrete.StintLength.Value == StintLengthKind.LastShort
                ? Volume.FromLiters(100 * virtualEnergyCoef) : Volume.FromLiters(pitStopVirtualEnergy * virtualEnergyCoef);
            StrategyViewModelConcrete.StartFuelLaps = GetLaps(StrategyViewModelConcrete.AutomaticFuelVolume);
        }

        private void PreRacePredictionWithSetStartingFuel(double requiredVirtualEnergy, double startingVirtualEnergy)
        {
            if (VirtualEnergyFuelCoef == null)
            {
                return;
            }

            double virtualEnergyCoef = VirtualEnergyFuelCoef.Value;
            
            double startingFuelInLiters = startingVirtualEnergy * virtualEnergyCoef;
            StrategyViewModelConcrete.CustomStartingFuel = Volume.FromLiters(startingFuelInLiters);
            StrategyViewModelConcrete.StartFuelLaps = GetLaps(Volume.FromLiters(startingFuelInLiters));
            StrategyViewModelConcrete.AutomaticFuelVolume = Volume.FromLiters(startingFuelInLiters);
            StrategyViewModelConcrete.AutomaticVirtualEnergyPercentage = (int)Math.Ceiling(startingVirtualEnergy);
            if (startingVirtualEnergy >= requiredVirtualEnergy)
            {
                StrategyViewModelConcrete.IsStrategyValid = true;
                StrategyViewModelConcrete.StrategyRemark = "No Refueling Required";
                StrategyViewModelConcrete.IsPitStopRequired = false;
                return;
            }

            StrategyViewModelConcrete.IsPitStopRequired = true;
            StrategyViewModelConcrete.IsStrategyValid = true;
            StrategyViewModelConcrete.StrategyRemark = string.Empty;
            requiredVirtualEnergy -= startingVirtualEnergy - _virtualTankConsumptionInfo.GetAveragePerDistance(TrackInfo.LayoutLength).InPercentage;
            UpdatePreRacePitCount(requiredVirtualEnergy, 1);
        }

        private double UpdatePreRacePitCount(double requiredVirtualEnergy, int extraStops)
        {
            if (VirtualEnergyFuelCoef == null)
            {
                return 0;
            }

            double virtualEnergyCoef = VirtualEnergyFuelCoef.Value;

            double stintVirtualEnergyCapacity = GetStintVirtualEnergyCapacity(_virtualTankConsumptionInfo);
            int pitStopCount = (int)Math.Ceiling(requiredVirtualEnergy / stintVirtualEnergyCapacity);
            StrategyViewModelConcrete.PitStopCount = pitStopCount - 1 + extraStops;

            if (StrategyViewModelConcrete.StintLength.Value == StintLengthKind.Equal)
            {
                double virtualEnergyPerStop = requiredVirtualEnergy / pitStopCount;
                Volume perPitFuel = Volume.FromLiters(virtualEnergyPerStop * virtualEnergyCoef);
                StrategyViewModelConcrete.StopDescription = "All Stops +";
                StrategyViewModelConcrete.PitStopVolumeLabel = $"VE {(int)Math.Ceiling(virtualEnergyPerStop)}% / {_quantityFormatter.Format(perPitFuel)}";
                StrategyViewModelConcrete.PitStopLapCount = GetLaps(perPitFuel);
                return virtualEnergyPerStop;
            }

            double lastStopVirtualEnergy = Math.Ceiling(requiredVirtualEnergy - ((pitStopCount - 1) * stintVirtualEnergyCapacity));
            Volume lastPitFuel = Volume.FromLiters(lastStopVirtualEnergy * virtualEnergyCoef);
            StrategyViewModelConcrete.StopDescription = "Last Stop +";
            StrategyViewModelConcrete.PitStopVolumeLabel = $"VE {(int)Math.Ceiling(lastStopVirtualEnergy)}% / {_quantityFormatter.Format(lastPitFuel)}";
            StrategyViewModelConcrete.PitStopLapCount = GetLaps(lastPitFuel);
            return lastStopVirtualEnergy;
        }

        private void ClearPitStopInformationFromViewModel()
        {
            PitStopsViewModelConcrete.IsNextPitStopInfoVisible = false;
            PitStopsViewModelConcrete.NextPitStopState = RefuelingWindowState.Closed;
            PitStopsViewModelConcrete.NextPitStopInfo = string.Empty;
            PitStopsViewModelConcrete.IsFuelToAddVisible = false;
            PitStopsViewModelConcrete.IsPitStopCountVisible = false;
            PitStopsViewModelConcrete.IsLastStopFuelToAddVisible = false;
            PitStopsViewModelConcrete.RefuelingWindowState = RefuelingWindowState.Closed;
        }

        private double GetStintVirtualEnergyCapacity(VirtualTankConsumptionInfo virtualTankConsumptionInfo)
        {
            return Math.Floor((100 - (virtualTankConsumptionInfo.GetAveragePerMinute().InPercentage * 0.333)) / virtualTankConsumptionInfo.GetAveragePerDistance(TrackInfo.LayoutLength).InPercentage)
                   * virtualTankConsumptionInfo.GetAveragePerDistance(TrackInfo.LayoutLength).InPercentage;
        }
    }
}
