namespace SecondMonitor.ViewModels.FuelConsumption.FuelStrategy.PitWindow
{
    using System;

    using SecondMonitor.Contracts.PitStatistics;
    using SecondMonitor.Contracts.Session;
    using SecondMonitor.DataModel;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Summary.FuelStrategy;
    using SecondMonitor.ViewModels.CarStatus;
    using SecondMonitor.ViewModels.CarStatus.FuelStatus;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.SessionEvents;
    using SecondMonitor.ViewModels.Settings;

    public class PitWindowStrategy : AbstractFuelStrategy<PitWindowStrategyViewModel, StandardPitStopViewModel>, IFuelStrategy
    {
        private readonly ISessionInformationProvider _sessionInformationProvider;
        private readonly IPaceProvider _paceProvider;

        public PitWindowStrategy(IViewModelFactory viewModelFactory, ISessionEventProvider sessionEventProvider, ISettingsProvider settingsProvider,
            ISessionInformationProvider sessionInformationProvider, IPaceProvider paceProvider, IPitStopStatisticProvider pitStopStatisticProvider) : base(sessionEventProvider,
            settingsProvider, pitStopStatisticProvider, viewModelFactory)
        {
            _sessionInformationProvider = sessionInformationProvider;
            _paceProvider = paceProvider;
        }

        public override string StrategyName => "Pit Window";
        public override int Priority => 2;

        public override bool IsEligible(SimulatorDataSet dataSet) => dataSet.Source != SimulatorsNameMap.LmUSimName &&
                                                                     (dataSet.SessionInfo.SessionType != SessionType.Race ||
                                                                      dataSet.SessionInfo.PitWindow.PitWindowState != PitWindowState.None);

        public override void LoadSettings(ClassFuelStrategy classFuelStrategy)
        {
            StrategyViewModelConcrete.FromModel(classFuelStrategy.PitWindowStrategySettings);
        }

        public override void SaveToSettings(ClassFuelStrategy classFuelStrategy)
        {
            classFuelStrategy.PitWindowStrategySettings = StrategyViewModelConcrete.SaveToNewModel();
        }

        public override void UpdateInRaceFuelPrediction(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel, ConsumptionMonitors consumptionMonitors)
        {
            UpdateInRaceFuelPrediction(dataSet, fuelOverviewViewModel, StintLengthKind.LastShort, consumptionMonitors.FuelConsumptionMonitor);
        }

        public override void UpdateNextPitStopInfo(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel, ConsumptionMonitors consumptionMonitors)
        {
            FuelConsumptionMonitor fuelConsumptionMonitor = consumptionMonitors.FuelConsumptionMonitor;
            if (dataSet.SessionInfo.SessionType != SessionType.Race
                || fuelConsumptionMonitor.LapStartQuantityStatus == null
                || dataSet.PlayerInfo == null
                || fuelConsumptionMonitor.LapStartQuantityStatus.FuelLevel.InLiters < dataSet.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining.InLiters)
            {
                PitStopsViewModelConcrete.IsNextPitStopInfoVisible = false;
                PitStopsViewModelConcrete.NextPitStopState = RefuelingWindowState.Closed;
                PitStopsViewModelConcrete.NextPitStopInfo = string.Empty;
                return;
            }

            bool doesFuelExpireInPitWindow;
            if (dataSet.SessionInfo.SessionLengthType == SessionLengthType.Laps)
            {
                double lapsOfFuelAtLapStart = fuelConsumptionMonitor.LapStartQuantityStatus.FuelLevel.InLiters / fuelConsumptionMonitor.TotalPerLap.InLiters;
                double lapsLeftInPitWindow = dataSet.SessionInfo.PitWindow.PitWindowEnd - dataSet.PlayerInfo.CompletedLaps;
                doesFuelExpireInPitWindow = lapsOfFuelAtLapStart < lapsLeftInPitWindow;
            }
            else
            {
                TimeSpan pitWindowLeft = TimeSpan.FromMinutes(dataSet.SessionInfo.PitWindow.PitWindowEnd) - dataSet.SessionInfo.SessionTime;
                doesFuelExpireInPitWindow = pitWindowLeft > fuelOverviewViewModel.TimeLeft;
            }

            if (doesFuelExpireInPitWindow || _sessionInformationProvider.HasDriverCompletedPitWindowStop(dataSet.PlayerInfo))
            {
                base.UpdateNextPitStopInfo(dataSet, fuelOverviewViewModel, consumptionMonitors);
                return;
            }

            if (dataSet.SessionInfo.SessionLengthType == SessionLengthType.Laps)
            {
                UpdateNextPitStopInfoLapWindow(dataSet, fuelOverviewViewModel);
            }
            else
            {
                UpdateNextPitStopInfoTimedWindow(dataSet, fuelOverviewViewModel);
            }
        }

