namespace SecondMonitor.ViewModels.FuelConsumption.FuelStrategy.Stint
{
    using System;

    using NLog;

    using SecondMonitor.Contracts.PitStatistics;
    using SecondMonitor.Contracts.Session;
    using SecondMonitor.DataModel;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Summary.FuelStrategy;
    using SecondMonitor.DataModel.Summary.FuelStrategy.TimedStintStrategy;
    using SecondMonitor.ViewModels.CarStatus;
    using SecondMonitor.ViewModels.CarStatus.FuelStatus;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.SessionEvents;
    using SecondMonitor.ViewModels.Settings;

    public class TimedStintStrategy : AbstractFuelStrategy<TimedStintStrategyViewModel, StintPitStopViewModel>, IFuelStrategy
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly TimeSpan stintTimeReserve = TimeSpan.FromSeconds(30);
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly IPaceProvider _paceProvider;
        private readonly ISessionInformationProvider _sessionInformationProvider;
        private readonly SessionRemainingCalculator _sessionRemainingCalculator;
        private bool _wasAutoSet;

        public TimedStintStrategy(ISessionEventProvider sessionEventProvider, ISettingsProvider settingsProvider,
            IPaceProvider paceProvider, IViewModelFactory viewModelFactory, ISessionInformationProvider sessionInformationProvider,
            IPitStopStatisticProvider pitStopStatisticProvider,
            SessionRemainingCalculator sessionRemainingCalculator) : base(sessionEventProvider, settingsProvider, pitStopStatisticProvider, viewModelFactory)
        {
            _sessionEventProvider = sessionEventProvider;
            _paceProvider = paceProvider;
            _sessionInformationProvider = sessionInformationProvider;
            _sessionRemainingCalculator = sessionRemainingCalculator;
        }

        public override string StrategyName => "Timed Stints";
        public override int Priority => 5;

        public override bool IsEligible(SimulatorDataSet dataSet)
        {
            return dataSet.Source == SimulatorsNameMap.AccSimName && (dataSet.SessionInfo.SessionType != SessionType.Race || dataSet.PlayerInfo.StintTimeLeft > TimeSpan.Zero);
        }

        public override void LoadSettings(ClassFuelStrategy classFuelStrategy)
        {
            logger.Info($"Loading Class Fuel Strategy Settings");
            SimulatorDataSet dataSet = _sessionEventProvider.LastDataSet;
            if (dataSet != null && dataSet.SessionInfo.SessionType == SessionType.Race && dataSet.PlayerInfo.StintTimeLeft > TimeSpan.Zero &&
                dataSet.SessionInfo.SessionPhase == SessionPhase.Countdown)
            {
                classFuelStrategy.TimedStintStrategySettings.StintDurationMinutes = (int)dataSet.PlayerInfo.StintTimeLeft.TotalMinutes;
            }

            StrategyViewModelConcrete.FromModel(classFuelStrategy.TimedStintStrategySettings);
        }

        public override void SaveToSettings(ClassFuelStrategy classFuelStrategy)
        {
            classFuelStrategy.TimedStintStrategySettings = StrategyViewModelConcrete.SaveToNewModel();
        }

