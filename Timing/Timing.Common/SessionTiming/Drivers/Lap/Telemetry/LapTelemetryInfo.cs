namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap.Telemetry
{
    using System;
    using System.Diagnostics;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using DataModel.Telemetry;
    using NLog;

    public class LapTelemetryInfo
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly InputInfo BlankInput = new InputInfo();
        private readonly Stopwatch _lastUpdateWatch;
        private bool _waitForSessionStart;

        public LapTelemetryInfo(DriverInfo driverInfo, SimulatorDataSet dataSet, LapInfo lapInfo, TimeSpan snapshotInterval, SimulatorSourceInfo simulatorSourceInfo, bool captureFullTelemetry)
        {
            _lastUpdateWatch = Stopwatch.StartNew();
            TelemetryFilePath = string.Empty;
            LapStarSnapshot = new TelemetrySnapshot(driverInfo, dataSet.SessionInfo.WeatherInfo, dataSet.InputInfo, simulatorSourceInfo);
            LapInfo = lapInfo;
            PortionTimes = new LapPortionTimes(dataSet.SessionInfo.TrackInfo.LayoutLength.InMeters, lapInfo);
            TimedTelemetrySnapshots = captureFullTelemetry ? new TimedTelemetrySnapshots(snapshotInterval) : new TimedTelemetrySnapshotsNullObject();

            _waitForSessionStart = dataSet.SessionInfo.SessionType == SessionType.Race && dataSet.SessionInfo.SessionPhase is SessionPhase.Countdown or SessionPhase.Unavailable;
        }

        public TelemetrySnapshot LapEndSnapshot { get; private set; }
        public TelemetrySnapshot LapStarSnapshot { get; private set; }
        public ITimedTelemetrySnapshots TimedTelemetrySnapshots { get; private set; }
        public bool IsTelemetryPurged { get; private set; }
        public bool IsPortionTimesPurged { get; private set; }

        public LapPortionTimes PortionTimes { get; private set; }
        public LapInfo LapInfo { get; }

        public string TelemetryFilePath { get; set; }
        public bool HasTelemetryData => !IsTelemetryPurged && TimedTelemetrySnapshots.Snapshots.Count > 0;

        public void CreateLapEndSnapshot(DriverInfo driverInfo, WeatherInfo weather, InputInfo inputInfo, SimulatorSourceInfo simulatorSourceInfo)
        {
            LapEndSnapshot = new TelemetrySnapshot(driverInfo, weather, inputInfo, simulatorSourceInfo);
        }

        public void UpdateTelemetry(SimulatorDataSet dataSet, DriverInfo driverInfo, bool fullTelemetryUpdate)
        {
            if (IsTelemetryPurged)
            {
                throw new InvalidOperationException("Cannot update Telemetry on a purged TelemetryInfo");
            }

            if (_waitForSessionStart && dataSet.SessionInfo.SessionPhase == SessionPhase.Green)
            {
                LapStarSnapshot = new TelemetrySnapshot(driverInfo, dataSet.SessionInfo.WeatherInfo, dataSet.InputInfo, dataSet.SimulatorSourceInfo);
                _waitForSessionStart = false;
            }

            PortionTimes.UpdateLapPortions();
            if (!driverInfo.InPits && fullTelemetryUpdate && (driverInfo.IsPlayer || _lastUpdateWatch.ElapsedMilliseconds > 100))
            {
                _lastUpdateWatch.Restart();
                var inputInfoToUse = driverInfo.IsPlayer ? dataSet.InputInfo : BlankInput;
                TimedTelemetrySnapshots.AddNextSnapshot(LapInfo.CurrentlyValidProgressTime, dataSet.SessionInfo.SessionTime, driverInfo, dataSet.SessionInfo.WeatherInfo, inputInfoToUse, dataSet.SimulatorSourceInfo);
            }
        }

        public void Complete(Distance lapDistance)
        {
            TimedTelemetrySnapshots.TrimInvalid(lapDistance);
        }

        public void PurgeTelemetry()
        {
            if (IsTelemetryPurged)
            {
                return;
            }

            Logger.Info($"Purging Lap {LapInfo.LapNumber}, of Driver {LapInfo.Driver.DriverId}");

            TimedTelemetrySnapshots = null;
            IsTelemetryPurged = true;
        }

        public void PurgeLapPortionTimes()
        {
            if (IsPortionTimesPurged)
            {
                return;
            }

            Logger.Info($"Lap Portion Times Purging Lap {LapInfo.LapNumber}, of Driver {LapInfo.Driver.DriverId}");

            PortionTimes = null;
            IsPortionTimesPurged = true;
        }
    }
}