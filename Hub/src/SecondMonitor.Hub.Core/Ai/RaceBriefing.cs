namespace SecondMonitor.Hub.Core.Ai
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using SecondMonitor.Hub.Core.Race;
    using SecondMonitor.Hub.Core.Telemetry;

    /// <summary>
    /// Transforme l'état de course en « fiche de faits » en français, chiffres déjà calculés, et propose une réponse.
    /// Les petits modèles (2B) calculent mal et appellent mal les outils : le hub prépare donc la réponse,
    /// le modèle ne fait que la reprendre (questions reconnues) ou s'appuyer sur la fiche (questions libres).
    /// </summary>
    public static class RaceBriefing
    {
        private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

        public enum Topic
        {
            Pit,
            Fuel,
            Energy,
            Tyres,
            Opponents,
            Gap,
            Position,
            Session,
            Weather,
            Car,
            Speed,
            Performance,
            Rain,
            Pace,
        }

        private static readonly (Topic Topic, string[] Words)[] Keywords =
        {
            (Topic.Pace, new[] { "rythme", "allure", "chrono", "temps au tour", "tourner en", "dois tourner", "pour gagner", "gagner la course", "aller plus vite", "plus vite", "quel temps je", "en combien je" }),
            (Topic.Pit, new[] { "box", "stand", "rentre", "rentrer", "arrêt", "arret", "pit", "ravitaill" }),
            (Topic.Speed, new[] { "vitesse", "pointe", "vmax", "v max", "vmin", "v min", "km/h", "kilomètre", "top speed", "ligne droite", "lignes droites" }),
            (Topic.Performance, new[] { "perd", "gagner du temps", "amélior", "ameliorer", "plus lent", "secteur", "temps perdu", "où je", "ou je" }),
            (Topic.Fuel, new[] { "essence", "carburant", "fuel", "litre", "conso", "réservoir", "plein" }),
            (Topic.Energy, new[] { "énergie", "energie", "virtuel", "batterie", "hybride" }),
            (Topic.Car, new[] { "moteur", "huile", "l'eau", " eau", "frein", "dégât", "degat", "cassé", "radiateur" }),
            (Topic.Tyres, new[] { "pneu", "gomme", "usure", "température", "pression", "dégrad", "carcasse" }),
            (Topic.Opponents, new[] { "stratégie", "strategie", "adversaire", "concurrent", "va s'arrêter", "va s'arreter", "ses pneus" }),
            (Topic.Gap, new[] { "écart", "ecart", "derrière", "derriere", "devant", "gap", "revient", "reprend", "attaque", "loin", "rattrape" }),
            (Topic.Position, new[] { "position", "place", "classement", "rang", "leader", "premier", "combientième" }),
            (Topic.Session, new[] { "temps restant", "tours restants", "tour rest", "il reste combien de tours", "combien de tour", "tours bouclés", "tour bouclé", "fin de course", "minutes", "drapeau", "safety", "arrivée" }),
            (Topic.Weather, new[] { "pluie", "météo", "meteo", "température de piste", "pleuvoir", "pleut", "temps qu'il", "averse", "vent", "prévision", "prevision", "évoluer", "evoluer", "évolution", "evolution", "tendance", "la piste va" }),
        };

        /// <summary>Sujets reconnus dans la question, dans l'ordre où ils apparaissent.</summary>
        public static IReadOnlyList<Topic> DetectTopics(string question)
        {
            string q = (question ?? string.Empty).ToLowerInvariant();
            var topics = Keywords
                .Select(k => (k.Topic, Index: k.Words.Select(w => q.IndexOf(w, StringComparison.Ordinal)).Where(i => i >= 0).DefaultIfEmpty(-1).Min()))
                .Where(x => x.Index >= 0)
                .OrderBy(x => x.Topic == Topic.Car ? -1 : x.Index) // « température des freins » : on parle de la voiture, pas des pneus
                .Select(x => x.Topic)
                .Distinct()
                .ToList();

            // « Combien de tours d'essence ? » parle de carburant, pas de la fin de course.
            if (topics.Contains(Topic.Session) && (topics.Contains(Topic.Fuel) || topics.Contains(Topic.Energy)))
            {
                topics.Remove(Topic.Session);
                topics.Add(Topic.Session);
            }

            // « Quel pneu avec la pluie ? », « on passe en wet ? » : décision pneus pluie.
            bool rainy = Regex.IsMatch(q, @"pluie|pleu|\bwets?\b|\binters?\b|interm|mouill|averse|humide");
            bool tyreChoice = Regex.IsMatch(q, @"pneu|gomme|\bwets?\b|\binters?\b|interm|passe|chang|chauss|mettre|slick");
            if (rainy && tyreChoice)
            {
                topics.Remove(Topic.Rain);
                topics.Insert(0, Topic.Rain);
            }

            return topics;
        }

        public static string Build(RaceSnapshot snap, string question = null, BriefingExtras extras = null) => Prepare(snap, question, extras).Facts;

        public static BriefingResult Prepare(RaceSnapshot snap, string question, BriefingExtras extras = null)
        {
            if (snap == null || !snap.HasData)
            {
                return new BriefingResult { Facts = "Aucune donnée de course pour le moment (le jeu n'envoie rien).", Suggestion = "Je n'ai pas encore de données de course." };
            }

            var sections = Collect(snap, extras ?? new BriefingExtras());
            IReadOnlyList<Topic> topics = DetectTopics(question);
            string q = (question ?? string.Empty).ToLowerInvariant();

            string suggestion = null;
            Topic? main = null;
            foreach (Topic topic in topics)
            {
                if (sections.ContainsKey(topic))
                {
                    main = topic;
                    break;
                }
            }

            if (main.HasValue)
            {
                var lines = sections[main.Value];
                // « devant » / « derrière » : on privilégie la ligne qui en parle.
                bool behind = q.Contains("derrière") || q.Contains("derriere");
                bool ahead = q.Contains("devant");
                suggestion = Answer(lines, q, behind, ahead);

                // Plusieurs sujets dans la même question (« ma position, la météo et les prévisions ? ») : on répond à chacun.
                foreach (Topic other in topics.Where(t => t != main.Value && sections.ContainsKey(t)).Take(2))
                {
                    suggestion += " " + Answer(sections[other], q, behind, ahead);
                }
            }

            var sb = new StringBuilder();
            var order = topics.Concat(Enum.GetValues<Topic>().Except(topics));
            sb.AppendLine("FICHE DE FAITS (chiffres exacts calculés par l'ordinateur) :");
            foreach (Topic topic in order.Where(sections.ContainsKey))
            {
                foreach (Fact f in sections[topic])
                {
                    sb.AppendLine("- " + f.Text);
                }
            }

            string memory = extras?.Memory?.Facts();
            if (memory != null)
            {
                sb.AppendLine().AppendLine(memory);
            }

            return new BriefingResult { Facts = sb.ToString().TrimEnd(), Suggestion = suggestion, Topics = topics.ToList() };
        }

        private static readonly Regex ForecastWords = new(@"prévision|prevision|évolu|evolu|tendance|va (pleuvoir|changer|monter|baisser|sécher|secher)|vont|reste de la course|plus tard|dans \d+ min", RegexOptions.Compiled);

        /// <summary>Réponse à un sujet : la ligne la plus pertinente (devant / derrière / prévision) et ses compléments.</summary>
        private static string Answer(List<Fact> lines, string q, bool behind, bool ahead)
        {
            Fact forecast = ForecastWords.IsMatch(q) ? lines.FirstOrDefault(l => l.Tag == "prévision") : null;
            if (forecast != null)
            {
                // « La météo va évoluer ? » : la situation actuelle, puis la tendance.
                Fact current = lines.FirstOrDefault(l => !l.Companion);
                return current != null ? $"{current.Text} {forecast.Text}" : forecast.Text;
            }

            if (lines.Any(l => l.Tag?.StartsWith("objectif", StringComparison.Ordinal) == true))
            {
                // Rythme : le vôtre, puis l'objectif demandé (gagner, ou rattraper la voiture devant par défaut).
                bool win = q.Contains("gagn") || q.Contains("victoire") || q.Contains("premier");
                Fact goal = (win ? lines.FirstOrDefault(l => l.Tag == "objectif-gagner") : null)
                    ?? lines.FirstOrDefault(l => l.Tag == "objectif-devant")
                    ?? lines.FirstOrDefault(l => l.Tag == "objectif-gagner");
                Fact base0 = lines.First();
                return goal != null && goal != base0 ? $"{base0.Text} {goal.Text}" : base0.Text;
            }

            Fact chosen = lines.FirstOrDefault(l => behind && l.Side == Side.Behind)
                ?? lines.FirstOrDefault(l => ahead && l.Side == Side.Ahead)
                ?? lines.FirstOrDefault(l => !l.Companion)
                ?? lines.First();
            var parts = new List<string> { chosen.Text };
            parts.AddRange(lines.Where(l => l != chosen && l.Side == chosen.Side && l.Companion && l.Tag != "prévision").Take(2).Select(l => l.Text));
            return string.Join(" ", parts);
        }

        private static Dictionary<Topic, List<Fact>> Collect(RaceSnapshot snap, BriefingExtras extras)
        {
            var sections = new Dictionary<Topic, List<Fact>>();
            void Add(Topic t, string text, Side side = Side.None, bool companion = false, string tag = null)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return;
                }

                if (!sections.TryGetValue(t, out var list))
                {
                    sections[t] = list = new List<Fact>();
                }

                list.Add(new Fact(text, side, companion, tag));
            }

            DriverRow me = snap.Drivers.FirstOrDefault(d => d.IsPlayer);
            PlayerStrategy p = snap.Player;

            // Session
            if (me != null && p?.LapsRemaining is double lr)
            {
                Add(Topic.Session, $"Vous avez bouclé {me.Laps} tours ; il en reste environ {N(Math.Ceiling(lr), 0)} avant l'arrivée.");
            }
            else if (me != null)
            {
                Add(Topic.Session, $"Vous avez bouclé {me.Laps} tours.");
            }

            if (snap.LengthType == "Laps" && snap.TotalLaps > 0)
            {
                Add(Topic.Session, $"Course en {snap.TotalLaps} tours, le leader est dans le tour {snap.LeaderLap}.");
            }
            else if (snap.TimeRemaining.HasValue)
            {
                Add(Topic.Session, $"Temps restant : {Duration(snap.TimeRemaining.Value)}.");
            }

            if (snap.Flags.Count > 0)
            {
                Add(Topic.Session, "Drapeaux en cours : " + string.Join(", ", snap.Flags) + ".");
            }

            if (!string.IsNullOrEmpty(snap.SafetyCar) && snap.SafetyCar != "None")
            {
                Add(Topic.Session, "Voiture de sécurité en piste.");
            }

            if (me != null)
            {
                Add(Topic.Position, snap.IsMultiClass ? $"Vous êtes P{me.ClassPosition} dans votre catégorie ({me.ClassName})." : $"Vous êtes P{me.Position}.");
                if (me.PositionsGained != 0)
                {
                    Add(Topic.Position, me.PositionsGained > 0 ? $"Vous avez gagné {me.PositionsGained} place(s) depuis le départ." : $"Vous avez perdu {-me.PositionsGained} place(s) depuis le départ.");
                }

                var classmates = snap.Drivers.Where(d => d.ClassId == me.ClassId).ToList();
                int idx = classmates.IndexOf(me);
                DriverRow aheadInClass = idx > 0 ? classmates[idx - 1] : null;
                DriverRow behindInClass = idx >= 0 && idx < classmates.Count - 1 ? classmates[idx + 1] : null;

                if (aheadInClass != null)
                {
                    Add(Topic.Gap, $"La voiture devant vous, numéro {aheadInClass.Number} {aheadInClass.Name}, est {Secs(-(aheadInClass.GapToPlayer ?? double.NaN))} devant.{Pace(me, aheadInClass, true)}{Contact(p, aheadInClass.Id)}", Side.Ahead);
                }
                else
                {
                    Add(Topic.Gap, snap.IsMultiClass ? "Personne devant vous dans votre catégorie : vous êtes en tête." : "Vous êtes en tête, personne devant.", Side.Ahead);
                }

                if (behindInClass != null)
                {
                    Add(Topic.Gap, $"La voiture derrière vous, numéro {behindInClass.Number} {behindInClass.Name}, est {Secs(behindInClass.GapToPlayer ?? double.NaN)} derrière.{Pace(me, behindInClass, false)}{Contact(p, behindInClass.Id)}", Side.Behind);
                }
                else
                {
                    Add(Topic.Gap, "Personne derrière vous dans votre catégorie.", Side.Behind);
                }

                var onTrack = snap.Drivers.Where(d => !d.IsPlayer && !d.InPits && d.GapToPlayer.HasValue).ToList();
                DriverRow trackAhead = onTrack.Where(d => d.GapToPlayer < 0).OrderByDescending(d => d.GapToPlayer).FirstOrDefault();
                DriverRow trackBehind = onTrack.Where(d => d.GapToPlayer > 0).OrderBy(d => d.GapToPlayer).FirstOrDefault();
                // Autres catégories : seulement le trafic proche (moins de 5 secondes), le reste ne compte pas pour vous.
                if (trackAhead != null && trackAhead.ClassId != me.ClassId && -trackAhead.GapToPlayer <= 5)
                {
                    Add(Topic.Gap, $"Trafic : la {trackAhead.ClassName} numéro {trackAhead.Number} est juste devant sur la piste, à {Secs(-trackAhead.GapToPlayer.Value)}.", Side.Ahead, companion: true);
                }

                if (trackBehind != null && trackBehind.ClassId != me.ClassId && trackBehind.GapToPlayer <= 5)
                {
                    Add(Topic.Gap, $"Trafic : la {trackBehind.ClassName} numéro {trackBehind.Number} arrive derrière, à {Secs(trackBehind.GapToPlayer.Value)}.", Side.Behind, companion: true);
                }

                DriverRow classLeader = classmates.FirstOrDefault();
                if (classLeader != null && classLeader != me)
                {
                    Add(Topic.Position, $"Le leader de votre catégorie est le numéro {classLeader.Number} {classLeader.Name}, à {GapText(me.GapToClassLeader, me.GapToClassLeaderLaps)} devant vous.");
                }

                if (snap.IsMultiClass)
                {
                    Add(Topic.Position, $"Au classement général (toutes catégories), vous êtes P{me.Position}.");
                }

                foreach (var (rival, side, label) in new[] { (aheadInClass, Side.Ahead, "devant"), (behindInClass, Side.Behind, "derrière") })
                {
                    if (rival == null)
                    {
                        Add(Topic.Opponents, side == Side.Ahead ? "Personne devant vous dans votre catégorie : vous êtes en tête." : "Personne derrière vous dans votre catégorie.", side);
                        continue;
                    }

                    string text = $"Stratégie de la voiture {label}, numéro {rival.Number} {rival.Name} : pneus {Compound(rival.Tyres.Compound)} depuis {rival.Tyres.StintLaps} tours, {Stops(rival.PitStops)}";
                    text += rival.NextStop == null
                        ? ", pas encore d'estimation de son prochain arrêt."
                        : rival.NextStop.LapsUntil <= 1
                            ? ", son arrêt est attendu à tout moment (estimation)."
                            : $", prochain arrêt estimé au tour {rival.NextStop.Lap}, dans {rival.NextStop.LapsUntil} tours (estimation).";
                    Add(Topic.Opponents, text, side);
                }
            }

            if (p != null)
            {
                // Arrêt au stand (en premier : c'est la réponse à « je rentre quand ? »).
                int? planned = extras.Memory?.PlannedStopLap > me?.Laps ? extras.Memory.PlannedStopLap : null;
                if (extras.Memory?.NoStopPlanned == true && me != null)
                {
                    Add(Topic.Pit, "Pas d'arrêt prévu : on va au bout, comme décidé ensemble. Surveillez les pneus.");
                }
                else if ((planned ?? p.RecommendedStopLap).HasValue && me != null)
                {
                    int stop = (planned ?? p.RecommendedStopLap).Value;
                    int inLaps = stop - me.Laps;
                    string when = inLaps <= 1 ? "ce tour-ci, box box" : $"dans {inLaps} tours";
                    string time = p.AverageLap is double avg && inLaps > 1 ? $", soit environ {Duration(Math.Max(0, inLaps - me.LapFraction) * avg)}" : string.Empty;
                    string why = planned.HasValue ? "comme décidé ensemble (relais allongé)" : $"à cause {Because(p.RecommendedStopReason)}";
                    Add(Topic.Pit, $"Rentrez au stand au tour {stop}, {when}{time}, {why}.{Load(p)}");
                }
                else if (snap.SessionType == "Race" && p.Fuel?.Status == "ok" && (p.Energy == null || p.Energy.Status == "ok"))
                {
                    Add(Topic.Pit, "Pas besoin de rentrer au stand : vous avez de quoi finir la course.");
                }
                else if (snap.SessionType == "Race")
                {
                    Add(Topic.Pit, "Je n'ai pas encore assez de tours pour calculer votre arrêt ; encore un ou deux tours.");
                }

                if (p.Fuel != null)
                {
                    if (p.Fuel.PerLap.HasValue)
                    {
                        string fuel = $"Il vous reste {N(p.Fuel.LapsLeft ?? 0, 1)} tours d'essence : {N(p.Fuel.Remaining, 1)} litres, vous consommez {N(p.Fuel.PerLap.Value, 2)} litres par tour.";
                        if (p.Fuel.ToAdd <= 0)
                        {
                            fuel += " C'est assez pour finir.";
                        }
                        else if (p.Fuel.ToAdd.HasValue)
                        {
                            fuel += p.Fuel.StopsNeeded > 1
                                ? $" Il manque {N(p.Fuel.ToAdd.Value, 0)} litres pour finir, soit {p.Fuel.StopsNeeded} arrêts."
                                : $" Il manque {N(p.Fuel.ToAdd.Value, 0)} litres pour finir.";
                        }

                        Add(Topic.Fuel, fuel);
                    }
                    else
                    {
                        Add(Topic.Fuel, $"Il vous reste {N(p.Fuel.Remaining, 1)} litres ; je calcule encore la consommation.");
                    }
                }

                if (p.Energy != null)
                {
                    string e = p.Energy.PerLapPct.HasValue
                        ? $"Il vous reste {N(p.Energy.LapsLeft ?? 0, 1)} tours d'énergie virtuelle : {N(p.Energy.RemainingPct, 0)} %, vous utilisez {N(p.Energy.PerLapPct.Value, 1)} % par tour."
                        : $"Énergie virtuelle : {N(p.Energy.RemainingPct, 0)} % ; je calcule encore la consommation.";
                    Add(Topic.Energy, e);
                }

                if (p.Wheels.Count == 4)
                {
                    WheelStatus worst = p.Wheels.OrderByDescending(w => w.WearPct).First();
                    string name = worst.Position switch { "FL" => "avant gauche", "FR" => "avant droit", "RL" => "arrière gauche", _ => "arrière droit" };
                    string t = $"Pneus {Compound(worst.Compound)} depuis {p.StintLaps} tours. Le plus usé est l'{name}, à {N(worst.WearPct, 0)} % d'usure";
                    t += worst.LapsToHeavyWear.HasValue ? $", encore environ {N(worst.LapsToHeavyWear.Value, 0)} tours avant d'être très usé." : ".";
                    Add(Topic.Tyres, t);
                    var hot = p.Wheels.Where(w => w.TempMiddle.HasValue && w.IdealTemp.HasValue && w.TempMiddle > w.IdealTemp + 10).Select(w => WheelName(w.Position)).ToList();
                    var cold = p.Wheels.Where(w => w.TempMiddle.HasValue && w.IdealTemp.HasValue && w.TempMiddle < w.IdealTemp - 15).Select(w => WheelName(w.Position)).ToList();
                    Add(Topic.Tyres, hot.Count > 0 ? "Pneus trop chauds : " + string.Join(", ", hot) + "." : cold.Count > 0 ? "Pneus trop froids : " + string.Join(", ", cold) + "." : "Températures des pneus correctes.", companion: true);
                    if (p.Wheels.All(w => w.TempMiddle.HasValue))
                    {
                        Add(Topic.Tyres, "Températures de surface : " + string.Join(", ", p.Wheels.Select(w => $"{WheelName(w.Position)} {N(w.TempMiddle.Value, 0)} degrés")) + ".");
                    }

                    if (p.Wheels.All(w => w.Core.HasValue))
                    {
                        Add(Topic.Tyres, "Carcasse des pneus : " + string.Join(", ", p.Wheels.Select(w => $"{WheelName(w.Position)} {N(w.Core.Value, 0)} degrés")) + ".");
                    }

                    if (p.Wheels.All(w => w.Pressure.HasValue))
                    {
                        Add(Topic.Tyres, "Pressions : " + string.Join(", ", p.Wheels.Select(w => $"{WheelName(w.Position)} {N(w.Pressure.Value / 100, 2)} bar")) + ".");
                    }
                }

                if (p.Wheels.Count == 4 && p.Wheels.All(w => w.Brake.HasValue))
                {
                    WheelStatus hottest = p.Wheels.OrderByDescending(w => w.Brake).First();
                    Add(Topic.Car, "Freins : " + string.Join(", ", p.Wheels.Select(w => $"{WheelName(w.Position)} {N(w.Brake.Value, 0)} degrés")) + $". Le plus chaud est l'{WheelName(hottest.Position)}.");
                }

                if (p.Engine is { } eng)
                {
                    var parts = new List<string>();
                    if (eng.WaterTemp.HasValue)
                    {
                        parts.Add($"eau {N(eng.WaterTemp.Value, 0)} degrés{Range(eng.WaterTemp.Value, eng.WaterIdeal, eng.WaterWindow)}");
                    }

                    if (eng.OilTemp.HasValue)
                    {
                        parts.Add($"huile {N(eng.OilTemp.Value, 0)} degrés{Range(eng.OilTemp.Value, eng.OilIdeal, eng.OilWindow)}");
                    }

                    if (parts.Count > 0)
                    {
                        Add(Topic.Car, "Moteur : " + string.Join(", ", parts) + ".");
                    }

                    var damage = new List<string>();
                    if (eng.DamageEngine > 1)
                    {
                        damage.Add($"moteur {N(eng.DamageEngine.Value, 0)} %");
                    }

                    if (eng.DamageTransmission > 1)
                    {
                        damage.Add($"boîte {N(eng.DamageTransmission.Value, 0)} %");
                    }

                    if (eng.DamageSuspension > 1)
                    {
                        damage.Add($"suspensions {N(eng.DamageSuspension.Value, 0)} %");
                    }

                    if (eng.DamageBodywork > 1)
                    {
                        damage.Add($"carrosserie {N(eng.DamageBodywork.Value, 0)} %");
                    }

                    Add(Topic.Car, damage.Count == 0 ? "Aucun dégât signalé sur la voiture." : "Dégâts : " + string.Join(", ", damage) + ".");
                }

                if (p.Degradation.HasValue)
                {
                    Add(Topic.Tyres, p.Degradation.Value >= 0
                        ? $"Votre rythme baisse de {N(p.Degradation.Value, 2)} seconde par tour sur ce relais."
                        : $"Votre rythme s'améliore de {N(-p.Degradation.Value, 2)} seconde par tour sur ce relais (la voiture s'allège).");
                }
            }

            if (me != null)
            {
                AddSpeedFacts(snap, me, extras.Speeds, (t, s) => Add(t, s));
                AddPerformanceFacts(snap, me, extras, (t, s) => Add(t, s));
                AddRainFacts(snap, me, extras, (t, s) => Add(t, s));
            }

            if (snap.Weather.Air.HasValue)
            {
                Add(Topic.Weather, $"Air {N(snap.Weather.Air.Value, 0)} degrés, piste {N(snap.Weather.TrackTemp ?? 0, 0)} degrés, {(snap.Weather.Rain > 0 ? $"pluie à {snap.Weather.Rain} %" : "pas de pluie")}{(snap.Weather.Wetness > 0 ? $", piste humide à {snap.Weather.Wetness} %" : string.Empty)}.");
            }

            if (!string.IsNullOrEmpty(snap.Weather.Trend))
            {
                Add(Topic.Weather, snap.Weather.Trend + " C'est une tendance, pas la prévision officielle du jeu.", companion: true);
            }

            if (snap.Weather.Wind.HasValue)
            {
                Add(Topic.Weather, $"Vent de {N(snap.Weather.Wind.Value, 0)} kilomètres heure.", companion: true);
            }

            AddForecastFacts(extras.Weather, (t, s, c, tag) => Add(t, s, companion: c, tag: tag));
            if (me != null)
            {
                AddPaceFacts(snap, me, p, extras, (t, s, side, tag) => Add(t, s, side, tag: tag));
            }

            if (p?.TyreThermal is { Count: > 0 } thermal)
            {
                var avg = thermal.Where(t => t.LastLapAverage.HasValue).ToList();
                if (avg.Count > 0)
                {
                    Add(Topic.Tyres, "Moyenne de surface sur le dernier tour : " + string.Join(", ", avg.Select(t => $"{WheelName(t.Position)} {N(t.LastLapAverage.Value, 0)} degrés")) +
                        (avg[0].Min.HasValue ? $", plage idéale {N(avg[0].Min.Value, 0)} à {N(avg[0].Max ?? 0, 0)}." : "."), companion: true);
                }

                foreach (string alert in p.Alerts ?? new List<string>())
                {
                    Add(Topic.Tyres, alert.Replace("°", " degrés").Replace("–", " à "), companion: true);
                }
            }

            return sections;
        }

        /// <summary>Tendance météo calculée sur l'évolution récente : température de piste, pluie, dans 30 minutes.</summary>
        private static void AddForecastFacts(WeatherReportDto w, Action<Topic, string, bool, string> add)
        {
            if (w == null)
            {
                return;
            }

            if (w.Forecast.Count == 0)
            {
                add(Topic.Weather, "Prévision : pas encore assez d'historique pour une tendance, il faut quelques minutes de course.", true, "prévision");
                return;
            }

            WeatherSample last = w.Forecast[^1];
            WeatherSample first = w.History.Count > 0 ? w.History[^1] : w.Forecast[0];
            double minutes = Math.Max(1, Math.Round((last.T - first.T) / 60));
            var parts = new List<string>();
            if (last.Track.HasValue && first.Track.HasValue)
            {
                double d = last.Track.Value - first.Track.Value;
                parts.Add(Math.Abs(d) < 1 ? $"piste stable autour de {N(last.Track.Value, 0)} degrés" : $"piste à {N(last.Track.Value, 0)} degrés ({(d > 0 ? "plus" : "moins")} {N(Math.Abs(d), 0)})");
            }

            if (last.Air.HasValue)
            {
                parts.Add($"air à {N(last.Air.Value, 0)} degrés");
            }

            parts.Add(last.Rain > 0 ? $"pluie autour de {last.Rain} %" : "pas de pluie attendue");
            if (last.Wetness > 0)
            {
                parts.Add($"piste humide à {last.Wetness} %");
            }

            add(Topic.Weather, $"Prévision dans {N(minutes, 0)} minutes : {string.Join(", ", parts)}. C'est une tendance calculée sur l'évolution récente, pas la prévision officielle du jeu.", true, "prévision");
        }

        /// <summary>
        /// Rythme : le vôtre, celui qu'il faut pour rattraper la voiture devant ou pour gagner, et ce que donne le rythme actuel à l'arrivée.
        /// En essais et qualification : écart au meilleur tour de la catégorie et meilleur tour théorique.
        /// </summary>
        private static void AddPaceFacts(RaceSnapshot snap, DriverRow me, PlayerStrategy p, BriefingExtras extras, Action<Topic, string, Side, string> add)
        {
            double? mine = me.PaceLast3 ?? p?.AverageLap ?? me.LastLap;
            if (mine is not double myPace)
            {
                add(Topic.Pace, "Je n'ai pas encore de tour complet pour calculer votre rythme ; encore un tour.", Side.None, null);
                return;
            }

            string deg = p?.Degradation is double dg && Math.Abs(dg) >= 0.05 ? (dg > 0 ? $", il baisse de {N(dg, 2)} seconde par tour" : $", il s'améliore de {N(-dg, 2)} seconde par tour") : string.Empty;
            add(Topic.Pace, $"Votre rythme est de {LapSpoken(myPace)} sur les derniers tours (meilleur tour {LapSpoken(me.BestLap)}){deg}.", Side.None, null);

            var cls = snap.Drivers.Where(d => d.ClassId == me.ClassId && d.FinishStatus is not ("Dnf" or "Dq")).OrderBy(d => d.ClassPosition).ToList();
            if (snap.SessionType != "Race")
            {
                DriverRow best = cls.Where(d => d.BestLap > 0).OrderBy(d => d.BestLap).FirstOrDefault();
                if (best != null && me.BestLap > 0 && best.Id != me.Id)
                {
                    add(Topic.Pace, $"Meilleur tour de la catégorie : {LapSpoken(best.BestLap)} par le numéro {best.Number} {best.Name}, vous êtes à {N(me.BestLap.Value - best.BestLap.Value, 3)} seconde.", Side.None, "objectif-devant");
                }

                var ms = extras.MicroSectors?.Drivers.FirstOrDefault(d => d.Id == me.Id);
                if (ms?.TheoreticalBest is double theo && me.BestLap > theo)
                {
                    add(Topic.Pace, $"En assemblant vos meilleurs morceaux de tour, vous pouvez faire {LapSpoken(theo)}, soit {N(me.BestLap.Value - theo, 2)} seconde de mieux.", Side.None, "objectif-gagner");
                }

                return;
            }

            double? lapsLeft = p?.LapsRemaining;
            int idx = cls.FindIndex(d => d.Id == me.Id);
            if (idx < 0)
            {
                return;
            }

            if (idx == 0)
            {
                DriverRow next = cls.ElementAtOrDefault(1);
                if (next?.PaceLast3 is double theirs && next.GapToPlayer is double g && g > 0)
                {
                    string keep = lapsLeft is double ll && ll >= 1 ? $" Pour rester devant jusqu'à l'arrivée, ne tournez pas plus lentement que {LapSpoken(theirs + (g / ll))}." : string.Empty;
                    add(Topic.Pace, $"Vous menez avec {N(g, 1)} secondes d'avance sur le numéro {next.Number}, qui tourne en {LapSpoken(theirs)}.{keep}", Side.None, "objectif-devant");
                }
                else
                {
                    add(Topic.Pace, "Vous menez : gardez ce rythme.", Side.None, "objectif-devant");
                }

                return;
            }

            void Target(DriverRow rival, string goal, string tag)
            {
                if (rival.PaceLast3 is not double theirs || rival.GapToPlayer is not double g || me.GapToClassLeaderLaps != rival.GapToClassLeaderLaps)
                {
                    return;
                }

                double gap = -g; // positif : il est devant
                if (gap <= 0)
                {
                    return;
                }

                string now = theirs > myPace + 0.05
                    ? $" Au rythme actuel vous lui reprenez {N(theirs - myPace, 2)} seconde par tour : contact dans environ {N(Math.Ceiling(gap / (theirs - myPace)), 0)} tours."
                    : theirs < myPace - 0.05 ? $" Au rythme actuel c'est lui qui vous reprend {N(myPace - theirs, 2)} seconde par tour." : " Vous tournez au même rythme.";
                if (lapsLeft is double ll && ll >= 1)
                {
                    double need = gap / ll;
                    add(Topic.Pace, $"{goal} : {N(gap, 1)} secondes à reprendre au numéro {rival.Number} en {N(Math.Floor(ll), 0)} tours, soit {N(need, 2)} seconde par tour. Il faut tourner en {LapSpoken(theirs - need)} ; il tourne en {LapSpoken(theirs)}.{now}", Side.None, tag);
                }
                else
                {
                    add(Topic.Pace, $"{goal} : le numéro {rival.Number} est {N(gap, 1)} secondes devant et tourne en {LapSpoken(theirs)}.{now}", Side.None, tag);
                }
            }

            // Deuxième : rattraper la voiture devant, c'est aussi gagner.
            Target(cls[idx - 1], idx == 1 ? "Pour gagner, il faut rattraper le leader" : "Pour rattraper la voiture devant", idx == 1 ? "objectif-gagner" : "objectif-devant");
            if (idx > 1)
            {
                Target(cls[0], "Pour gagner", "objectif-gagner");
            }
        }

        /// <summary>Temps au tour dit à la radio : « 1 minute 42,3 ».</summary>
        private static string LapSpoken(double? seconds)
        {
            if (seconds is not double s || s <= 0)
            {
                return "inconnu";
            }

            int m = (int)(s / 60);
            double rest = s - (m * 60);
            return m > 0 ? $"{m} minute{(m > 1 ? "s" : string.Empty)} {N(rest, 1)}" : $"{N(rest, 1)} secondes";
        }

        private static string Load(PlayerStrategy p)
        {
            var parts = new List<string>();
            if (p.Fuel?.NextStopAdd > 0.5)
            {
                parts.Add($"mettre {N(p.Fuel.NextStopAdd.Value, 0)} litres");
            }

            if (p.Energy?.NextStopAddPct > 0.5)
            {
                parts.Add($"recharger {N(p.Energy.NextStopAddPct.Value, 0)} % d'énergie");
            }

            return parts.Count == 0 ? string.Empty : " À cet arrêt : " + string.Join(" et ", parts) + ".";
        }

        private static string Because(string reason) => reason switch
        {
            "carburant" => "du carburant",
            "énergie virtuelle" => "de l'énergie virtuelle",
            "usure des pneus" => "de l'usure des pneus",
            _ => reason,
        };

        private static string WheelName(string position) => position switch { "FL" => "avant gauche", "FR" => "avant droit", "RL" => "arrière gauche", _ => "arrière droit" };

        private static string Range(double value, double? ideal, double? window)
        {
            if (!ideal.HasValue || !window.HasValue)
            {
                return string.Empty;
            }

            return value > ideal + window ? " (trop chaud)" : value < ideal - window ? " (encore froid)" : " (normal)";
        }

        private static string Contact(PlayerStrategy p, string id)
        {
            BattleDto b = p?.Battles?.FirstOrDefault(x => x.Id == id);
            if (b?.LapsToContact is not double laps)
            {
                return string.Empty;
            }

            string when = laps < 1 ? "dès ce tour" : $"dans environ {N(Math.Ceiling(laps), 0)} tours";
            return b.Side == "ahead" ? $" Au rythme actuel, vous serez dans ses roues {when}." : $" Au rythme actuel, il sera dans vos roues {when}.";
        }

        private static string Stops(int n) => n == 0 ? "aucun arrêt effectué" : n == 1 ? "un arrêt effectué" : $"{n} arrêts effectués";

        private static string Compound(string c) => string.IsNullOrWhiteSpace(c) ? "inconnus" : c;

        private static string Pace(DriverRow me, DriverRow other, bool otherAhead)
        {
            if (me.PaceLast3 is not double mine || other.PaceLast3 is not double theirs)
            {
                return string.Empty;
            }

            double diff = theirs - mine; // > 0 : l'autre est plus lent
            if (Math.Abs(diff) < 0.05)
            {
                return " Même rythme que vous.";
            }

            if (otherAhead)
            {
                return diff > 0 ? $" Vous lui reprenez {N(diff, 1)} seconde par tour." : $" Il s'échappe de {N(-diff, 1)} seconde par tour.";
            }

            return diff < 0 ? $" Il revient de {N(-diff, 1)} seconde par tour." : $" Vous le distancez de {N(diff, 1)} seconde par tour.";
        }

        private static string GapText(double? seconds, int laps) => laps > 0 ? $"{laps} tour(s)" : Secs(seconds ?? double.NaN);

        private static string Secs(double s) => double.IsNaN(s) ? "à un écart inconnu" : $"{N(Math.Abs(s), 1)} secondes";

        private static string Duration(double seconds)
        {
            var t = TimeSpan.FromSeconds(seconds);
            if (t.TotalHours >= 1)
            {
                return $"{(int)t.TotalHours} heure {t.Minutes:00}";
            }

            if (t.TotalMinutes >= 2)
            {
                return $"{(int)Math.Round(t.TotalMinutes)} minutes";
            }

            return t.TotalSeconds >= 60 ? $"1 minute {t.Seconds:00}" : $"{(int)t.TotalSeconds} secondes";
        }

        private static void AddSpeedFacts(RaceSnapshot snap, DriverRow me, SpeedAnalysisDto speeds, Action<Topic, string> add)
        {
            DriverSpeedDto mine = speeds?.Drivers.FirstOrDefault(d => d.Id == me.Id);
            if (mine?.TopSpeedBest == null)
            {
                add(Topic.Speed, "Je n'ai pas encore mesuré votre vitesse de pointe : il faut un tour complet.");
                return;
            }

            var classIds = snap.Drivers.Where(d => d.ClassId == me.ClassId).Select(d => d.Id).ToHashSet();
            var rivals = speeds.Drivers.Where(d => classIds.Contains(d.Id) && d.Id != me.Id && d.TopSpeedBest.HasValue).ToList();
            DriverSpeedDto fastest = rivals.OrderByDescending(d => d.TopSpeedBest).FirstOrDefault();
            string text = $"Votre vitesse de pointe : {N(mine.TopSpeedBest.Value, 0)} kilomètres heure sur votre meilleur passage";
            text += mine.TopSpeedLast.HasValue ? $", {N(mine.TopSpeedLast.Value, 0)} au dernier tour." : ".";
            if (fastest != null)
            {
                DriverRow f = snap.Drivers.FirstOrDefault(d => d.Id == fastest.Id);
                text += fastest.TopSpeedBest > mine.TopSpeedBest
                    ? $" La plus rapide de votre catégorie : {N(fastest.TopSpeedBest.Value, 0)}, numéro {f?.Number}."
                    : " C'est la meilleure de votre catégorie.";
            }

            add(Topic.Speed, text);

            // Lignes droites et virages où l'écart avec le meilleur de la catégorie est le plus grand.
            var gaps = new List<(TrackSegment Seg, double Mine, double Best)>();
            foreach (TrackSegment seg in speeds.Segments)
            {
                if (!mine.Best.TryGetValue(seg.Id, out double v))
                {
                    continue;
                }

                double best = rivals.Select(r => r.Best.TryGetValue(seg.Id, out double rv) ? rv : 0).DefaultIfEmpty(0).Max();
                gaps.Add((seg, v, Math.Max(best, v)));
            }

            var straights = gaps.Where(g => g.Seg.Kind == "straight").ToList();
            if (straights.Count > 0)
            {
                add(Topic.Speed, "Vmax par ligne droite (vous / meilleur de la catégorie) : " + string.Join(", ", straights.Select(g => $"{g.Seg.Name.Replace(" (principale)", string.Empty)} {N(g.Mine, 0)} / {N(g.Best, 0)}")) + ".");
            }

            var corners = gaps.Where(g => g.Seg.Kind == "corner").OrderByDescending(g => g.Best - g.Mine).ToList();
            if (corners.Count > 0)
            {
                var worst = corners[0];
                add(Topic.Speed, worst.Best - worst.Mine < 1
                    ? "Dans les virages, vos vitesses de passage sont au niveau des meilleurs de la catégorie."
                    : $"Virage où vous passez le moins vite par rapport aux meilleurs : {worst.Seg.Name.ToLowerInvariant()}, {N(worst.Mine, 0)} contre {N(worst.Best, 0)} kilomètres heure.");
            }
        }

        private static void AddPerformanceFacts(RaceSnapshot snap, DriverRow me, BriefingExtras extras, Action<Topic, string> add)
        {
            MicroSectorReportDto ms = extras.MicroSectors;
            DriverMicroSectorsDto mine = ms?.Drivers.FirstOrDefault(d => d.Id == me.Id);
            if (mine == null || !ms.ClassBest.TryGetValue(me.ClassId ?? string.Empty, out double?[] classBest))
            {
                add(Topic.Performance, "Je n'ai pas encore assez de tours pour comparer vos secteurs.");
                return;
            }

            var losses = Enumerable.Range(0, ms.Count)
                .Where(i => mine.Best[i].HasValue && classBest[i].HasValue)
                .Select(i => (Index: i, Loss: mine.Best[i].Value - classBest[i].Value))
                .ToList();
            if (losses.Count < ms.Count / 2)
            {
                add(Topic.Performance, "Je n'ai pas encore assez de tours pour comparer vos secteurs.");
                return;
            }

            double total = losses.Sum(l => l.Loss);
            var top = losses.OrderByDescending(l => l.Loss).Take(2).Where(l => l.Loss > 0.03).ToList();
            if (top.Count == 0)
            {
                add(Topic.Performance, "Vous êtes au niveau des meilleurs de votre catégorie sur tout le tour.");
            }
            else
            {
                string where = string.Join(" et ", top.Select(l => $"le micro-secteur {l.Index + 1}{Near(l.Index, ms, extras.Speeds)} ({N(l.Loss, 2)} seconde)"));
                add(Topic.Performance, $"Vous perdez le plus de temps dans {where}, par rapport aux meilleurs de votre catégorie. Au total : {N(total, 1)} seconde sur un tour idéal.");
            }

            if (mine.TheoreticalBest.HasValue && me.BestLap.HasValue && me.BestLap - mine.TheoreticalBest > 0.05)
            {
                add(Topic.Performance, $"En enchaînant vos meilleurs secteurs, vous gagneriez {N(me.BestLap.Value - mine.TheoreticalBest.Value, 1)} seconde sur votre meilleur tour.");
            }
        }

        private static string Near(int index, MicroSectorReportDto ms, SpeedAnalysisDto speeds)
        {
            if (speeds?.Segments == null || ms.LapLength <= 0)
            {
                return string.Empty;
            }

            double center = (index + 0.5) * ms.LapLength / ms.Count;
            TrackSegment seg = speeds.Segments.Where(s => s.Kind == "corner").OrderBy(s => Math.Min(Math.Abs(s.Apex - center), ms.LapLength - Math.Abs(s.Apex - center))).FirstOrDefault();
            return seg == null ? string.Empty : $", vers le {seg.Name.ToLowerInvariant()}";
        }

        private static void AddRainFacts(RaceSnapshot snap, DriverRow me, BriefingExtras extras, Action<Topic, string> add)
        {
            string c = (me.Tyres.Compound ?? string.Empty).ToLowerInvariant();
            bool onWets = c.Contains("wet") || c.Contains("rain") || c.Contains("pluie") || c.Contains("inter");
            int wet = snap.Weather.Wetness;
            int rain = snap.Weather.Rain;
            int threshold = extras.WetThresholdPct;
            double? perHour = extras.Weather?.WetnessPerHour;
            string text;
            if (onWets)
            {
                text = wet < threshold / 2 && rain == 0
                    ? $"La piste sèche (humidité {wet} %) : on peut repasser en pneus secs."
                    : $"Restez en pneus pluie : piste mouillée à {wet} %, pluie à {rain} %.";
            }
            else if (wet >= threshold)
            {
                text = $"Passez en pneus pluie : la piste est mouillée à {wet} %, au-dessus du seuil de {threshold} %.";
            }
            else if (rain == 0 && wet == 0)
            {
                text = "Pas de pluie : restez en pneus secs.";
            }
            else
            {
                text = $"Pas encore de pneus pluie : piste humide à {wet} %, pluie à {rain} %. Je les conseille au-delà de {threshold} % d'humidité.";
                if (perHour > 0.5)
                {
                    double minutes = (threshold - wet) / perHour.Value * 60;
                    text += minutes <= 90 ? $" Au rythme actuel, ce serait dans environ {N(Math.Max(1, minutes), 0)} minutes (tendance)." : " Au rythme actuel, pas avant longtemps.";
                }
                else
                {
                    text += " L'humidité n'augmente pas pour l'instant.";
                }
            }

            add(Topic.Rain, text);
        }

        private static string N(double v, int digits) => v.ToString("F" + digits, Fr);

        private enum Side
        {
            None,
            Ahead,
            Behind,
        }

        private sealed record Fact(string Text, Side Side, bool Companion, string Tag = null);
    }

    /// <summary>Analyses supplémentaires (calculées à la demande) pour enrichir la fiche de faits.</summary>
    public sealed class BriefingExtras
    {
        public SpeedAnalysisDto Speeds { get; set; }

        public MicroSectorReportDto MicroSectors { get; set; }

        public WeatherReportDto Weather { get; set; }

        public RaceMemory Memory { get; set; }

        public int WetThresholdPct { get; set; } = 25;
    }

    public sealed class BriefingResult
    {
        public string Facts { get; set; }

        /// <summary>Réponse préparée par le hub pour une question reconnue ; null pour une question libre.</summary>
        public string Suggestion { get; set; }

        public List<RaceBriefing.Topic> Topics { get; set; } = new();
    }
}
