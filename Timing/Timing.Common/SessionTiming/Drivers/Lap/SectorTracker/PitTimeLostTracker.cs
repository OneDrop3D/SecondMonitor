namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap.SectorTracker
{
    using System;

    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using NLog;

    using SecondMonitor.DataModel.Extensions;

    public class PitTimeLostTracker : ISpecificLapTracker
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public const string TrackerName = "PitTimeLost";
        private bool _inPits;
        private TimeSpan _normalLapPitStartTime;
        private TimeSpan _pitLapPitStopStart;

        public TimeSpan LastPitStopTimeLost { get; private set; }

        public bool IsLastPitStopTimeLostFilled { get; private set; }

        public string Name => TrackerName;

        public void OnSectionEntryCreated(SimulatorDataSet simulatorDataSet, DriverTiming driverTiming, in SectionRecord newRecord, in SectionRecord replacedRecord, bool isFirstInBatch, bool isLastInBatch, DriverLapSectorsTracker parentTracker)
        {
            if (simulatorDataSet.SessionInfo.SessionType != SessionType.Race || simulatorDataSet.SessionInfo.SessionPhase != SessionPhase.Green)
            {
                return;
            }

            SectionRecord oldRecordToUse;
            switch (_inPits)
            {
                case false when driverTiming.InPits:
                    oldRecordToUse = driverTiming.CurrentLap.LapNumber != replacedRecord.LapNumber ? replacedRecord : parentTracker.FindNext(replacedRecord.SectionIndex, x => x.LapNumber != driverTiming.CurrentLap.LapNumber);
                    Logger.Info($"Pit Stop Started: Driver: {driverTiming.DriverId}, Record Index: {newRecord.SectionIndex}/{oldRecordToUse.SectionIndex}, NewRecord: {newRecord.SessionTime.FormatTimeSpanOnlySecondNoMiliseconds(false)}, Old Record: {oldRecordToUse.SessionTime.FormatTimeSpanOnlySecondNoMiliseconds(false)} ");
                    LogPitStarted(newRecord, oldRecordToUse);
                    return;
                case true when !driverTiming.InPits && isLastInBatch:
                    oldRecordToUse = driverTiming.CurrentLap.LapNumber != replacedRecord.LapNumber ? replacedRecord : parentTracker.FindNext(replacedRecord.SectionIndex, x => x.LapNumber != driverTiming.CurrentLap.LapNumber);
                    Logger.Info($"Pit Stop Ended: Driver: {driverTiming.DriverId}, Record Index: {newRecord.SectionIndex}/{oldRecordToUse.SectionIndex}, NewRecord: {newRecord.SessionTime.FormatTimeSpanOnlySecondNoMiliseconds(false)}, Old Record: {oldRecordToUse.SessionTime.FormatTimeSpanOnlySecondNoMiliseconds(false)} ");
                    LogPitEnd(newRecord, oldRecordToUse);
                    break;
            }
        }

        private void LogPitStarted(in SectionRecord newRecord, in SectionRecord replacedRecord)
        {
            _inPits = true;
            IsLastPitStopTimeLostFilled = false;
            LastPitStopTimeLost = TimeSpan.Zero;
            _normalLapPitStartTime = replacedRecord.SessionTime;
            _pitLapPitStopStart = newRecord.SessionTime;
        }

        private void LogPitEnd(in SectionRecord newRecord, in SectionRecord replacedRecord)
        {
            _inPits = false;
            TimeSpan regularTime = replacedRecord.SessionTime - _normalLapPitStartTime;
            TimeSpan pitStopTime = newRecord.SessionTime - _pitLapPitStopStart;
            LastPitStopTimeLost = pitStopTime - regularTime;
            IsLastPitStopTimeLostFilled = true;
        }
    }
}