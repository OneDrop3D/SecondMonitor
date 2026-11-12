namespace SecondMonitor.DataModel.Visitors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BasicProperties;

    using SecondMonitor.DataModel.Extensions;

    using Snapshot;
    using Snapshot.Drivers;

    public class YellowFlagPopulatorVisitor : ISimulatorDataSetVisitor
    {
        private static readonly string[] SupportedSims = { SimulatorsNameMap.PCars2SimName, SimulatorsNameMap.Ams2SimName };
        private static readonly string[] IgnoredClasses = { "Kart1", "KartRental", "KartShifter", "Kart125cc", "KartGX390" };
        private static readonly TimeSpan YellowFlagTimeout = TimeSpan.FromSeconds(5);

        private readonly Dictionary<string, TimeSpan> _driverCausingYellowEnd = new();

        private bool _isSimulatorCheckRequired;
        private bool _isEnabled;
        private bool _wasSessionGreen;

        private TimeSpan _initialCheckTime;
        private TimeSpan _yellowFlagSector1End;
        private TimeSpan _yellowFlagSector2End;
        private TimeSpan _yellowFlagSector3End;

        public YellowFlagPopulatorVisitor()
        {
            Reset();
        }

        public void Visit(SimulatorDataSet simulatorDataSet)
        {
            if (_isSimulatorCheckRequired)
            {
                CheckIfSimulatorIsSupported(simulatorDataSet);
            }

            if (_wasSessionGreen && simulatorDataSet.SessionInfo.SessionPhase == SessionPhase.Countdown)
            {
                _wasSessionGreen = false;
                _initialCheckTime = TimeSpan.MaxValue;
            }

            if (!_wasSessionGreen && simulatorDataSet.SessionInfo.SessionPhase == SessionPhase.Green)
            {
                _wasSessionGreen = true;
                _initialCheckTime = simulatorDataSet.SessionInfo.SessionTime + TimeSpan.FromSeconds(20);
            }

            if (!_isEnabled || simulatorDataSet?.DriversInfo == null || simulatorDataSet.DriversInfo.Length == 0 || simulatorDataSet.SessionInfo.SessionTime.TotalSeconds < 20 || !_wasSessionGreen || _initialCheckTime > simulatorDataSet.SessionInfo.SessionTime)
            {
                return;
            }

            CheckDriversSpeed(simulatorDataSet);
            ApplyYellowFlags(simulatorDataSet);
        }

        public void Reset()
        {
            _isEnabled = false;
            _wasSessionGreen = false;
            _isSimulatorCheckRequired = true;
            _yellowFlagSector1End = TimeSpan.MinValue;
            _yellowFlagSector2End = TimeSpan.MinValue;
            _yellowFlagSector3End = TimeSpan.MinValue;
            _initialCheckTime = TimeSpan.MaxValue;
            _driverCausingYellowEnd.Clear();
        }

        private void CheckIfSimulatorIsSupported(SimulatorDataSet dataSet)
        {
            _isEnabled = SupportedSims.Contains(dataSet.Source);
            _isSimulatorCheckRequired = false;
        }

        private void ApplyYellowFlags(SimulatorDataSet dataSet)
        {
            if (dataSet.SessionInfo.SessionTime < _yellowFlagSector1End)
            {
                dataSet.SessionInfo.ActiveFlags |= FlagKind.YellowSector1;
            }

            if (dataSet.SessionInfo.SessionTime < _yellowFlagSector2End)
            {
                dataSet.SessionInfo.ActiveFlags |= FlagKind.YellowSector2;
            }

            if (dataSet.SessionInfo.SessionTime < _yellowFlagSector3End)
            {
                dataSet.SessionInfo.ActiveFlags |= FlagKind.YellowSector3;
            }
        }

        private void CheckDriversSpeed(SimulatorDataSet simulatorDataSet)
        {
            foreach (DriverInfo driverInfo in simulatorDataSet.DriversInfo.Where(x => !x.InPits && x.FinishStatus == DriverFinishStatus.None && x.Speed.InKph < 10 && !IgnoredClasses.Contains(x.CarClassName)))
            {
                _driverCausingYellowEnd[driverInfo.DriverSessionId] = simulatorDataSet.SessionInfo.SessionTime + YellowFlagTimeout;
                switch (driverInfo.Timing.CurrentSector)
                {
                    case 1:
                        _yellowFlagSector1End = simulatorDataSet.SessionInfo.SessionTime + YellowFlagTimeout;
                        break;
                    case 2:
                        _yellowFlagSector2End = simulatorDataSet.SessionInfo.SessionTime + YellowFlagTimeout;
                        break;
                    case 3:
                        _yellowFlagSector3End = simulatorDataSet.SessionInfo.SessionTime + YellowFlagTimeout;
                        break;
                }
            }

            simulatorDataSet.DriversInfo.Where(x => _driverCausingYellowEnd.ContainsKey(x.DriverSessionId)).ForEach(x => x.IsCausingYellow = true);
            _driverCausingYellowEnd.Where(x => x.Value < simulatorDataSet.SessionInfo.SessionTime).Select(x => x.Key)
                .ToList().ForEach(x => _driverCausingYellowEnd.Remove(x));
        }
    }
}