namespace SecondMonitor.Hub.Core.Race
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>Un événement de course (équivalent « direction de course ») pour le bloc Événements de l'accueil.</summary>
    public sealed class RaceEventDto
    {
        public long Id { get; set; }

        /// <summary>Temps de session (s) de l'événement.</summary>
        public double Time { get; set; }

        /// <summary>Tour en cours du joueur (ou du leader) au moment de l'événement.</summary>
        public int? Lap { get; set; }

        /// <summary>flag, session, pit, bestlap, track, penalty, retire, position, tyres, damage.</summary>
        public string Kind { get; set; }

        /// <summary>info, good, warn, bad.</summary>
        public string Level { get; set; }

        public string Title { get; set; }

        public string Text { get; set; }

        public string DriverId { get; set; }

        public bool IsPlayer { get; set; }
    }

    /// <summary>
    /// Journal des événements de course, déduit en comparant les instantanés successifs :
    /// drapeaux, départ et arrivée, arrêts des voisins, meilleurs tours, tours invalidés, abandons et disqualifications,
    /// changements de place du joueur, alertes pneus et dégâts. Pas thread-safe (appelé sous le verrou du RaceEngine).
    /// </summary>
    public sealed class RaceEventLog
    {
        private const int MaxEvents = 300;
        private const double FlagGrace = 3;
        private const double PositionGrace = 3;
        private const double AlertRepeat = 90;
        private static readonly Regex Digits = new(@"[0-9]+([.,][0-9]+)?", RegexOptions.Compiled);

        private readonly List<RaceEventDto> _events = new();
        private readonly Dictionary<string, DriverState> _drivers = new();
        private readonly Dictionary<string, double> _flagsReported = new();
        private readonly Dictionary<string, double> _classBest = new();
        private readonly Dictionary<string, double> _alertsSeen = new();
        private long _nextId = 1;
        private long _lastSequence = -1;
        private double _lastTime;
        private string _key;
        private string _phase;
        private string _safetyCar;
        private bool _initialised;
        private int _playerPosition;
        private int _pendingPosition;
        private double _pendingSince;
        private int _invalidLapReported = -1;

        public long SessionId { get; private set; } = 1;

        public long LastId => _events.Count == 0 ? 0 : _events[^1].Id;

        public IReadOnlyList<RaceEventDto> Since(long afterId) => _events.Where(e => e.Id > afterId).ToList();

        public void Reset()
        {
            _events.Clear();
            _drivers.Clear();
            _flagsReported.Clear();
            _classBest.Clear();
            _alertsSeen.Clear();
            _initialised = false;
            _phase = null;
            _safetyCar = null;
            _playerPosition = 0;
            _pendingPosition = 0;
            _invalidLapReported = -1;
            _lastSequence = -1;
            SessionId++;
        }

        /// <param name="snap">Instantané complet.</param>
        /// <param name="sessionKey">Identifiant de session (jeu + circuit + type) : un changement remet le journal à zéro.</param>
        public void Observe(RaceSnapshot snap, string sessionKey)
        {
            if (snap == null || !snap.HasData || snap.Sequence == _lastSequence)
            {
                return;
            }

            if (_initialised && (sessionKey != _key || snap.SessionTime < _lastTime - 5))
            {
                Reset();
            }

            _lastSequence = snap.Sequence;
            _lastTime = snap.SessionTime;
            _key = sessionKey;
            double now = snap.SessionTime;
            bool race = snap.SessionType == "Race";
            DriverRow me = snap.Drivers.FirstOrDefault(d => d.IsPlayer);
            int? lap = me != null ? me.Laps + 1 : snap.LeaderLap > 0 ? snap.LeaderLap : null;

            if (!_initialised)
            {
                _initialised = true;
                _phase = snap.SessionPhase;
                _safetyCar = snap.SafetyCar;
                foreach (DriverRow d in snap.Drivers)
                {
                    _drivers[d.Id] = DriverState.From(d);
                    Best(d, out _);
                }

                foreach (string f in snap.Flags)
                {
                    _flagsReported[f] = now;
                }

                _playerPosition = me != null ? Pos(snap, me) : 0;
                Add(now, lap, "session", "info", $"{SessionName(snap.SessionType)} · {snap.Track?.Name ?? "circuit"}", "Suivi des événements démarré.");
                return;
            }

            // ── Départ, arrivée ──
            if (snap.SessionPhase != _phase)
            {
                if (snap.SessionPhase == "Green" && _phase == "Countdown")
                {
                    Add(now, lap, "session", "good", race ? "Départ" : "Session lancée", race ? "Feu vert." : null);
                }
                else if (snap.SessionPhase == "Checkered")
                {
                    Add(now, lap, "flag", "info", "Drapeau à damier", race ? "Le leader a franchi la ligne : chacun termine son tour." : "Fin de la session.");
                }

                _phase = snap.SessionPhase;
            }

            // ── Drapeaux (avec un délai de grâce pour ne pas répéter un drapeau qui clignote) ──
            foreach (string f in snap.Flags)
            {
                if (!_flagsReported.ContainsKey(f))
                {
                    Add(now, lap, "flag", f == "Red" ? "bad" : "warn", FlagName(f), null);
                }

                _flagsReported[f] = now;
            }

            foreach (string f in _flagsReported.Keys.ToList())
            {
                if (!snap.Flags.Contains(f) && now - _flagsReported[f] >= FlagGrace)
                {
                    _flagsReported.Remove(f);
                    Add(now, lap, "flag", "good", $"Fin : {FlagName(f).ToLowerInvariant()}", "Piste dégagée.");
                }
            }

            if (snap.SafetyCar != _safetyCar)
            {
                if (!string.IsNullOrEmpty(snap.SafetyCar) && snap.SafetyCar != "None")
                {
                    Add(now, lap, "flag", "warn", "Voiture de sécurité", SafetyCarText(snap.SafetyCar));
                }

                _safetyCar = snap.SafetyCar;
            }

            // ── Pilotes ──
            int myPos = me != null ? Pos(snap, me) : 0;
            foreach (DriverRow d in snap.Drivers)
            {
                if (!_drivers.TryGetValue(d.Id, out DriverState prev))
                {
                    _drivers[d.Id] = DriverState.From(d);
                    continue;
                }

                bool neighbour = me != null && d.ClassId == me.ClassId && (d.IsPlayer || Math.Abs(Pos(snap, d) - myPos) <= 3 || Pos(snap, d) == 1);
                string who = Who(d);

                if (race && neighbour && d.InPits && !prev.InPits)
                {
                    Add(now, lap, "pit", d.IsPlayer ? "info" : "info", d.IsPlayer ? "Vous entrez au stand" : $"{who} au stand",
                        $"P{Pos(snap, d)} · relais de {d.Tyres?.StintLaps ?? 0} tour(s){(string.IsNullOrEmpty(d.Tyres?.Compound) ? string.Empty : $" en {d.Tyres.Compound}")}", d);
                }
                else if (race && neighbour && !d.InPits && prev.InPits)
                {
                    Add(now, lap, "pit", "info", d.IsPlayer ? "Vous ressortez des stands" : $"{who} ressort des stands",
                        $"P{Pos(snap, d)}{(d.LastPitDuration is > 0 ? $" · arrêt de {d.LastPitDuration.Value.ToString("0.0", Inv)} s" : string.Empty)}", d);
                }
                else if (!race && d.IsPlayer && d.InPits != prev.InPits)
                {
                    Add(now, lap, "pit", "info", d.InPits ? "Vous rentrez au stand" : "Vous repartez en piste", null, d);
                }

                // Meilleurs tours : record de la catégorie (violet) et record personnel du joueur.
                if (Best(d, out double? improvedFrom))
                {
                    Add(now, lap, "bestlap", "good", $"Meilleur tour {(snap.IsMultiClass ? d.ClassName ?? d.ClassId : "de la session")}",
                        $"{who} en {LapTime(d.BestLap)}{(improvedFrom is double old ? $" ({Signed(d.BestLap.Value - old)})" : string.Empty)}", d);
                }
                else if (d.IsPlayer && d.BestLap is double b && prev.BestLap is double pb && b < pb - 0.0005)
                {
                    Add(now, lap, "bestlap", "good", "Votre meilleur tour", $"{LapTime(b)} ({Signed(b - pb)})", d);
                }

                // Tour invalidé (limites de piste, sortie) : seulement pour le joueur, une fois par tour.
                if (d.IsPlayer && prev.CurrentLapValid && !d.CurrentLapValid && d.Laps != _invalidLapReported && !d.InPits)
                {
                    _invalidLapReported = d.Laps;
                    Add(now, lap, "track", "warn", "Tour invalidé", $"Tour {d.Laps + 1} : limites de piste ou sortie.", d);
                }

                if (d.FinishStatus != prev.FinishStatus && (neighbour || d.IsPlayer || me == null))
                {
                    switch (d.FinishStatus)
                    {
                        case "Dq":
                            Add(now, lap, "penalty", "bad", d.IsPlayer ? "Vous êtes disqualifié" : $"{who} disqualifié", null, d);
                            break;
                        case "Dnf":
                            Add(now, lap, "retire", "warn", d.IsPlayer ? "Abandon" : $"Abandon de {who}", null, d);
                            break;
                        case "Finished" when d.IsPlayer:
                            Add(now, lap, "session", "good", "Arrivée", $"Vous terminez P{Pos(snap, d)}.", d);
                            break;
                    }
                }

                _drivers[d.Id] = DriverState.From(d);
            }

            // ── Places du joueur (en course, après le premier tour, stable quelques secondes) ──
            if (race && me != null && me.Laps >= 1 && myPos > 0)
            {
                if (myPos != _pendingPosition)
                {
                    _pendingPosition = myPos;
                    _pendingSince = now;
                }
                else if (myPos != _playerPosition && now - _pendingSince >= PositionGrace)
                {
                    int diff = _playerPosition - myPos;
                    if (_playerPosition > 0 && !me.InPits)
                    {
                        Add(now, lap, "position", diff > 0 ? "good" : "warn", diff > 0 ? $"Vous gagnez {Places(diff)}" : $"Vous perdez {Places(-diff)}", $"Vous êtes P{myPos}.", me);
                    }

                    _playerPosition = myPos;
                }
            }
            else if (me != null && myPos > 0)
            {
                _playerPosition = myPos;
                _pendingPosition = myPos;
            }

            // ── Alertes pneus et dégâts (une fois par alerte, répétée seulement après une longue absence) ──
            foreach (string alert in snap.Player?.Alerts ?? new List<string>())
            {
                string key = Digits.Replace(alert, "#");
                bool known = _alertsSeen.TryGetValue(key, out double seen) && now - seen < AlertRepeat;
                _alertsSeen[key] = now;
                if (!known)
                {
                    bool damage = alert.StartsWith("Dégât", StringComparison.Ordinal);
                    Add(now, lap, damage ? "damage" : "tyres", damage ? "bad" : "warn", damage ? "Dégâts" : "Pneus", alert);
                }
            }
        }

        private bool Best(DriverRow d, out double? previous)
        {
            previous = null;
            if (d.BestLap is not double b || b <= 0)
            {
                return false;
            }

            string cls = d.ClassId ?? string.Empty;
            if (_classBest.TryGetValue(cls, out double best) && b >= best - 0.0005)
            {
                return false;
            }

            bool had = _classBest.ContainsKey(cls);
            previous = had ? best : null;
            _classBest[cls] = b;
            return _initialised;
        }

        private void Add(double time, int? lap, string kind, string level, string title, string text, DriverRow d = null)
        {
            _events.Add(new RaceEventDto { Id = _nextId++, Time = Math.Round(time, 1), Lap = lap, Kind = kind, Level = level, Title = title, Text = text, DriverId = d?.Id, IsPlayer = d?.IsPlayer == true });
            if (_events.Count > MaxEvents)
            {
                _events.RemoveRange(0, _events.Count - MaxEvents);
            }
        }

        private static int Pos(RaceSnapshot snap, DriverRow d) => snap.IsMultiClass ? d.ClassPosition : d.Position;

        private static string Who(DriverRow d) => d.IsPlayer ? "Vous" : d.Number is int n ? $"#{n} {d.Name}" : d.Name;

        private static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;

        private static string Signed(double v) => (v < 0 ? "−" : "+") + Math.Abs(v).ToString("0.000", Inv);

        private static string Places(int n) => n == 1 ? "une place" : $"{n} places";

        private static string LapTime(double? s)
        {
            if (s is not double v || v <= 0)
            {
                return "—";
            }

            int m = (int)(v / 60);
            return m > 0 ? $"{m}:{(v - (m * 60)).ToString("00.000", Inv)}" : v.ToString("0.000", Inv);
        }

        private static string SessionName(string type) => type switch
        {
            "Race" => "Course",
            "Qualification" => "Qualification",
            "Practice" => "Essais",
            "WarmUp" => "Warm-up",
            _ => "Session",
        };

        public static string FlagName(string flag) => flag switch
        {
            "YellowSector1" => "Drapeau jaune secteur 1",
            "YellowSector2" => "Drapeau jaune secteur 2",
            "YellowSector3" => "Drapeau jaune secteur 3",
            "FullCourseYellow" => "Full Course Yellow",
            "VirtualSafetyCar" => "Voiture de sécurité virtuelle",
            "SafetyCar" => "Voiture de sécurité",
            "Red" => "Drapeau rouge",
            _ => flag,
        };

        private static string SafetyCarText(string state) => state switch
        {
            "WaitingForLeader" => "Elle attend le leader.",
            "PitsClosed" => "Stands fermés.",
            "PitsOpenForLeadLapCars" => "Stands ouverts aux voitures du tour du leader.",
            "PitOpen" => "Stands ouverts.",
            "LastScLap" => "Dernier tour sous voiture de sécurité.",
            "LightsOff" => "Gyrophares éteints : relance imminente.",
            _ => state,
        };

        private sealed class DriverState
        {
            public bool InPits { get; private init; }

            public double? BestLap { get; private init; }

            public bool CurrentLapValid { get; private init; }

            public string FinishStatus { get; private init; }

            public static DriverState From(DriverRow d) => new() { InPits = d.InPits, BestLap = d.BestLap, CurrentLapValid = d.CurrentLapValid, FinishStatus = d.FinishStatus };
        }
    }
}
