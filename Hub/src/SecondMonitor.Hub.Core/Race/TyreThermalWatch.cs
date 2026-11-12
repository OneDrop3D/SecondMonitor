namespace SecondMonitor.Hub.Core.Race
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Surveille la température des pneus du joueur dans la durée : moyenne de surface et de carcasse sur chaque tour,
    /// et temps passé d'affilée au-dessus de la plage. Une pointe d'une seconde n'est pas une surchauffe ;
    /// un tour entier au-dessus de la plage, ou 30 s d'affilée, oui.
    /// </summary>
    public sealed class TyreThermalWatch
    {
        private readonly Dictionary<string, WheelWatch> _wheels = new();
        private int _lap = -1;

        public void Reset()
        {
            _wheels.Clear();
            _lap = -1;
        }

        /// <param name="now">Temps de session (s).</param>
        /// <param name="completedLaps">Tours bouclés par le joueur.</param>
        /// <param name="moving">La voiture roule (pas arrêtée au stand ni dans un menu).</param>
        public List<TyreThermalDto> Update(double now, int completedLaps, bool moving, IEnumerable<WheelStatus> wheels)
        {
            bool newLap = _lap >= 0 && completedLaps > _lap;
            _lap = completedLaps;
            var result = new List<TyreThermalDto>();
            foreach (WheelStatus w in wheels)
            {
                if (!_wheels.TryGetValue(w.Position, out WheelWatch ww))
                {
                    ww = new WheelWatch();
                    _wheels[w.Position] = ww;
                }

                if (newLap)
                {
                    ww.CloseLap();
                }

                double? surface = Mean(w.TempInner, w.TempMiddle, w.TempOuter);
                double? min = w.IdealTemp - (w.TempWindow ?? 10);
                double? max = w.IdealTemp + (w.TempWindow ?? 10);
                ww.Add(now, surface, w.Core, moving);

                if (surface.HasValue && max.HasValue && moving && surface > max + 2)
                {
                    ww.HotSince ??= now;
                }
                else
                {
                    ww.HotSince = null;
                }

                result.Add(new TyreThermalDto
                {
                    Position = w.Position,
                    Surface = surface.HasValue ? Math.Round(surface.Value, 1) : null,
                    CurrentLapAverage = ww.CurrentAverage(),
                    LastLapAverage = ww.LastLapSurface,
                    LastLapCoreAverage = ww.LastLapCore,
                    Min = min.HasValue ? Math.Round(min.Value, 0) : null,
                    Max = max.HasValue ? Math.Round(max.Value, 0) : null,
                    HotSeconds = ww.HotSince.HasValue ? Math.Round(now - ww.HotSince.Value, 0) : 0,
                    LastLapState = State(ww.LastLapSurface, min, max),
                    State = State(surface, min, max),
                });
            }

            return result;
        }

        /// <summary>Alertes lisibles (écran et radio) : tour entier trop chaud, surchauffe qui dure, pneus froids.</summary>
        public static List<string> Alerts(IEnumerable<TyreThermalDto> thermal)
        {
            var alerts = new List<string>();
            foreach (TyreThermalDto t in thermal)
            {
                string name = WheelName(t.Position);
                if (t.LastLapState == "chaud" && t.LastLapAverage.HasValue)
                {
                    alerts.Add($"Pneu {name} trop chaud sur tout le dernier tour : {t.LastLapAverage:0}° de moyenne (plage {t.Min:0}–{t.Max:0}°).");
                }
                else if (t.HotSeconds >= 30 && t.Surface.HasValue)
                {
                    alerts.Add($"Pneu {name} en surchauffe depuis {t.HotSeconds:0} s : {t.Surface:0}° (maximum {t.Max:0}°).");
                }
                else if (t.LastLapState == "froid" && t.LastLapAverage.HasValue)
                {
                    alerts.Add($"Pneu {name} trop froid sur le dernier tour : {t.LastLapAverage:0}° (minimum {t.Min:0}°).");
                }
            }

            return alerts;
        }

        public static string WheelName(string position) => position switch
        {
            "FL" => "avant gauche",
            "FR" => "avant droit",
            "RL" => "arrière gauche",
            _ => "arrière droit",
        };

        private static string State(double? v, double? min, double? max)
        {
            if (!v.HasValue || !min.HasValue || !max.HasValue)
            {
                return null;
            }

            return v > max ? "chaud" : v < min - 5 ? "froid" : "bon";
        }

        private static double? Mean(params double?[] values)
        {
            var v = values.Where(x => x.HasValue).Select(x => x.Value).ToList();
            return v.Count == 0 ? null : v.Average();
        }

        private sealed class WheelWatch
        {
            private double _sumSurface;
            private double _sumCore;
            private double _timeSurface;
            private double _timeCore;
            private double? _lastT;

            public double? HotSince { get; set; }

            public double? LastLapSurface { get; private set; }

            public double? LastLapCore { get; private set; }

            public void Add(double now, double? surface, double? core, bool moving)
            {
                double dt = _lastT.HasValue ? now - _lastT.Value : 0;
                _lastT = now;
                if (!moving || dt <= 0 || dt > 2)
                {
                    return; // arrêt, pause ou trou dans les données : ne compte pas dans la moyenne
                }

                if (surface.HasValue)
                {
                    _sumSurface += surface.Value * dt;
                    _timeSurface += dt;
                }

                if (core.HasValue)
                {
                    _sumCore += core.Value * dt;
                    _timeCore += dt;
                }
            }

            public double? CurrentAverage() => _timeSurface > 5 ? Math.Round(_sumSurface / _timeSurface, 1) : null;

            public void CloseLap()
            {
                // Un tour trop court (sortie des stands, données manquantes) ne remplace pas la moyenne précédente.
                if (_timeSurface >= 20)
                {
                    LastLapSurface = Math.Round(_sumSurface / _timeSurface, 1);
                    LastLapCore = _timeCore >= 20 ? Math.Round(_sumCore / _timeCore, 1) : null;
                }

                _sumSurface = _sumCore = _timeSurface = _timeCore = 0;
            }
        }
    }

    public sealed class TyreThermalDto
    {
        public string Position { get; set; }

        /// <summary>Moyenne des trois zones de la bande de roulement, maintenant.</summary>
        public double? Surface { get; set; }

        public double? CurrentLapAverage { get; set; }

        public double? LastLapAverage { get; set; }

        public double? LastLapCoreAverage { get; set; }

        public double? Min { get; set; }

        public double? Max { get; set; }

        /// <summary>Secondes d'affilée au-dessus de la plage (0 sinon).</summary>
        public double HotSeconds { get; set; }

        /// <summary>« froid », « bon », « chaud » (maintenant).</summary>
        public string State { get; set; }

        /// <summary>« froid », « bon », « chaud » (moyenne du dernier tour).</summary>
        public string LastLapState { get; set; }
    }
}
