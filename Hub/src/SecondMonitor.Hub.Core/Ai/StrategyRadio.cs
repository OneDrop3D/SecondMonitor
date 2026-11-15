namespace SecondMonitor.Hub.Core.Ai
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using SecondMonitor.Hub.Core.Race;
    using SecondMonitor.Hub.Core.Strategy;
    using static SecondMonitor.Hub.Core.Ai.RadioFormat;

    /// <summary>
    /// Questions de stratégie de course, répondues par le calcul (<see cref="RacePlanner"/>), courtes, chiffres d'abord :
    /// « combien d'arrêts ? », « deux ou trois box ? », « je suis obligé de faire un quatrième arrêt ? », « au tour 21 »,
    /// « un tour plus tard ? », « le ratio carburant / énergie », « qu'est-ce qu'on peut jouer ? », « combien de tours avant la fin ? »,
    /// « on peut finir avec ce plein ? », « c'est quoi l'énergie virtuelle ? ».
    /// </summary>
    public static class StrategyRadio
    {
        private static readonly Regex StopWords = new(@"\b(arrets?|box|boxs|stops?|pit stops?|passages? au stand|ravitaillements?)\b", RegexOptions.Compiled);

        /// <summary>Réponse calculée, ou null si la phrase n'est pas une question de stratégie reconnue ici.</summary>
        public static string Answer(string question, RaceSnapshot snap, RaceMemory memory, IReadOnlyList<LapDto> laps = null)
        {
            if (snap?.HasData != true)
            {
                return null;
            }

            string t = Numbers(RadioCommands.Normalize(question));
            if (t.Length == 0)
            {
                return null;
            }

            DriverRow me = snap.Drivers.FirstOrDefault(d => d.IsPlayer);
            if (me == null)
            {
                return null;
            }

            int words = t.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

            if (Glossary(t, snap) is string definition)
            {
                return definition;
            }

            bool race = snap.SessionType == "Race";
            if (!race)
            {
                return null;
            }

            RacePlanDto plan = RacePlanner.Build(snap, LiftCoast.Context(memory));

            if (Regex.IsMatch(t, @"\bratio\b"))
            {
                return Ratio(t, snap, plan);
            }

            if (Shift(t, words, question, snap, memory, plan, me) is string shift)
            {
                return shift;
            }

            if (StopsQuestion(t) is { Match: true } stopsQ)
            {
                return Stops(snap, plan, me, stopsQ.Said, stopsQ.Ordinal);
            }

            if (Regex.IsMatch(t, @"\b(qu est ce qu on (peut )?(jouer|faire)|on (peut )?joue(r)? quoi|que peut on jouer|quelles? (sont les )?options?|les options|quel(le)? (plan|strategie)|point (sur la |de |au niveau )?strat\w*|point strategique|niveau strategie|strategie (on|qu)|on fait quoi (niveau|en) strat\w*)\b"))
            {
                return Options(snap, plan, me);
            }


            if (Regex.IsMatch(t, @"\b(finir|terminer|aller (jusqu )?au bout|tenir jusqu (a la fin|au bout))\b") && Regex.IsMatch(t, @"\b(avec ce plein|sans (s )?arret\w*|sans (re)?passer|sans box\w*|sans rentrer|avec ce qu (il|on) (me |nous )?reste|ce plein|l essence|le carburant|l energie)\b"))
            {
                return Finish(plan, snap);
            }

            if (Regex.IsMatch(t, @"\b(combien de tours? (il )?(me |nous )?reste|tours? restants?|il (me |nous )?reste combien de tours?|reste combien de tours?|avant la fin|fin de la course|c est (bien )?le dernier tour|est ce (que c est )?le dernier tour|on est (dans|au) (le )?dernier tour)\b") && !Regex.IsMatch(t, @"\b(essence|carburant|energie|batterie|pneus?)\b"))
            {
                return LapsLeft(snap, me);
            }

            return null;
        }

        /// <summary>« Il faudra faire encore deux arrêts ? », « un box ou deux ? », « quatrième arrêt », « combien d'arrêts ? ».</summary>
        private static (bool Match, int? Said, bool Ordinal)? StopsQuestion(string t)
        {
            if (!StopWords.IsMatch(t))
            {
                return null;
            }

            Match ordinal = Regex.Match(t, @"\b(premier|deuxieme|second|troisieme|quatrieme|cinquieme|sixieme|\d+e|\d+eme) (arret|box|stop)\b");
            if (ordinal.Success)
            {
                return (true, Ordinal(ordinal.Groups[1].Value), true);
            }

            if (Regex.IsMatch(t, @"\bcombien (d |de )(arrets?|box|stops?|passages?)\b|\bnombre d arrets\b|\b(un|1) arret de (plus|moins)\b|\bobliges? de (faire|refaire)\b|\bencore (\d+ )?(arrets?|box)\b|\bfaire (\d+ )?(arrets?|box)\b|\b(\d+) (ou|a) (\d+) (arrets?|box)\b|\b(\d+) (arrets?|box|boxs|stops)\b"))
            {
                // « Un box ou deux ? » : un choix, pas une affirmation à confirmer.
                bool choice = Regex.IsMatch(t, @"\b(un|une|\d+) (arrets?|box|boxs|stops?)? ?ou (\d+|un|une)\b");
                Match said = choice ? Match.Empty : Regex.Match(t, @"\b(\d+) (arrets?|box|boxs|stops?)\b");
                return (true, said.Success ? int.Parse(said.Groups[1].Value, CultureInfo.InvariantCulture) : null, false);
            }

            return null;
        }

        private static string Stops(RaceSnapshot snap, RacePlanDto plan, DriverRow me, int? said, bool ordinal)
        {
            if (plan == null)
            {
                return "Il me faut un tour complet de consommation pour compter les arrêts.";
            }

            int done = me.PitStops;
            if (plan.StopsNeeded == 0)
            {
                string spare = plan.SpareLaps is double sp ? $", {N(Math.Max(0, sp), 1)} tour{(sp >= 2 ? "s" : string.Empty)} de marge" : string.Empty;
                string yes0 = said is int s0 && (ordinal ? s0 > done : s0 > 0) ? "Non. " : string.Empty;
                return $"{yes0}Plus aucun arrêt : tu as de quoi finir{spare}.";
            }

            if (plan.StopsNeeded is not int stops)
            {
                return "Il me faut un tour complet de consommation pour compter les arrêts.";
            }

            // « Encore trois box ? » : oui / non d'abord.
            string verdict = string.Empty;
            if (said is int n)
            {
                bool right = ordinal ? n <= done + stops : n == stops;
                verdict = right ? "Oui. " : "Non. ";
            }

            string total = done > 0 ? $" ({LiftCoast.Stops(done + stops)} en tout)" : string.Empty;
            string next = Window(plan);
            string text = $"{verdict}Encore {LiftCoast.Stops(stops)}{total}{(ordinal ? $" : le {Ord(done + stops)} sera le dernier" : string.Empty)}. {next}";
            if (plan.OneLess is OneLessStopDto o)
            {
                text += " " + OneLessLine(o, plan.LiftCoast != null);
            }

            return text.Trim();
        }

        /// <summary>« Prochain arrêt au tour 20, fenêtre du tour 18 au 21. »</summary>
        private static string Window(RacePlanDto plan)
        {
            if (plan.NextStopLap is not int nextLap)
            {
                return string.Empty;
            }

            string window = plan.WindowEarliest is int a && plan.WindowLatest is int b && a < b ? $", fenêtre du tour {a} au {b}" : plan.WindowLatest is int l ? ", au plus tard" : string.Empty;
            return $"Prochain au tour {nextLap}{window}.";
        }

        private static string OneLessLine(OneLessStopDto o, bool measured)
        {
            string fewer = $"Pour {LiftCoast.Stops(o.Stops)}";
            if (!o.Realistic)
            {
                return $"{fewer}, il faudrait {LiftCoast.Saving(o)} de moins par tour : pas jouable.";
            }

            if (o.NetGain is double net)
            {
                return net > 5 ? $"{fewer} en lift and coast : on gagnerait {N(net, 0)} s."
                    : net < -5 ? $"{fewer} en lift and coast : on perdrait {N(-net, 0)} s."
                    : $"{fewer} en lift and coast : pareil, pas la peine.";
            }

            if (measured && o.LiftCoastCoversIt == false)
            {
                return $"{fewer}, il faudrait {LiftCoast.Saving(o)} de moins par tour : ton lift and coast n'y suffit pas.";
            }

            return $"{fewer} : {LiftCoast.Saving(o)} de moins par tour, à mesurer en lift and coast.";
        }

        /// <summary>« Au tour 21 », « je boxe au tour d'après », « un tour plus tôt ? », « et si je rentre au 19 ? ».</summary>
        private static string Shift(string t, int words, string question, RaceSnapshot snap, RaceMemory memory, RacePlanDto plan, DriverRow me)
        {
            bool pitContext = Regex.IsMatch(t, @"\b(box\w*|stand|stands|arret|rentre|rentrer|rentrerai|pit)\b");
            bool shortReply = words <= 7 && Regex.IsMatch(t, @"^(non |plutot |alors |ok |et |bon |ben |euh |non plutot )*(au|le|a|pour le) tour\b|^(un|1) tour (plus tot|plus tard|de plus|de moins|avant|apres)\b");
            if (!pitContext && !shortReply)
            {
                return null;
            }

            int? baseLap = memory?.PlannedStopLap is int pl && pl > me.Laps ? pl : plan?.NextStopLap;
            int? target = null;
            Match lapNumber = Regex.Match(t, @"\b(au|le|a la fin du|en fin de|pour le|a) tour (\d{1,3})\b|\bau (\d{1,3})(e|eme)?\b");
            if (lapNumber.Success)
            {
                target = int.Parse(lapNumber.Groups[2].Success ? lapNumber.Groups[2].Value : lapNumber.Groups[3].Value, CultureInfo.InvariantCulture);
            }
            else if (Regex.IsMatch(t, @"\b(au tour (suivant|d apres)|(un|1) tour (plus tard|de plus|apres)|le tour (suivant|d apres)|au prochain tour)\b"))
            {
                // « Je boxe au tour d'après » quand l'ingénieur vient de dire « box ce tour » : un tour après l'arrêt prévu.
                target = (baseLap ?? me.Laps + 1) + 1;
            }
            else if (Regex.IsMatch(t, @"\b(au tour (precedent|d avant)|(un|1) tour (plus tot|de moins|avant)|le tour (precedent|d avant))\b"))
            {
                target = (baseLap ?? me.Laps + 2) - 1;
            }
            else if (Regex.IsMatch(t, @"\b(ce tour( ci)?|maintenant|tout de suite)\b") && Regex.IsMatch(t, @"\b(ca passe|possible|on peut|je peux)\b") && !Regex.IsMatch(t, @"\b(ressor\w*|resor\w*|position|place|sortir|sors)\b"))
            {
                target = me.Laps + 1;
            }

            // « Le tour 5 était bon » : un tour passé, sans parler d'arrêt, n'est pas une demande d'arrêt.
            if (target is not int lap || (!pitContext && lap <= me.Laps))
            {
                return null;
            }

            if (plan == null || plan.StopsNeeded is null)
            {
                return "Il me faut un tour complet de consommation pour calculer cet arrêt.";
            }

            if (plan.StopsNeeded == 0 && pitContext)
            {
                return $"Pas besoin de t'arrêter : tu as de quoi finir. Si tu rentres au tour {lap}, ce sera seulement pour les pneus.";
            }

            // Question (« et si je boxe un tour plus tôt ? ») : on répond sans rien décider. Sinon (« au tour 21 ») : c'est décidé.
            bool question0 = (question ?? string.Empty).Contains('?') || Regex.IsMatch(t, @"^(et )?si\b|\b(si je|et si|ca passe|possible|on peut|je peux|est ce que|est ce qu)\b");
            var resources = RacePlanner.Resources(snap.Player);
            bool lmu = snap.Player?.Energy != null;
            StopShiftDto s = RacePlanner.Shift(plan, resources, me.Laps, lap - me.Laps, Math.Clamp(me.LapFraction, 0, 0.999), plan.LapsRemaining, PlayerStrategyTracker.FuelMarginLaps, plan.FullStint, lmu);
            if (!s.Feasible)
            {
                string fix = s.SavingNeededPerLap is double sv && sv > 0
                    ? $" Il faudrait économiser {N(sv, s.SavingUnit == "L" ? 2 : 1)} {(s.SavingUnit == "L" ? "L" : "% d'énergie")} par tour d'ici là."
                    : string.Empty;
                string latest = plan.WindowLatest is int wl ? $" Au plus tard : tour {wl}." : string.Empty;
                return s.Reason == "tour déjà passé" ? $"Le tour {lap} est déjà commencé.{latest}"
                    : s.Reason == "après l'arrivée" ? $"Le tour {lap}, c'est après l'arrivée."
                    : $"Tour {lap}, trop tard : {s.Reason} ce tour.{fix}{latest}";
            }

            string load = Load(s);
            string stops = s.ExtraStop ? $" mais ça fait un arrêt de plus ({s.Stops} au lieu de {plan.StopsNeeded})" : $", toujours {LiftCoast.Stops(plan.StopsNeeded.Value)}";
            string when = lap == me.Laps + 1 ? "box ce tour" : $"box au tour {lap}";
            if (question0)
            {
                return $"Oui, tour {lap} ça passe{stops}.{load}";
            }

            if (memory != null)
            {
                memory.PlannedStopLap = lap;
                memory.NoStopPlanned = false;
                memory.Add("pilote", "stand", $"arrêt décidé au tour {lap}", me.Laps);
            }

            return $"Copy, {when}{stops}.{load}";
        }

        private static string Load(StopShiftDto s)
        {
            var parts = new List<string>();
            if (s.EnergyPct is double e && e > 0.5)
            {
                parts.Add($"{N(Math.Ceiling(e), 0)} % d'énergie");
            }

            if (s.FuelLitres is double f && f > 0.5)
            {
                parts.Add($"{N(Math.Ceiling(f), 0)} L");
            }

            if (parts.Count == 0)
            {
                return string.Empty;
            }

            string time = s.StopSeconds is double st ? $", arrêt d'environ {N(st, 0)} s" : string.Empty;
            return $" On mettra {string.Join(" et ", parts)}{time}.";
        }

        /// <summary>Ratio carburant / énergie du menu des stands de LMU (litres par % d'énergie virtuelle).</summary>
        private static string Ratio(string t, RaceSnapshot snap, RacePlanDto plan)
        {
            PlayerStrategy p = snap.Player;
            double? ideal = plan?.IdealRatio ?? RacePlanner.Ratio(p);
            if (p?.Energy == null)
            {
                return "Pas d'énergie virtuelle sur cette voiture : pas de ratio à régler, seulement l'essence.";
            }

            if (ideal is not double r)
            {
                return "Il me faut un tour mesuré en essence et en énergie pour calculer le ratio.";
            }

            double advised = Math.Ceiling((r + 0.01) * 100) / 100; // une petite marge d'essence : la panne sèche coûte plus cher que 1 L
            string text = $"Ratio idéal {N(r, 2)} : {N(p.Fuel.PerLap ?? 0, 2)} L pour {N(p.Energy.PerLapPct ?? 0, 1)} % d'énergie par tour. Mets {N(advised, 2)}.";
            if (SaidRatio(t) is double mine)
            {
                double diff = mine - r;
                if (Math.Abs(diff) < 0.012)
                {
                    text = $"À {N(mine, 2)}, tu es pile dessus : ratio idéal {N(r, 2)}, garde-le.";
                }
                else if (diff > 0)
                {
                    double extra = diff / mine * 100;
                    double litres = diff * 100; // au plein d'énergie (100 %)
                    text = $"À {N(mine, 2)}, tu emportes {N(extra, 0)} % d'essence en trop, environ {N(litres, 0)} L sur un plein : descends à {N(advised, 2)}. Idéal {N(r, 2)} ({N(p.Fuel.PerLap ?? 0, 2)} L pour {N(p.Energy.PerLapPct ?? 0, 1)} % par tour).";
                }
                else
                {
                    text = $"À {N(mine, 2)}, l'essence s'épuisera avant l'énergie : monte à {N(advised, 2)}. Idéal {N(r, 2)} ({N(p.Fuel.PerLap ?? 0, 2)} L pour {N(p.Energy.PerLapPct ?? 0, 1)} % par tour).";
                }
            }

            // Déséquilibre à bord en ce moment (« les données ne sont pas bonnes ») : ce qu'il y a dans la voiture.
            if (p.Fuel?.LapsLeft is double fl && p.Energy.LapsLeft is double el && Math.Abs(fl - el) >= 0.4)
            {
                text += fl > el
                    ? $" À bord : essence pour {N(fl, 1)} tours, énergie pour {N(el, 1)} : trop d'essence ce relais."
                    : $" À bord : essence pour {N(fl, 1)} tours, énergie pour {N(el, 1)} : l'essence est juste, surveille.";
            }

            return text;
        }

        /// <summary>« Je suis à 0,97 », « zéro virgule quatre-vingt-dix-sept », « 97 » dans une phrase sur le ratio.</summary>
        private static double? SaidRatio(string t)
        {
            Match m = Regex.Match(t, @"\b0 (?:virgule )?(\d{1,2})\b|\b1 (?:virgule )?(\d{1,2})\b|\b(?:a|de|est a|suis a|mis) (\d{2})\b");
            if (!m.Success)
            {
                return null;
            }

            string g = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Value;
            double frac = double.Parse(g, CultureInfo.InvariantCulture) / (g.Length == 1 ? 10 : 100);
            double v = m.Groups[2].Success ? 1 + frac : frac;
            return v is > 0.3 and < 1.6 ? v : null;
        }

        /// <summary>« Qu'est-ce qu'on peut jouer ? » : le plan, un arrêt de moins, l'undercut, les pneus. Trois phrases au plus.</summary>
        private static string Options(RaceSnapshot snap, RacePlanDto plan, DriverRow me)
        {
            if (plan == null || plan.StopsNeeded is null)
            {
                return "Il me faut un tour complet de consommation pour te donner les options.";
            }

            var lines = new List<string>();
            if (plan.StopsNeeded == 0)
            {
                lines.Add("Aucun arrêt nécessaire, tu as de quoi finir : on attaque.");
            }
            else
            {
                lines.Add($"Plan : {LiftCoast.Stops(plan.StopsNeeded.Value)}. {Window(plan)}");
                if (plan.OneLess is OneLessStopDto o)
                {
                    lines.Add(OneLessLine(o, plan.LiftCoast != null));
                }
            }

            // Pneus : en LMU un train de pneus coûte 12 s d'arrêt ; s'ils tiennent jusqu'au relais suivant, on les garde.
            if (plan.StopsNeeded > 0 && plan.TyreLapsLeft is double tyres && plan.FullStint > 1)
            {
                double stintAfter = Math.Min(plan.FullStint, Math.Max(0, plan.LapsRemaining - ((plan.NextStopLap ?? me.Laps) - me.Laps)));
                double atStop = tyres - ((plan.NextStopLap ?? me.Laps) - me.Laps);
                if (atStop >= stintAfter + 1 && snap.Player?.Energy != null)
                {
                    lines.Add($"Pneus : ils tiennent encore le relais suivant, ne les change pas au prochain arrêt (12 s de gagnées).");
                }
            }

            CutDto cut = snap.Player?.PitPlan?.Cuts?.FirstOrDefault(c => c.Feasible && c.Kind is "undercut" or "overcut");
            if (cut != null)
            {
                lines.Add(cut.Text);
            }
            else if (snap.Player?.PitPlan?.Cuts?.FirstOrDefault(c => c.Kind == "menace-undercut" && c.Feasible) is CutDto threat)
            {
                lines.Add(threat.Text);
            }

            return string.Join(" ", lines.Take(3).Select(l => l.Trim()));
        }

        private static string Finish(RacePlanDto plan, RaceSnapshot snap)
        {
            if (plan == null)
            {
                return "Il me faut un tour complet de consommation pour te répondre.";
            }

            if (plan.CanFinish)
            {
                return $"Oui : de quoi faire {N(plan.RangeNow, 1)} tours pour {N(plan.LapsRemaining, 1)} à faire.";
            }

            string limiter = plan.Limiter == RacePlanner.Resource.EnergyName ? "d'énergie virtuelle" : "d'essence";
            double missing = Math.Max(0.1, plan.LapsRemaining - plan.RangeNow);
            string fewer = plan.OneLess is { Stops: 0, Realistic: true } o ? $" Sans arrêt, il faudrait {LiftCoast.Saving(o)} de moins par tour." : string.Empty;
            return $"Non : il manque {N(missing, 1)} tour{(missing >= 2 ? "s" : string.Empty)} {limiter}, encore {LiftCoast.Stops(plan.StopsNeeded ?? 1)}.{fewer}";
        }

        private static string LapsLeft(RaceSnapshot snap, DriverRow me)
        {
            RaceLengthDto len = snap.RaceLength;
            double? left = len?.PlayerLapsRemaining ?? snap.Player?.LapsRemaining;
            if (left is not double l)
            {
                return snap.TimeRemaining is double tr ? $"Encore {Duration(tr)} de course ; il me faut un tour complet pour compter les tours." : null;
            }

            if (l <= 1.0)
            {
                return "Dernier tour, tout donner jusqu'au drapeau.";
            }

            int laps = (int)Math.Ceiling(l - 0.02);
            string after = laps - 1 <= 1 ? string.Empty : $" : celui-ci et {laps - 1} autres";
            string time = len?.PlayerFinishIn is double fin ? $", arrivée dans environ {Duration(fin)}" : snap.TimeRemaining is double t2 ? $", {Duration(t2)} au chrono" : string.Empty;
            string estimate = len?.Estimated == true ? " (estimé sur ton rythme)" : string.Empty;
            return $"Encore {laps} tours{after}{time}{estimate}.";
        }

        /// <summary>« C'est quoi l'énergie virtuelle ? » : définition en une phrase et la valeur actuelle.</summary>
        private static string Glossary(string t, RaceSnapshot snap)
        {
            if (!Regex.IsMatch(t, @"\b(c est quoi|qu est ce que c est|qu est ce que|ca veut dire|ca sert a quoi|explique|definition|c est comment)\b"))
            {
                return null;
            }

            PlayerStrategy p = snap.Player;
            if (Regex.IsMatch(t, @"\benergie virtuelle\b|\bvirtual energy\b"))
            {
                string now = p?.Energy is { } e ? $" Il t'en reste {N(e.RemainingPct, 0)} %{(e.LapsLeft is double l ? $", {N(l, 1)} tours" : string.Empty)}." : string.Empty;
                return "L'énergie virtuelle, c'est le quota d'énergie par relais imposé par le règlement, essence et hybride compris : à zéro, il faut rentrer." + now;
            }

            if (Regex.IsMatch(t, @"\bratio\b"))
            {
                string now = RacePlanner.Ratio(p) is double r ? $" Le tien, idéal : {N(r, 2)}." : string.Empty;
                return "Le ratio, c'est les litres d'essence mis pour chaque % d'énergie virtuelle au stand ; bien réglé, l'essence et l'énergie s'épuisent ensemble." + now;
            }

            if (LiftCoast.Words.IsMatch(t))
            {
                return "Lift and coast : lever le pied 100 à 150 mètres avant le point de freinage et rouler en roue libre. Moins d'énergie et d'essence par tour, un peu de temps perdu.";
            }

            if (Regex.IsMatch(t, @"\bundercut\b"))
            {
                return "Undercut : s'arrêter avant l'adversaire et lui reprendre du temps avec les pneus neufs pendant qu'il fait son tour sur des pneus usés.";
            }

            if (Regex.IsMatch(t, @"\bovercut\b"))
            {
                return "Overcut : rester dehors quand l'adversaire s'arrête, et profiter de son tour de sortie sur pneus froids pour passer devant.";
            }

            return null;
        }

        private static int? Ordinal(string w) => w switch
        {
            "premier" => 1,
            "deuxieme" or "second" => 2,
            "troisieme" => 3,
            "quatrieme" => 4,
            "cinquieme" => 5,
            "sixieme" => 6,
            _ => int.TryParse(Regex.Match(w, @"\d+").Value, out int n) ? n : null,
        };

        private static string Ord(int n) => n switch
        {
            1 => "premier",
            2 => "deuxième",
            3 => "troisième",
            4 => "quatrième",
            5 => "cinquième",
            6 => "sixième",
            _ => $"{n}e",
        };

        private static readonly Dictionary<string, int> Units = new()
        {
            ["zero"] = 0, ["un"] = 1, ["une"] = 1, ["deux"] = 2, ["trois"] = 3, ["quatre"] = 4, ["cinq"] = 5, ["six"] = 6, ["sept"] = 7, ["huit"] = 8, ["neuf"] = 9,
            ["dix"] = 10, ["onze"] = 11, ["douze"] = 12, ["treize"] = 13, ["quatorze"] = 14, ["quinze"] = 15, ["seize"] = 16,
        };

        private static readonly Dictionary<string, int> Tens = new()
        {
            ["vingt"] = 20, ["trente"] = 30, ["quarante"] = 40, ["cinquante"] = 50, ["soixante"] = 60,
        };

        /// <summary>
        /// Nombres dictés en lettres remis en chiffres (« au tour vingt et un » → « au tour 21 », « zéro virgule quatre-vingt-dix-sept »
        /// → « 0 virgule 97 ») et « autour » → « au tour » devant un nombre (erreur de dictée courante).
        /// Un « un » seul reste un article (« un arrêt », « un tour »), sauf après « tour ».
        /// </summary>
        public static string Numbers(string normalized)
        {
            string[] tok = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var outTok = new List<string>();
            for (int i = 0; i < tok.Length;)
            {
                int start = i;
                int value = 0;
                bool any = false;
                if (tok[i] == "cent")
                {
                    value = 100;
                    i++;
                    any = true;
                }

                if (i + 1 < tok.Length && tok[i] == "quatre" && tok[i + 1] is "vingt" or "vingts")
                {
                    value += 80;
                    i += 2;
                    any = true;
                }
                else if (i < tok.Length && Tens.TryGetValue(tok[i], out int tens))
                {
                    value += tens;
                    i++;
                    any = true;
                }

                if (any && i + 1 < tok.Length && tok[i] == "et" && tok[i + 1] is "un" or "une" or "onze")
                {
                    i++;
                }

                if (i + 1 < tok.Length && tok[i] == "dix" && tok[i + 1] is "sept" or "huit" or "neuf")
                {
                    value += 10 + Units[tok[i + 1]];
                    i += 2;
                    any = true;
                }
                else if (i < tok.Length && Units.TryGetValue(tok[i], out int unit))
                {
                    bool article = !any && tok[i] is "un" or "une" && !(start > 0 && tok[start - 1] is "tour" or "virgule" or "a" or "au");
                    if (!article)
                    {
                        value += unit;
                        i++;
                        any = true;
                    }
                }

                if (any)
                {
                    outTok.Add(value.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    outTok.Add(tok[i]);
                    i++;
                }
            }

            string s = string.Join(' ', outTok);
            return Regex.Replace(s, @"\bautour (?=\d|suivant|d apres|precedent|d avant)", "au tour ");
        }
    }
}