        private void UpdateNextPitStopInfoTimedWindow(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel)
        {
            if (!_paceProvider.PlayersPace.HasValue || _paceProvider.PlayersPace.Value == TimeSpan.Zero)
            {
                PitStopsViewModelConcrete.IsNextPitStopInfoVisible = false;
                PitStopsViewModelConcrete.NextPitStopState = RefuelingWindowState.Closed;
                PitStopsViewModelConcrete.NextPitStopInfo = string.Empty;
                return;
            }

            TimeSpan pitWindowLeft = TimeSpan.FromMinutes(dataSet.SessionInfo.PitWindow.PitWindowEnd) - dataSet.SessionInfo.SessionTime;
            TimeSpan pitWindowAtLapStart = pitWindowLeft + _sessionInformationProvider.GetDriverCurrentLapTime(dataSet.PlayerInfo);

            int lapsToGoAtLapStart = (int)(pitWindowAtLapStart.TotalSeconds / _paceProvider.PlayersPace.Value.TotalSeconds);
            UpdateNextPitStopInfo(lapsToGoAtLapStart - 1, dataSet, fuelOverviewViewModel);
        }

        private void UpdateNextPitStopInfoLapWindow(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel)
        {
            int lapsLeftInPitWindow = (int)dataSet.SessionInfo.PitWindow.PitWindowEnd - dataSet.PlayerInfo.CompletedLaps;
            UpdateNextPitStopInfo(lapsLeftInPitWindow - 1, dataSet, fuelOverviewViewModel);
        }