        public override void UpdateNextPitStopInfo(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel, ConsumptionMonitors consumptionMonitors)
        {
            FuelConsumptionMonitor fuelConsumptionMonitor = consumptionMonitors.FuelConsumptionMonitor;

            if (dataSet.SessionInfo.SessionType != SessionType.Race
                || fuelConsumptionMonitor.LapStartQuantityStatus == null
                || dataSet.PlayerInfo == null
                || fuelConsumptionMonitor.LapStartQuantityStatus.FuelLevel.InLiters < dataSet.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining.InLiters
                || !_paceProvider.PlayersPace.HasValue)
            {
                ClearPitStopInformationFromViewModel();
                return;
            }

            if (dataSet.PlayerInfo.InPits)
            {
                return;
            }

            TimeSpan stintTimeLeft = dataSet.PlayerInfo.StintTimeLeft - stintTimeReserve;
            PitStopsViewModelConcrete.IsStintTimeVisible = true;

            double lapsRemaining = _sessionRemainingCalculator.GetLapsRemaining(dataSet);
            if (double.IsNaN(lapsRemaining) || double.IsInfinity(lapsRemaining))
            {
                ClearPitStopInformationFromViewModel();
                return;
            }

            TimeSpan sessionTimeRemaining = TimeSpan.FromSeconds(lapsRemaining * _paceProvider.PlayersPace.Value.TotalSeconds);

            if (fuelOverviewViewModel.FuelDelta.InLiters >= 0
                || fuelOverviewViewModel.TimeDelta.TotalSeconds > 0
                || stintTimeLeft > sessionTimeRemaining)
            {
                ClearPitStopInformationFromViewModel();
                return;
            }

            PitStopsViewModelConcrete.CurrentStintTime = dataSet.PlayerInfo.StintTimeLeft.FormatToMinutesSeconds();
            TimeSpan playerLapTime = _sessionInformationProvider.GetDriverCurrentLapTime(dataSet.PlayerInfo);
            TimeSpan runTimeAtLapStart = TimeSpan.FromSeconds(Math.Min(stintTimeLeft.TotalSeconds + playerLapTime.TotalSeconds,
                (fuelConsumptionMonitor.LapStartQuantityStatus.FuelLevel.InLiters / fuelConsumptionMonitor.TotalPerMinute.InLiters) * 60 - 20));

            int lapsToGoAtLapStart = (int)(runTimeAtLapStart.TotalSeconds / _paceProvider.PlayersPace.Value.TotalSeconds);
            UpdateNextPitStopInfo(lapsToGoAtLapStart - 1, dataSet, fuelOverviewViewModel);

            Volume fuelAtStintEnd = Volume.FromLiters(fuelConsumptionMonitor.LapStartQuantityStatus.FuelLevel.InLiters -
                                                      fuelConsumptionMonitor.TotalPerLap.InLiters * lapsToGoAtLapStart);
            TimeSpan sessionTimeRemainingAtStintEnd = sessionTimeRemaining + playerLapTime - TimeSpan.FromSeconds(lapsToGoAtLapStart * _paceProvider.PlayersPace.Value.TotalSeconds);
            TimeSpan usableStintTime = EstimateRealStintRunTime(_paceProvider.PlayersPace.Value);
            TimeSpan nextStintRunTime = TimeSpan.FromSeconds(Math.Min(sessionTimeRemainingAtStintEnd.TotalSeconds, usableStintTime.TotalSeconds));

            int pitStopCount = (int)Math.Ceiling(sessionTimeRemainingAtStintEnd.TotalSeconds / usableStintTime.TotalSeconds);

            int regularStintTimeToUse = StrategyViewModelConcrete.SelectedStingLength.Value == StintLengthKind.Equal
                ? (int)Math.Ceiling(sessionTimeRemainingAtStintEnd.TotalMinutes / pitStopCount) : (int)Math.Ceiling(nextStintRunTime.TotalMinutes);

            PitStopsViewModelConcrete.IsFuelToAddVisible = true;
            PitStopsViewModelConcrete.PitStopCount = pitStopCount;
            PitStopsViewModelConcrete.IsPitStopCountVisible = pitStopCount > 1;

            double fuelForNextPitStopLiters = ((regularStintTimeToUse + 1) * fuelConsumptionMonitor.TotalPerMinute.InLiters) - fuelAtStintEnd.InLiters;
            fuelForNextPitStopLiters = Math.Max(0, fuelForNextPitStopLiters);

            if (PitStopsViewModelConcrete.PitStopCount <= 1)
            {
                PitStopsViewModelConcrete.NextStintTime = $"{Math.Min(regularStintTimeToUse, StrategyViewModelConcrete.StintDurationMinutes)}min";
                PitStopsViewModelConcrete.FuelForNextPitStopLabel = $"Add (Any Lap): ";
                fuelForNextPitStopLiters = -fuelOverviewViewModel.FuelDelta.InLiters;
                double fuelToAddWithoutContingency = fuelForNextPitStopLiters;
                fuelForNextPitStopLiters = AddExtraFuel(fuelForNextPitStopLiters, fuelConsumptionMonitor);
                PitStopsViewModelConcrete.FuelForNextPitStop = Volume.FromWholeLiters(Math.Max(fuelForNextPitStopLiters, 0));
                PitStopsViewModelConcrete.IsLastStopFuelToAddVisible = false;
                PitStopsViewModelConcrete.IsFuelToAddPitLapVisible = true;
                PitStopsViewModelConcrete.IsFuelToAddThisLapVisible = false;
                UpdateViewModelRefuelingState(dataSet, fuelOverviewViewModel, fuelToAddWithoutContingency, fuelConsumptionMonitor, pitStopCount, usableStintTime);
                return;
            }

            PitStopsViewModelConcrete.FuelForNextPitStopLabel = $"Add (Pit lap): ";
            PitStopsViewModelConcrete.NextStintTime = $"{Math.Min(regularStintTimeToUse, StrategyViewModelConcrete.StintDurationMinutes)}min";

            PitStopsViewModelConcrete.FuelForNextPitStop = Volume.FromWholeLiters(Math.Max(fuelForNextPitStopLiters, 0));
            PitStopsViewModelConcrete.FuelToAddThisLap = Volume.FromWholeLiters(fuelForNextPitStopLiters + fuelAtStintEnd.InLiters - (fuelConsumptionMonitor.LapStartQuantityStatus.FuelLevel.InLiters - fuelConsumptionMonitor.TotalPerLap.InLiters));
            PitStopsViewModelConcrete.IsLastStopFuelToAddVisible = true;
            PitStopsViewModelConcrete.IsFuelToAddPitLapVisible = lapsToGoAtLapStart <= 1 || StrategyViewModelConcrete.SelectedStingPitStopInfo.Value is StintPitStopInfo.Both or StintPitStopInfo.FuelAtPitLap;
            PitStopsViewModelConcrete.IsFuelToAddThisLapVisible = lapsToGoAtLapStart > 1 && StrategyViewModelConcrete.SelectedStingPitStopInfo.Value is StintPitStopInfo.Both or StintPitStopInfo.FuelAtEndOfLap;

            int lastStintTime = (int)Math.Ceiling((sessionTimeRemainingAtStintEnd.TotalMinutes - (regularStintTimeToUse * (pitStopCount - 1))));
            double lastStopFuel = (lastStintTime * fuelConsumptionMonitor.TotalPerMinute.InLiters); //- fuelAtStintEnd.InLiters;
            lastStopFuel = AddExtraFuel(lastStopFuel, fuelConsumptionMonitor);
            PitStopsViewModelConcrete.FuelToAddLastStopLabel = "Last stint: ";
            PitStopsViewModelConcrete.LastStintTime = $"{lastStintTime}min";
            PitStopsViewModelConcrete.FuelToAddLastStop = Volume.FromWholeLiters(lastStopFuel);
            UpdateViewModelRefuelingState(dataSet, fuelOverviewViewModel, fuelForNextPitStopLiters, fuelConsumptionMonitor, pitStopCount, usableStintTime);
        }

