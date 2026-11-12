namespace SecondMonitor.ViewModels.FuelConsumption.FuelStrategy.TankToTank
{
    using System;

    using SecondMonitor.Contracts.PitStatistics;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Summary.FuelStrategy;
    using SecondMonitor.ViewModels.CarStatus.FuelStatus;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.SessionEvents;
    using SecondMonitor.ViewModels.Settings;

    public class TankToTankStrategy : AbstractFuelStrategy<TankToTankViewModel, StandardPitStopViewModel>, IFuelStrategy
    {
        private readonly VirtualEnergyCondition _virtualEnergyCondition;
        public TankToTankStrategy(IViewModelFactory viewModelFactory, ISessionEventProvider sessionEventProvider, IPitStopStatisticProvider pitStopStatisticProvider,
            ISettingsProvider settingsProvider, VirtualEnergyCondition virtualEnergyCondition) : base(sessionEventProvider, settingsProvider, pitStopStatisticProvider, viewModelFactory)
        {
            _virtualEnergyCondition = virtualEnergyCondition;
        }

        public override string StrategyName => "Tank to Tank";
        public override int Priority => 0;

        public override bool IsEligible(SimulatorDataSet dataSet) => !_virtualEnergyCondition.HasVirtualEnergy(dataSet);

        public override void LoadSettings(ClassFuelStrategy classFuelStrategy)
        {
            StrategyViewModelConcrete.FromModel(classFuelStrategy.TankToTankStrategySettings);
        }

        public override void SaveToSettings(ClassFuelStrategy classFuelStrategy)
        {
            classFuelStrategy.TankToTankStrategySettings = StrategyViewModelConcrete.SaveToNewModel();
        }

        public override void UpdateInRaceFuelPrediction(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel, ConsumptionMonitors consumptionMonitors)
        {
            UpdateInRaceFuelPrediction(dataSet, fuelOverviewViewModel, StrategyViewModelConcrete.StintLength.Value, consumptionMonitors.FuelConsumptionMonitor);
        }

        protected override void UpdatePreRaceCalculation(SimulatorDataSet dataSet)
        {
            double requiredFuel = RequiredFuel.InLiters;
            double stintFuelCapacity = FuelTankSize.InLiters -
                                       FuelConsumptionInfo.GetAveragePerDistance(TrackInfo.LayoutLength).InLiters;
            int optimalStopCount = (int)(requiredFuel / stintFuelCapacity);

            if (StrategyViewModelConcrete.StartFuel.Value != FuelLoadKind.Auto)
            {
                double startingFuel = StrategyViewModelConcrete.StartFuel.Value == FuelLoadKind.Custom ? StrategyViewModelConcrete.CustomStartingFuelAsVolume.InLiters : FuelTankSize.InLiters;
                PreRacePredictionWithSetStartingFuel(requiredFuel, startingFuel);

                if (StrategyViewModelConcrete.IsPitStopRequired && StrategyViewModelConcrete.PitStopCount != optimalStopCount)
                {
                    StrategyViewModelConcrete.IsStrategyValid = false;
                    StrategyViewModelConcrete.StrategyRemark = $"Too many Stops, Optimal is {optimalStopCount}.";
                }

                return;
            }

            if (FuelTankSize.InLiters >= requiredFuel)
            {
                StrategyViewModelConcrete.AutomaticFuelVolume = Volume.FromLiters(requiredFuel);
                StrategyViewModelConcrete.StartFuelLaps = GetLaps(StrategyViewModelConcrete.AutomaticFuelVolume);
                StrategyViewModelConcrete.IsStrategyValid = true;
                StrategyViewModelConcrete.StrategyRemark = "No Refueling Required";
                StrategyViewModelConcrete.IsPitStopRequired = false;
                return;
            }

            StrategyViewModelConcrete.IsPitStopRequired = true;
            StrategyViewModelConcrete.IsStrategyValid = true;
            StrategyViewModelConcrete.StrategyRemark = string.Empty;
            UpdatePreRacePitCount(requiredFuel, 0);

            StrategyViewModelConcrete.AutomaticFuelVolume = StrategyViewModelConcrete.StintLength.Value == StintLengthKind.LastShort ? FuelTankSize : StrategyViewModelConcrete.PitStopVolume;
            StrategyViewModelConcrete.StartFuelLaps = GetLaps(StrategyViewModelConcrete.AutomaticFuelVolume);
        }

        private void PreRacePredictionWithSetStartingFuel(double requiredFuel, double startingFuelInLiters)
        {
            StrategyViewModelConcrete.StartFuelLaps = GetLaps(Volume.FromLiters(startingFuelInLiters));
            StrategyViewModelConcrete.AutomaticFuelVolume = Volume.FromLiters(startingFuelInLiters);
            if (startingFuelInLiters >= requiredFuel)
            {
                StrategyViewModelConcrete.IsStrategyValid = true;
                StrategyViewModelConcrete.StrategyRemark = "No Refueling Required";
                StrategyViewModelConcrete.IsPitStopRequired = false;
                return;
            }

            StrategyViewModelConcrete.IsPitStopRequired = true;
            StrategyViewModelConcrete.IsStrategyValid = true;
            StrategyViewModelConcrete.StrategyRemark = string.Empty;
            requiredFuel -= (startingFuelInLiters - FuelConsumptionInfo.GetAveragePerDistance(TrackInfo.LayoutLength).InLiters);
            UpdatePreRacePitCount(requiredFuel, 1);
        }

        private void UpdatePreRacePitCount(double requiredFuel, int extraStops)
        {
            double stintFuelCapacity = FuelTankSize.InLiters -
                                       FuelConsumptionInfo.GetAveragePerDistance(TrackInfo.LayoutLength).InLiters;
            int pitStopCount = (int)Math.Ceiling(requiredFuel / stintFuelCapacity);
            StrategyViewModelConcrete.PitStopCount = pitStopCount - 1 + extraStops;

            if (StrategyViewModelConcrete.StintLength.Value == StintLengthKind.Equal)
            {
                Volume perPitFuel = Volume.FromLiters(Math.Ceiling(requiredFuel / pitStopCount));
                StrategyViewModelConcrete.StopDescription = "All Stops +";
                StrategyViewModelConcrete.PitStopVolume = perPitFuel;
                StrategyViewModelConcrete.PitStopLapCount = GetLaps(perPitFuel);
            }
            else
            {
                Volume lastPitFuel = Volume.FromLiters(Math.Ceiling((requiredFuel) - ((pitStopCount - 1) * stintFuelCapacity)));
                StrategyViewModelConcrete.StopDescription = "Last Stop +";
                StrategyViewModelConcrete.PitStopVolume = lastPitFuel;
                StrategyViewModelConcrete.PitStopLapCount = GetLaps(lastPitFuel);
            }
        }
    }
}
