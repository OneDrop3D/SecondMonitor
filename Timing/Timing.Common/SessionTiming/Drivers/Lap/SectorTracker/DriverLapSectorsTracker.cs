namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap.SectorTracker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;

    using SecondMonitor.DataModel.Extensions;

    using ViewModels.SessionEvents;

    public class DriverLapSectorsTracker : IDriverLapSectorsTracker
    {
        private const int MaximumSectors = 400;
        private const int DesiredSectionLength = 5;
        private readonly DriverTiming _driverTiming;
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly Dictionary<string, ISpecificLapTracker> _specificLapTrackers;
        private SectionRecord[] _sections;
        private double _layoutDistance;
        private int _sectionLength;
        private int _lastSectionIndex;
        private bool _lastUpdateInPits;

        public DriverLapSectorsTracker(DriverTiming driverTiming, ISessionEventProvider sessionEventProvider, IEnumerable<ISpecificLapTracker> specificLapTrackers)
        {
            SimulatorDataSet lastDataSet = sessionEventProvider.LastDataSet;
            if (lastDataSet == null)
            {
                throw new ArgumentException("lastDataSet is null");
            }

            if (lastDataSet.SessionInfo == null)
            {
                throw new ArgumentException("lastDataSet session info is null");
            }

            if (lastDataSet.SessionInfo.TrackInfo == null)
            {
                throw new ArgumentException("lastDataSet Track Info is null");
            }

            _layoutDistance = lastDataSet.SessionInfo.TrackInfo.LayoutLength.InMeters;
            _driverTiming = driverTiming;
            _sessionEventProvider = sessionEventProvider;
            InitializeSections();
            _specificLapTrackers = specificLapTrackers.ToDictionary(x => x.Name);
        }

        private void InitializeSections()
        {
            _lastSectionIndex = -1;
            int sections = Math.Min(MaximumSectors, (int)(_layoutDistance / DesiredSectionLength));
            _sectionLength = (int)Math.Ceiling(_layoutDistance / sections);
            _sections = new SectionRecord[(int)(_layoutDistance / _sectionLength) + 2];
        }

        public static TimeSpan GetRelativeGapTo(DriverTiming driverInFront, DriverTiming driverInBack)
        {
            double lapDistance = driverInBack.DriverInfo.LapDistance;
            TimeSpan driverInFrontTime = driverInFront.DriverLapSectorsTracker.GetSectionTime(lapDistance);
            TimeSpan driverInBackTime = driverInBack.DriverLapSectorsTracker.GetSectionTime(lapDistance);
            if (driverInFront.InPits && !driverInBack.InPits && driverInFront.LastPitStop != null)
            {
                driverInFrontTime += TimeSpan.FromSeconds(driverInFront.LastPitStop.PitStopDuration.TotalSeconds * 0.7);
                if (driverInFrontTime > driverInBackTime)
                {
                    driverInFrontTime = driverInBackTime;
                }
            }

            return driverInFrontTime - driverInBackTime;
        }

        public void Update()
        {
            SimulatorDataSet lastDataSet = _sessionEventProvider.LastDataSet;
            if (_layoutDistance == 0 && lastDataSet.SessionInfo.TrackInfo.LayoutLength.InMeters > 0)
            {
                _layoutDistance = lastDataSet.SessionInfo.TrackInfo.LayoutLength.InMeters;
                InitializeSections();
            }

            int currentSectionIndex = (int)_driverTiming.DriverInfo.LapDistance / _sectionLength;

            //Clear Tracking when entering pits in practice, as player can jump back to pits, entries that are results of the jump are not valid
            if (!_lastUpdateInPits && _driverTiming.InPits && lastDataSet.SessionInfo.SessionType != SessionType.Race)
            {
                _lastSectionIndex = 0;
                _lastUpdateInPits = _driverTiming.InPits;
                for (int i = 0; i < _sections.Length; i++)
                {
                    _sections[i] = SectionRecord.EmptyRecord;
                }

                return;
            }

            _lastUpdateInPits = _driverTiming.InPits;

            // Current Section out of bounds - avoid application crash
            if (currentSectionIndex < 0 || currentSectionIndex >= _sections.Length)
            {
                return;
            }

            // Moved backwards - only allow for end of lap => to start of another lap. So, when the change is more than half of a lap, and the last tracked part was on the second part of the lap
            if (currentSectionIndex < _lastSectionIndex && (_lastSectionIndex - currentSectionIndex < _sections.Length / 2 || _lastSectionIndex < _sections.Length / 2))
            {
                return;
            }

            // Jumped more than half track ahead - not allowed, most likely noise around the finishing line
            if (_lastSectionIndex > -1 && currentSectionIndex - _lastSectionIndex > _sections.Length / 2)
            {
                return;
            }

            if (currentSectionIndex == _lastSectionIndex)
            {
                SectionRecord oldRecord = _sections[_lastSectionIndex];
                SectionRecord newRecord = CreateSectionRecord(lastDataSet, _lastSectionIndex);
                _specificLapTrackers.Values.ForEach(x => x.OnSectionEntryCreated(lastDataSet, _driverTiming, newRecord, oldRecord, true, true, this));
                _sections[_lastSectionIndex] = newRecord;
                return;
            }

            bool isFirstRecord = true;
            while (_lastSectionIndex != currentSectionIndex)
            {
                _lastSectionIndex = (_lastSectionIndex + 1) % _sections.Length;
                SectionRecord oldRecord = _sections[_lastSectionIndex];
                SectionRecord newRecord = CreateSectionRecord(lastDataSet, _lastSectionIndex);
                _specificLapTrackers.Values.ForEach(x => x.OnSectionEntryCreated(lastDataSet, _driverTiming, newRecord, oldRecord, isFirstRecord, _lastSectionIndex == currentSectionIndex, this));
                _sections[_lastSectionIndex] = newRecord;
                isFirstRecord = false;
            }
        }

        private SectionRecord CreateSectionRecord(SimulatorDataSet lastDataSet, int sectionIndex)
        {
            return new SectionRecord(sectionIndex,
                lastDataSet.SessionInfo.SessionTime,
                _driverTiming.CurrentLap.CurrentlyValidProgressTime,
                _driverTiming.CurrentLap.CompletedDistance,
                _driverTiming.TotalDistanceTraveled,
                _driverTiming.DriverInfo.WorldPosition,
                _driverTiming.CurrentLap.LapNumber);
        }

        public TimeSpan GetSectionTime(double lapDistance)
        {
            return GetSectionRecord(lapDistance).SessionTime;
        }

        public ref readonly SectionRecord GetSectionRecord(double lapDistance)
        {
            int currentSectionIndex = Math.Min((int)lapDistance / _sectionLength, _sections.Length - 1);
            if (currentSectionIndex >= _sections.Length || currentSectionIndex < 0)
            {
                return ref SectionRecord.EmptyRecord;
            }

            return ref _sections[currentSectionIndex];
        }

        public SectionRecord GetSectionRecordBeforeTime(TimeSpan timeBefore)
        {
            if (_lastSectionIndex == -1)
            {
                return SectionRecord.EmptyRecord;
            }

            int currentSectionIndex = _lastSectionIndex;
            int previousSectionIndex = currentSectionIndex - 1;
            ref SectionRecord currentRecord = ref _sections[currentSectionIndex];
            TimeSpan timespanToFind = _sessionEventProvider.LastDataSet.SessionInfo.SessionTime - timeBefore;
            int lapsDown = 0;
            while (currentRecord.SessionTime > timespanToFind)
            {
                if (previousSectionIndex < 0)
                {
                    previousSectionIndex = _sections.Length - 1;
                }

                currentRecord = ref _sections[currentSectionIndex];
                ref SectionRecord previousRecord = ref _sections[previousSectionIndex];
                if (currentRecord.SessionTime == TimeSpan.Zero || previousRecord.SessionTime == TimeSpan.Zero)
                {
                    return currentRecord;
                }

                if (currentRecord.SessionTime < previousRecord.SessionTime)
                {
                    timespanToFind += previousRecord.SessionTime - currentRecord.SessionTime;
                    lapsDown++;
                }

                currentSectionIndex = previousSectionIndex;
                previousSectionIndex--;
            }

            var referencedSectionRecord = _sections[currentSectionIndex];
            return new SectionRecord(
                referencedSectionRecord.SectionIndex,
                referencedSectionRecord.SessionTime,
                referencedSectionRecord.LapTime,
                referencedSectionRecord.LapDistance,
                referencedSectionRecord.TotalDistance - (_layoutDistance * lapsDown),
                referencedSectionRecord.WorldPos,
                referencedSectionRecord.LapNumber);
        }

        public bool TryGetSpecificTracker(string trackerName, out ISpecificLapTracker specificLapTracker)
        {
            return _specificLapTrackers.TryGetValue(trackerName, out specificLapTracker);
        }

        public bool TryGetSpecificTracker<T>(out T specificLapTracker)
        {
            var specificLapTrackers = _specificLapTrackers.Values.OfType<T>().ToList();
            if (specificLapTrackers.Count != 1)
            {
                specificLapTracker = default(T);
                return false;
            }

            specificLapTracker = specificLapTrackers[0];
            return true;
        }

        public TimeSpan GetRelativeGapToPlayer()
        {
            DriverTiming playerTiming = _driverTiming.Session?.Player;
            if (playerTiming == null)
            {
                return TimeSpan.Zero;
            }

            if (Math.Abs(_driverTiming.DistanceToPlayer) < 10)
            {
                return TimeSpan.Zero;
            }

            if (_driverTiming.DistanceToPlayer < 0)
            {
                return GetRelativeGapTo(_driverTiming, playerTiming);
            }

            return -GetRelativeGapTo(playerTiming, _driverTiming);
        }

        public TimeSpan GetGapToDriverRelative(DriverTiming otherDriver)
        {
            if (_sessionEventProvider.LastDataSet?.SessionInfo?.TrackInfo?.LayoutLength == null || otherDriver?.DriverInfo == null || _driverTiming.DriverInfo == null || _sessionEventProvider.LastDataSet.SessionInfo.TrackInfo.LayoutLength.IsZero)
            {
                return TimeSpan.Zero;
            }

            double distanceToOtherDriver = otherDriver.DriverInfo.ComputeRelativeDistanceTo(_driverTiming.DriverInfo, _sessionEventProvider.LastDataSet.SessionInfo.TrackInfo.LayoutLength.InMeters);
            if (Math.Abs(distanceToOtherDriver) < 10)
            {
                return TimeSpan.Zero;
            }

            if (distanceToOtherDriver >= 0)
            {
                return GetRelativeGapTo(_driverTiming, otherDriver);
            }

            return -GetRelativeGapTo(otherDriver, _driverTiming);
        }

        public TimeSpan GetRelativeGapToLeader()
        {
            DriverTiming leaderTiming = _driverTiming.Session?.Leader;
            if (leaderTiming == null)
            {
                return TimeSpan.Zero;
            }

            return -GetRelativeGapTo(leaderTiming, _driverTiming);
        }

        public TimeSpan GetGapToPlayerAbsolute() => GetGapToDriverAbsolute(_driverTiming?.Session?.Player);

        public TimeSpan GetGapToDriverAbsolute(DriverTiming otherDriver)
        {
            if (otherDriver == null)
            {
                return TimeSpan.Zero;
            }

            if (_driverTiming.Position < otherDriver.Position)
            {
                return GetRelativeGapTo(_driverTiming, otherDriver);
            }

            return -GetRelativeGapTo(otherDriver, _driverTiming);
        }

        public ref readonly SectionRecord FindNext(int startRecord, Predicate<SectionRecord> condition)
        {
            int currentRecord = (startRecord + 1) % _sections.Length;
            while (currentRecord != startRecord)
            {
                if (condition(_sections[currentRecord]))
                {
                    return ref _sections[currentRecord];
                }

                currentRecord = (currentRecord + 1) % _sections.Length;
            }

            return ref SectionRecord.EmptyRecord;
        }

        public ref readonly SectionRecord FindNext(Predicate<SectionRecord> condition)
        {
            return ref FindNext(_lastSectionIndex, condition);
        }
    }
}