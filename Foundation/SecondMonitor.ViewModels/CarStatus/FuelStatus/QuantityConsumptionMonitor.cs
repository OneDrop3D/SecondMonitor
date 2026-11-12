namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    using System;
    using System.Diagnostics;

    using NLog;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Summary;

    public abstract class QuantityConsumptionMonitor<TQuantity, TQuantityAvg, TSnapshot, TConsumptionInfo> : IConsumptionMonitor
        where TQuantity : IQuantity
        where TQuantityAvg : IQuantity
        where TSnapshot : QuantityStatusSnapshot<TQuantity>
        where TConsumptionInfo : QuantityConsumptionInfo<TQuantity, TQuantityAvg, TConsumptionInfo>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly bool _logExtendedFuelInfo = true;

        private readonly Stopwatch _extendedLogStopwatch = Stopwatch.StartNew();
        private int _lastLapNumber = -1;
        private TSnapshot _lastMinuteQuantityStatus = null;
        private TSnapshot _lastTickQuantityStatus = null;
        private TimeSpan _nextMinuteConsumptionUpdate = TimeSpan.Zero;

        /*#if !DEBUG
                        _logExtendedFuelInfo = false;
            #endif*/

        public TSnapshot LapStartQuantityStatus { get; protected set; }

        public TConsumptionInfo TotalQuantityConsumptionInfo { get; protected set; }

        public abstract string Name { get; }
        IQuantity IConsumptionMonitor.ActPerMinute => ActPerMinute;
        IQuantity IConsumptionMonitor.ActPerLap => ActPerLap;
        IQuantity IConsumptionMonitor.TotalPerMinute => TotalPerMinute;
        IQuantity IConsumptionMonitor.TotalPerLap => TotalPerLap;

        public TQuantity ActPerMinute
        {
            get;
            protected set;
        }

        public TQuantity ActPerLap
        {
            get;
            protected set;
        }

        public TQuantity TotalPerMinute
        {
            get;
            protected set;
        }

        public TQuantity TotalPerLap
        {
            get;
            protected set;
        }

        protected abstract TConsumptionInfo CreateConsumptionInfo();

        protected abstract TConsumptionInfo CreateConsumptionInfo(TSnapshot snapshot1, TSnapshot snapshot2);

        protected abstract TSnapshot CreateSnapshot(SimulatorDataSet simulatorDataSet);

        public virtual void Reset()
        {
            LapStartQuantityStatus = null;
            TotalQuantityConsumptionInfo = CreateConsumptionInfo();
            _nextMinuteConsumptionUpdate = TimeSpan.Zero;
            _lastTickQuantityStatus = null;
            _lastMinuteQuantityStatus = null;
            _lastLapNumber = -1;
        }

        public abstract TQuantity GetMonitoredQuantity(SimulatorDataSet simulatorDataSet);

        IQuantity IConsumptionMonitor.GetQuantityIn(double laps, SimulatorDataSet dataSet)
        {
            return GetQuantityIn(laps, dataSet);
        }

        public abstract TQuantity GetQuantityIn(double laps, SimulatorDataSet dataSet);

        IQuantity IConsumptionMonitor.GetMonitoredQuantity(SimulatorDataSet simulatorDataSet)
        {
            return GetMonitoredQuantity(simulatorDataSet);
        }

        public void UpdateQuantityConsumption(SimulatorDataSet simulatorDataSet)
        {
            if (simulatorDataSet?.PlayerInfo == null)
            {
                Logger.Info("Ignoring DataSet, Player Info is null");
                return;
            }

            if (simulatorDataSet?.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining.InLiters <= 0)
            {
                return;
            }

            if (_lastTickQuantityStatus != null && _lastTickQuantityStatus.SessionTime >= simulatorDataSet.SessionInfo.SessionTime)
            {
                return;
            }

            if (SkipThisTick(simulatorDataSet))
            {
                // Force to also skip next tick
                _lastTickQuantityStatus = null;
                return;
            }

            if (_lastTickQuantityStatus == null)
            {
                _lastTickQuantityStatus = CreateSnapshot(simulatorDataSet);
                LapStartQuantityStatus = null;
                _lastMinuteQuantityStatus = null;
                return;
            }

            UpdateLapConsumption(simulatorDataSet);
            UpdateMinuteConsumption(simulatorDataSet);

            TSnapshot currentSnapshot = CreateSnapshot(simulatorDataSet);
            TConsumptionInfo lastTickConsumptionInfo = CreateConsumptionInfo(_lastTickQuantityStatus, currentSnapshot);

            if (!lastTickConsumptionInfo.IsConsumptionValid(simulatorDataSet))
            {
                _lastTickQuantityStatus = currentSnapshot;
                return;
            }

            TotalQuantityConsumptionInfo = TotalQuantityConsumptionInfo.AddConsumption(lastTickConsumptionInfo);

            if (_logExtendedFuelInfo && _extendedLogStopwatch.Elapsed.TotalSeconds > 30)
            {
                Logger.Info("-------------------------Fuel Info Starts---------------------------");
                LogInfo(currentSnapshot);
                LogInfo(lastTickConsumptionInfo);
                LogInfo(TotalQuantityConsumptionInfo);
                Logger.Info("-------------------------Fuel Info Ends---------------------------");
                _extendedLogStopwatch.Restart();
            }

            UpdateTotalData(simulatorDataSet);
            _lastTickQuantityStatus = currentSnapshot;
        }

        public double? GetLapsLeft(SimulatorDataSet simulatorDataSet)
        {
            if (TotalPerLap.RawValue <= 0)
            {
                return null;
            }

            return GetQuantityInRawValue(simulatorDataSet) / TotalPerLap.RawValue;
        }

        public TimeSpan? GetTimeLeft(SimulatorDataSet simulatorDataSet)
        {
            if (TotalPerMinute.RawValue <= 0)
            {
                return null;
            }

            return TimeSpan.FromMinutes(GetQuantityInRawValue(simulatorDataSet) / TotalPerMinute.RawValue);
        }

        public int GetLapsOfFuelLeftAtNextLapStart()
        {
            double fuelAtEndOfLap = LapStartQuantityStatus.QuantityLevel.RawValue -
                                    ActPerLap.RawValue -
                                    ActPerMinute.RawValue * 0.333; //keep 20 seconds of reserve fuel
            int lapsOfFuelLeft = (int)(fuelAtEndOfLap / TotalPerLap.RawValue);
            return lapsOfFuelLeft;
        }

        protected abstract double GetQuantityInRawValue(SimulatorDataSet simulatorDataSet);

        private void UpdateTotalData(SimulatorDataSet dataSet)
        {
            TotalPerMinute = TotalQuantityConsumptionInfo.GetAveragePerMinute();
            TotalPerLap = TotalQuantityConsumptionInfo.GetAveragePerDistance(dataSet.SessionInfo.TrackInfo.LayoutLength);
        }

        private void LogInfo(TConsumptionInfo consumptionInfo)
        {
            Logger.Info($"Consumption Info: Elapsed Time (min) : {consumptionInfo.ElapsedTime.TotalMinutes:F2}, Total Distance: {consumptionInfo.TraveledDistance.InMeters:F2}, Total Consumtption: {consumptionInfo.ConsumedQuantity.RawValue:F2}");
        }

        private void LogInfo(TSnapshot currentSnapshot)
        {
            Logger.Info($"Consumption Snapshot: Session Time (min) : {currentSnapshot.SessionTime.TotalMinutes:F2}, Total Distance: {currentSnapshot.TotalDistance:F2}, Fuel Level: {currentSnapshot.QuantityLevel.RawValue:F2}");
        }

        private void UpdateMinuteConsumption(SimulatorDataSet simulatorDataSet)
        {
            if (_nextMinuteConsumptionUpdate > simulatorDataSet.SessionInfo.SessionTime)
            {
                return;
            }

            _nextMinuteConsumptionUpdate = simulatorDataSet.SessionInfo.SessionTime + TimeSpan.FromMinutes(1);

            if (_lastMinuteQuantityStatus == null)
            {
                _lastMinuteQuantityStatus = CreateSnapshot(simulatorDataSet);
                return;
            }

            TSnapshot currentMinuteFuelConsumption = CreateSnapshot(simulatorDataSet);
            TConsumptionInfo quantityConsumption = CreateConsumptionInfo(_lastMinuteQuantityStatus, currentMinuteFuelConsumption); //FuelConsumptionInfo.CreateConsumption(_lastMinuteQuantityStatus, currentMinuteFuelConsumption);
            ActPerMinute = quantityConsumption.ConsumedQuantity;
            _lastMinuteQuantityStatus = currentMinuteFuelConsumption;
        }

        private void UpdateLapConsumption(SimulatorDataSet simulatorDataSet)
        {
            if (_lastLapNumber == simulatorDataSet.PlayerInfo.CompletedLaps)
            {
                return;
            }

            _lastLapNumber = simulatorDataSet.PlayerInfo.CompletedLaps;

            if (LapStartQuantityStatus == null)
            {
                LapStartQuantityStatus = CreateSnapshot(simulatorDataSet);
                return;
            }

            TSnapshot currentLapConsumption = CreateSnapshot(simulatorDataSet);
            TConsumptionInfo quantityConsumptionInfo = CreateConsumptionInfo(LapStartQuantityStatus, currentLapConsumption);
            ActPerLap = quantityConsumptionInfo.ConsumedQuantity;
            LapStartQuantityStatus = currentLapConsumption;
        }

        private bool SkipThisTick(SimulatorDataSet dataSet)
        {
            if (dataSet.PlayerInfo.InPits)
            {
                return true;
            }

            if (dataSet.SessionInfo.SessionType == SessionType.Race && (dataSet.SessionInfo.SessionPhase is SessionPhase.Countdown or SessionPhase.Unavailable || dataSet.PlayerInfo.TotalDistance < 300))
            {
                return true;
            }

            if (dataSet.SessionInfo.ActiveFlags.HasFlag(FlagKind.FullCourseYellow) || dataSet.SessionInfo.ActiveFlags.HasFlag(FlagKind.SafetyCar) || dataSet.SessionInfo.ActiveFlags.HasFlag(FlagKind.VirtualSafetyCar))
            {
                return true;
            }

            if (_lastTickQuantityStatus?.SessionTime >= dataSet.SessionInfo.SessionTime)
            {
                return true;
            }

            return false;
        }
    }
}
