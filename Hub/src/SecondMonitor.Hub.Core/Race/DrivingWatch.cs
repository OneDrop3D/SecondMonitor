namespace SecondMonitor.Hub.Core.Race
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.DataModel.Snapshot.Systems;

    /// <summary>
    /// Surveillance du pilotage à pleine fréquence (chaque donnée du joueur) :
    /// - usure brutale d'un pneu (plusieurs points en quelques secondes) avec la phase où elle arrive (freinage, entrée, milieu, sortie) ;
    /// - limites de piste (points ajoutés, trop large ou coupé), tour annulé, nouvelle pénalité ;
    /// - début de session moteur coupé ou au point mort ;
    /// - traces pédales et volant par tour (tour en cours, dernier, meilleur), pour l'analyse du pilotage,
    ///   avec le rapport engagé, le régime, le temps écoulé depuis la ligne, les passages de rapports et les réglages en roulant changés.
    /// Les événements urgents sont lus par le service radio (Drain) et dits tout de suite, en une phrase.
    /// </summary>
    public sealed class DrivingWatch
    {
        /// <summary>Pas des traces de pilotage (mètres).</summary>
        public const double BinMeters = 10;

        /// <summary>Tours gardés en mémoire pour l'analyse du pilotage et la comparaison des tours.</summary>
        public const int MaxStoredLaps = 40;

        private const double SpikeWindowSeconds = 4;
        private const double SpikeThresholdPct = 1.5;
        private static readonly string[] WheelKeys = { "FL", "FR", "RL", "RR" };

        /// <summary>Réglages en roulant suivis tour par tour (clé courte de l'interface → lecture dans les données du jeu).</summary>
        private static readonly (string Key, Func<CarControls, double?> Read)[] ControlKeys =
        {
            ("bias", c => c.FrontBrakeBiasPct.HasValue ? Math.Round(c.FrontBrakeBiasPct.Value, 1) : null),
            ("tc", c => c.Tc),
            ("tcCut", c => c.TcCut),
            ("tcSlip", c => c.TcSlip),
            ("abs", c => c.Abs),
            ("map", c => c.MotorMap),
            ("migration", c => c.Migration),
            ("arbFront", c => c.FrontAntiRoll),
            ("arbRear", c => c.RearAntiRoll),
        };

        private readonly Queue<Sample> _recent = new();
        private readonly List<DrivingEvent> _events = new();
        private readonly List<WearSpike> _spikes = new();
        private readonly Dictionary<int, InputLap> _laps = new();
        private readonly Dictionary<string, double> _controlValues = new();
        private InputLap _current;
        private Sample _previousTraced;
        private Sample _lastInGear;
        private double _lastSpikeAt = double.NegativeInfinity;
        private double? _trackLimitPoints;
        private int? _penalties;
        private bool _lapInvalidated;
        private int _invalidatedLap = -1;
        private bool _startChecked;
        private double _sessionStart = double.NaN;
        private long _nextId;

        /// <summary>Réglages et état de la voiture du dernier instantané.</summary>
        public ControlsDto Controls { get; private set; }

        /// <summary>Pédales et volant à l'instant.</summary>
        public InputsDto Inputs { get; private set; }

        /// <summary>Usures brutales de la session (les plus récentes en dernier), pour l'ingénieur.</summary>
        public IReadOnlyList<WearSpike> Spikes => _spikes;

        public void Reset()
        {
            _recent.Clear();
            _events.Clear();
            _spikes.Clear();
            _laps.Clear();
            _controlValues.Clear();
            _current = null;
            _previousTraced = null;
            _lastInGear = null;
            _lastSpikeAt = double.NegativeInfinity;
            _trackLimitPoints = null;
            _penalties = null;
            _lapInvalidated = false;
            _invalidatedLap = -1;
            _startChecked = false;
            _sessionStart = double.NaN;
            Controls = null;
            Inputs = null;
        }

        /// <summary>Événements à dire tout de suite (vidés à la lecture).</summary>
        public List<DrivingEvent> Drain()
        {
            var list = _events.ToList();
            _events.Clear();
            return list;
        }

        /// <param name="corners">Virages connus (nom, début, sommet, fin en mètres), pour situer les événements.</param>
        public void Update(double now, DriverInfo player, InputInfo input, double lapLength, IReadOnlyList<TrackSegment> corners, string sessionType, string sessionPhase)
        {
            CarInfo car = player?.CarInfo;
            if (car == null)
            {
                return;
            }

            if (double.IsNaN(_sessionStart))
            {
                _sessionStart = now;
            }

            double speed = player.Speed?.InKph ?? 0;
            Inputs = input == null ? null : new InputsDto
            {
                Throttle = Math.Round(Math.Clamp(input.ThrottlePedalPosition, 0, 1) * 100),
                Brake = Math.Round(Math.Clamp(input.BrakePedalPosition, 0, 1) * 100),
                Clutch = Math.Round(Math.Clamp(input.ClutchPedalPosition, 0, 1) * 100),
                Steering = Math.Round(Math.Clamp(input.SteeringInput, -1, 1) * 100),
                Gear = car.CurrentGear,
                Rpm = car.EngineRpm,
            };
            Controls = ControlsDto.From(car);

            var sample = new Sample
            {
                T = now,
                Dist = player.LapDistance,
                Lap = player.CompletedLaps,
                Speed = speed,
                Throttle = input?.ThrottlePedalPosition ?? 0,
                Brake = input?.BrakePedalPosition ?? 0,
                Steer = input?.SteeringInput ?? 0,
                Wear = Wear(car.WheelsInfo),
                Detached = Detached(car.WheelsInfo),
                InPits = player.InPits,
                Gear = GearNumber(car.CurrentGear),
                Rpm = car.EngineRpm,
            };

            if (_recent.Count > 0 && now < _recent.Peek().T - 1)
            {
                _recent.Clear(); // temps qui recule (retour au stand, rembobinage)
            }

            RecordTrace(sample, lapLength, car.Controls);
            _recent.Enqueue(sample);
            while (_recent.Count > 0 && now - _recent.Peek().T > SpikeWindowSeconds)
            {
                _recent.Dequeue();
            }

            DetectWearSpike(sample, corners, lapLength);
            DetectRules(sample, car.Controls, corners, lapLength);
            DetectStart(now, sample, car, sessionType, sessionPhase);
        }

        /// <summary>Trace pédales / volant d'un tour : « current », « last », « best » ou un numéro de tour (1 = premier tour).</summary>
        public InputTraceDto Trace(string which, IReadOnlyDictionary<int, double?> lapTimes)
        {
            var complete = _laps.Values.Where(l => l.Complete).OrderBy(l => l.Lap).ToList();
            double? TimeOf(InputLap l) => lapTimes != null && lapTimes.TryGetValue(l.Lap + 1, out double? t) ? t : null;
            InputLap lap = which switch
            {
                "current" => _current,
                "last" => complete.LastOrDefault(),
                "best" => complete.Where(l => TimeOf(l) > 0).OrderBy(l => TimeOf(l)).FirstOrDefault() ?? complete.LastOrDefault(),
                _ => int.TryParse(which, out int n) && _laps.TryGetValue(n - 1, out InputLap l2) ? l2 : null,
            };

            if (lap == null)
            {
                return null;
            }

            return new InputTraceDto
            {
                Lap = lap.Lap + 1,
                Time = TimeOf(lap),
                Step = BinMeters,
                Throttle = lap.Throttle.ToList(),
                Brake = lap.Brake.ToList(),
                Steering = lap.Steer.ToList(),
                Speed = lap.Speed.ToList(),
                Laps = complete.Select(l => l.Lap + 1).ToList(),
            };
        }

        /// <summary>Résumé du style de pilotage sur le dernier tour complet (pour l'ingénieur).</summary>
        public string DrivingSummary()
        {
            InputLap lap = _laps.Values.Where(l => l.Complete).OrderBy(l => l.Lap).LastOrDefault();
            if (lap == null)
            {
                return null;
            }

            var idx = Enumerable.Range(0, lap.Throttle.Length).Where(i => lap.Throttle[i].HasValue).ToList();
            if (idx.Count < 20)
            {
                return null;
            }

            double Pct(Func<int, bool> f) => Math.Round(idx.Count(f) * 100.0 / idx.Count);
            double full = Pct(i => lap.Throttle[i] >= 98);
            double braking = Pct(i => lap.Brake[i] >= 5);
            double coasting = Pct(i => lap.Throttle[i] < 5 && (lap.Brake[i] ?? 0) < 5);
            double overlap = Pct(i => lap.Throttle[i] > 20 && lap.Brake[i] > 10);
            return $"Pilotage du tour {lap.Lap + 1} : plein gaz {full} % du tour, au freinage {braking} %, en roue libre {coasting} %" +
                (overlap >= 1 ? $", frein et accélérateur ensemble {overlap} %." : ".");
        }

        /// <summary>Numéros des tours gardés (1 = premier tour), du plus ancien au plus récent, tour en cours compris.</summary>
        public List<(int Lap, bool Complete, bool Current)> RecordedLaps()
        {
            var list = _laps.Values.OrderBy(l => l.Lap).Select(l => (l.Lap + 1, l.Complete, false)).ToList();
            if (_current != null && !_laps.ContainsKey(_current.Lap))
            {
                list.Add((_current.Lap + 1, false, true));
            }

            return list;
        }

        /// <summary>
        /// Détail d'un tour pour la comparaison (1 = premier tour) : vitesse, pédales, volant, rapport, régime et temps écoulé
        /// tous les <see cref="BinMeters"/> m, passages de rapports et réglages en roulant. Null si le tour n'est plus en mémoire.
        /// </summary>
        public LapDetailDto LapDetail(int lapNumber)
        {
            InputLap l = _current?.Lap == lapNumber - 1 ? _current : _laps.TryGetValue(lapNumber - 1, out InputLap stored) ? stored : null;
            if (l == null)
            {
                return null;
            }

            static List<double?> Rounded(double?[] values, int digits) => values.Select(v => v.HasValue ? Math.Round(v.Value, digits) : (double?)null).ToList();
            static double? Max(double?[] values) => values.Any(v => v.HasValue) ? values.Max() : null;
            var elapsed = l.StartTime is double start
                ? l.Time.Select(t => t.HasValue ? Math.Round(t.Value - start, 3) : (double?)null).ToList()
                : l.Time.Select(_ => (double?)null).ToList();
            return new LapDetailDto
            {
                Lap = l.Lap + 1,
                Complete = l.Complete,
                Current = l == _current,
                Speed = Rounded(l.Speed, 0),
                Throttle = Rounded(l.Throttle, 0),
                Brake = Rounded(l.Brake, 0),
                Steering = Rounded(l.Steer, 0),
                Gear = Rounded(l.Gear, 0),
                Rpm = Rounded(l.Rpm, 0),
                Elapsed = elapsed,
                TopSpeed = Max(l.Speed),
                MaxRpm = Max(l.Rpm) is double rpm ? (int)rpm : null,
                Shifts = l.Shifts.ToList(),
                Controls = new Dictionary<string, double>(l.StartControls),
                ControlChanges = l.Changes.Select(c => new ControlChangeDto { Key = c.Key, Distance = c.Distance, From = c.From, To = c.To }).ToList(),
            };
        }

        private void RecordTrace(Sample s, double lapLength, CarControls controls)
        {
            if (lapLength <= 100 || s.InPits)
            {
                _previousTraced = null;
                _lastInGear = null;
                return;
            }

            int bins = (int)Math.Ceiling(lapLength / BinMeters);
            // Le temps de session peut rester le même sur deux mesures (le jeu ne l'avance pas à chaque envoi) : mesure d'avant quand même.
            Sample previous = _previousTraced is Sample p && s.T >= p.T && s.T - p.T < 2 ? p : null;
            if (_current == null || _current.Lap != s.Lap)
            {
                if (_current != null)
                {
                    _current.Complete = _current.Lap == s.Lap - 1 && _current.Throttle.Count(v => v.HasValue) > _current.Throttle.Length * 0.6;
                    _laps[_current.Lap] = _current;
                }

                _current = new InputLap(s.Lap, bins) { StartTime = LineCrossing(previous, s, lapLength) };
                // Réglages au départ du tour : ceux lus maintenant (changés au stand compris), sinon les derniers connus.
                foreach (var (key, read) in ControlKeys)
                {
                    double? value = controls?.Available == true ? read(controls) : null;
                    if (value.HasValue)
                    {
                        _controlValues[key] = value.Value;
                    }
                    else if (_controlValues.TryGetValue(key, out double known))
                    {
                        value = known;
                    }

                    if (value.HasValue)
                    {
                        _current.StartControls[key] = value.Value;
                    }
                }

                // On garde les derniers tours (quelques milliers de valeurs par tour).
                foreach (int old in _laps.Keys.OrderBy(k => k).Take(Math.Max(0, _laps.Count - MaxStoredLaps)).ToList())
                {
                    _laps.Remove(old);
                }
            }

            int bin = Math.Clamp((int)(s.Dist / BinMeters), 0, _current.Throttle.Length - 1);
            double thr = Math.Round(Math.Clamp(s.Throttle, 0, 1) * 100);
            double brk = Math.Round(Math.Clamp(s.Brake, 0, 1) * 100);
            double str = Math.Round(Math.Clamp(s.Steer, -1, 1) * 100);
            double spd = Math.Round(s.Speed);
            // Point mort vu un instant pendant un passage de rapport : on garde le rapport d'avant (pas de trou dans la trace).
            double? gear = s.Gear == 0 && s.Speed > 30 && _lastInGear is Sample engaged && s.T - engaged.T < 1.5 ? engaged.Gear : s.Gear;

            // Données espacées (jeu à 5-10 Hz, 300 km/h = 8 à 17 m entre deux mesures) : on relie les deux mesures en ligne droite
            // pour que la trace du tour soit continue (jusqu'à 250 m de trou, démonstration accélérée comprise ; au-delà, pas de données inventées).
            // Le rapport ne se relie pas : l'ancien jusqu'à mi-chemin, le nouveau ensuite.
            InputLap l = _current;
            if (l.LastBin >= 0 && bin > l.LastBin + 1 && bin - l.LastBin <= 25)
            {
                int from = l.LastBin;
                for (int i = from + 1; i < bin; i++)
                {
                    double f = (double)(i - from) / (bin - from);
                    l.Throttle[i] = Math.Round(l.Throttle[from].Value + ((thr - l.Throttle[from].Value) * f));
                    l.Brake[i] = Math.Round(l.Brake[from].Value + ((brk - l.Brake[from].Value) * f));
                    l.Steer[i] = Math.Round(l.Steer[from].Value + ((str - l.Steer[from].Value) * f));
                    l.Speed[i] = Math.Round(l.Speed[from].Value + ((spd - l.Speed[from].Value) * f));
                    l.Rpm[i] = l.Rpm[from].HasValue ? Math.Round(l.Rpm[from].Value + ((s.Rpm - l.Rpm[from].Value) * f)) : s.Rpm;
                    l.Gear[i] = f < 0.5 ? l.Gear[from] : gear;
                }
            }

            l.Throttle[bin] = thr;
            l.Brake[bin] = brk;
            l.Steer[bin] = str;
            l.Speed[bin] = spd;
            l.Rpm[bin] = s.Rpm;
            l.Gear[bin] = gear;
            l.LastBin = bin;
            RecordPassingTimes(previous, s, l);

            RecordShift(s, l);
            RecordControls(s, l, controls);
            _previousTraced = s;
        }

        /// <summary>
        /// Heure de passage au centre de chaque case, interpolée entre deux mesures dont la position a changé.
        /// Le Mans Ultimate avance le temps de session un envoi avant la position : une mesure « temps neuf, position ancienne »
        /// fausserait l'écart entre deux tours (dents de scie de ±0,2 s) ; elle ne sert pas de repère.
        /// </summary>
        private static void RecordPassingTimes(Sample previous, Sample s, InputLap l)
        {
            bool freshPosition = previous == null || previous.Lap != s.Lap || Math.Abs(s.Dist - previous.Dist) > 0.01;
            if (!freshPosition)
            {
                return;
            }

            (double T, double Dist)? anchor = l.TimeAnchor ?? (l.StartTime is double start ? (start, 0.0) : null);
            if (anchor is (double t0, double d0) && s.Dist > d0 && s.T >= t0 && s.Dist - d0 <= 250)
            {
                for (int i = Math.Max(0, (int)Math.Ceiling((d0 / BinMeters) - 0.5)); i < l.Time.Length && (i + 0.5) * BinMeters <= s.Dist; i++)
                {
                    double c = (i + 0.5) * BinMeters;
                    l.Time[i] = t0 + ((s.T - t0) * (c - d0) / (s.Dist - d0));
                }
            }

            l.TimeAnchor = (s.T, s.Dist);
        }

        /// <summary>Heure du passage sur la ligne, interpolée entre la dernière mesure du tour précédent et la première de celui-ci.</summary>
        private static double? LineCrossing(Sample previous, Sample s, double lapLength)
        {
            if (previous != null && previous.Lap == s.Lap - 1)
            {
                double before = Math.Max(0, lapLength - previous.Dist);
                double total = before + Math.Max(0, s.Dist);
                return total > 0 ? previous.T + ((s.T - previous.T) * before / total) : s.T;
            }

            // Début de tour sans la mesure d'avant (sortie des stands, données reprises) : estimé seulement tout près de la ligne.
            return s.Dist < 60 && s.Speed > 20 ? s.T - (Math.Max(0, s.Dist) / (s.Speed / 3.6)) : null;
        }

        /// <summary>
        /// Passage de rapport entre deux mesures : situé à mi-chemin (distance, vitesse), régime juste avant et juste après.
        /// Le point mort vu pendant le passage (Le Mans Ultimate l'envoie souvent) est ignoré : on compare au dernier rapport engagé.
        /// Un saut de plusieurs rapports entre deux mesures (données espacées) compte pour chacun, au même endroit, pour comparer les tours.
        /// </summary>
        private void RecordShift(Sample s, InputLap l)
        {
            if (s.Gear is not int to || to < 1)
            {
                return; // point mort, marche arrière : on garde le dernier rapport engagé
            }

            Sample previous = _lastInGear;
            _lastInGear = s;
            if (previous == null || previous.Gear is not int from || from == to || s.T < previous.T || s.T - previous.T > 1.5 || l.Shifts.Count >= 300)
            {
                return;
            }

            double distance = previous.Lap == s.Lap ? (previous.Dist + s.Dist) / 2 : s.Dist / 2;
            int step = to > from ? 1 : -1;
            for (int g = from; g != to; g += step)
            {
                l.Shifts.Add(new ShiftPointDto
                {
                    Distance = Math.Round(distance, 1),
                    Speed = Math.Round((previous.Speed + s.Speed) / 2, 1),
                    RpmBefore = previous.Rpm,
                    RpmAfter = s.Rpm,
                    From = g,
                    To = g + step,
                });
            }
        }

        /// <summary>Réglage en roulant changé pendant le tour (plusieurs crans d'affilée en moins de 3 s = un seul changement).</summary>
        private void RecordControls(Sample s, InputLap l, CarControls controls)
        {
            if (controls?.Available != true)
            {
                return;
            }

            foreach (var (key, read) in ControlKeys)
            {
                if (read(controls) is not double value)
                {
                    continue;
                }

                if (!_controlValues.TryGetValue(key, out double before))
                {
                    _controlValues[key] = value;
                    l.StartControls.TryAdd(key, value);
                    continue;
                }

                if (Math.Abs(value - before) < 1e-6)
                {
                    continue;
                }

                _controlValues[key] = value;
                ControlChangeDto last = l.Changes.LastOrDefault(c => c.Key == key);
                if (last != null && s.T - last.Time < 3)
                {
                    last.To = value;
                    last.Time = s.T;
                }
                else if (l.Changes.Count < 100)
                {
                    l.Changes.Add(new ControlChangeDto { Key = key, Distance = Math.Round(s.Dist), From = before, To = value, Time = s.T });
                }
            }
        }

        /// <summary>« 1 » à « 9 » → rapport ; « N » → 0 ; « R » → -1 ; rien → null.</summary>
        private static int? GearNumber(string gear)
        {
            if (string.IsNullOrWhiteSpace(gear))
            {
                return null;
            }

            string g = gear.Trim();
            if (g.Equals("N", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (g.Equals("R", StringComparison.OrdinalIgnoreCase))
            {
                return -1;
            }

            return int.TryParse(g, out int n) ? n : null;
        }

        private void DetectWearSpike(Sample s, IReadOnlyList<TrackSegment> corners, double lapLength)
        {
            if (s.InPits || s.Speed < 30 || s.Detached || s.Wear == null || s.T - _lastSpikeAt < 20)
            {
                return;
            }

            Sample first = _recent.FirstOrDefault(r => r.Wear != null && !r.InPits && !r.Detached);
            if (first == null || s.T - first.T < 0.5)
            {
                return;
            }

            int worst = -1;
            double delta = 0;
            for (int i = 0; i < 4; i++)
            {
                double d = s.Wear[i] - first.Wear[i];
                if (d > delta)
                {
                    delta = d;
                    worst = i;
                }
            }

            // Plus de 30 points d'un coup : roue changée, voiture remise au stand… pas de l'usure.
            if (worst < 0 || delta < SpikeThresholdPct || delta > 30)
            {
                return;
            }

            var window = _recent.Where(r => r.T >= first.T).ToList();
            TrackSegment corner = Corner(corners, s.Dist, lapLength);
            bool rear = worst >= 2;
            double otherAxle = rear
                ? Math.Max(s.Wear[0] - first.Wear[0], s.Wear[1] - first.Wear[1])
                : Math.Max(s.Wear[2] - first.Wear[2], s.Wear[3] - first.Wear[3]);
            var spike = new WearSpike
            {
                Time = s.T,
                Lap = s.Lap + 1,
                Wheel = WheelKeys[worst],
                DeltaPct = Math.Round(delta, 1),
                Seconds = Math.Round(s.T - first.T, 1),
                Phase = Phase(window),
                Corner = corner?.Name,
                Distance = Math.Round(s.Dist),
                BothAxles = otherAxle >= delta * 0.6,
                MaxBrake = Math.Round(window.Max(w => w.Brake) * 100),
                MaxThrottle = Math.Round(window.Max(w => w.Throttle) * 100),
                MaxSteer = Math.Round(window.Max(w => Math.Abs(w.Steer)) * 100),
            };
            _spikes.Add(spike);
            if (_spikes.Count > 50)
            {
                _spikes.RemoveAt(0);
            }

            _lastSpikeAt = s.T;
            _recent.Clear();
            Raise("usure", $"wear-{spike.Wheel}-{s.T:0}", spike.Lap, corner: spike.Corner, spike: spike);
        }

        private void DetectRules(Sample s, CarControls c, IReadOnlyList<TrackSegment> corners, double lapLength)
        {
            if (c == null || !c.Available)
            {
                return;
            }

            // Points de limites de piste : un point ajouté = une sortie de piste comptée.
            if (c.TrackLimitsPoints is double pts)
            {
                if (_trackLimitPoints is double before && pts > before + 0.01 && !s.InPits)
                {
                    TrackSegment corner = Corner(corners, s.Dist, lapLength);
                    // Trop large : en sortie, accélérateur appuyé. Coupé : au freinage ou au milieu du virage.
                    var window = _recent.Where(r => s.T - r.T <= 1.5).ToList();
                    string kind = window.Count == 0 || window.Average(r => r.Throttle) >= 0.6 ? "large" : "coupé";
                    Raise("limites", $"limits-{pts:0.00}", s.Lap + 1, corner: corner?.Name, kind: kind, points: pts, perPenalty: c.TrackLimitsPointsPerPenalty);
                }

                _trackLimitPoints = pts;
            }

            // Tour annulé par le jeu (coupé, limites de piste, accident) : une fois par tour.
            if (c.LapInvalidated && !_lapInvalidated && !s.InPits && _invalidatedLap != s.Lap)
            {
                _invalidatedLap = s.Lap;
                TrackSegment corner = Corner(corners, s.Dist, lapLength);
                Raise("tour-annulé", $"invalid-{s.Lap}", s.Lap + 1, corner: corner?.Name);
            }

            _lapInvalidated = c.LapInvalidated;

            if (c.Penalties is int pen)
            {
                if (_penalties is int before && pen > before)
                {
                    Raise("pénalité", $"penalty-{pen}-{s.T:0}", s.Lap + 1, points: pen);
                }

                _penalties = pen;
            }
        }

        private void DetectStart(double now, Sample s, CarInfo car, string sessionType, string sessionPhase)
        {
            if (_startChecked || now - _sessionStart > 900)
            {
                return;
            }

            // Une fois la voiture en mouvement, plus rien à rappeler.
            if (s.Speed > 15)
            {
                _startChecked = true;
                return;
            }

            CarControls c = car.Controls;
            if (c?.Available != true || now - _sessionStart < 4)
            {
                return; // laisser au jeu le temps d'envoyer un état stable
            }

            bool engineOff = car.EngineRpm < 200 && c.Ignition is int ign && ign < 2;
            bool neutral = car.CurrentGear is "N" or "0" or "";
            if (engineOff)
            {
                _startChecked = true;
                Raise("départ", "start-engine", s.Lap + 1, kind: "moteur");
            }
            else if (neutral && car.EngineRpm > 200 && (s.InPits || sessionType == "Race"))
            {
                _startChecked = true;
                Raise("départ", "start-gear", s.Lap + 1, kind: "première");
            }
        }

        private void Raise(string type, string key, int lap, string corner = null, string kind = null, double? points = null, double? perPenalty = null, WearSpike spike = null)
        {
            _events.Add(new DrivingEvent { Id = ++_nextId, Type = type, Key = key, Lap = lap, Corner = corner, Detail = kind, Points = points, PointsPerPenalty = perPenalty, Spike = spike });
            if (_events.Count > 20)
            {
                _events.RemoveAt(0);
            }
        }

        private static string Phase(List<Sample> window)
        {
            int braking = 0, entry = 0, mid = 0, exit = 0, straight = 0;
            foreach (Sample w in window)
            {
                double steer = Math.Abs(w.Steer);
                if (w.Brake > 0.2 && steer < 0.08)
                {
                    braking++;
                }
                else if (w.Brake > 0.05)
                {
                    entry++;
                }
                else if (w.Throttle > 0.5 && steer >= 0.06)
                {
                    exit++;
                }
                else if (steer >= 0.08)
                {
                    mid++;
                }
                else
                {
                    straight++;
                }
            }

            var phases = new[] { ("freinage", braking), ("entrée", entry), ("milieu", mid), ("sortie", exit), ("ligne droite", straight) };
            return phases.OrderByDescending(p => p.Item2).First().Item1;
        }

        private static TrackSegment Corner(IReadOnlyList<TrackSegment> corners, double dist, double lapLength)
        {
            if (corners == null || corners.Count == 0)
            {
                return null;
            }

            var list = corners.Where(c => c.Kind == "corner").ToList();
            TrackSegment inside = list.FirstOrDefault(c => c.Start <= c.End
                ? dist >= c.Start - 30 && dist <= c.End + 30
                : dist >= c.Start - 30 || dist <= c.End + 30);
            if (inside != null)
            {
                return inside;
            }

            double Gap(TrackSegment c)
            {
                double d = Math.Abs(c.Apex - dist);
                return lapLength > 0 ? Math.Min(d, lapLength - d) : d;
            }

            TrackSegment near = list.OrderBy(Gap).FirstOrDefault();
            return near != null && Gap(near) < 150 ? near : null;
        }

        private static double[] Wear(Wheels w)
        {
            if (w == null)
            {
                return null;
            }

            WheelInfo[] all = { w.FrontLeft, w.FrontRight, w.RearLeft, w.RearRight };
            if (all.Any(x => x?.TyreWear == null))
            {
                return null;
            }

            double[] wear = all.Select(x => x.TyreWear.ActualWear * 100).ToArray();
            return wear.All(v => v <= 0.0001) ? null : wear;
        }

        private static bool Detached(Wheels w) =>
            w != null && new[] { w.FrontLeft, w.FrontRight, w.RearLeft, w.RearRight }.Any(x => x?.Detached == true);

        private sealed class Sample
        {
            public double T;
            public double Dist;
            public int Lap;
            public double Speed;
            public double Throttle;
            public double Brake;
            public double Steer;
            public double[] Wear;
            public bool Detached;
            public bool InPits;
            public int? Gear;
            public int Rpm;
        }

        private sealed class InputLap
        {
            public InputLap(int lap, int bins)
            {
                Lap = lap;
                Throttle = new double?[bins];
                Brake = new double?[bins];
                Steer = new double?[bins];
                Speed = new double?[bins];
                Gear = new double?[bins];
                Rpm = new double?[bins];
                Time = new double?[bins];
            }

            public int Lap { get; }

            public bool Complete { get; set; }

            /// <summary>Dernière case remplie (pour relier deux mesures espacées), -1 au début du tour.</summary>
            public int LastBin { get; set; } = -1;

            public double?[] Throttle { get; }

            public double?[] Brake { get; }

            public double?[] Steer { get; }

            public double?[] Speed { get; }

            /// <summary>Rapport engagé (0 = point mort, -1 = marche arrière).</summary>
            public double?[] Gear { get; }

            public double?[] Rpm { get; }

            /// <summary>Heure de session au passage de chaque case (s).</summary>
            public double?[] Time { get; }

            /// <summary>Heure de session au passage de la ligne (null si inconnue : tour commencé ailleurs).</summary>
            public double? StartTime { get; set; }

            /// <summary>Dernière mesure (heure, position) qui sert de repère pour les heures de passage.</summary>
            public (double T, double Dist)? TimeAnchor { get; set; }

            public List<ShiftPointDto> Shifts { get; } = new();

            public Dictionary<string, double> StartControls { get; } = new();

            public List<ControlChangeDto> Changes { get; } = new();
        }
    }

    /// <summary>Passage de rapport : où (m depuis la ligne), à quelle vitesse et à quel régime.</summary>
    public sealed class ShiftPointDto
    {
        public double Distance { get; set; }

        public double Speed { get; set; }

        /// <summary>Régime à la dernière mesure avant le passage (montée : régime de passage).</summary>
        public int RpmBefore { get; set; }

        /// <summary>Régime à la première mesure après le passage (descente : régime atteint).</summary>
        public int RpmAfter { get; set; }

        public int From { get; set; }

        public int To { get; set; }
    }

    /// <summary>Réglage en roulant changé pendant un tour (« bias », « tc », « tcCut », « tcSlip », « abs », « map », « migration », « arbFront », « arbRear »).</summary>
    public sealed class ControlChangeDto
    {
        public string Key { get; set; }

        public double Distance { get; set; }

        public double From { get; set; }

        public double To { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public double Time { get; set; }
    }

    /// <summary>Un tour du joueur pour la comparaison, tous les <see cref="DrivingWatch.BinMeters"/> m.</summary>
    public sealed class LapDetailDto
    {
        public int Lap { get; set; }

        public double? Time { get; set; }

        public bool Complete { get; set; }

        /// <summary>Tour en cours (encore incomplet).</summary>
        public bool Current { get; set; }

        public bool Valid { get; set; } = true;

        public bool Pit { get; set; }

        public bool OutLap { get; set; }

        public string Compound { get; set; }

        public int? TyreLaps { get; set; }

        public double? Fuel { get; set; }

        public double? TopSpeed { get; set; }

        public int? MaxRpm { get; set; }

        public List<double?> Speed { get; set; } = new();

        public List<double?> Throttle { get; set; } = new();

        public List<double?> Brake { get; set; } = new();

        public List<double?> Steering { get; set; } = new();

        public List<double?> Gear { get; set; } = new();

        public List<double?> Rpm { get; set; } = new();

        /// <summary>Temps écoulé depuis la ligne (s) : sert au delta cumulé entre deux tours.</summary>
        public List<double?> Elapsed { get; set; } = new();

        public List<ShiftPointDto> Shifts { get; set; } = new();

        /// <summary>Réglages en roulant au départ du tour.</summary>
        public Dictionary<string, double> Controls { get; set; } = new();

        public List<ControlChangeDto> ControlChanges { get; set; } = new();
    }

    /// <summary>Événement de pilotage à annoncer tout de suite (usure brutale, limites de piste, tour annulé, pénalité, départ).</summary>
    public sealed class DrivingEvent
    {
        public long Id { get; set; }

        /// <summary>« usure », « limites », « tour-annulé », « pénalité », « départ ».</summary>
        public string Type { get; set; }

        public string Key { get; set; }

        public int Lap { get; set; }

        public string Corner { get; set; }

        /// <summary>Limites : « large » ou « coupé ». Départ : « moteur » ou « première ».</summary>
        public string Detail { get; set; }

        /// <summary>Limites : points accumulés ; pénalité : nombre de pénalités à purger.</summary>
        public double? Points { get; set; }

        public double? PointsPerPenalty { get; set; }

        public WearSpike Spike { get; set; }
    }

    /// <summary>Usure brutale d'un pneu : de combien, en combien de temps, où et dans quelle phase du virage.</summary>
    public sealed class WearSpike
    {
        public double Time { get; set; }

        public int Lap { get; set; }

        public string Wheel { get; set; }

        public double DeltaPct { get; set; }

        public double Seconds { get; set; }

        /// <summary>« freinage », « entrée », « milieu », « sortie », « ligne droite ».</summary>
        public string Phase { get; set; }

        public string Corner { get; set; }

        public double Distance { get; set; }

        /// <summary>Les deux essieux s'usent ensemble (glissade des quatre roues) : pas seulement un blocage ou un patinage.</summary>
        public bool BothAxles { get; set; }

        public double MaxBrake { get; set; }

        public double MaxThrottle { get; set; }

        public double MaxSteer { get; set; }
    }

    /// <summary>Pédales et volant à l'instant (%, volant de -100 à 100).</summary>
    public sealed class InputsDto
    {
        public double Throttle { get; set; }

        public double Brake { get; set; }

        public double Clutch { get; set; }

        public double Steering { get; set; }

        public string Gear { get; set; }

        public int Rpm { get; set; }
    }

    /// <summary>Trace pédales / volant d'un tour, par pas de <see cref="DrivingWatch.BinMeters"/> mètres.</summary>
    public sealed class InputTraceDto
    {
        public int Lap { get; set; }

        public double? Time { get; set; }

        public double Step { get; set; }

        public List<double?> Throttle { get; set; } = new();

        public List<double?> Brake { get; set; } = new();

        public List<double?> Steering { get; set; } = new();

        public List<double?> Speed { get; set; } = new();

        /// <summary>Tours complets disponibles.</summary>
        public List<int> Laps { get; set; } = new();
    }

    /// <summary>Réglages en roulant, moteur, hybride et règlement (pour l'écran et l'ingénieur).</summary>
    public sealed class ControlsDto
    {
        public double? FrontBrakeBiasPct { get; set; }

        public int? MotorMap { get; set; }

        public int? MotorMapMax { get; set; }

        public int? Tc { get; set; }

        public int? TcMax { get; set; }

        public int? TcCut { get; set; }

        public int? TcCutMax { get; set; }

        public int? TcSlip { get; set; }

        public int? TcSlipMax { get; set; }

        public int? Abs { get; set; }

        public int? AbsMax { get; set; }

        public int? Migration { get; set; }

        public int? MigrationMax { get; set; }

        public int? FrontAntiRoll { get; set; }

        public int? FrontAntiRollMax { get; set; }

        public int? RearAntiRoll { get; set; }

        public int? RearAntiRollMax { get; set; }

        public double? RegenKw { get; set; }

        /// <summary>« inactif », « déploiement », « récupération » (null : pas de moteur électrique).</summary>
        public string ElectricMotor { get; set; }

        public double? BatteryPct { get; set; }

        public double? VirtualEnergyPct { get; set; }

        public double? TrackLimitsPoints { get; set; }

        public double? TrackLimitsPointsPerPenalty { get; set; }

        public int? Penalties { get; set; }

        public bool LapInvalidated { get; set; }

        public bool EngineRunning { get; set; }

        public bool PitRequested { get; set; }

        public static ControlsDto From(CarInfo car)
        {
            CarControls c = car?.Controls;
            if (c == null || !c.Available)
            {
                return null;
            }

            return new ControlsDto
            {
                FrontBrakeBiasPct = c.FrontBrakeBiasPct,
                MotorMap = c.MotorMap,
                MotorMapMax = c.MotorMapMax,
                Tc = c.Tc,
                TcMax = c.TcMax,
                TcCut = c.TcCut,
                TcCutMax = c.TcCutMax,
                TcSlip = c.TcSlip,
                TcSlipMax = c.TcSlipMax,
                Abs = c.Abs,
                AbsMax = c.AbsMax,
                Migration = c.Migration,
                MigrationMax = c.MigrationMax,
                FrontAntiRoll = c.FrontAntiRoll,
                FrontAntiRollMax = c.FrontAntiRollMax,
                RearAntiRoll = c.RearAntiRoll,
                RearAntiRollMax = c.RearAntiRollMax,
                RegenKw = c.RegenKw,
                ElectricMotor = c.ElectricMotorState switch { 1 => "inactif", 2 => "déploiement", 3 => "récupération", _ => null },
                BatteryPct = c.BatteryPct,
                VirtualEnergyPct = c.VirtualEnergyPct,
                TrackLimitsPoints = c.TrackLimitsPoints,
                TrackLimitsPointsPerPenalty = c.TrackLimitsPointsPerPenalty,
                Penalties = c.Penalties,
                LapInvalidated = c.LapInvalidated,
                EngineRunning = car.EngineRpm > 200,
                PitRequested = c.PitRequested,
            };
        }
    }
}
