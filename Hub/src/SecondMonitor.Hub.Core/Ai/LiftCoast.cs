namespace SecondMonitor.Hub.Core.Ai
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using SecondMonitor.Hub.Core.Race;
    using SecondMonitor.Hub.Core.Strategy;
    using static SecondMonitor.Hub.Core.Ai.RadioFormat;

    /// <summary>État de l'essai de lift and coast (dans la mémoire de course).</summary>
    public sealed class LiftCoastTest
    {
        /// <summary>null (rien), « armé » (tour de test à venir), « à valider » (mesuré, le pilote doit dire s'il a fait une erreur).</summary>
        public string State { get; set; }

        /// <summary>Tour de test (numéro du tour une fois bouclé, comme dans la liste des tours).</summary>
        public int? TestLap { get; set; }

        /// <summary>Tour de référence choisi par le pilote (« le tour 18, c'est ma référence »), sinon le dernier tour propre.</summary>
        public int? ReferenceLap { get; set; }

        /// <summary>Mesure du dernier tour de test, en attente du « pas d'erreur » du pilote.</summary>
        public LiftCoastMeasure Candidate { get; set; }

        /// <summary>Mesure validée par le pilote : sert au plan de course (un arrêt de moins rentable ou non).</summary>
        public LiftCoastMeasure Measure { get; set; }

        /// <summary>Lift and coast décidé jusqu'à la fin : consommation d'énergie (%) ou d'essence (L) à ne pas dépasser par tour.</summary>
        public double? TargetPerLap { get; set; }

        public string TargetUnit { get; set; }

        public int LastCheckedLap { get; set; }

        public DateTime? ProposedAtUtc { get; set; }
    }

    /// <summary>
    /// Lift and coast : l'ingénieur fait faire un tour d'essai (lever le pied avant les gros freinages), le compare à un tour
    /// de référence du pilote (temps perdu, énergie et essence économisées), demande s'il y a eu une erreur, puis dit si
    /// faire un arrêt de moins en lift and coast fait gagner ou perdre du temps. Ensuite, il surveille la cible au tour.
    /// </summary>
    public static class LiftCoast
    {
        public const string PendingCheck = "lift-check";
        public const string PendingGo = "lift-go";
        public const string PendingPropose = "lift-propose";
        private const string Topic = "stratégie";

        /// <summary>Lift and coast dans la phrase (dictée comprise : « Lyssenko », « live and course », « lifted »).</summary>
        public static readonly Regex Words = new(
            @"\b(lift and coast|lift (and|end|en|et|n) co\w*|lifted|lift|lyss?enko|liss?enko|live (and|end|en|et) co\w*|lever le pied|leve le pied|lever du pied|roue libre|economiser de l energie|economie d energie)\b",
            RegexOptions.Compiled);

        private static readonly Regex StartWords = new(
            @"\b(je (fais|vais faire|refais|tente|teste|lance|te fais)|on (fait|refait|teste|essaie|essaye|tente|lance|y va)|fais moi|essai|test|ce tour|au prochain tour|prochain tour|un tour (en|de)|je commence)\b",
            RegexOptions.Compiled);

        private static readonly Regex ValueWords = new(
            @"\b(vaut|rentable|interet|gagner|gagne|gagnerait|perdre|perd|perdrait|combien|est ce que|si je|on peut|ca vaut|faut il|faudrait)\b",
            RegexOptions.Compiled);

        private static readonly Regex NoMistake = new(
            @"\b(pas (fait )?d erreurs?|aucune erreur|sans erreur|pas de faute|tour propre|propre|nickel|c est bon|valide|il est bon|c etait bon|parfait)\b",
            RegexOptions.Compiled);

        private static readonly Regex Mistake = new(
            @"\b(erreur|faute|rate|loupe|trafic|bloque|blocage|sorti|sortie de piste|tout droit|tete a queue|touche|jaune|drapeau|double|pas propre|pas bon)\b",
            RegexOptions.Compiled);

        private static readonly Regex ReferenceWords = new(@"\b(reference|tour de ref|ma ref|en ref)\b", RegexOptions.Compiled);

        /// <summary>
        /// Phrase du pilote sur le lift and coast (lancer l'essai, choisir la référence, valider la mesure, décider) : réponse, ou null.
        /// </summary>
        public static string Handle(string question, RaceMemory memory, RaceSnapshot snap, IReadOnlyList<LapDto> laps)
        {
            if (memory == null || snap?.HasData != true)
            {
                return null;
            }

            string t = RadioCommands.Normalize(question);
            DriverRow me = snap.Drivers.FirstOrDefault(d => d.IsPlayer);
            if (me == null || t.Length == 0)
            {
                return null;
            }

            LiftCoastTest test = memory.LiftCoast ??= new LiftCoastTest();
            PendingQuestion pending = memory.Pending(300);
            int words = t.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

            // Réponse à « Tour propre, sans erreur ? »
            if (pending?.Topic == PendingCheck && test.State == "à valider" && test.Candidate != null && words <= 16)
            {
                // « Tour propre, sans erreur ? » : « oui », « pas d'erreur » = propre ; « non », « j'ai fait une erreur », « trafic » = à refaire.
                bool surelyDirty = Regex.IsMatch(t, @"\b(pas propre|pas bon|pas clean|j ai (fait )?(une|des) (erreur|faute)s?|j ai (rate|loupe|bloque)|du trafic)\b");
                bool clean = !surelyDirty && (NoMistake.IsMatch(t) || (RadioCommands.YesNo(question) == true && !Mistake.IsMatch(t)));
                bool dirty = !clean && (surelyDirty || Mistake.IsMatch(t) || RadioCommands.YesNo(question) == false);
                if (clean || dirty)
                {
                    memory.ClearPending();
                    return clean ? Validate(test, memory, snap, me) : Retry(test, me, "Copy, tour pas représentatif.");
                }
            }

            // Réponse à « On le fait ? » (un arrêt de moins en lift and coast).
            if (pending?.Topic == PendingGo && RadioCommands.YesNo(question) is bool go && words <= 12)
            {
                memory.ClearPending();
                return go ? Commit(test, memory, snap, me) : Decline(test, snap);
            }

            // Réponse à « Tu me fais un tour en lift and coast pour mesurer ? »
            if (pending?.Topic == PendingPropose && RadioCommands.YesNo(question) is bool ok && words <= 12)
            {
                memory.ClearPending();
                return ok ? Arm(test, me, laps) : "Copy, on reste en attaque.";
            }

            bool lift = Words.IsMatch(t);

            // « Le tour 18, c'est ma référence », « j'ai pas fait d'erreur, voilà un tour de référence ».
            if (ReferenceWords.IsMatch(t) && (lift || Regex.IsMatch(t, @"\b(tour|ce tour|dernier tour|celui la|celui ci)\b")) && !RadioCommands.IsQuestion(question))
            {
                int? lap = LapNumber(t) ?? LastCleanLap(laps, me.Laps + 1);
                LapDto reference = laps?.FirstOrDefault(l => l.Lap == lap);
                if (reference?.Time is not double rt)
                {
                    return "Pas encore de tour propre mesuré pour servir de référence : boucle un tour sans erreur et redis-le moi.";
                }

                test.ReferenceLap = lap;
                string load = Load(reference);
                return $"Copy, tour {lap} en référence : {Lap(rt, 1)}{(load.Length > 0 ? ", " + load : string.Empty)}.";
            }

            if (!lift)
            {
                return null;
            }

            // Question de valeur (« ça vaut le coup ? », « si je fais du lift and coast on gagne du temps ? ») : verdict, ou essai proposé.
            bool start = StartWords.IsMatch(t) && !Regex.IsMatch(t, @"\b(si je|est ce que|ca vaut|vaut le coup)\b");
            if (!start && (ValueWords.IsMatch(t) || RadioCommands.IsQuestion(question)))
            {
                return Value(test, memory, snap, me);
            }

            if (start || words <= 5)
            {
                // « Je fais du lift and coast jusqu'à la fin » avec une mesure déjà validée : c'est la décision, pas un nouvel essai.
                if (test.Measure != null && Regex.IsMatch(t, @"\b(jusqu a la fin|jusqu au bout|tout le relais|on y va|on le fait)\b"))
                {
                    return Commit(test, memory, snap, me);
                }

                return Arm(test, me, laps);
            }

            return null;
        }

        /// <summary>
        /// Appelé toutes les quelques secondes : tour de test bouclé → mesure et question « sans erreur ? » ;
        /// lift and coast décidé → un mot si le dernier tour dépasse la cible. Null s'il n'y a rien à dire.
        /// </summary>
        public static RadioLine Check(RaceMemory memory, RaceSnapshot snap, IReadOnlyList<LapDto> laps)
        {
            LiftCoastTest test = memory?.LiftCoast;
            DriverRow me = snap?.Drivers?.FirstOrDefault(d => d.IsPlayer);
            if (test == null || me == null || laps == null)
            {
                return null;
            }

            if (test.State == "armé" && test.TestLap is int tl)
            {
                LapDto lap = laps.FirstOrDefault(l => l.Lap == tl);
                if (lap == null)
                {
                    if (me.Laps > tl + 1)
                    {
                        test.State = null; // tour jamais reçu (session changée, saut de tours) : essai oublié
                    }

                    return null;
                }

                if (lap.Pit || !lap.Valid || lap.Time is not > 0 || (lap.Energy == null && lap.Fuel == null))
                {
                    string why = lap.Pit ? "passage au stand" : !lap.Valid ? "tour annulé" : "pas de mesure";
                    return new RadioLine(Retry(test, me, $"Tour {tl} pas exploitable ({why})."), Topic, false);
                }

                int? refLap = test.ReferenceLap is int chosen && laps.Any(l => l.Lap == chosen && l.Time > 0) ? chosen : LastCleanLap(laps, tl);
                LapDto reference = laps.FirstOrDefault(l => l.Lap == refLap);
                if (reference?.Time is not double rt)
                {
                    test.State = null;
                    return new RadioLine("Tour de lift and coast bouclé, mais pas de tour propre avant pour comparer. Fais un tour normal, puis on recommence.", Topic, false);
                }

                var m = new LiftCoastMeasure
                {
                    TestLap = tl,
                    ReferenceLap = refLap.Value,
                    TestTime = lap.Time.Value,
                    ReferenceTime = rt,
                    TimeLoss = Math.Round(lap.Time.Value - rt, 2),
                    TestEnergy = lap.Energy,
                    ReferenceEnergy = reference.Energy,
                    EnergySaved = lap.Energy is double te && reference.Energy is double re ? Math.Round(re - te, 2) : null,
                    TestFuel = lap.Fuel,
                    ReferenceFuel = reference.Fuel,
                    FuelSaved = lap.Fuel is double tf && reference.Fuel is double rf ? Math.Round(rf - tf, 2) : null,
                };
                test.Candidate = m;
                test.State = "à valider";
                memory.Ask(new PendingQuestion { Key = PendingCheck + tl, Topic = PendingCheck, Text = "Tour propre, sans erreur ?", Lap = me.Laps });
                return new RadioLine(Describe(m) + " Tour propre, sans erreur ?", Topic, true);
            }

            // Lift and coast décidé : un mot quand le dernier tour dépasse nettement la cible (au plus un tour sur deux).
            if (test.TargetPerLap is double target && me.Laps > test.LastCheckedLap)
            {
                LapDto last = laps.LastOrDefault();
                if (last == null || last.Lap != me.Laps)
                {
                    return null;
                }

                test.LastCheckedLap = me.Laps;
                double? used = test.TargetUnit == "L" ? last.Fuel : last.Energy;
                double tolerance = test.TargetUnit == "L" ? 0.15 : 0.2;
                if (!last.Pit && used is double u && u > target + tolerance && me.Laps % 2 == 0)
                {
                    string unit = test.TargetUnit == "L" ? " L" : " %";
                    return new RadioLine($"Tour {last.Lap} : {N(u, 1)}{unit} pour une cible de {N(target, 1)}{unit}. Lève le pied un peu plus tôt.", Topic, false);
                }
            }

            return null;
        }

        /// <summary>
        /// Proposition spontanée : un arrêt de moins serait possible avec une petite économie et rien n'est mesuré.
        /// Une fois toutes les 25 minutes au plus, jamais en duel ni dans les 6 derniers tours.
        /// </summary>
        public static RadioLine Propose(RaceMemory memory, RaceSnapshot snap, DateTime nowUtc)
        {
            LiftCoastTest test = memory?.LiftCoast;
            RacePlanDto plan = snap?.Player?.Plan;
            DriverRow me = snap?.Drivers?.FirstOrDefault(d => d.IsPlayer);
            if (test == null || plan?.OneLess == null || me == null || me.InPits || test.Measure != null || test.State != null || memory.Pending() != null)
            {
                return null;
            }

            if (test.ProposedAtUtc is DateTime at && (nowUtc - at).TotalMinutes < 25)
            {
                return null;
            }

            OneLessStopDto o = plan.OneLess;
            bool battle = snap.Player.Battles?.Any(b => b.Gap is double g && Math.Abs(g) < 1.5) == true;
            if (battle || plan.LapsRemaining < 6 || o.SavingPct < 1 || o.SavingPct > 10 || me.Laps < 3)
            {
                return null;
            }

            test.ProposedAtUtc = nowUtc;
            string text = $"On peut faire {Stops(o.Stops)} au lieu de {Stops(o.Stops + 1)} si tu économises {Saving(o)} par tour. Tu me fais un tour en lift and coast pour mesurer ?";
            memory.Ask(new PendingQuestion { Key = PendingPropose, Topic = PendingPropose, Text = text, Lap = me.Laps });
            return new RadioLine(text, Topic, true);
        }

        /// <summary>« Si je fais du lift and coast, on gagne du temps ? » : verdict d'après la mesure, ou ce qu'il faudrait économiser et l'essai proposé.</summary>
        public static string Value(LiftCoastTest test, RaceMemory memory, RaceSnapshot snap, DriverRow me)
        {
            RacePlanDto plan = RacePlanner.Build(snap, Context(memory, test.Measure));
            if (plan == null)
            {
                return "Il me faut un ou deux tours de consommation avant de te dire si le lift and coast paie.";
            }

            if (plan.StopsNeeded == 0)
            {
                return "Tu as déjà de quoi finir sans arrêt : pas besoin de lift and coast, attaque.";
            }

            OneLessStopDto o = plan.OneLess;
            if (test.Measure != null)
            {
                return Verdict(plan, test.Measure, memory, me, ask: true);
            }

            if (o != null && !o.Realistic)
            {
                return $"Pour {Stops(o.Stops)} au lieu de {Stops(o.Stops + 1)}, il faudrait économiser {Saving(o)} par tour ({N(o.SavingPct, 0)} %) : trop, le lift and coast ne suffira pas. On garde l'attaque.";
            }

            string need = o == null ? string.Empty : $"Pour {Stops(o.Stops)} au lieu de {Stops(o.Stops + 1)}, il faut économiser {Saving(o)} par tour. ";
            memory.Ask(new PendingQuestion { Key = PendingPropose, Topic = PendingPropose, Text = "Je lance l'essai au prochain tour ?", Lap = me.Laps });
            return need + "Pour savoir ce que tu y perds, fais-moi un tour en lift and coast. Je lance l'essai au prochain tour ?";
        }

        private static string Arm(LiftCoastTest test, DriverRow me, IReadOnlyList<LapDto> laps)
        {
            bool thisLap = me.LapFraction < 0.08 && !me.InPits;
            test.TestLap = me.Laps + (thisLap ? 1 : 2);
            test.State = "armé";
            test.Candidate = null;
            int? reference = test.ReferenceLap ?? LastCleanLap(laps, me.Laps + 1);
            string vs = reference.HasValue ? $" Je compare au tour {reference}." : " Je compare à ton dernier tour propre.";
            return $"Copy, lift and coast {(thisLap ? "sur ce tour" : $"au prochain tour complet, le tour {test.TestLap}")} : lève le pied 100 à 150 mètres avant chaque gros freinage, roue libre jusqu'à ton point de freinage habituel.{vs}";
        }

        private static string Retry(LiftCoastTest test, DriverRow me, string why)
        {
            test.State = "armé";
            test.Candidate = null;
            test.TestLap = me.Laps + (me.LapFraction < 0.08 ? 1 : 2);
            return $"{why} On refait : lift and coast au tour {test.TestLap}.";
        }

        private static string Validate(LiftCoastTest test, RaceMemory memory, RaceSnapshot snap, DriverRow me)
        {
            LiftCoastMeasure m = test.Candidate;
            m.Validated = true;
            test.Measure = m;
            test.Candidate = null;
            test.State = null;
            memory.Add("ingénieur", "stratégie", $"lift and coast mesuré au tour {m.TestLap} contre le tour {m.ReferenceLap} : {Short(m)}", me.Laps);
            RacePlanDto plan = RacePlanner.Build(snap, Context(memory, m));
            if (plan == null)
            {
                return $"Noté : {Short(m)}.";
            }

            if (plan.StopsNeeded == 0)
            {
                return $"Noté : {Short(m)}. Tu peux déjà finir sans arrêt, pas besoin de lift and coast.";
            }

            return Verdict(plan, m, memory, me, ask: true);
        }

        /// <summary>Gagne-t-on du temps en faisant un arrêt de moins en lift and coast ? (« On le fait ? » si oui.)</summary>
        private static string Verdict(RacePlanDto plan, LiftCoastMeasure m, RaceMemory memory, DriverRow me, bool ask)
        {
            OneLessStopDto o = plan.OneLess;
            string measured = $"Lift and coast : {Short(m)}.";
            if (o == null)
            {
                return measured;
            }

            string fewer = $"{Stops(o.Stops)} au lieu de {Stops(o.Stops + 1)}";
            if (!o.Realistic || o.LiftCoastCoversIt == false)
            {
                return $"{measured} Pour {fewer}, il faudrait {Saving(o)} de moins par tour : ton lift and coast n'y suffit pas. On garde {Stops(o.Stops + 1)}, attaque.";
            }

            if (o.NetGain is not double net || o.LiftCoastCost is not double cost)
            {
                return measured;
            }

            string sums = $"Sur {N(Math.Floor(o.Laps), 0)} tours, tu perds environ {N(cost, 0)} s ; un arrêt en coûte {N(o.PitTimeSaved ?? 0, 0)}.";
            if (net > 5)
            {
                if (ask)
                {
                    memory.Ask(new PendingQuestion { Key = PendingGo, Topic = PendingGo, Text = "On le fait ?", Lap = me.Laps });
                }

                return $"{measured} {Cap(fewer)} : {sums} On gagne {N(net, 0)} s.{(ask ? " On le fait ?" : string.Empty)}";
            }

            return net < -5
                ? $"{measured} {Cap(fewer)} : {sums} On y perdrait {N(-net, 0)} s. On garde {Stops(o.Stops + 1)}, attaque."
                : $"{measured} {Cap(fewer)} : {sums} Pratiquement pareil : on garde l'attaque, c'est moins risqué.";
        }

        private static string Commit(LiftCoastTest test, RaceMemory memory, RaceSnapshot snap, DriverRow me)
        {
            RacePlanDto plan = RacePlanner.Build(snap, Context(memory, test.Measure));
            OneLessStopDto o = plan?.OneLess;
            if (o == null)
            {
                return "Copy. Pas d'arrêt à gagner pour l'instant, je te redis si ça change.";
            }

            bool energy = o.Limiter == RacePlanner.Resource.EnergyName;
            test.TargetPerLap = energy ? o.EnergyPerLapTarget : o.FuelPerLapTarget;
            test.TargetUnit = energy ? "%" : "L";
            test.LastCheckedLap = me.Laps;
            memory.Add("pilote", "stratégie", $"lift and coast jusqu'à la fin pour {Stops(o.Stops)} : cible {N(test.TargetPerLap ?? 0, 1)} {test.TargetUnit} par tour", me.Laps);
            string unit = energy ? " % d'énergie" : " L";
            return $"Copy, lift and coast jusqu'à la fin, {Stops(o.Stops)}. Cible : {N(test.TargetPerLap ?? 0, 1)}{unit} par tour maximum, je te dis si tu dépasses.";
        }

        private static string Decline(LiftCoastTest test, RaceSnapshot snap)
        {
            test.TargetPerLap = null;
            int? stops = snap?.Player?.Plan?.StopsNeeded;
            return stops is int s && s > 0 ? $"Copy, on garde {Stops(s)}, attaque." : "Copy, on reste en attaque.";
        }

        /// <summary>« Tour de lift and coast : 3:33.1, 1,4 s de plus que le tour 22. Énergie 7,1 % au lieu de 7,9 %, essence 6,8 L au lieu de 7,3. »</summary>
        public static string Describe(LiftCoastMeasure m)
        {
            string time = m.TimeLoss is double loss
                ? Math.Abs(loss) < 0.05 ? $"même temps que le tour {m.ReferenceLap}"
                : loss > 0 ? $"{Sec(loss)} de plus que le tour {m.ReferenceLap}" : $"{Sec(-loss)} de moins que le tour {m.ReferenceLap}"
                : $"comparé au tour {m.ReferenceLap}";
            var parts = new List<string>();
            if (m.TestEnergy is double te && m.ReferenceEnergy is double re)
            {
                parts.Add($"énergie {N(te, 1)} % au lieu de {N(re, 1)} %");
            }

            if (m.TestFuel is double tf && m.ReferenceFuel is double rf)
            {
                parts.Add($"essence {N(tf, 2)} L au lieu de {N(rf, 2)}");
            }

            return $"Tour de lift and coast : {Lap(m.TestTime, 1)}, {time}." + (parts.Count > 0 ? " " + Cap(string.Join(", ", parts)) + "." : string.Empty);
        }

        /// <summary>« 1,4 s par tour pour 0,8 % d'énergie et 0,50 L économisés ».</summary>
        public static string Short(LiftCoastMeasure m)
        {
            var saved = new List<string>();
            if (m.EnergySaved is double e)
            {
                saved.Add($"{N(e, 1)} % d'énergie");
            }

            if (m.FuelSaved is double f)
            {
                saved.Add($"{N(f, 2)} L");
            }

            string cost = m.TimeLoss is double loss ? (loss >= 0.05 ? $"{Sec(loss)} perdue par tour" : "aucun temps perdu") : "temps inconnu";
            return saved.Count == 0 ? cost : $"{cost} pour {string.Join(" et ", saved)} économisés";
        }

        public static string Saving(OneLessStopDto o) => o.Limiter == RacePlanner.Resource.EnergyName && o.EnergySavingPerLap is double e
            ? $"{N(e, 1)} % d'énergie"
            : o.FuelSavingPerLap is double f ? $"{N(f, 2)} L" : $"{N(o.SavingPct, 0)} %";

        public static PlanContext Context(RaceMemory memory, LiftCoastMeasure measure = null) =>
            new() { LiftCoast = measure ?? memory?.LiftCoast?.Measure, PlannedStopLap = memory?.PlannedStopLap };

        public static string Stops(int n) => n switch
        {
            0 => "aucun arrêt",
            1 => "un arrêt",
            _ => $"{n} arrêts",
        };

        /// <summary>Dernier tour propre avant <paramref name="beforeLap"/> : ni stand (entrée ou sortie), valide, avec une mesure.</summary>
        public static int? LastCleanLap(IReadOnlyList<LapDto> laps, int beforeLap)
        {
            if (laps == null)
            {
                return null;
            }

            // Tours d'entrée et de sortie des stands sont déjà marqués « stand » : le tour qui suit la sortie est un tour normal.
            return laps.Where(l => l.Lap < beforeLap && !l.Pit && l.Valid && l.Time > 0 && (l.Energy.HasValue || l.Fuel.HasValue))
                .Select(l => (int?)l.Lap)
                .LastOrDefault();
        }

        private static string Load(LapDto lap)
        {
            var parts = new List<string>();
            if (lap.Energy is double e)
            {
                parts.Add($"{N(e, 1)} % d'énergie");
            }

            if (lap.Fuel is double f)
            {
                parts.Add($"{N(f, 2)} L");
            }

            return string.Join(", ", parts);
        }

        private static int? LapNumber(string t)
        {
            Match m = Regex.Match(t, @"\btour (\d{1,3})\b|\ble (\d{1,3}) (est|c est)\b");
            if (!m.Success)
            {
                return null;
            }

            return int.TryParse(m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value, out int n) ? n : null;
        }
    }
}