        private double AddExtraFuel(double fuelInLiter, FuelConsumptionMonitor fuelConsumptionMonitor)
        {
            fuelInLiter += DisplaySettingsViewModel.ExtraRaceLaps * fuelConsumptionMonitor.TotalPerLap.InLiters;
            fuelInLiter += DisplaySettingsViewModel.ExtraRaceMinutes * fuelConsumptionMonitor.TotalPerMinute.InLiters;
            fuelInLiter = Math.Max(fuelInLiter, 0);
            fuelInLiter *= 1 + DisplaySettingsViewModel.ExtraContingencyFuel / 100.0;
            return fuelInLiter;
        }

        private void UpdateViewModelRefuelingState(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel, double fuelToAddWithoutContingency,
            FuelConsumptionMonitor fuelConsumptionMonitor, int desiredPitStopsCount, TimeSpan usableStintTime)
        {
            if ((int)(dataSet.SessionInfo.SessionTimeRemaining / usableStintTime.TotalSeconds) >= desiredPitStopsCount)
            {
                PitStopsViewModelConcrete.RefuelingWindowState = RefuelingWindowState.Closed;
                return;
            }

            double fuelRemainingInLiters = dataSet.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining.InLiters;

            if (fuelRemainingInLiters + PitStopsViewModelConcrete.FuelForNextPitStop.InLiters < FuelTankSize.InLiters
                || fuelRemainingInLiters <= fuelConsumptionMonitor.TotalPerLap.InLiters)
            {
                PitStopsViewModelConcrete.RefuelingWindowState = RefuelingWindowState.WithContingency;
            }
            else if (fuelRemainingInLiters + fuelToAddWithoutContingency < FuelTankSize.InLiters && PitStopsViewModelConcrete.PitStopCount == 1)
            {
                PitStopsViewModelConcrete.RefuelingWindowState = RefuelingWindowState.NoContingency;
            }
            else
            {
                PitStopsViewModelConcrete.RefuelingWindowState = RefuelingWindowState.Closed;
            }
        }

        public override void UpdateInRaceFuelPrediction(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel, ConsumptionMonitors consumptionMonitors)
        {
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
            PitStopsViewModelConcrete.IsStintTimeVisible = false;
        }

