namespace SecondMonitor.Hub.Core.Strategy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SecondMonitor.Hub.Core.Race;

    /// <summary>
    /// Plan de course complet du joueur, jusqu'au drapeau : nombre d'arrêts restants, fenêtre du prochain arrêt (au plus tôt,
    /// au plus tard), ce qu'on met à chaque arrêt, arrêt un tour plus tôt ou plus tard, ratio carburant / énergie virtuelle,
    /// et ce qu'il faudrait économiser par tour pour faire un arrêt de moins (avec le coût mesuré du lift and coast s'il existe).
    /// Calcul fait sur la consommation mesurée : rien n'est inventé, tout est null tant qu'un tour n'est pas mesuré.
    /// </summary>
    public static class RacePlanner
    {
        /// <summary>Économie au-delà de laquelle un arrêt de moins n'est pas réaliste (15 % de consommation en moins par tour).</summary>
        public const double MaxRealisticSaving = 0.15;

        /// <summary>Le Mans Ultimate : temps immobile = 2 s + 0,40 s par % d'énergie remis + 12 s pour quatre pneus.</summary>
        public static double LmuStopSeconds(double energyPct, bool tyres = true) => 2 + (0.40 * Math.Max(0, energyPct)) + (tyres ? 12 : 0);

        public static RacePlanDto Build(RaceSnapshot snap, PlanContext context = null)
        {
            LiftCoastMeasure liftCoast = context?.LiftCoast;
            DriverRow me = snap?.Drivers?.FirstOrDefault(d => d.IsPlayer);
            PlayerStrategy p = snap?.Player;
            if (me == null || p == null || snap.SessionType != "Race")
            {
                return null;
            }

            // Mêmes tours restants que le calcul du carburant (« il manque X L »), pour ne jamais se contredire.
            double? lapsLeft = p.LapsRemaining ?? snap.RaceLength?.PlayerLapsRemaining;
            var resources = Resources(p);
            if (lapsLeft is not double left || resources.Count == 0)
            {
                return null;
            }

            double fraction = Math.Clamp(me.LapFraction, 0, 0.999);
            double margin = PlayerStrategyTracker.FuelMarginLaps;
            double need = left + margin;
            Resource limiting = resources.OrderBy(r => r.RangeNow).First();
            double rangeNow = limiting.RangeNow;
            double fullStint = resources.Min(r => r.FullStint);
            bool lmu = snap.Source?.Contains("LM Ultimate", StringComparison.OrdinalIgnoreCase) == true || p.Energy != null;

            var plan = new RacePlanDto
            {
                CompletedLaps = me.Laps,
                LapsRemaining = Math.Round(left, 2),
                Limiter = limiting.Name,
                RangeNow = Math.Round(rangeNow, 2),
                FullStint = Math.Round(fullStint, 2),
                PitLoss = p.PitPlan?.PitLoss,
                LiftCoast = liftCoast,
            };

            // Pneus : combien de tours tient un train (jusqu'à l'usure forte), pour savoir si l'on peut doubler les relais.
            double? wearPerLap = p.Wheels.Where(w => w.WearPerLapPct is > 0.01).Select(w => w.WearPerLapPct).DefaultIfEmpty(null).Max();
            double? heavy = p.Wheels.Select(w => (double?)w.HeavyWearLimitPct).DefaultIfEmpty(null).Min();
            if (wearPerLap is double wpl && heavy is double hv)
            {
                plan.TyreLife = Math.Round(hv / wpl, 1);
                plan.TyreLapsLeft = p.Wheels.Where(w => w.LapsToHeavyWear.HasValue).Select(w => w.LapsToHeavyWear).DefaultIfEmpty(null).Min() is double tl ? Math.Round(tl, 1) : null;
            }

            plan.IdealRatio = Ratio(p);
            if (p.Fuel?.ToAdd is double fAdd && fAdd > 0 && p.Energy?.ToAddPct is double eAdd && eAdd > 0.5)
            {
                plan.RatioToFinish = Math.Round(fAdd / eAdd, 3);
            }

            if (rangeNow >= need)
            {
                plan.StopsNeeded = 0;
                plan.CanFinish = true;
                plan.SpareLaps = Math.Round(rangeNow - left, 1);
                return plan;
            }

            if (fullStint <= 0.5)
            {
                return plan;
            }

            int stops = (int)Math.Ceiling(((need - rangeNow) / fullStint) - 1e-9);
            plan.StopsNeeded = Math.Max(1, stops);

            // Fenêtre du prochain arrêt, en tours bouclés à partir de maintenant (j = 1 : à la fin de ce tour).
            int latest = Math.Max(1, (int)Math.Floor(rangeNow + fraction - 0.05));
            int earliest = Math.Max(1, (int)Math.Ceiling(need + fraction - (plan.StopsNeeded.Value * fullStint) - 1e-9));
            plan.WindowEarliest = me.Laps + Math.Min(earliest, latest);
            plan.WindowLatest = me.Laps + latest;
            // Arrêt décidé avec le pilote (« au tour 21 ») s'il est dans la fenêtre, sinon l'arrêt conseillé, sinon le plus tard possible.
            int? wanted = context?.PlannedStopLap is int decided && decided > me.Laps ? decided : p.RecommendedStopLap;
            // Relais allongé à la radio en économisant (« deux tours de plus ») : l'arrêt décidé, même après la fenêtre, et les arrêts qui en découlent.
            bool stretched = context?.StretchLap is int st && st == context.PlannedStopLap && st > plan.WindowLatest;
            int chosen = stretched ? context.StretchLap.Value - me.Laps : wanted is int rec && rec >= plan.WindowEarliest && rec <= plan.WindowLatest ? rec - me.Laps : latest;
            if (stretched)
            {
                plan.StopsNeeded = Math.Max(1, (int)Math.Ceiling(((need - (chosen - fraction)) / fullStint) - 1e-9));
            }

            plan.PlannedByDriver = context?.PlannedStopLap is int d2 && d2 == me.Laps + chosen;
            plan.NextStopLap = me.Laps + chosen;

            // Arrêts suivants, au plus tard à chaque fois ; le dernier ne remet que ce qu'il faut pour finir.
            double firstDistance = chosen - fraction; // tours roulés jusqu'à la première entrée au stand
            int stintLaps = Math.Max(1, (int)Math.Floor(fullStint - 0.05));
            var levels = resources.ToDictionary(r => r.Name, r => r.Remaining);
            double drivenTotal = 0;
            for (int i = 0; i < plan.StopsNeeded && i < 12; i++)
            {
                double driven = i == 0 ? firstDistance : stintLaps;
                drivenTotal += driven;
                var stop = new PlannedStopDto { Lap = i == 0 ? plan.NextStopLap.Value : plan.Stops[^1].Lap + stintLaps };
                double toFinish = Math.Max(0, left - drivenTotal + margin);
                foreach (Resource r in resources)
                {
                    double arrival = Math.Max(0, levels[r.Name] - (r.PerLap * driven));
                    double add = Math.Max(0, Math.Min(r.Capacity - arrival, (toFinish * r.PerLap) - arrival));
                    levels[r.Name] = arrival + add;
                    if (r.Name == Resource.EnergyName)
                    {
                        stop.EnergyPct = Math.Round(add, 1);
                        stop.EnergyAfterPct = Math.Round(levels[r.Name], 1);
                    }
                    else
                    {
                        stop.FuelLitres = Math.Round(add, 1);
                        stop.FuelAfterLitres = Math.Round(levels[r.Name], 1);
                    }
                }

                stop.Last = i == plan.StopsNeeded - 1;
                stop.StopSeconds = lmu && stop.EnergyPct.HasValue ? Math.Round(LmuStopSeconds(stop.EnergyPct.Value), 0) : p.PitPlan?.StopTimeEstimate;
                plan.Stops.Add(stop);
            }

            // Un tour plus tôt, au tour prévu, un tour plus tard.
            foreach (int delta in new[] { -1, 0, 1 })
            {
                plan.Shifts.Add(Shift(plan, resources, me.Laps, chosen + delta, fraction, left, margin, fullStint, lmu));
            }

            plan.OneLess = OneLess(plan, resources, left, margin, liftCoast);
            return plan;
        }

        /// <summary>Arrêt à la fin du tour « bouclés + j » : possible ? combien d'arrêts au total ? combien remettre ?</summary>
        public static StopShiftDto Shift(RacePlanDto plan, List<Resource> resources, int completedLaps, int j, double fraction, double left, double margin, double fullStint, bool lmu)
        {
            var s = new StopShiftDto { Lap = completedLaps + j, Delta = (completedLaps + j) - (plan.NextStopLap ?? completedLaps + j) };
            if (j < 1)
            {
                s.Feasible = false;
                s.Reason = "tour déjà passé";
                return s;
            }

            double distance = j - fraction;
            if (distance >= left)
            {
                s.Feasible = false;
                s.Reason = "après l'arrivée";
                return s;
            }

            Resource dry = resources.Where(r => r.Remaining - (r.PerLap * distance) < 0).OrderBy(r => r.RangeNow).FirstOrDefault();
            s.Feasible = dry == null;
            if (dry != null)
            {
                s.Reason = dry.Name == Resource.EnergyName ? "énergie virtuelle vide avant" : "panne d'essence avant";
                // Économie à faire d'ici là pour tenir jusqu'à ce tour, et consommation à viser par tour pour tout ce qui ne suffit pas.
                s.SavingNeededPerLap = Math.Round(dry.PerLap - (dry.Remaining / distance), 2);
                s.SavingUnit = dry.Unit;
                s.SavingPct = Math.Round((1 - (dry.RangeNow / distance)) * 100, 1);
                foreach (Resource r in resources.Where(r => r.Remaining < r.PerLap * distance))
                {
                    if (r.Name == Resource.EnergyName)
                    {
                        s.EnergyPerLapTarget = Math.Round(r.Remaining / distance, 2);
                    }
                    else
                    {
                        s.FuelPerLapTarget = Math.Round(r.Remaining / distance, 2);
                    }
                }
            }

            // Arrêts et ce qu'on remet : en économisant, on arrive à vide.
            double toFinish = Math.Max(0, left - distance + margin);
            int stops = 1 + Math.Max(0, (int)Math.Ceiling(((toFinish - fullStint) / fullStint) - 1e-9));
            s.Stops = stops;
            s.ExtraStop = stops > (plan.StopsNeeded ?? stops);
            foreach (Resource r in resources)
            {
                double arrival = Math.Max(0, r.Remaining - (r.PerLap * distance));
                double add = Math.Max(0, Math.Min(r.Capacity - arrival, (toFinish * r.PerLap) - arrival));
                if (r.Name == Resource.EnergyName)
                {
                    s.EnergyPct = Math.Round(add, 1);
                }
                else
                {
                    s.FuelLitres = Math.Round(add, 1);
                }
            }

            s.StopSeconds = lmu && s.EnergyPct.HasValue ? Math.Round(LmuStopSeconds(s.EnergyPct.Value), 0) : null;
            return s;
        }

        /// <summary>Un arrêt de moins : consommation à viser par tour, économie, et si le lift and coast mesuré le rend rentable.</summary>
        private static OneLessStopDto OneLess(RacePlanDto plan, List<Resource> resources, double left, double margin, LiftCoastMeasure lc)
        {
            int target = (plan.StopsNeeded ?? 0) - 1;
            if (target < 0)
            {
                return null;
            }

            double need = left + margin;
            var o = new OneLessStopDto { Stops = target, Laps = Math.Round(left, 1) };
            double worst = 0;
            foreach (Resource r in resources)
            {
                // Tout ce qu'on peut emporter d'ici l'arrivée avec un arrêt de moins (on arrive à vide, on remplit à fond).
                double available = r.Remaining + (target * r.Capacity);
                double perLapTarget = available / need;
                double saving = r.PerLap - perLapTarget;
                if (saving <= 0)
                {
                    continue;
                }

                double pct = saving / r.PerLap;
                if (r.Name == Resource.EnergyName)
                {
                    o.EnergyPerLapTarget = Math.Round(perLapTarget, 2);
                    o.EnergySavingPerLap = Math.Round(saving, 2);
                }
                else
                {
                    o.FuelPerLapTarget = Math.Round(perLapTarget, 2);
                    o.FuelSavingPerLap = Math.Round(saving, 2);
                }

                if (pct > worst)
                {
                    worst = pct;
                    o.Limiter = r.Name;
                }
            }

            o.SavingPct = Math.Round(worst * 100, 1);
            o.Realistic = worst <= MaxRealisticSaving;
            o.PitTimeSaved = plan.PitLoss;

            // Coût du lift and coast mesuré, mis à l'échelle de l'économie demandée (au-delà de 30 % de plus que la mesure : pas couvert).
            if (lc?.TimeLoss is double loss && worst > 0)
            {
                double measured = o.Limiter == Resource.EnergyName ? lc.EnergySaved ?? 0 : lc.FuelSaved ?? 0;
                double wanted = o.Limiter == Resource.EnergyName ? o.EnergySavingPerLap ?? 0 : o.FuelSavingPerLap ?? 0;
                if (measured > 0.01)
                {
                    o.LiftCoastCoversIt = wanted <= measured * 1.3;
                    double perLap = Math.Max(0, loss) * Math.Max(0.2, wanted / measured);
                    o.LiftCoastCostPerLap = Math.Round(perLap, 2);
                    o.LiftCoastCost = Math.Round(perLap * left, 0);
                    if (plan.PitLoss is double pit)
                    {
                        o.NetGain = Math.Round(pit - o.LiftCoastCost.Value, 0);
                    }
                }
                else
                {
                    o.LiftCoastCoversIt = false;
                }
            }

            return o;
        }

        /// <summary>Ratio carburant / énergie virtuelle du menu des stands de LMU (litres par % d'énergie) qui équilibre les deux.</summary>
        public static double? Ratio(PlayerStrategy p) =>
            p?.Fuel?.PerLap is double f && f > 0 && p.Energy?.PerLapPct is double e && e > 0.3 ? Math.Round(f / e, 3) : null;

        public static List<Resource> Resources(PlayerStrategy p)
        {
            var list = new List<Resource>();
            if (p?.Fuel is { PerLap: double f } fuel && f > 0.01 && fuel.Capacity > 0)
            {
                list.Add(new Resource(Resource.FuelName, "L", fuel.Remaining, fuel.Capacity, f));
            }

            if (p?.Energy is { PerLapPct: double e } en && e > 0.05)
            {
                list.Add(new Resource(Resource.EnergyName, "%", en.RemainingPct, 100, e));
            }

            return list;
        }

        public sealed class Resource
        {
            public const string FuelName = "carburant";
            public const string EnergyName = "énergie virtuelle";

            public Resource(string name, string unit, double remaining, double capacity, double perLap)
            {
                Name = name;
                Unit = unit;
                Remaining = Math.Max(0, remaining);
                Capacity = capacity;
                PerLap = perLap;
            }

            public string Name { get; }

            public string Unit { get; }

            public double Remaining { get; }

            public double Capacity { get; }

            public double PerLap { get; }

            public double RangeNow => Remaining / PerLap;

            public double FullStint => Capacity / PerLap;
        }
    }

    /// <summary>Ce que l'ingénieur sait en plus du jeu : lift and coast mesuré, arrêt décidé avec le pilote.</summary>
    public sealed class PlanContext
    {
        public LiftCoastMeasure LiftCoast { get; set; }

        public int? PlannedStopLap { get; set; }

        /// <summary>Arrêt repoussé à la radio en économisant d'ici là : gardé même après la fenêtre.</summary>
        public int? StretchLap { get; set; }
    }

    /// <summary>Plan de course jusqu'au drapeau (voir <see cref="RacePlanner"/>).</summary>
    public sealed class RacePlanDto
    {
        public int CompletedLaps { get; set; }

        public double LapsRemaining { get; set; }

        /// <summary>« carburant » ou « énergie virtuelle » : ce qui s'épuise le premier.</summary>
        public string Limiter { get; set; }

        /// <summary>Tours possibles avec ce qu'il reste à bord.</summary>
        public double RangeNow { get; set; }

        /// <summary>Tours d'un relais réservoir et énergie pleins.</summary>
        public double FullStint { get; set; }

        public int? StopsNeeded { get; set; }

        public bool CanFinish { get; set; }

        /// <summary>Tours d'avance à l'arrivée quand on peut finir sans s'arrêter.</summary>
        public double? SpareLaps { get; set; }

        public int? NextStopLap { get; set; }

        /// <summary>Le prochain arrêt est celui décidé avec le pilote.</summary>
        public bool PlannedByDriver { get; set; }

        public int? WindowEarliest { get; set; }

        public int? WindowLatest { get; set; }

        public List<PlannedStopDto> Stops { get; set; } = new();

        public List<StopShiftDto> Shifts { get; set; } = new();

        public OneLessStopDto OneLess { get; set; }

        public double? IdealRatio { get; set; }

        /// <summary>Ratio qui remet exactement l'essence et l'énergie qui manquent pour finir.</summary>
        public double? RatioToFinish { get; set; }

        public double? PitLoss { get; set; }

        /// <summary>Tours que tient un train de pneus (jusqu'à l'usure forte) et tours qui restent au train monté.</summary>
        public double? TyreLife { get; set; }

        public double? TyreLapsLeft { get; set; }

        public LiftCoastMeasure LiftCoast { get; set; }
    }

    public sealed class PlannedStopDto
    {
        public int Lap { get; set; }

        public double? FuelLitres { get; set; }

        public double? EnergyPct { get; set; }

        public double? StopSeconds { get; set; }

        public bool Last { get; set; }

        /// <summary>Niveaux après l'arrêt : ce que règle le menu des stands de LMU.</summary>
        public double? EnergyAfterPct { get; set; }

        public double? FuelAfterLitres { get; set; }
    }

    public sealed class StopShiftDto
    {
        public int Lap { get; set; }

        /// <summary>−1 : un tour plus tôt que prévu, +1 : un tour plus tard.</summary>
        public int Delta { get; set; }

        public bool Feasible { get; set; }

        public string Reason { get; set; }

        public int? Stops { get; set; }

        public bool ExtraStop { get; set; }

        public double? FuelLitres { get; set; }

        public double? EnergyPct { get; set; }

        public double? StopSeconds { get; set; }

        /// <summary>Arrêt trop tard : économie à faire par tour d'ici là (dans <see cref="SavingUnit"/>).</summary>
        public double? SavingNeededPerLap { get; set; }

        public string SavingUnit { get; set; }

        /// <summary>Arrêt trop tard : économie en % de la consommation actuelle, et consommation à viser par tour d'ici là.</summary>
        public double? SavingPct { get; set; }

        public double? EnergyPerLapTarget { get; set; }

        public double? FuelPerLapTarget { get; set; }
    }

    public sealed class OneLessStopDto
    {
        public int Stops { get; set; }

        public double Laps { get; set; }

        public string Limiter { get; set; }

        public double? EnergyPerLapTarget { get; set; }

        public double? EnergySavingPerLap { get; set; }

        public double? FuelPerLapTarget { get; set; }

        public double? FuelSavingPerLap { get; set; }

        /// <summary>Économie demandée, en % de la consommation actuelle.</summary>
        public double SavingPct { get; set; }

        public bool Realistic { get; set; }

        public double? PitTimeSaved { get; set; }

        public bool? LiftCoastCoversIt { get; set; }

        public double? LiftCoastCostPerLap { get; set; }

        public double? LiftCoastCost { get; set; }

        /// <summary>Temps gagné (positif) ou perdu (négatif) en faisant un arrêt de moins en lift and coast.</summary>
        public double? NetGain { get; set; }
    }

    /// <summary>Tour de lift and coast mesuré contre un tour de référence du même pilote.</summary>
    public sealed class LiftCoastMeasure
    {
        public int TestLap { get; set; }

        public int ReferenceLap { get; set; }

        public double TestTime { get; set; }

        public double ReferenceTime { get; set; }

        /// <summary>Temps perdu par tour (s).</summary>
        public double? TimeLoss { get; set; }

        public double? TestEnergy { get; set; }

        public double? ReferenceEnergy { get; set; }

        /// <summary>Énergie virtuelle économisée par tour (points de %).</summary>
        public double? EnergySaved { get; set; }

        public double? TestFuel { get; set; }

        public double? ReferenceFuel { get; set; }

        /// <summary>Essence économisée par tour (L).</summary>
        public double? FuelSaved { get; set; }

        public bool Validated { get; set; }
    }
}
