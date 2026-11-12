namespace SecondMonitor.Hub.Core.Race
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.DataModel.Snapshot.Systems;

    /// <summary>
    /// Dégâts de la voiture du joueur, pièce par pièce quand le jeu les donne (F1 : ailerons, fond plat, éléments moteur…),
    /// sinon les totaux communs (moteur, boîte, suspension, carrosserie, freins). Repère l'arrivée d'un nouveau dégât
    /// et mesure ce qu'il coûte au tour (rythme avant / après).
    /// </summary>
    public sealed class DamageWatch
    {
        private static readonly Dictionary<string, string> Labels = new()
        {
            ["front-wing-left"] = "aileron avant gauche",
            ["front-wing-right"] = "aileron avant droit",
            ["rear-wing"] = "aileron arrière",
            ["floor"] = "fond plat",
            ["diffuser"] = "diffuseur",
            ["sidepod"] = "pontons",
            ["gearbox"] = "boîte de vitesses",
            ["engine"] = "moteur",
            ["engine-ice-wear"] = "usure moteur thermique",
            ["engine-mguh-wear"] = "usure MGU-H",
            ["engine-mguk-wear"] = "usure MGU-K",
            ["engine-es-wear"] = "usure batterie",
            ["engine-ce-wear"] = "usure électronique de contrôle",
            ["engine-tc-wear"] = "usure turbo",
            ["tyre-fl"] = "pneu avant gauche abîmé",
            ["tyre-fr"] = "pneu avant droit abîmé",
            ["tyre-rl"] = "pneu arrière gauche abîmé",
            ["tyre-rr"] = "pneu arrière droit abîmé",
            ["drs-fault"] = "DRS en panne",
            ["ers-fault"] = "ERS en panne",
            ["engine-blown"] = "moteur cassé",
            ["engine-seized"] = "moteur serré",
            ["transmission"] = "transmission",
            ["suspension"] = "suspension",
            ["bodywork"] = "carrosserie / aéro",
            ["brake-fl"] = "frein avant gauche",
            ["brake-fr"] = "frein avant droit",
            ["brake-rl"] = "frein arrière gauche",
            ["brake-rr"] = "frein arrière droit",
            ["wheel-fl"] = "roue avant gauche arrachée",
            ["wheel-fr"] = "roue avant droite arrachée",
            ["wheel-rl"] = "roue arrière gauche arrachée",
            ["wheel-rr"] = "roue arrière droite arrachée",
            ["parts-detached"] = "pièce arrachée",
            ["engine-overheating"] = "moteur en surchauffe",
            ["puncture-fl"] = "crevaison avant gauche",
            ["puncture-fr"] = "crevaison avant droite",
            ["puncture-rl"] = "crevaison arrière gauche",
            ["puncture-rr"] = "crevaison arrière droite",
            ["body-front"] = "carrosserie avant",
            ["body-rear"] = "carrosserie arrière",
            ["body-left"] = "carrosserie côté gauche",
            ["body-right"] = "carrosserie côté droit",
            ["suspension-fl"] = "suspension avant gauche",
            ["suspension-fr"] = "suspension avant droite",
            ["suspension-rl"] = "suspension arrière gauche",
            ["suspension-rr"] = "suspension arrière droite",
        };

        private readonly Dictionary<string, double> _previous = new();
        private DamageEvent _pending;

        /// <summary>Dernier dégât apparu (pour l'alerte et l'ingénieur), et son coût au tour une fois mesuré.</summary>
        public DamageEvent LastEvent { get; private set; }

        public void Reset()
        {
            _previous.Clear();
            _pending = null;
            LastEvent = null;
        }

        /// <param name="now">Temps de session (s).</param>
        /// <param name="laps">Tours bouclés.</param>
        /// <param name="cleanLaps">Temps des tours propres du joueur (dans l'ordre), pour mesurer la perte de rythme.</param>
        public List<DamagePartDto> Update(DriverInfo player, double now, int laps, IReadOnlyList<(int Lap, double Time)> cleanLaps)
        {
            var parts = Read(player?.CarInfo);
            foreach (DamagePartDto d in parts)
            {
                _previous.TryGetValue(d.Key, out double before);
                // Usure moteur : elle monte doucement, ce n'est pas un choc. Le reste : +5 points d'un coup = nouveau dégât.
                bool wear = d.Key.EndsWith("-wear", StringComparison.Ordinal);
                if (!wear && d.Percent >= 5 && d.Percent - before >= 5)
                {
                    double? pace = Median(cleanLaps.Where(l => l.Lap <= laps).TakeLast(3).Select(l => l.Time));
                    LastEvent = new DamageEvent { Key = d.Key, Label = d.Label, Percent = d.Percent, Time = now, Lap = laps, PaceBefore = pace };
                    _pending = LastEvent;
                }

                _previous[d.Key] = Math.Max(before, d.Percent);
            }

            // Coût au tour : deux tours propres complets après le dégât, comparés aux trois d'avant.
            if (_pending?.PaceBefore is double paceBefore)
            {
                var after = cleanLaps.Where(l => l.Lap > _pending.Lap + 1).Take(3).Select(l => l.Time).ToList();
                if (after.Count >= 2)
                {
                    _pending.LossPerLap = Math.Round(Median(after).Value - paceBefore, 2);
                    _pending = null;
                }
            }

            return parts;
        }

        public static string Label(string key) => Labels.TryGetValue(key, out string label) ? label : key;

        private static List<DamagePartDto> Read(CarInfo car)
        {
            var list = new List<DamagePartDto>();
            CarDamageInformation dmg = car?.CarDamageInformation;
            if (dmg == null)
            {
                return list;
            }

            if (dmg.Parts is { Count: > 0 })
            {
                list.AddRange(dmg.Parts.Where(p => p != null && !string.IsNullOrEmpty(p.Key)).Select(p => Dto(p.Key, p.Group, p.Damage)));
            }

            // Totaux communs pour ce que le détail du jeu ne couvre pas (ou pour les jeux sans détail).
            void Generic(string key, string group, SecondMonitor.DataModel.BasicProperties.DamageInformation d)
            {
                if (d != null && !list.Any(x => x.Key == key || (x.Group == group && key != "engine")))
                {
                    list.Add(Dto(key, group, d.Damage));
                }
            }

            Generic("engine", "engine", dmg.Engine);
            Generic("transmission", "gearbox", dmg.Transmission);
            Generic("suspension", "suspension", dmg.Suspension);
            Generic("bodywork", "aero", dmg.Bodywork);

            var wheels = car.WheelsInfo;
            if (wheels != null)
            {
                foreach (var (key, w) in new[] { ("fl", wheels.FrontLeft), ("fr", wheels.FrontRight), ("rl", wheels.RearLeft), ("rr", wheels.RearRight) })
                {
                    if (w == null)
                    {
                        continue;
                    }

                    if (w.BrakesDamage != null && w.BrakesDamage.Damage > 0)
                    {
                        list.Add(Dto($"brake-{key}", "brakes", w.BrakesDamage.Damage));
                    }

                    if (w.Detached)
                    {
                        list.Add(Dto($"wheel-{key}", "suspension", 1));
                    }
                }
            }

            return list;
        }

        private static DamagePartDto Dto(string key, string group, double damage) => new()
        {
            Key = key,
            Group = group,
            Label = Label(key),
            Percent = Math.Round(Math.Clamp(double.IsFinite(damage) ? damage : 0, 0, 1) * 100, 1),
        };

        private static double? Median(IEnumerable<double> values)
        {
            var v = values.OrderBy(x => x).ToList();
            return v.Count == 0 ? null : v.Count % 2 == 1 ? v[v.Count / 2] : (v[(v.Count / 2) - 1] + v[v.Count / 2]) / 2;
        }
    }

    public sealed class DamagePartDto
    {
        public string Key { get; set; }

        public string Label { get; set; }

        /// <summary>« aero », « engine », « gearbox », « suspension », « brakes », « tyres », « electronics ».</summary>
        public string Group { get; set; }

        /// <summary>0 à 100.</summary>
        public double Percent { get; set; }
    }

    public sealed class DamageEvent
    {
        public string Key { get; set; }

        public string Label { get; set; }

        public double Percent { get; set; }

        public double Time { get; set; }

        public int Lap { get; set; }

        public double? PaceBefore { get; set; }

        /// <summary>Perte mesurée au tour (s) après deux tours propres, null avant.</summary>
        public double? LossPerLap { get; set; }
    }
}
