namespace SecondMonitor.AccConnector.UDPData
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Structs;

    public class FullUdpData
    {
        private readonly Stopwatch _lastUpdateStopWatch;
        private RealtimeUpdate _lastRealtimeUpdate;

        public FullUdpData()
        {
            _lastUpdateStopWatch = Stopwatch.StartNew();
            CarsDictionary = new Dictionary<int, FullCarData>();
            CurrentTrack = new TrackData()
            {
                TrackId = -200,
                TrackMeters = 100,
                TrackName = "Unknown (Start To Receive Proper Data)"
            };
        }

        public bool LeaderFinished { get; set; }

        public bool IsFilled => !string.IsNullOrWhiteSpace(CurrentTrack.TrackName) && CarsDictionary.Count > 0 && CarsDictionary.All(x => x.Value.LastUpdate.CurrentLap?.Splits != null);

        public Dictionary<int, FullCarData> CarsDictionary { get; }

        public TrackData CurrentTrack { get; set; }

        public RealtimeUpdate LastRealtimeUpdate
        {
            get => _lastRealtimeUpdate;
            set
            {
                _lastUpdateStopWatch.Restart();
                _lastRealtimeUpdate = value;
            }
        }

        public TimeSpan TimeSinceLastUpdate => _lastUpdateStopWatch.Elapsed;
    }
}