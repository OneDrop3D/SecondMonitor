namespace SecondMonitor.ViewModels.CarStatus
{
    using System;
    using System.Collections.Generic;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using FuelStatus;

    public class TyreLifeTimeMonitor
    {
        private readonly IPaceProvider _paceProvider;
        private readonly SessionRemainingCalculator _sessionRemainingCalculator;
        private readonly IFuelPredictionProvider _fuelPredictionProvider;
        private readonly Queue<double> _tyreWears;
        private TimeSpan _lastCheckTime;
        private double _wearPerMinute;
        public TyreLifeTimeMonitor(IPaceProvider paceProvider, SessionRemainingCalculator sessionRemainingCalculator, IFuelPredictionProvider fuelPredictionProvider)
        {
            _lastCheckTime = TimeSpan.Zero;
            _paceProvider = paceProvider;
            _sessionRemainingCalculator = sessionRemainingCalculator;
            _fuelPredictionProvider = fuelPredictionProvider;
            _tyreWears = new Queue<double>();
        }

        public double WearAtRaceEnd
        {
            get;
            private set;
        }

        public int LapsUntilHeavyWear
        {
            get;
            private set;
        }
        
        public int MinutesUntilHeavyWear
        {
            get;
            private set;
        }

        public double WearAtStintEnd
        {
            get;
            private set;
        }

        public void ApplyWheelInfo(SimulatorDataSet dataSet, WheelInfo wheelInfo)
        {
            SessionInfo sessionInfo = dataSet.SessionInfo;
            if (_lastCheckTime != TimeSpan.Zero && (sessionInfo.SessionTime - _lastCheckTime).TotalSeconds < 30)
            {
                return;
            }

            if (dataSet.SessionInfo.SessionType == SessionType.Na || dataSet.SessionInfo.SessionTime == TimeSpan.Zero)
            {
                return;
            }

            _lastCheckTime = sessionInfo.SessionTime;
            UpdateTyreWear(wheelInfo, dataSet);
            CalculateWearAtRaceEnd(wheelInfo, dataSet);
            CalculateWearAtStintEnd(wheelInfo);
            CalculateLapsUntilHeavyWear(wheelInfo, dataSet);
        }

        private void CalculateWearAtStintEnd(WheelInfo wheelInfo)
        {
            double minutesTillStintEnd = _fuelPredictionProvider.GetRemainingFuelTime().TotalMinutes;
            if (minutesTillStintEnd <= 0)
            {
                WearAtStintEnd = 0;
                return;
            }

            WearAtStintEnd = Math.Max(1, 100 - (100 * (wheelInfo.TyreWear.ActualWear + (_wearPerMinute * _fuelPredictionProvider.GetRemainingFuelTime().TotalMinutes))));
        }

        private void UpdateTyreWear(WheelInfo wheelInfo, SimulatorDataSet simulatorDataSet)
        {
            if (simulatorDataSet.PlayerInfo?.InPits != false)
            {
                return;
            }

            double currentWear = wheelInfo.TyreWear.ActualWear;

            if (_tyreWears.Count < 1)
            {
                _tyreWears.Enqueue(currentWear);
                return;
            }

            double firstCachedWear = _tyreWears.Peek();

            if (currentWear < firstCachedWear)
            {
                OnPitStop();
                _tyreWears.Enqueue(currentWear);
                return;
            }

            _tyreWears.Enqueue(currentWear);

            _wearPerMinute = (currentWear - firstCachedWear) / ((_tyreWears.Count - 1) / 2.0);

            if (_tyreWears.Count > 8)
            {
                _tyreWears.Dequeue();
            }
        }

        private void CalculateLapsUntilHeavyWear(WheelInfo wheelInfo, SimulatorDataSet simulatorDataSet)
        {
            TimeSpan? playersPace = _paceProvider.PlayersPace;
            if (_wearPerMinute == 0 || !playersPace.HasValue || playersPace.Value == TimeSpan.Zero)
            {
                LapsUntilHeavyWear = 0;
                MinutesUntilHeavyWear = 0;
                return;
            }

            double wearLimit = simulatorDataSet.SimulatorSourceInfo.DetermineLapsUntilHeavyWearByLightWear
                ? wheelInfo.TyreWear.LightWearLimit
                : wheelInfo.TyreWear.HeavyWearLimit;
            double wearLeft = (wearLimit - wheelInfo.TyreWear.ActualWear);
            double minutesLeft = wearLeft / _wearPerMinute;
            LapsUntilHeavyWear = (int)Math.Floor(minutesLeft / playersPace.Value.TotalMinutes);
            MinutesUntilHeavyWear = (int)minutesLeft;
        }

        private void CalculateWearAtRaceEnd(WheelInfo wheelInfo, SimulatorDataSet simulatorDataSet)
        {
            if (simulatorDataSet.SessionInfo.SessionType != SessionType.Race || _wearPerMinute == 0)
            {
                WearAtRaceEnd = 0;
                return;
            }

            TimeSpan timeRemaining = _sessionRemainingCalculator.GetTimeRemaining(simulatorDataSet);
            if (timeRemaining == TimeSpan.Zero)
            {
                WearAtRaceEnd = 0;
                return;
            }

            WearAtRaceEnd = Math.Max(1, 100 - (100 * (wheelInfo.TyreWear.ActualWear + (_wearPerMinute * timeRemaining.TotalMinutes))));
        }

        private void OnPitStop()
        {
            _tyreWears.Clear();
            _wearPerMinute = 0;
            WearAtRaceEnd = 0;
        }

        public void Reset()
        {
            _tyreWears.Clear();
            _lastCheckTime = TimeSpan.Zero;
            WearAtRaceEnd = 0;
            LapsUntilHeavyWear = 0;
            MinutesUntilHeavyWear = 0;
        }
    }
}