        protected override void UpdatePreRaceCalculation(SimulatorDataSet dataSet)
        {
            double raceLengthMinutes = dataSet.SessionInfo.SessionType == SessionType.Race ? dataSet.SessionInfo.SessionTimeRemaining / 60 : RequiredFuel.InLiters / FuelConsumptionInfo.GetAveragePerMinute().InLiters;
            double estimatedLapTimeMinutes = FuelPerLap.InLiters / FuelConsumptionInfo.GetAveragePerMinute().InLiters;
            if (double.IsNaN(estimatedLapTimeMinutes) || double.IsInfinity(estimatedLapTimeMinutes))
            {
                return;
            }

            if (dataSet.SessionInfo.SessionType == SessionType.Race && dataSet.SessionInfo.SessionPhase == SessionPhase.Countdown && dataSet.PlayerInfo.StintTimeLeft.TotalMinutes > 1 && !_wasAutoSet)
            {
                logger.Info($"Setting Stint Duration to {(int)dataSet.PlayerInfo.StintTimeLeft.TotalMinutes}");
                _wasAutoSet = true;
                StrategyViewModelConcrete.StintDurationMinutes = (int)dataSet.PlayerInfo.StintTimeLeft.TotalMinutes;
            }

            if (dataSet.SessionInfo.SessionType != SessionType.Race && _wasAutoSet)
            {
                _wasAutoSet = false;
            }

            TimeSpan estimatedLapTime = TimeSpan.FromMinutes(FuelPerLap.InLiters / FuelConsumptionInfo.GetAveragePerMinute().InLiters);
            double estimatedRealStintTimeMinutes = EstimateRealStintRunTime(estimatedLapTime).TotalMinutes;
            int requiredStints = (int)Math.Ceiling(raceLengthMinutes / estimatedRealStintTimeMinutes);

            int regularStintTimeToUse = StrategyViewModelConcrete.SelectedStingLength.Value == StintLengthKind.Equal || requiredStints == 1 ? (int)Math.Ceiling(raceLengthMinutes / requiredStints) : (int)Math.Ceiling(estimatedRealStintTimeMinutes);
            Volume requiredFuel = (regularStintTimeToUse + 1) * FuelConsumptionInfo.GetAveragePerMinute();
            int laps = GetLaps(requiredFuel);

            StrategyViewModelConcrete.RegularStintMinutes = Math.Min(regularStintTimeToUse, StrategyViewModelConcrete.StintDurationMinutes);
            StrategyViewModelConcrete.RegularStintRequiresFuel = Volume.FromWholeLiters(requiredFuel.InLiters);
            StrategyViewModelConcrete.RegularStintLaps = laps;
            StrategyViewModelConcrete.IsStrategyValid = true;
            StrategyViewModelConcrete.StrategyRemark = string.Empty;

            if (StrategyViewModelConcrete.SelectedStingLength.Value == StintLengthKind.Equal || requiredStints == 1)
            {
                StrategyViewModelConcrete.RegularStintCount = requiredStints;
                StrategyViewModelConcrete.ShowLastStintInfo = false;
                return;
            }

            int lastStintTime = (int)Math.Ceiling((raceLengthMinutes - (regularStintTimeToUse * (requiredStints - 1))));
            requiredFuel = lastStintTime * FuelConsumptionInfo.GetAveragePerMinute();
            if (dataSet.SessionInfo.SessionType == SessionType.Race)
            {
                requiredFuel += DisplaySettingsViewModel.ExtraRaceMinutes * FuelConsumptionInfo.GetAveragePerMinute();
                requiredFuel += DisplaySettingsViewModel.ExtraRaceLaps * FuelPerLap;
                requiredFuel *= 1 + DisplaySettingsViewModel.ExtraContingencyFuel;
            }

            laps = GetLaps(requiredFuel);

            StrategyViewModelConcrete.RegularStintCount = requiredStints - 1;
            StrategyViewModelConcrete.ShowLastStintInfo = true;
            StrategyViewModelConcrete.LastStintMinutes = lastStintTime;
            StrategyViewModelConcrete.LastStintFuel = Volume.FromWholeLiters(requiredFuel.InLiters);
            StrategyViewModelConcrete.LastStintLaps = laps;
        }

        private TimeSpan EstimateRealStintRunTime(TimeSpan estimatedLapTime)
        {
            int lapsPerStint = (int)(StrategyViewModelConcrete.StintDurationMinutes / estimatedLapTime.TotalMinutes);
            double estimatedRealStintTimeMinutes = (lapsPerStint * estimatedLapTime.TotalMinutes) + 0.75;
            return double.IsInfinity(estimatedRealStintTimeMinutes) || double.IsNaN(estimatedRealStintTimeMinutes) ? TimeSpan.Zero : TimeSpan.FromMinutes(estimatedRealStintTimeMinutes);
        }
    }
}
