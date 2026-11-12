namespace SecondMonitor.ViewModels.FuelConsumption
{
    using System;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.DataModel.Snapshot.Systems;
    using SecondMonitor.ViewModels.CarStatus.FuelStatus;

    public class HybridConsumptionMonitor
    {
        private TimeSpan _nextTick = TimeSpan.Zero;
        private static readonly TimeSpan tickInterval = TimeSpan.FromMilliseconds(100);

        public int PreviousCompletedLaps { get; private set; }
        public double LapStartHybridState { get; private set; }

        public double CurrentHybridCharge { get; set; }
        public double PreviousLapHybridUsage { get; private set; }
        public double CurrentLapHybridUsage { get; private set; }
        public double LapsUntilEmptyOrFull { get; private set; }

        public double HybridChargeAtRaceEnd { get; private set; }

        public bool IsStateAtRaceEndValid { get; set; }

        public HybridSystemMode HybridSystemMode { get; private set; }

        public HybridChargeChange HybridChargeChange { get; private set; }
        
        public HybridEnduranceState HybridEnduranceState { get; private set; }

        public HybridConsumptionMonitor()
        {
            Reset();
        }

        public void UpdateConsumption(SimulatorDataSet simulatorDataSet)
        {
            UpdateConsumption(simulatorDataSet.PlayerInfo, simulatorDataSet.SessionInfo.SessionType, simulatorDataSet.SessionInfo.SessionTime);
        }

        public void UpdateConsumption(DriverInfo driverInfo, SessionType sessionType, TimeSpan sessionTime)
        {
            if (sessionTime < _nextTick)
            {
                return;
            }

            _nextTick = sessionTime + tickInterval;

            if (driverInfo.InPits && sessionType != SessionType.Race)
            {
                OnLapCompletedNonRace(driverInfo);
            }

            HybridSystem hybridSystem = driverInfo.CarInfo.HybridSystem;

            HybridSystemMode = hybridSystem.HybridSystemMode;

            if (HybridSystemMode == HybridSystemMode.UnAvailable)
            {
                HybridChargeChange = HybridChargeChange.Unavailable;
                return;
            }

            double lastTickHybridChange = hybridSystem.RemainingChargePercentage - CurrentHybridCharge;
            if (Math.Abs(lastTickHybridChange) > 10)
            {
                LapStartHybridState = driverInfo.CarInfo.HybridSystem.RemainingChargePercentage;
            }

            if (hybridSystem.HybridChargeChange != HybridChargeChange.Unavailable)
            {
                HybridChargeChange = hybridSystem.HybridChargeChange;
            }
            else
            {
                HybridChargeChange = lastTickHybridChange switch
                {
                    0 => HybridChargeChange.None,
                    < 0 => HybridChargeChange.Discharge,
                    > 0 => HybridChargeChange.Recharge,
                    _ => HybridChargeChange
                };
            }

            CurrentHybridCharge = hybridSystem.RemainingChargePercentage;
            CurrentLapHybridUsage = CurrentHybridCharge - LapStartHybridState;
        }

        public void OnLapCompletedNonRace(IDriverInfo driverInfo)
        {
            PreviousCompletedLaps = driverInfo.CompletedLaps;
            PreviousLapHybridUsage = CurrentLapHybridUsage;
            LapStartHybridState = driverInfo.CarInfo.HybridSystem.RemainingChargePercentage;
            LapsUntilEmptyOrFull = PreviousLapHybridUsage < 0 ? Math.Abs(LapStartHybridState / PreviousLapHybridUsage)
                : Math.Abs((1 - LapStartHybridState) / PreviousLapHybridUsage);
            HybridEnduranceState = LapsUntilEmptyOrFull switch
            {
                < 1 => HybridEnduranceState.Error,
                < 2 => HybridEnduranceState.Warning,
                _ => HybridEnduranceState.Ok,
            };
            IsStateAtRaceEndValid = false;
        }

        public void OnLapCompletedInRace(IDriverInfo driverInfo, double lapsRemaining)
        {
            OnLapCompletedNonRace(driverInfo);
            if (!driverInfo.CarInfo.HybridSystem.IsElectricOnly || double.IsInfinity(lapsRemaining) || double.IsNaN(lapsRemaining))
            {
                HybridChargeAtRaceEnd = -1;
                return;
            }

            HybridChargeAtRaceEnd = Math.Min(driverInfo.CarInfo.HybridSystem.RemainingChargePercentage + PreviousLapHybridUsage * lapsRemaining, 1.0);
            if (double.IsNaN(HybridChargeAtRaceEnd) || double.IsInfinity(HybridChargeAtRaceEnd))
            {
                HybridChargeAtRaceEnd = -1;
                return;
            }

            IsStateAtRaceEndValid = true;

            HybridEnduranceState = HybridChargeAtRaceEnd switch
            {
                <= 0 => HybridEnduranceState.Error,
                < 0.02 => HybridEnduranceState.Warning,
                _ => HybridEnduranceState.Ok,
            };
        }

        public void Reset()
        {
            _nextTick = TimeSpan.Zero;
            PreviousCompletedLaps = -1;
            LapStartHybridState = 0;
            PreviousLapHybridUsage = 0;
            CurrentLapHybridUsage = 0;
            LapsUntilEmptyOrFull = 0;
            HybridChargeAtRaceEnd = -1;
            IsStateAtRaceEndValid = false;
        }
    }
}
