namespace SecondMonitor.Remote.Debug.Simulation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    public class RaceSession
    {
        private readonly Driver[] _trackedDrivers;
        private readonly Entry[] _entries;
        private readonly Driver _teamFirstDriver;
        private readonly Driver _teamSecondDriver;
        private readonly Driver _trackedDriver;

        public RaceSession() : this(new Driver("Violet Mercado"), new Driver("Chloe Baldwin"), new Driver("Donald Cruz"))
        {
        }

        private RaceSession(Driver teamFirstDriver, Driver teamSecondDriver, Driver trackedDriver)
        {
            Track = new Track();

            _teamFirstDriver = teamFirstDriver;
            _teamSecondDriver = teamSecondDriver;
            _trackedDriver = trackedDriver;

            var untrackedDriver = new Driver("Dexter Mccoy");

            _trackedDrivers = new[]
            {
                _trackedDriver,
                _teamFirstDriver,
                _teamSecondDriver
            };

            var carName = "Mazda 626";
            var className = "Group B";
            var random = new Random();
            _entries = new[]
            {
                new Entry(new Car(carName, className), random.Next(3, 7), _trackedDriver),
                new Entry(new Car(carName, className), random.Next(3, 7), _teamFirstDriver, _teamSecondDriver),
                new Entry(new Car(carName, className), random.Next(3, 7), untrackedDriver)
            };

            for (int i = 0; i < _entries.Length; i++)
            {
                _entries[i].Position = i + 1;
                _entries[i].MoveToGridSlot(Track);
            }

            Laps = 10;
        }

        public IReadOnlyList<Driver> TrackedDrivers => _trackedDrivers;

        public IReadOnlyList<Entry> Entries => _entries;
        public Track Track { get; }
        public int Laps
        {
            get;
        }

        public void StepSimulation(double timeStep)
        {
            foreach (var entry in _entries)
            {
                entry.Drive(Track, timeStep);
            }

            var orderedEntry = _entries.Select(entry =>
                {
                    var totalDistance = (entry.CompletedLaps * Track.TrackLength) + entry.LapDistance;
                    return (entry, totalDistance);
                }).OrderByDescending(x => x.totalDistance)
                .Select(x => x.entry);

            var position = 1;
            foreach (var entry in orderedEntry)
            {
                entry.Position = position++;
            }
        }

        public RaceSession CreateFollowUpSession()
        {
            return new RaceSession(_teamSecondDriver, _teamFirstDriver, _trackedDriver);
        }
    }
}