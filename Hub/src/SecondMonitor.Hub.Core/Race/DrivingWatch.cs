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
    /// - traces pédales et volant par tour (tour en cours, dernier, meilleur), pour l'analyse du pilotage.
    /// Les événements urgents sont lus par le service radio (Drain) et dits tout de suite, en une phrase.
    /// </summary>
    public sealed class DrivingWatch
    {
        /// <summary>Pas des traces de pilotage (mètres).</summary>
        public const double BinMeters = 10;

        private const double SpikeWindowSeconds = 4;
        private const double SpikeThresholdPct = 1.5;
        private static readonly string[] WheelKeys = { "FL", "FR", "RL", "RR" };

        private readonly Queue<Sample> _recent = new();
        private readonly List<DrivingEvent> _events = new();
        private readonly List<WearSpike> _spikes = new();
        private readonly Dictionary<int, InputLap> _laps = new();
        private InputLap _current;
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
            _current = null;
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
            };

            if (_recent.Count > 0 && now < _recent.Peek().T - 1)
            {
                _recent.Clear(); // temps qui recule (retour au stand, rembobinage)
            }

            RecordTrace(sample, lapLength);
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

        private void RecordTrace(Sample s, double lapLength)
        {
            if (lapLength <= 100 || s.InPits)
            {
                return;
            }

            int bins = (int)Math.Ceiling(lapLength / BinMeters);
            if (_current == null || _current.Lap != s.Lap)
            {
                if (_current != null)
                {
                    _current.Complete = _current.Lap == s.Lap - 1 && _current.Throttle.Count(v => v.HasValue) > _current.Throttle.Length * 0.6;
                    _laps[_current.Lap] = _current;
                }

                _current = new InputLap(s.Lap, bins);
                // On garde les 40 derniers tours (quelques centaines de valeurs par tour).
                foreach (int old in _laps.Keys.OrderBy(k => k).Take(Math.Max(0, _laps.Count - 40)).ToList())
                {
                    _laps.Remove(old);
                }
            }

            int bin = Math.Clamp((int)(s.Dist / BinMeters), 0, _current.Throttle.Length - 1);
            double thr = Math.Round(Math.Clamp(s.Throttle, 0, 1) * 100);
            double brk = Math.Round(Math.Clamp(s.Brake, 0, 1) * 100);
            double str = Math.Round(Math.Clamp(s.Steer, -1, 1) * 100);
            double spd = Math.Round(s.Speed);

            // Données espacées (jeu à 5-10 Hz, 300 km/h = 8 à 17 m entre deux mesures) : on relie les deux mesures en ligne droite
            // pour que la trace du tour soit continue (jusqu'à 250 m de trou, démonstration accélérée comprise ; au-delà, pas de données inventées).
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
                }
            }

            l.Throttle[bin] = thr;
            l.Brake[bin] = brk;
            l.Steer[bin] = str;
            l.Speed[bin] = spd;
            l.LastBin = bin;
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
            }

            public int Lap { get; }

            public bool Complete { get; set; }

            /// <summary>Dernière case remplie (pour relier deux mesures espacées), -1 au début du tour.</summary>
            public int LastBin { get; set; } = -1;

            public double?[] Throttle { get; }

            public double?[] Brake { get; }

            public double?[] Steer { get; }

            public double?[] Speed { get; }
        }
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