        protected override void UpdatePreRaceCalculation(SimulatorDataSet dataSet)
        {
            if (!StrategyViewModelConcrete.IsRefuelingAllowed)
            {
                UpdatePreRaceCalculationNoPitStop();
                return;
            }

            if (StrategyViewModelConcrete.StartFuel.Value == FuelLoadKind.Custom)
            {
                StrategyViewModelConcrete.StartFuelMinutes = GetMinutes(Volume.FromUnits(StrategyViewModelConcrete.StartFuelCustom, DisplaySettingsViewModel.VolumeUnits));
            }

            if (StrategyViewModelConcrete.PitWindowFuel.Value == FuelLoadKind.Custom)
            {
                StrategyViewModelConcrete.PitFuelMinutes = GetMinutes(Volume.FromUnits(StrategyViewModelConcrete.PitWindowFuelCustom, DisplaySettingsViewModel.VolumeUnits));
            }

            PitWindowInformation pitWindow = dataSet.SessionInfo.PitWindow;
            double pitWindowDuration = pitWindow.PitWindowEnd - pitWindow.PitWindowStart;
            double sessionTimeOffset = dataSet.SessionInfo.SessionType == SessionType.Race ? dataSet.SessionInfo.SessionTime.TotalMinutes : 0;
            double desiredPitTime = pitWindow.PitWindowStart - sessionTimeOffset + (pitWindowDuration * StrategyViewModelConcrete.PitWindowPercentage / 100.0);
            Volume fuelRequiredBeforePit = Volume.FromLiters(desiredPitTime * FuelConsumptionInfo.GetAveragePerMinute().InLiters + FuelPerLap.InLiters * 0.5);
            Volume fuelRequiredAfterPit = RequiredFuel - fuelRequiredBeforePit;

            if (fuelRequiredAfterPit > FuelTankSize)
            {
                StrategyViewModelConcrete.IsStrategyValid = false;
                StrategyViewModelConcrete.StrategyRemark = "Cannot complete distance from pit stop to end of race on one fuel tank.";
                return;
            }

            Volume fuelInTankAtPitStop = GetFuelInTankAtPitStop(fuelRequiredBeforePit);
            Volume idealStopFuel = GetIdealStopFuel(fuelRequiredAfterPit, fuelInTankAtPitStop);

            if (idealStopFuel.InLiters < 0)
            {
                idealStopFuel = Volume.FromLiters(0);
            }

            if (StrategyViewModelConcrete.PitWindowFuel.Value == FuelLoadKind.Custom && idealStopFuel > FuelTankSize)
            {
                StrategyViewModelConcrete.IsStrategyValid = false;
                StrategyViewModelConcrete.StrategyRemark = $"Unable to refuel required amount.\nFuel tank size is {FuelTankSize.GetValueInUnits(DisplaySettingsViewModel.VolumeUnits):F1}{Volume.GetUnitSymbol(DisplaySettingsViewModel.VolumeUnits)}";
                return;
            }

            Volume idealStartFuel = GetIdealStartFuel(fuelRequiredBeforePit);

            if (idealStartFuel > FuelTankSize)
            {
                StrategyViewModelConcrete.IsStrategyValid = false;
                StrategyViewModelConcrete.StrategyRemark = "Cannot complete distance until stop on one fuel tank.";
                return;
            }

            bool isAchievable = IsAchievable(fuelRequiredBeforePit, idealStopFuel, fuelRequiredAfterPit, ref idealStartFuel);

            StrategyViewModelConcrete.StartFuelMinutes = GetMinutes(idealStartFuel);
            StrategyViewModelConcrete.AutomaticStartFuelVolume = idealStartFuel;
            StrategyViewModelConcrete.PitFuelMinutes = GetMinutes(idealStopFuel);
            StrategyViewModelConcrete.AutomaticPitFuelVolume = idealStopFuel;

            if (!isAchievable)
            {
                StrategyViewModelConcrete.IsStrategyValid = false;
                StrategyViewModelConcrete.StrategyRemark = "Not achievable.";
                return;
            }

            StrategyViewModelConcrete.IsStrategyValid = true;
            Volume excessFuel = (idealStartFuel + idealStopFuel) - RequiredFuel;
            if (excessFuel > FuelPerLap && StrategyViewModelConcrete.PitWindowFuel.Value == FuelLoadKind.Custom && pitWindowDuration > 0)
            {
                Volume recommendedStartingFuel = RequiredFuel - idealStopFuel;
                int recommendedStartingFuelMinutes = GetMinutes(recommendedStartingFuel);
                double recommendedPitTime = Math.Max(1, recommendedStartingFuelMinutes - pitWindow.PitWindowStart);
                int recommendedPitPercentage = (int)((recommendedPitTime / pitWindowDuration) * 100);
                bool isSoonerPitPossible = (RequiredFuel - recommendedStartingFuel) < FuelTankSize;
                StrategyViewModelConcrete.StrategyRemark = StrategyViewModelConcrete.PitWindowPercentage != recommendedPitPercentage && isSoonerPitPossible ?
                    $"Excess Fuel ({GetLaps(excessFuel)}laps).\nRecommended pit {recommendedPitPercentage}% into the pit window for optimal fuel."
                    : $"Excess Fuel ({GetLaps(excessFuel)}laps).\nLess fuel can be refueled during pit stop.";
            }
            else
            {
                StrategyViewModelConcrete.StrategyRemark = string.Empty;
            }
        }

