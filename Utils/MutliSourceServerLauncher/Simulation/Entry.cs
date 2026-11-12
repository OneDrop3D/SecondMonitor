namespace SecondMonitor.Remote.Debug.Simulation
{
    using System;
    using System.Collections.Generic;
    using DataModel.BasicProperties;
    public class Entry
    {
        private readonly int _pitLap;

        private Driver _otherDriver;
        private PitStatus? _pitStatus;
        private double _stationaryTimer;

        public Entry(Car car, int pitLap, Driver firstDriver, Driver otherDriver = null)
        {
            Car = car;
            _pitLap = pitLap;
            CurrentDriver = firstDriver;
            _otherDriver = otherDriver;
            _pitStatus = PitStatus.Parked;
            CurrentLaptime = TimeSpan.Zero;
        }

        private enum PitStatus
        {
            Pitting,
            Parked,
            Pitted
        }

        public Driver CurrentDriver { get; private set; }
        public TimeSpan CurrentLaptime { get; private set; }
        public TimeSpan LastLaptime { get; private set; }

        public IEnumerable<Driver> Drivers
        {
            get
            {
                var drivers = new List<Driver>() { CurrentDriver };

                if (_otherDriver != null)
                {
                    drivers.Add(_otherDriver);
                }

                return drivers;
            }
        }

        public bool InPits => _pitStatus.HasValue;

        public Car Car
        {
            get;
        }

        public int CompletedLaps
        {
            get;
            private set;
        }

        public double LapDistance
        {
            get;
            private set;
        }

        public int Position { get; set; }

        public void MoveToGridSlot(Track track)
        {
            LapDistance = track.GridSlotDistance(Position);
            _pitStatus = null;
        }

        public void Drive(Track track, double timeStep)
        {
            double targetSpeed;

            switch (_pitStatus)
            {
                case PitStatus.Parked:
                case PitStatus.Pitting when IsInBrakingDistance(track.PitBoxLapDistance, 0):
                    targetSpeed = 0;
                    break;
                case PitStatus.Pitting:
                case PitStatus.Pitted:
                case null when CompletedLaps == _pitLap && IsInBrakingDistance(track.PitEntryLapDistance - 5, track.PitSpeedLimit):
                    targetSpeed = track.PitSpeedLimit - Velocity.FromKph(5).InMs;
                    break;
                default:
                    targetSpeed = double.MaxValue;
                    break;
            }

            var distanceTraveled = Car.Drive(targetSpeed, timeStep);

            LapDistance += distanceTraveled;
            _stationaryTimer += timeStep;

            if (CompletedLaps == _pitLap && !InPits && LapDistance > track.PitEntryLapDistance - 1)
            {
                _pitStatus = PitStatus.Pitting;
                Car.LimiterSpeed = track.PitSpeedLimit;
            }
            else if (_pitStatus == PitStatus.Pitting && Car.CurrentSpeed == 0)
            {
                _pitStatus = PitStatus.Parked;
                _stationaryTimer = 0;
            }
            else if (_pitStatus == PitStatus.Parked && _stationaryTimer > 30)
            {
                _pitStatus = PitStatus.Pitted;

                if (_otherDriver != null)
                {
                    (CurrentDriver, _otherDriver) = (_otherDriver, CurrentDriver);
                }
            }
            else if (_pitStatus == PitStatus.Parked)
            {
                _stationaryTimer += timeStep;
            }
            else if (CompletedLaps > _pitLap && _pitStatus == PitStatus.Pitted && LapDistance > track.PitExitLapDistance + 1)
            {
                _pitStatus = null;
                Car.LimiterSpeed = null;
            }

            CurrentLaptime = CurrentLaptime.Add(TimeSpan.FromSeconds(timeStep));

            if (LapDistance > track.TrackLength)
            {
                CompletedLaps++;
                LastLaptime = CurrentLaptime;
                CurrentLaptime = TimeSpan.Zero;
                LapDistance -= track.TrackLength;
            }
        }

        private bool IsInBrakingDistance(int targetDistance, double targetSpeed)
        {
            var time = (targetSpeed - Car.CurrentSpeed) / -Car.BrakeDeceleration;
            var targetSlowDistance = targetDistance - Car.CurrentSpeed * time - 0.5 * -Car.BrakeDeceleration * (time * time);

            return LapDistance > targetSlowDistance;
        }
    }
}