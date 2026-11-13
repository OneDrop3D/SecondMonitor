namespace SecondMonitor.Hub.Core.Race
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.Hub.Core.Strategy;
    using SecondMonitor.Hub.Core.Telemetry;
    using SecondMonitor.Hub.Core.Track;
    using SecondMonitor.Remote.Models;

    /// <summary>
    /// Agrège le flux de données du jeu en un état de course complet : classement, écarts, relais, stratégies.
    /// Toutes les méthodes publiques sont thread-safe.
    /// </summary>
    public sealed partial class RaceEngine
    {
        private readonly object _lock = new();
        private readonly Dictionary<string, DriverTracker> _trackers = new();
        private readonly PlayerStrategyTracker _player = new();
        private readonly TrackMapBuilder _trackMap;
        private readonly TelemetryRecorder _telemetry = new();
        private readonly WeatherRecorder _weather = new();
        private SimulatorDataSet _last;
        private string _sessionKey;
        private double _lastSessionTime;
        private double _maxLapDistanceSeen;
        private long _sequence;

        public RaceEngine(string dataDirectory)
        {
            _trackMap = new TrackMapBuilder(System.IO.Path.Combine(dataDirectory, "tracks"));
        }

        public event Action SessionReset;

        /// <summary>Plages de température personnalisées (réglages du hub), consultées à chaque instantané.</summary>
        public Func<IEnumerable<ThermalProfile>> ThermalProfiles { get; set; }

        public DateTime LastUpdateUtc { get; private set; } = DateTime.MinValue;

        public void Process(SimulatorDataSet data, DatagramPayloadKind kind = DatagramPayloadKind.Normal)
        {
            if (data?.SessionInfo == null || kind == DatagramPayloadKind.HearthBeat && data.DriversInfo?.Length == 0)
            {
                return;
            }

            bool reset;
            lock (_lock)
            {
                double now = data.SessionInfo.SessionTime.TotalSeconds;
                string key = $"{data.Source}|{data.SessionInfo.TrackInfo?.TrackFullName}|{data.SessionInfo.SessionType}";
                reset = kind == DatagramPayloadKind.SessionStart || key != _sessionKey || now < _lastSessionTime - 5;
                if (reset)
                {
                    ResetInternal();
                    _sessionKey = key;
                    _trackMap.SetTrack(data.Source, data.SessionInfo.TrackInfo?.TrackFullName ?? "inconnu");
                }

                _lastSessionTime = now;
                _last = data;
                LastUpdateUtc = DateTime.UtcNow;
                _sequence++;

                UseFreshPlayerInfo(data);
                double lapLength = LapLength(data);
                bool worldValid = data.SimulatorSourceInfo?.WorldPositionInvalid != true;
                var seen = new HashSet<string>();
                foreach (DriverInfo driver in data.DriversInfo ?? Array.Empty<DriverInfo>())
                {
                    if (driver == null)
                    {
                        continue;
                    }

                    string id = DriverId(driver);
                    seen.Add(id);
                    if (!_trackers.TryGetValue(id, out DriverTracker tracker))
                    {
                        tracker = new DriverTracker(id);
                        _trackers[id] = tracker;
                    }

                    _maxLapDistanceSeen = Math.Max(_maxLapDistanceSeen, driver.LapDistance);
                    tracker.Update(driver, now, lapLength, worldValid);
                }

                DriverInfo player = PlayerOf(data);
                if (player != null && _trackers.TryGetValue(DriverId(player), out DriverTracker playerTracker))
                {
                    _player.Update(player, playerTracker);
                    _telemetry.Add(now, player, data.SessionInfo);
                    _driving.Update(now, player, data.InputInfo, lapLength, _segments, data.SessionInfo.SessionType.ToString(), data.SessionInfo.SessionPhase.ToString());
                }

                _weather.Add(now, data.SessionInfo.WeatherInfo);

                FeedTrackMap(data, player, lapLength);
            }

            if (reset)
            {
                SessionReset?.Invoke();
            }
        }

        /// <summary>« Nouvelle session » demandée par le pilote : calculs remis à zéro, comme un changement de session dans le jeu.</summary>
        public void StartNewSession()
        {
            lock (_lock)
            {
                ResetInternal();
            }

            SessionReset?.Invoke();
        }

        public void Reset()
        {
            lock (_lock)
            {
                ResetInternal();
                _sessionKey = null;
            }
        }

        private readonly TyreThermalWatch _thermal = new();
        private readonly DrivingWatch _driving = new();

        /// <summary>Événements de pilotage à dire tout de suite (usure brutale, limites de piste, tour annulé, pénalité, départ).</summary>
        public List<DrivingEvent> DrainDrivingEvents()
        {
            lock (_lock)
            {
                return _driving.Drain();
            }
        }

        /// <summary>Usures brutales de la session (pour l'ingénieur).</summary>
        public List<WearSpike> WearSpikes()
        {
            lock (_lock)
            {
                return _driving.Spikes.ToList();
            }
        }

        /// <summary>Résumé du pilotage (pédales) du dernier tour complet.</summary>
        public string DrivingSummary()
        {
            lock (_lock)
            {
                return _driving.DrivingSummary();
            }
        }

        /// <summary>Trace pédales / volant d'un tour du joueur : « current », « last », « best » ou un numéro de tour.</summary>
        public InputTraceDto InputTrace(string which)
        {
            lock (_lock)
            {
                DriverInfo player = _last == null ? null : PlayerOf(_last);
                var times = new Dictionary<int, double?>();
                if (player != null && _trackers.TryGetValue(DriverId(player), out DriverTracker t))
                {
                    foreach (var lap in t.Laps.Where(l => l.Valid && !l.IsPitLap && !l.IsOutLap))
                    {
                        times[lap.Lap] = lap.Time;
                    }
                }

                return _driving.Trace(which ?? "last", times);
            }
        }
        private readonly DamageWatch _damage = new();
        private SecondMonitor.Hub.Core.Track.LibraryTrack _libraryTrack;
        private readonly SecondMonitor.Hub.Core.Track.TrackCalibration _libraryCalibration = new();

        public RaceSnapshot BuildSnapshot()
        {
            lock (_lock)
            {
                RaceSnapshot snap = BuildSnapshotInternal();
                DecorateSnapshot(snap); // temps / tours restants, événements de course (RaceEngine.Dashboard.cs)
                return snap;
            }
        }

        /// <summary>Accès en lecture à l'historique d'un pilote (pour l'IA et les vues de détail).</summary>
        public T Read<T>(Func<IReadOnlyDictionary<string, DriverTracker>, SimulatorDataSet, T> reader)
        {
            lock (_lock)
            {
                return reader(_trackers, _last);
            }
        }

        public static string DriverId(DriverInfo d) =>
            !string.IsNullOrEmpty(d.DriverSessionId) ? d.DriverSessionId : $"{d.DriverLongName}#{d.CarRaceNumber}";

        private static DriverInfo PlayerOf(SimulatorDataSet data) =>
            data.DriversInfo?.FirstOrDefault(d => d?.IsPlayer == true) ?? (data.PlayerInfo?.DriverLongName != null ? data.PlayerInfo : null);

        private double LapLength(SimulatorDataSet data)
        {
            double length = data.SessionInfo.TrackInfo?.LayoutLength?.InMeters ?? 0;
            return length > 100 ? length : (_maxLapDistanceSeen > 500 ? _maxLapDistanceSeen : 0);
        }

        /// <summary>
        /// Le PC envoie la voiture du joueur 5×/s mais la liste complète des pilotes seulement 1×/s (réglage par défaut) :
        /// on remplace l'entrée du joueur par sa version la plus récente pour des calculs à pleine fréquence.
        /// </summary>
        private static void UseFreshPlayerInfo(SimulatorDataSet data)
        {
            DriverInfo fresh = data.PlayerInfo;
            DriverInfo[] drivers = data.DriversInfo;
            if (fresh == null || drivers == null || string.IsNullOrEmpty(fresh.DriverSessionId))
            {
                return;
            }

            int idx = Array.FindIndex(drivers, d => d?.DriverSessionId == fresh.DriverSessionId);
            if (idx < 0 || ReferenceEquals(drivers[idx], fresh))
            {
                return;
            }

            var copy = (DriverInfo[])drivers.Clone();
            fresh.IsPlayer = true;
            copy[idx] = fresh;
            data.DriversInfo = copy;
        }

        private void ResetInternal()
        {
            _thermal.Reset();
            _damage.Reset();
            _driving.Reset();
            _libraryTrack = null;
            _libraryCalibration.Reset();
            _trackers.Clear();
            _player.Reset();
            _telemetry.Reset();
            _weather.Reset();
            _segments = new List<TrackSegment>();
            _segmentsVersion++;
            _maxLapDistanceSeen = 0;
            _last = null;
        }

        private void FeedTrackMap(SimulatorDataSet data, DriverInfo player, double lapLength)
        {
            if (data.SimulatorSourceInfo?.WorldPositionInvalid == true)
            {
                return;
            }

            // Recalage du tracé de bibliothèque : toutes les voitures en piste donnent (fraction du tour, x, z).
            if (_trackMap.Outline == null && lapLength > 0)
            {
                foreach (DriverInfo d in data.DriversInfo ?? Array.Empty<DriverInfo>())
                {
                    if (d?.WorldPosition != null && !d.InPits && d.Speed?.InMs > 5)
                    {
                        _libraryCalibration.Add(d.LapDistance / lapLength, d.WorldPosition.X.InMeters, d.WorldPosition.Z.InMeters);
                    }
                }
            }

            DriverInfo reference = player != null && player.Speed?.InMs > 5 ? player
                : data.DriversInfo?.Where(d => d != null && !d.InPits && d.Speed?.InMs > 5).OrderBy(d => d.Position).FirstOrDefault();
            if (reference?.WorldPosition == null)
            {
                return;
            }

            _trackMap.AddSample(reference.CompletedLaps, reference.LapDistance, lapLength, reference.WorldPosition.X.InMeters, reference.WorldPosition.Z.InMeters, reference.InPits);
        }

        private RaceSnapshot BuildSnapshotInternal()
        {
            var snap = new RaceSnapshot { Sequence = _sequence };
            SimulatorDataSet data = _last;
            if (data == null)
            {
                return snap;
            }

            SessionInfo session = data.SessionInfo;
            double now = session.SessionTime.TotalSeconds;
            double lapLength = LapLength(data);
            bool isRace = session.SessionType == SessionType.Race;
            bool worldValid = data.SimulatorSourceInfo?.WorldPositionInvalid != true;

            // Le collecteur peut annoncer un jeu sans voiture (menus, télémétrie du jeu coupée) : ce ne sont pas encore des données.
            snap.Source = data.Source;
            snap.HasData = data.DriversInfo?.Any(d => d != null) == true;
            if (!snap.HasData)
            {
                return snap;
            }

            snap.SessionType = session.SessionType.ToString();
            snap.SessionPhase = session.SessionPhase.ToString();
            snap.LengthType = session.SessionLengthType.ToString();
            snap.SessionTime = now;
            snap.TimeRemaining = session.SessionTimeRemaining > 0 ? session.SessionTimeRemaining : null;
            snap.TotalLaps = session.TotalNumberOfLaps;
            snap.LeaderLap = session.LeaderCurrentLap;
            snap.IsMultiClass = session.IsMultiClass;
            snap.IsMultiplayer = session.IsMultiplayer;
            snap.Flags = Enum.GetValues<FlagKind>().Where(f => f != FlagKind.None && session.ActiveFlags.HasFlag(f)).Select(f => f.ToString()).ToList();
            snap.SafetyCar = session.SafetyCarState.ToString();
            snap.Track = new TrackDto
            {
                Name = session.TrackInfo?.TrackName,
                Layout = session.TrackInfo?.TrackLayoutName,
                Length = lapLength,
                Outline = _trackMap.Outline,
                OutlineSource = _trackMap.Outline != null ? "jeu" : null,
                WorldPositionsValid = worldValid,
            };
            if (snap.Track.Outline == null)
            {
                // Pas encore de tracé appris (premier tour, ou jeu sans positions) : le vrai tracé du circuit s'il est connu.
                // Départ et sens recalés sur les positions du jeu ; rien n'est affiché tant qu'un tracé peu sûr n'est pas calé.
                _libraryTrack ??= SecondMonitor.Hub.Core.Track.TrackLibrary.Find(snap.Track.Name, snap.Track.Layout, lapLength);
                var library = _libraryCalibration.Resolve(_libraryTrack);
                if (library != null)
                {
                    snap.Track.Outline = library.Points;
                    snap.Track.OutlineSource = "bibliothèque";
                    snap.Track.OutlineCredit = $"{_libraryTrack.Name} · {_libraryTrack.Source}";
                    snap.Track.OutlineCalibration = library.Calibrated ? "calé" : "estimé";
                }
            }
            snap.Weather = new WeatherDto
            {
                Air = TempOrNull(session.WeatherInfo?.AirTemperature),
                TrackTemp = TempOrNull(session.WeatherInfo?.TrackTemperature),
                Rain = session.WeatherInfo?.RainIntensity ?? 0,
                Wetness = session.WeatherInfo?.TrackWetness ?? 0,
                Wind = session.WeatherInfo?.HasWindInformation == true ? Math.Round(session.WeatherInfo.WindSpeed?.InKph ?? 0, 1) : null,
                WindFrom = session.WeatherInfo?.HasWindInformation == true ? Math.Round(session.WeatherInfo.WindDirectionFrom, 0) : null,
                Trend = _weather.TrendSummary,
            };
            if (session.PitWindow != null && session.PitWindow.PitWindowState != PitWindowState.None)
            {
                snap.PitWindow = new PitWindowDto { State = session.PitWindow.PitWindowState.ToString(), Start = session.PitWindow.PitWindowStart, End = session.PitWindow.PitWindowEnd };
            }

            var current = (data.DriversInfo ?? Array.Empty<DriverInfo>())
                .Where(d => d != null && _trackers.ContainsKey(DriverId(d)))
                .Select(d => _trackers[DriverId(d)])
                .OrderBy(t => t.Latest.Position <= 0 ? int.MaxValue : t.Latest.Position)
                .ToList();

            DriverInfo playerInfo = PlayerOf(data);
            DriverTracker playerTracker = playerInfo != null && _trackers.TryGetValue(DriverId(playerInfo), out var pt) ? pt : null;

            double overallBest = current.Select(t => t.Latest.Timing?.BestLapTime.TotalSeconds ?? 0).Where(v => v > 0).DefaultIfEmpty(0).Min();
            var classBest = current.GroupBy(t => t.Latest.CarClassId ?? string.Empty)
                .ToDictionary(g => g.Key, g => g.Select(t => t.Latest.Timing?.BestLapTime.TotalSeconds ?? 0).Where(v => v > 0).DefaultIfEmpty(0).Min());

            // Relais typiques du plateau (par catégorie) pour estimer les arrêts des adversaires.
            var fieldStints = current.GroupBy(t => t.Latest.CarClassId ?? string.Empty).ToDictionary(
                g => g.Key,
                g => g.SelectMany(t => t.Stints.Where(s => s.EndLap.HasValue && s.Laps >= 3).Select(s => (double)s.Laps)).ToList());

            PlayerStrategy playerStrategy = playerInfo != null ? _player.Build(data, playerInfo, playerTracker, lapLength) : null;
            snap.Player = playerStrategy;
            if (playerStrategy != null)
            {
                ThermalProfile profile = ThermalProfile.Find(ThermalProfiles?.Invoke(), data.Source, playerInfo.CarName);
                ApplyProfile(playerStrategy, profile);
                if (profile != null)
                {
                    snap.ThermalProfile = string.Join(" · ", new[] { profile.Game, profile.Car }.Where(x => !string.IsNullOrWhiteSpace(x)).DefaultIfEmpty("tous les jeux"));
                }

                playerStrategy.Controls = _driving.Controls;
                playerStrategy.Inputs = _driving.Inputs;
                bool moving = !playerInfo.InPits && (playerInfo.Speed?.InKph ?? 0) > 20;
                playerStrategy.TyreThermal = _thermal.Update(now, playerInfo.CompletedLaps, moving, playerStrategy.Wheels);
                playerStrategy.Alerts = TyreThermalWatch.Alerts(playerStrategy.TyreThermal);

                var clean = playerTracker?.Laps.Where(l => l.Time > 0 && !l.IsPitLap && !l.IsOutLap && l.Valid).Select(l => (l.Lap, l.Time.Value)).ToList() ?? new List<(int, double)>();
                playerStrategy.Damage = _damage.Update(playerInfo, now, playerInfo.CompletedLaps, clean);
                if (_damage.LastEvent is DamageEvent ev)
                {
                    playerStrategy.DamageEvent = ev;
                    if (now - ev.Time < 120 || ev.LossPerLap.HasValue && now - ev.Time < 600)
                    {
                        playerStrategy.Alerts.Insert(0, ev.LossPerLap is double loss
                            ? $"Dégât : {ev.Label} ({ev.Percent:0} %), il coûte {loss:0.00} s par tour."
                            : $"Dégât : {ev.Label} ({ev.Percent:0} %).");
                    }
                }
            }

            DriverTracker leader = current.FirstOrDefault();
            var classLeaders = new Dictionary<string, DriverTracker>();
            var classPrevious = new Dictionary<string, DriverTracker>();
            DriverTracker previous = null;
            bool anyOpponentWear = false;
            bool anyCompound = false;

            foreach (DriverTracker t in current)
            {
                DriverInfo d = t.Latest;
                string classId = d.CarClassId ?? string.Empty;
                classLeaders.TryAdd(classId, t);
                classPrevious.TryGetValue(classId, out DriverTracker previousInClass);

                var row = new DriverRow
                {
                    Id = t.Id,
                    Name = d.DriverLongName,
                    ShortName = string.IsNullOrWhiteSpace(d.DriverShortName) ? ShortName(d.DriverLongName) : d.DriverShortName,
                    Number = d.IsCarRaceNumberFilled || d.CarRaceNumber > 0 ? d.CarRaceNumber : null,
                    Team = d.TeamName,
                    Car = d.CarName,
                    ClassId = classId,
                    ClassName = d.CarClassName,
                    Position = d.Position,
                    ClassPosition = d.PositionInClass > 0 ? d.PositionInClass : d.Position,
                    IsPlayer = d.IsPlayer,
                    InPits = d.InPits,
                    PitRequested = d.PitStopRequested,
                    FinishStatus = d.FinishStatus.ToString(),
                    Laps = d.CompletedLaps,
                    LapFraction = lapLength > 0 ? Math.Clamp(d.LapDistance / lapLength, 0, 1) : 0,
                    LastLap = Seconds(d.Timing?.LastLapTime),
                    BestLap = Seconds(d.Timing?.BestLapTime),
                    CurrentLap = Seconds(d.Timing?.CurrentLapTime),
                    Sector = d.Timing?.CurrentSector ?? 0,
                    LastSectors = new[] { Seconds(d.Timing?.LastSector1Time), Seconds(d.Timing?.LastSector2Time), Seconds(d.Timing?.LastSector3Time) },
                    BestSectors = (double?[])t.BestSectors.Clone(),
                    BestSectorLaps = (int?[])t.BestSectorLaps.Clone(),
                    BestLapNumber = t.Laps.Where(l => l.Time > 0 && !l.IsPitLap).OrderBy(l => l.Time).FirstOrDefault()?.Lap,
                    Speed = d.Speed?.InKph ?? 0,
                    X = worldValid && d.WorldPosition != null ? Math.Round(d.WorldPosition.X.InMeters, 1) : null,
                    Z = worldValid && d.WorldPosition != null ? Math.Round(d.WorldPosition.Z.InMeters, 1) : null,
                    CurrentLapValid = d.CurrentLapValid,
                    PitStops = t.PitStops.Count,
                    LastPitLap = t.PitStops.LastOrDefault()?.Lap,
                    LastPitDuration = t.PitStops.LastOrDefault()?.Duration,
                    PaceLast3 = StrategyMath.RobustAverage(t.Laps.Where(l => l.IsClean && l.Time.HasValue).TakeLast(3).Select(l => l.Time.Value)),
                    Degradation = t.CurrentStintDegradation(),
                    PositionsGained = isRace && t.StartPosition > 0 && d.Position > 0 ? t.StartPosition - d.Position : 0,
                };

                row.HasOverallBest = row.BestLap > 0 && Math.Abs(row.BestLap.Value - overallBest) < 0.0005;
                row.HasClassBest = row.BestLap > 0 && classBest.TryGetValue(classId, out double cb) && Math.Abs(row.BestLap.Value - cb) < 0.0005;
                row.LastLapIsPersonalBest = row.LastLap > 0 && row.BestLap > 0 && Math.Abs(row.LastLap.Value - row.BestLap.Value) < 0.0005;

                // Pneus
                row.Tyres.Compound = t.CurrentCompound;
                row.Tyres.StintLaps = t.CurrentStint?.Laps ?? 0;
                anyCompound |= !string.IsNullOrEmpty(t.CurrentCompound) && !d.IsPlayer;
                var wheels = d.CarInfo?.WheelsInfo;
                if (wheels != null)
                {
                    double[] wear = { wheels.FrontLeft.TyreWear.ActualWear, wheels.FrontRight.TyreWear.ActualWear, wheels.RearLeft.TyreWear.ActualWear, wheels.RearRight.TyreWear.ActualWear };
                    if (wear.Any(w => w > 0.0001))
                    {
                        row.Tyres.Wear = wear.Select(w => Math.Round(w * 100, 1)).ToArray();
                        anyOpponentWear |= !d.IsPlayer;
                    }
                }

                double fuel = d.CarInfo?.FuelSystemInfo?.FuelRemaining?.InLiters ?? 0;
                row.FuelLitres = fuel > 0 ? Math.Round(fuel, 1) : null;

                row.Stints = t.Stints.Select(s => new StintDto
                {
                    Index = s.Index,
                    Compound = s.Compound,
                    StartLap = s.StartLap,
                    EndLap = s.EndLap,
                    Laps = s.Laps,
                    AverageLap = s.AverageLap,
                    BestLap = s.BestLap,
                    Current = s == t.CurrentStint,
                }).ToList();

                // Écarts
                if (isRace)
                {
                    (row.GapToLeader, row.GapToLeaderLaps) = leader == null || leader == t ? (0d, 0) : Gap(leader, t, now, lapLength);
                    (row.Interval, row.IntervalLaps) = previous == null ? (0d, 0) : Gap(previous, t, now, lapLength);
                    DriverTracker classLeader = classLeaders[classId];
                    (row.GapToClassLeader, row.GapToClassLeaderLaps) = classLeader == t ? (0d, 0) : Gap(classLeader, t, now, lapLength);

                    if (row.Interval == null && row.IntervalLaps == 0 && d.Timing?.GapAhead > TimeSpan.Zero)
                    {
                        row.Interval = d.Timing.GapAhead.TotalSeconds; // repli : écart fourni par le jeu
                    }

                    if (playerTracker != null && playerTracker != t)
                    {
                        row.GapToPlayer = SignedGap(playerTracker, t, now, lapLength);
                    }

                    row.NextStop = d.IsPlayer && playerStrategy?.RecommendedStopLap is int stopLap
                        ? new PitEstimate { Lap = stopLap, LapsUntil = Math.Max(0, stopLap - d.CompletedLaps), Confidence = "high", Reason = $"votre consommation réelle ({playerStrategy.RecommendedStopReason})" }
                        : EstimateNextStop(t, fieldStints.TryGetValue(classId, out var fs) ? fs : null, playerStrategy, playerInfo, d);
                }
                else
                {
                    double best = row.BestLap ?? 0;
                    double leaderBest = leader?.Latest.Timing?.BestLapTime.TotalSeconds ?? 0;
                    double aheadBest = previous?.Latest.Timing?.BestLapTime.TotalSeconds ?? 0;
                    double classLeaderBest = classLeaders[classId].Latest.Timing?.BestLapTime.TotalSeconds ?? 0;
                    row.GapToLeader = best > 0 && leaderBest > 0 ? best - leaderBest : null;
                    row.Interval = best > 0 && aheadBest > 0 && previous != null ? best - aheadBest : (previous == null ? 0 : null);
                    row.GapToClassLeader = best > 0 && classLeaderBest > 0 ? best - classLeaderBest : null;
                    double playerBest = playerInfo?.Timing?.BestLapTime.TotalSeconds ?? 0;
                    row.GapToPlayer = best > 0 && playerBest > 0 ? best - playerBest : null;
                }

                snap.Drivers.Add(row);
                previous = t;
                classPrevious[classId] = t;
            }

            if (playerStrategy != null && playerTracker != null)
            {
                (playerStrategy.DeltaBest, playerStrategy.DeltaLast) = playerTracker.Analysis.LiveDelta();
                playerStrategy.Speed = Math.Round(playerInfo.Speed?.InKph ?? 0, 1);
            }

            if (playerStrategy != null && playerTracker != null && isRace)
            {
                playerStrategy.Battles = BuildBattles(snap, playerTracker.Id);
            }

            if (playerStrategy != null)
            {
                playerStrategy.PitPlan = SecondMonitor.Hub.Core.Strategy.PitPlanner.Build(snap, _trackers);
            }

            snap.DataRate.PlayerHz = Math.Round(playerTracker?.Analysis.SampleHz ?? 0, 1);
            var opponentRates = current.Where(t => t != playerTracker && t.Analysis.SampleHz > 0).Select(t => t.Analysis.SampleHz).ToList();
            snap.DataRate.OpponentsHz = opponentRates.Count == 0 ? 0 : Math.Round(StrategyMath.Median(opponentRates), 1);
            if (opponentRates.Count > 0 && snap.DataRate.OpponentsHz < 3)
            {
                snap.Capabilities.Add("lowRateOpponents");
            }

            if (worldValid)
            {
                snap.Capabilities.Add("worldPositions");
            }

            if (anyCompound)
            {
                snap.Capabilities.Add("opponentCompounds");
            }

            if (anyOpponentWear)
            {
                snap.Capabilities.Add("opponentTyreWear");
            }

            if (playerStrategy?.Energy != null)
            {
                snap.Capabilities.Add("virtualEnergy");
            }

            return snap;
        }

        /// <summary>Écart « sur la piste » entre une voiture devant (ahead) et une derrière (behind).</summary>
        private static (double? Seconds, int Laps) Gap(DriverTracker ahead, DriverTracker behind, double now, double lapLength)
        {
            double diff = ahead.Distance - behind.Distance;
            if (lapLength > 0 && diff >= lapLength)
            {
                return (null, (int)Math.Floor(diff / lapLength));
            }

            double? passed = ahead.Timeline.TimeAtDistance(behind.Distance);
            if (passed.HasValue)
            {
                double gap = now - passed.Value;
                if (gap >= 0 && gap < 900)
                {
                    return (gap, 0);
                }
            }

            return (null, 0);
        }

        /// <summary>Écart signé par rapport au joueur : négatif = devant le joueur.</summary>
        private static double? SignedGap(DriverTracker player, DriverTracker other, double now, double lapLength)
        {
            if (other.Distance >= player.Distance)
            {
                var (s, laps) = Gap(other, player, now, lapLength);
                return laps > 0 ? null : -s;
            }

            var (s2, laps2) = Gap(player, other, now, lapLength);
            return laps2 > 0 ? null : s2;
        }

        private static PitEstimate EstimateNextStop(DriverTracker t, List<double> fieldStints, PlayerStrategy player, DriverInfo playerInfo, DriverInfo d)
        {
            StintRecord stint = t.CurrentStint;
            if (stint == null || d.FinishStatus is DriverFinishStatus.Finished or DriverFinishStatus.Dnf or DriverFinishStatus.Dq || d.InPits)
            {
                return null;
            }

            double? length = null;
            string confidence = null;
            string reason = null;
            var own = t.Stints.Where(s => s.EndLap.HasValue && s.Laps >= 3).Select(s => (double)s.Laps).ToList();
            if (own.Count > 0)
            {
                length = StrategyMath.Median(own);
                confidence = "high";
                reason = $"ses relais précédents (~{length:0} tours)";
            }
            else if (fieldStints?.Count >= 2)
            {
                length = StrategyMath.Median(fieldStints);
                confidence = "medium";
                reason = $"relais typiques de sa catégorie (~{length:0} tours)";
            }
            else if (player?.Fuel?.PerLap > 0 && playerInfo != null && playerInfo.CarClassId == d.CarClassId)
            {
                length = Math.Floor(player.Fuel.Capacity / player.Fuel.PerLap.Value);
                confidence = "low";
                reason = $"autonomie d'un plein de votre voiture (~{length:0} tours)";
            }

            if (!length.HasValue)
            {
                return null;
            }

            int lap = stint.StartLap + (int)Math.Round(length.Value);
            if (lap <= d.CompletedLaps)
            {
                // Relais déjà plus long que prévu : l'arrêt est attendu à tout moment.
                lap = d.CompletedLaps + 1;
                reason += ", relais déjà plus long que prévu";
            }

            return new PitEstimate { Lap = lap, LapsUntil = Math.Max(0, lap - d.CompletedLaps), Confidence = confidence, Reason = reason };
        }

        private static double? Seconds(TimeSpan? value) => value.HasValue && value.Value > TimeSpan.Zero ? Math.Round(value.Value.TotalSeconds, 3) : null;

        private static double? TempOrNull(Temperature t) => t == null || t.IsZero || Math.Abs(t.InCelsius + 1) < 0.001 ? null : Math.Round(t.InCelsius, 1);

        private static void ApplyProfile(PlayerStrategy p, ThermalProfile profile)
        {
            foreach (WheelStatus w in p.Wheels)
            {
                w.CoreIdeal = w.IdealTemp;
                w.CoreWindow = w.TempWindow;
                if (profile == null)
                {
                    continue;
                }

                if (profile.TyreMin.HasValue && profile.TyreMax.HasValue)
                {
                    w.IdealTemp = (profile.TyreMin + profile.TyreMax) / 2;
                    w.TempWindow = Math.Abs(profile.TyreMax.Value - profile.TyreMin.Value) / 2;
                    w.CoreIdeal = w.IdealTemp;
                    w.CoreWindow = w.TempWindow;
                }

                if (profile.CoreMin.HasValue && profile.CoreMax.HasValue)
                {
                    w.CoreIdeal = (profile.CoreMin + profile.CoreMax) / 2;
                    w.CoreWindow = Math.Abs(profile.CoreMax.Value - profile.CoreMin.Value) / 2;
                }

                if (profile.BrakeMin.HasValue && profile.BrakeMax.HasValue)
                {
                    w.BrakeIdeal = (profile.BrakeMin + profile.BrakeMax) / 2;
                    w.BrakeWindow = Math.Abs(profile.BrakeMax.Value - profile.BrakeMin.Value) / 2;
                }

                if (profile.PressureIdeal.HasValue)
                {
                    w.IdealPressure = profile.PressureIdeal;
                }
            }
        }

        private static List<BattleDto> BuildBattles(RaceSnapshot snap, string playerId)
        {
            var list = new List<BattleDto>();
            DriverRow me = snap.Drivers.FirstOrDefault(d => d.Id == playerId);
            if (me == null)
            {
                return list;
            }

            var classmates = snap.Drivers.Where(d => d.ClassId == me.ClassId).ToList();
            int idx = classmates.IndexOf(me);
            foreach (var (other, side) in new[] { (idx > 0 ? classmates[idx - 1] : null, "ahead"), (idx < classmates.Count - 1 ? classmates[idx + 1] : null, "behind") })
            {
                if (other == null)
                {
                    continue;
                }

                double? gap = other.GapToPlayer.HasValue ? Math.Abs(other.GapToPlayer.Value) : null;
                // Rythme : moyenne des 3 derniers tours propres ; > 0 = le joueur est plus rapide.
                double? delta = me.PaceLast3.HasValue && other.PaceLast3.HasValue ? Math.Round(other.PaceLast3.Value - me.PaceLast3.Value, 3) : null;
                double? toContact = null;
                if (gap.HasValue && delta.HasValue)
                {
                    double closing = side == "ahead" ? delta.Value : -delta.Value; // secondes par tour où l'écart se réduit
                    if (closing > 0.03)
                    {
                        toContact = Math.Round(Math.Max(0, gap.Value - 1.0) / closing, 1);
                    }
                }

                list.Add(new BattleDto
                {
                    Id = other.Id,
                    Side = side,
                    Name = other.Name,
                    Number = other.Number,
                    Gap = gap.HasValue ? Math.Round(gap.Value, 3) : null,
                    PaceDelta = delta,
                    LapsToContact = toContact,
                    Compound = other.Tyres.Compound,
                    TyreLaps = other.Tyres.StintLaps,
                    NextStopLap = other.NextStop?.Lap,
                });
            }

            return list;
        }

        private static string ShortName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "???";
            }

            string[] parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string last = parts[^1];
            return last.Length <= 3 ? last.ToUpperInvariant() : last[..3].ToUpperInvariant();
        }
    }
}