        private bool IsAchievable(Volume fuelRequiredBeforePit, Volume idealStopFuel, Volume fuelRequiredAfterPit, ref Volume idealStartFuel)
        {
            bool isAchievable = false;
            if (idealStartFuel >= fuelRequiredBeforePit && (idealStopFuel >= fuelRequiredAfterPit || StrategyViewModelConcrete.PitWindowFuel.Value != FuelLoadKind.Custom))
            {
                isAchievable = true;
            }
            else if (idealStopFuel < fuelRequiredAfterPit && StrategyViewModelConcrete.StartFuel.Value != FuelLoadKind.Custom)
            {
                if (StrategyViewModelConcrete.StartFuel.Value == FuelLoadKind.FullTank)
                {
                    Volume volumeLeftAfterPitStop = FuelTankSize - fuelRequiredBeforePit;
                    isAchievable = fuelRequiredAfterPit <= idealStopFuel + volumeLeftAfterPitStop;
                }
                else
                {
                    Volume requiredFuelAtPitStop = fuelRequiredAfterPit - idealStopFuel;
                    idealStartFuel += requiredFuelAtPitStop;
                    isAchievable = idealStartFuel <= FuelTankSize;
                }
            }

            return isAchievable;
        }

        private Volume GetIdealStartFuel(Volume fuelRequiredBeforePit)
        {
            Volume idealStartFuel;
            switch (StrategyViewModelConcrete.StartFuel.Value)
            {
                case FuelLoadKind.Auto:
                    idealStartFuel = Volume.FromLiters(Math.Min(fuelRequiredBeforePit.InLiters, FuelTankSize.InLiters));
                    break;
                case FuelLoadKind.FullTank:
                    idealStartFuel = FuelTankSize;
                    break;
                case FuelLoadKind.Custom:
                    idealStartFuel = Volume.FromUnits(StrategyViewModelConcrete.StartFuelCustom, DisplaySettingsViewModel.VolumeUnits);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return idealStartFuel;
        }

        private Volume GetIdealStopFuel(Volume fuelRequiredAfterPit, Volume fuelInTankAtPitStop)
        {
            Volume idealStopFuel;
            switch (StrategyViewModelConcrete.PitWindowFuel.Value)
            {
                case FuelLoadKind.Auto:
                    idealStopFuel = fuelRequiredAfterPit - fuelInTankAtPitStop;
                    break;
                case FuelLoadKind.FullTank:
                    idealStopFuel = FuelTankSize;
                    break;
                case FuelLoadKind.Custom:
                    idealStopFuel = Volume.FromUnits(StrategyViewModelConcrete.PitWindowFuelCustom, DisplaySettingsViewModel.VolumeUnits);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return idealStopFuel;
        }

        private Volume GetFuelInTankAtPitStop(Volume fuelRequiredBeforePit)
        {
            Volume fuelInTankAtPitStop;
            switch (StrategyViewModelConcrete.StartFuel.Value)
            {
                case FuelLoadKind.Auto:
                    fuelInTankAtPitStop = Volume.FromLiters(0);
                    break;
                case FuelLoadKind.FullTank:
                    fuelInTankAtPitStop = FuelTankSize - fuelRequiredBeforePit;
                    break;
                case FuelLoadKind.Custom:
                    fuelInTankAtPitStop = Volume.FromLiters(StrategyViewModelConcrete.StartFuelCustom) - fuelRequiredBeforePit;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return fuelInTankAtPitStop;
        }

        private void UpdatePreRaceCalculationNoPitStop()
        {
            Volume startVolume;
            switch (StrategyViewModelConcrete.StartFuel.Value)
            {
                case FuelLoadKind.Auto:
                    startVolume = Volume.FromLiters(Math.Min(RequiredFuel.InLiters, FuelTankSize.InLiters));
                    break;
                case FuelLoadKind.FullTank:
                    startVolume = FuelTankSize;
                    break;
                case FuelLoadKind.Custom:
                    startVolume = Volume.FromUnits(StrategyViewModelConcrete.StartFuelCustom, DisplaySettingsViewModel.VolumeUnits);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            StrategyViewModelConcrete.AutomaticStartFuelVolume = startVolume;
            StrategyViewModelConcrete.StartFuelMinutes = GetMinutes(startVolume);
            StrategyViewModelConcrete.IsStrategyValid = startVolume >= RequiredFuel;
            if (!StrategyViewModelConcrete.IsStrategyValid)
            {
                StrategyViewModelConcrete.StrategyRemark = "Not Enough Fuel.";
            }
        }
    }
}
