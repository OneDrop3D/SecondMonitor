namespace SecondMonitor.Hub.Core.Strategy
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using SecondMonitor.Hub.Core.Race;

    /// <summary>
    /// Simulations d'arrêt, avec des valeurs mesurées sur tout le plateau quand elles existent :
    /// - perte au stand : tour d'entrée + tour de sortie − deux tours normaux, médiane des arrêts observés ;
    /// - gain des pneus neufs : rythme juste avant l'arrêt contre rythme juste après, médiane du plateau ;
    /// - position à la sortie si vous rentrez maintenant ;
    /// - undercut (s'arrêter avant la voiture devant), overcut (rester dehors quand elle s'arrête),
    ///   menace d'undercut de la voiture derrière.
    /// </summary>
    public static class PitPlanner
    {
        public const double DefaultPitLoss = 22;
        public const double DefaultWarmup = 0.8;
        private const double PassMargin = 0.5; // marge pour ressortir vraiment devant
        private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

        public static PitPlanDto Build(RaceSnapshot snap, IReadOnlyDictionary<string, DriverTracker> trackers)
        {
            DriverRow me = snap.Drivers.FirstOrDefault(d => d.IsPlayer);
            if (me == null || snap.SessionType != "Race")
            {
                return null;
            }

            var (pitLoss, stops) = MeasurePitLoss(trackers);
            var (fieldGain, gainSamples) = MeasureFreshGain(trackers);
            double warmup = DefaultWarmup;
            double myPace = me.PaceLast3 ?? snap.Player?.AverageLap ?? me.LastLap ?? 0;
            int myAge = me.Tyres.StintLaps;
            double? playerGain = snap.Player?.Degradation is double deg && deg > 0 && myAge > 0 ? deg * myAge : null;
            double? freshGain = fieldGain ?? playerGain;

            var plan = new PitPlanDto
            {
                PitLoss = Math.Round(pitLoss ?? DefaultPitLoss, 1),
                PitLossSource = pitLoss.HasValue ? $"mesuré sur {stops} arrêt{(stops > 1 ? "s" : string.Empty)}" : "estimation, aucun arrêt vu pour l'instant",
                FreshTyreGain = freshGain.HasValue ? Math.Round(freshGain.Value, 2) : null,
                FreshTyreGainSource = fieldGain.HasValue ? $"mesuré sur {gainSamples} relais" : playerGain.HasValue ? "d'après ta dégradation" : "inconnu pour l'instant",
                WarmupPenalty = warmup,
            };

            if (trackers != null && trackers.TryGetValue(me.Id, out DriverTracker mine) && LastStopLoss(mine) is var (lossLap, loss))
            {
                plan.PlayerLastStopLap = lossLap;
                plan.PlayerLastStopLoss = Math.Round(loss, 1);
            }

            var cls = snap.Drivers.Where(d => d.ClassId == me.ClassId && d.FinishStatus is not ("Dnf" or "Dq")).OrderBy(d => d.ClassPosition).ToList();
            plan.RejoinNow = Rejoin(snap, me, cls, plan.PitLoss);

            int idx = cls.FindIndex(d => d.Id == me.Id);
            DriverRow ahead = idx > 0 ? cls[idx - 1] : null;
            DriverRow behind = idx >= 0 && idx + 1 < cls.Count ? cls[idx + 1] : null;
            if (myPace <= 0)
            {
                return plan;
            }

            if (ahead != null && ahead.GapToPlayer is double ga && ahead.GapToClassLeaderLaps == me.GapToClassLeaderLaps)
            {
                plan.Cuts.Add(Undercut(me, ahead, -ga, myPace, freshGain, warmup));
                plan.Cuts.Add(Overcut(me, ahead, -ga, myPace, freshGain, warmup));
            }

            if (behind != null && behind.GapToPlayer is double gb && behind.GapToClassLeaderLaps == me.GapToClassLeaderLaps)
            {
                plan.Cuts.Add(Threat(me, behind, gb, myPace, freshGain, warmup));
            }

            plan.Cuts.RemoveAll(c => c == null);
            return plan;
        }

        private static CutDto Undercut(DriverRow me, DriverRow rival, double gap, double myPace, double? freshGain, double warmup)
        {
            var cut = Base("undercut", rival, gap);
            if (rival.PaceLast3 is not double rivalPace || freshGain is not double fg)
            {
                cut.Text = $"Undercut sur le numéro {rival.Number} : pas encore assez de tours pour le calculer.";
                return cut;
            }

            // En pneus neufs, vous tournez en myPace − fg ; lui reste sur ses vieux pneus tant qu'il ne s'arrête pas.
            double d = rivalPace - (myPace - fg);
            cut.GainPerLap = Math.Round(d, 2);
            if (d <= 0.05)
            {
                cut.Text = d <= 0
                    ? $"Undercut sur le numéro {rival.Number} ({N(gap, 1)} s devant) : non, même avec des pneus neufs tu serais plus lent que lui de {N(-d, 2)} seconde par tour."
                    : $"Undercut sur le numéro {rival.Number} ({N(gap, 1)} s devant) : non, les pneus neufs ne te feraient gagner que {N(d, 2)} seconde par tour.";
                return cut;
            }

            int k = (int)Math.Ceiling((gap + PassMargin + warmup) / d);
            cut.LapsNeeded = k;
            int? hisStopIn = rival.NextStop?.LapsUntil;
            cut.Feasible = k <= 2 || (hisStopIn is int h && h >= k);
            string when = k <= 1 ? "même s'il s'arrête au tour suivant" : $"s'il reste dehors au moins {k} tours après toi";
            cut.Text = cut.Feasible
                ? $"Undercut possible sur le numéro {rival.Number} ({N(gap, 1)} s devant) : en t'arrêtant avant lui, tu gagnes {N(d, 2)} s par tour avec des pneus neufs et passe devant {when}."
                : $"Undercut difficile sur le numéro {rival.Number} ({N(gap, 1)} s devant) : il faudrait qu'il reste dehors {k} tours de plus que toi (gain {N(d, 2)} s par tour).";
            return cut;
        }

        private static CutDto Overcut(DriverRow me, DriverRow rival, double gap, double myPace, double? freshGain, double warmup)
        {
            var cut = Base("overcut", rival, gap);
            if (rival.PaceLast3 is not double rivalPace || freshGain is not double fg)
            {
                return null;
            }

            // Il s'arrête d'abord : son tour de sortie sur pneus froids lui coûte « warmup », puis il va plus vite que vous.
            double hisFresh = rivalPace - fg;
            double d = myPace - hisFresh; // ce qu'il vous reprend par tour ensuite
            double gain1 = warmup - d;     // ce que vous gagnez sur son tour de sortie
            cut.GainPerLap = Math.Round(-d, 2);
            cut.LapsNeeded = 1;
            cut.Feasible = gain1 > gap + PassMargin;
            cut.Text = cut.Feasible
                ? $"Overcut possible sur le numéro {rival.Number} : s'il s'arrête avant toi, son tour de sortie sur pneus froids te fait gagner {N(gain1, 1)} s, plus que les {N(gap, 1)} s d'écart. Reste dehors un tour de plus que lui."
                : $"Overcut sur le numéro {rival.Number} : non, ses pneus neufs vont plus vite ({N(d, 2)} s par tour) que ce qu'il perd au tour de sortie ({N(warmup, 1)} s).";
            return cut;
        }

        private static CutDto Threat(DriverRow me, DriverRow rival, double gap, double myPace, double? freshGain, double warmup)
        {
            var cut = Base("menace-undercut", rival, gap);
            if (rival.PaceLast3 is not double rivalPace || freshGain is not double fg)
            {
                return null;
            }

            double d = myPace - (rivalPace - fg); // ce qu'il vous reprend par tour en pneus neufs
            cut.GainPerLap = Math.Round(d, 2);
            if (d <= 0.05)
            {
                cut.Text = $"Pas de menace d'undercut du numéro {rival.Number} ({N(gap, 1)} s derrière) : ses pneus neufs ne lui suffiraient pas.";
                return cut;
            }

            int k = (int)Math.Ceiling((gap + PassMargin + warmup) / d);
            cut.LapsNeeded = k;
            cut.Feasible = k <= 2;
            cut.Text = cut.Feasible
                ? $"Menace d'undercut : le numéro {rival.Number} ({N(gap, 1)} s derrière) te passe en {k} tour{(k > 1 ? "s" : string.Empty)} s'il s'arrête avant toi. Pour couvrir : rentre le tour qui suit son arrêt."
                : $"Faible menace d'undercut du numéro {rival.Number} ({N(gap, 1)} s derrière) : il lui faudrait {k} tours de pneus neufs avant ton arrêt.";
            return cut;
        }

        private static RejoinDto Rejoin(RaceSnapshot snap, DriverRow me, List<DriverRow> cls, double pitLoss)
        {
            // Voitures de la catégorie, dans le même tour, à moins de pitLoss derrière : elles passent devant pendant l'arrêt.
            var behindSameLap = cls.Where(d => d.Id != me.Id && d.GapToPlayer is double g && g > 0 && d.GapToClassLeaderLaps == me.GapToClassLeaderLaps)
                .OrderBy(d => d.GapToPlayer).ToList();
            var passed = behindSameLap.Where(d => d.GapToPlayer < pitLoss).ToList();
            DriverRow newAhead = passed.LastOrDefault();
            DriverRow newBehind = behindSameLap.FirstOrDefault(d => d.GapToPlayer >= pitLoss);
            int traffic = snap.Drivers.Count(d => d.Id != me.Id && d.ClassId != me.ClassId && d.GapToPlayer is double g && Math.Abs(g - pitLoss) < 1.5);
            var r = new RejoinDto
            {
                Position = me.ClassPosition + passed.Count,
                AheadNumber = newAhead?.Number,
                AheadName = newAhead?.Name,
                AheadGap = newAhead?.GapToPlayer is double ga ? Math.Round(pitLoss - ga, 1) : null,
                BehindNumber = newBehind?.Number,
                BehindName = newBehind?.Name,
                BehindGap = newBehind?.GapToPlayer is double gb ? Math.Round(gb - pitLoss, 1) : null,
                Traffic = traffic,
            };
            string pos = $"P{r.Position}";
            string around = string.Join(", ", new[]
            {
                newAhead != null ? $"{N(r.AheadGap ?? 0, 1)} s derrière le numéro {newAhead.Number}" : null,
                newBehind != null ? $"{N(r.BehindGap ?? 0, 1)} s devant le numéro {newBehind.Number}" : null,
            }.Where(x => x != null));
            r.Text = $"Si tu rentres maintenant, tu ressors {pos}{(around.Length > 0 ? $", {around}" : string.Empty)}{(traffic > 0 ? $", avec {traffic} voiture(s) d'une autre catégorie tout près" : string.Empty)}.";
            return r;
        }

        /// <summary>Perte au stand : (tour d'entrée + tour de sortie) − 2 × tour normal du même pilote, médiane sur le plateau.</summary>
        public static (double? Loss, int Count) MeasurePitLoss(IReadOnlyDictionary<string, DriverTracker> trackers)
        {
            var samples = new List<double>();
            foreach (DriverTracker t in trackers.Values)
            {
                var clean = t.Laps.Where(l => l.Time > 0 && !l.IsPitLap && !l.IsOutLap).Select(l => l.Time.Value).ToList();
                if (clean.Count < 2)
                {
                    continue;
                }

                double normal = Median(clean);
                for (int i = 0; i + 1 < t.Laps.Count; i++)
                {
                    LapRecord inLap = t.Laps[i];
                    LapRecord outLap = t.Laps[i + 1];
                    if (inLap.IsPitLap && outLap.IsOutLap && inLap.Time > 0 && outLap.Time > 0)
                    {
                        double loss = inLap.Time.Value + outLap.Time.Value - (2 * normal);
                        if (loss is > 8 and < 120)
                        {
                            samples.Add(loss);
                        }
                    }
                }
            }

            return samples.Count == 0 ? (null, 0) : (Median(samples), samples.Count);
        }

        /// <summary>
        /// Temps perdu par le dernier arrêt de ce pilote : tour d'entrée + tour de sortie − deux tours normaux (médiane de ses tours propres).
        /// Null tant que le tour de sortie n'est pas bouclé.
        /// </summary>
        public static (int Lap, double Loss)? LastStopLoss(DriverTracker t)
        {
            var clean = t.Laps.Where(l => l.Time > 0 && !l.IsPitLap && !l.IsOutLap).Select(l => l.Time.Value).ToList();
            if (clean.Count < 2)
            {
                return null;
            }

            double normal = Median(clean);
            for (int i = t.Laps.Count - 2; i >= 0; i--)
            {
                LapRecord inLap = t.Laps[i];
                LapRecord outLap = t.Laps[i + 1];
                if (inLap.IsPitLap && outLap.IsOutLap && inLap.Time > 0 && outLap.Time > 0)
                {
                    double loss = inLap.Time.Value + outLap.Time.Value - (2 * normal);
                    return loss is > 5 and < 300 ? (outLap.Lap, loss) : null;
                }
            }

            return null;
        }

        /// <summary>Gain des pneus neufs : médiane des 3 derniers tours d'un relais moins les tours 2 à 4 du suivant.</summary>
        public static (double? Gain, int Count) MeasureFreshGain(IReadOnlyDictionary<string, DriverTracker> trackers)
        {
            var samples = new List<double>();
            foreach (DriverTracker t in trackers.Values)
            {
                for (int s = 0; s + 1 < t.Stints.Count; s++)
                {
                    StintRecord before = t.Stints[s];
                    StintRecord after = t.Stints[s + 1];
                    if (before.EndLap is not int end)
                    {
                        continue;
                    }

                    var last = t.Laps.Where(l => l.Lap <= end && l.Lap > end - 3 && l.Time > 0 && !l.IsPitLap && !l.IsOutLap).Select(l => l.Time.Value).ToList();
                    var first = t.Laps.Where(l => l.Lap > after.StartLap + 1 && l.Lap <= after.StartLap + 4 && l.Time > 0 && !l.IsPitLap && !l.IsOutLap).Select(l => l.Time.Value).ToList();
                    if (last.Count >= 2 && first.Count >= 2)
                    {
                        double gain = Median(last) - Median(first);
                        if (gain is > -3 and < 6)
                        {
                            samples.Add(gain);
                        }
                    }
                }
            }

            return samples.Count == 0 ? (null, 0) : (Math.Max(0, Median(samples)), samples.Count);
        }

        private static CutDto Base(string kind, DriverRow rival, double gap) => new()
        {
            Kind = kind,
            RivalId = rival.Id,
            Number = rival.Number,
            Name = rival.Name,
            Gap = Math.Round(gap, 1),
        };

        private static double Median(List<double> values)
        {
            var v = values.OrderBy(x => x).ToList();
            return v.Count % 2 == 1 ? v[v.Count / 2] : (v[(v.Count / 2) - 1] + v[v.Count / 2]) / 2;
        }

        private static string N(double v, int digits) => v.ToString("F" + digits, Fr);
    }

    public sealed class PitPlanDto
    {
        public double PitLoss { get; set; }

        public string PitLossSource { get; set; }

        /// <summary>Dernier arrêt du joueur : tour de sortie et temps perdu (entrée + sortie des stands comparées à son rythme).</summary>
        public int? PlayerLastStopLap { get; set; }

        public double? PlayerLastStopLoss { get; set; }

        public double? FreshTyreGain { get; set; }

        public string FreshTyreGainSource { get; set; }

        public double WarmupPenalty { get; set; }

        public RejoinDto RejoinNow { get; set; }

        public List<CutDto> Cuts { get; set; } = new();
    }

    public sealed class RejoinDto
    {
        public int Position { get; set; }

        public int? AheadNumber { get; set; }

        public string AheadName { get; set; }

        public double? AheadGap { get; set; }

        public int? BehindNumber { get; set; }

        public string BehindName { get; set; }

        public double? BehindGap { get; set; }

        public int Traffic { get; set; }

        public string Text { get; set; }
    }

    public sealed class CutDto
    {
        /// <summary>« undercut », « overcut », « menace-undercut ».</summary>
        public string Kind { get; set; }

        public string RivalId { get; set; }

        public int? Number { get; set; }

        public string Name { get; set; }

        public double Gap { get; set; }

        public double? GainPerLap { get; set; }

        public int? LapsNeeded { get; set; }

        public bool Feasible { get; set; }

        public string Text { get; set; }
    }
}
