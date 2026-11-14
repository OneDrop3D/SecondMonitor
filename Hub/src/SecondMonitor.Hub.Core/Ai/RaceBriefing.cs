namespace SecondMonitor.Hub.Core.Ai
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using SecondMonitor.Hub.Core.Race;
    using SecondMonitor.Hub.Core.Telemetry;
    using static SecondMonitor.Hub.Core.Ai.RadioFormat;

    /// <summary>
    /// Transforme l'état de course en « fiche de faits » en français, chiffres déjà calculés, et propose une réponse.
    /// Les petits modèles (2B) calculent mal et appellent mal les outils : le hub prépare donc la réponse,
    /// le modèle ne fait que la reprendre (questions reconnues) ou s'appuyer sur la fiche (questions libres).
    /// Style radio : une ou deux phrases courtes, les chiffres d'abord.
    /// </summary>
    public static partial class RaceBriefing
    {
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
            Strategy,
            Damage,
            Laps,
            Hybrid,
            Controls,
            Driving,
            Rules,
        }

        private static readonly (Topic Topic, string[] Words)[] Keywords =
        {
            (Topic.Strategy, new[] { "undercut", "under cut", "overcut", "over cut", "si je rentre", "ressors", "ressortir", "ressortirai", "sortie des stands", "perte au stand", "couvrir", "stratégie d'arrêt", "strategie d'arret", "ma stratégie", "ma strategie", "la stratégie", "la strategie", "notre stratégie", "notre strategie" }),
            (Topic.Damage, new[] { "dégât", "degat", "cassé", "casse ", "aileron", "fond plat", "diffuseur", "ponton", "abîmé", "abime", "endommag", "touché", "réparer", "reparer", "mgu", "drs" }),
            (Topic.Laps, new[] { "meilleur tour", "meilleur temps", "meilleur chrono", "pire tour", "pire temps", "plus mauvais tour", "moins bon tour", "tour le plus lent", "plus lent tour", "dernier tour", "tour précédent", "tour precedent", "moyenne", "régularit", "regularit", "régulier", "regulier", "constan", "mes tours", "mes temps", "mes chronos" }),
            (Topic.Rules, new[] { "track limit", "tracks limit", "limites de piste", "limite de piste", "limites", "pénalité", "penalite", "pénalités", "penalites", "tour annulé", "tour annule", "temps annulé", "temps annule", "drive through", "stop and go" }),
            (Topic.Controls, new[] { "cartographie", "carto ", "carto?", "mapping", "map ", "réglages actuels", "reglages actuels", "mes réglages", "mes reglages", "traction", "contrôle de traction", "répartition", "repartition", "balance des freins", "balance de frein", "brake bias", "migration", "regen", "antiroulis", "anti-roulis", "coupure tc", "tc cut" }),
            (Topic.Driving, new[] { "pilotage", "ma conduite", "comment je conduis", "comment je pilote", "trajectoire", "pédale", "pedale", "accélérateur", "accelerateur", "volant", "freinage", "mes freinages", "mes entrées", "mes entrees", "mes sorties", "usure anormale" }),
            (Topic.Pace, new[] { "rythme", "allure", "chrono", "temps au tour", "tourner en", "dois tourner", "pour gagner", "gagner la course", "aller plus vite", "plus vite", "quel temps je", "en combien je", "théorique", "theorique" }),
            (Topic.Pit, new[] { "box", "stand", "rentre", "rentrer", "arrêt", "arret", "pit", "ravitaill" }),
            (Topic.Speed, new[] { "vitesse", "pointe", "vmax", "v max", "vmin", "v min", "km/h", "kilomètre", "top speed", "ligne droite", "lignes droites" }),
            (Topic.Performance, new[] { "perd", "gagner du temps", "amélior", "ameliorer", "plus lent", "secteur", "temps perdu", "où je", "ou je" }),
            (Topic.Fuel, new[] { "essence", "carburant", "fuel", "litre", "conso", "réservoir", "plein" }),
            (Topic.Hybrid, new[] { "batterie", "ers", "déploie", "deploie", "déployer", "deployer", "déploiement", "deploiement", "récolte", "recolte", "récupér", "recuper", "overtake", "hybride", "électrique", "electrique", "boost" }),
            (Topic.Energy, new[] { "énergie", "energie", "virtuel", "ratio" }),
            (Topic.Car, new[] { "moteur", "huile", "l'eau", " eau", "frein", "radiateur" }),
            (Topic.Tyres, new[] { "pneu", "gomme", "usure", "température", "pression", "dégrad", "carcasse" }),
            (Topic.Opponents, new[] { "sa stratégie", "sa strategie", "leur stratégie", "leur strategie", "adversaire", "concurrent", "va s'arrêter", "va s'arreter", "ses pneus" }),
            (Topic.Gap, new[] { "écart", "ecart", "derrière", "derriere", "devant", "gap", "revient", "reprend", "attaque", "loin", "rattrape" }),
            (Topic.Position, new[] { "position", "place", "classement", "rang", "leader", "premier", "combientième" }),
            (Topic.Session, new[] { "temps restant", "tours restants", "tour rest", "il reste combien de tours", "combien de tour", "tours bouclés", "tour bouclé", "fin de course", "minutes", "drapeau", "safety", "arrivée" }),
            (Topic.Weather, new[] { "pluie", "météo", "meteo", "température de piste", "pleuvoir", "pleut", "temps qu'il", "averse", "vent", "prévision", "prevision", "évoluer", "evoluer", "évolution", "evolution", "tendance", "la piste va" }),
        };

        /// <summary>Sujets reconnus dans la question, dans l'ordre où ils apparaissent.</summary>
        public static IReadOnlyList<Topic> DetectTopics(string question)
        {
            string q = Corrected(question);
            var topics = Keywords
                .Select(k => (k.Topic, Index: k.Words.Select(w => w == "ers" ? IndexOfWord(q, w) : q.IndexOf(w, StringComparison.Ordinal)).Where(i => i >= 0).DefaultIfEmpty(-1).Min()))
                .Where(x => x.Index >= 0)
                .OrderBy(x => x.Topic == Topic.Car && Regex.IsMatch(q, "frein") ? -1 : x.Index) // « température des freins » : on parle de la voiture, pas des pneus
                .Select(x => x.Topic)
                .Distinct()
                .ToList();

            // « La stratégie de la voiture derrière / du numéro 7 » : la stratégie de l'adversaire.
            if (Regex.IsMatch(q, @"strat[ée]gie") && Regex.IsMatch(q, @"voiture (devant|derri[èe]re)|adversaire|concurrent|num[ée]ro\s?\d+|\bdu \d+\b|#\d+"))
            {
                topics.Remove(Topic.Strategy);
                topics.Remove(Topic.Opponents);
                topics.Insert(0, Topic.Opponents);
            }

            // Mots isolés : « TC », « ABS » (trop courts pour la recherche par morceau de mot).
            if (Regex.IsMatch(q, @"\b(tc|abs)\b") && !topics.Contains(Topic.Controls))
            {
                topics.Insert(0, Topic.Controls);
            }

            // « Cartographie moteur », « la carto » : un réglage, pas les températures du moteur.
            if (topics.Contains(Topic.Controls) && Regex.IsMatch(q, @"carto|mapping|\bmap\b|r[ée]partition|balance|migration|regen|\btc\b|\babs\b"))
            {
                topics.Remove(Topic.Car);
                if (!Regex.IsMatch(q, @"pneu|gomme|usure"))
                {
                    topics.Remove(Topic.Tyres);
                }

                if (Regex.IsMatch(q, @"r[ée]partition|balance|migration") && !Regex.IsMatch(q, @"temp[ée]rature|chaud|froid"))
                {
                    topics.Remove(Topic.Car);
                }
            }

            // « Mode d'énergie », « énergie déployée » : l'énergie et l'état du moteur électrique.
            if (Regex.IsMatch(q, @"mode (d.)?[ée]nergie|[ée]nergie d[ée]ploy|d[ée]ploiement|moteur [ée]lectrique") && !topics.Contains(Topic.Energy))
            {
                topics.Insert(0, Topic.Energy);
            }

            // « Freinage » dans une question sur le pilotage, mais « température des freins » reste la voiture.
            if (topics.Contains(Topic.Driving) && topics.Contains(Topic.Car) && !Regex.IsMatch(q, @"temp[ée]rature|chaud|froid"))
            {
                topics.Remove(Topic.Car);
            }

            // « Combien de tours d'essence ? » parle de carburant, pas de la fin de course.
            if (topics.Contains(Topic.Session) && (topics.Contains(Topic.Fuel) || topics.Contains(Topic.Energy)))
            {
                topics.Remove(Topic.Session);
            }

            // « Mon réglage d'aileron ? » : c'est le setup, pas un dégât.
            if (topics.Contains(Topic.Damage) && Regex.IsMatch(q, @"r[ée]glage|setup|\bcrans?\b") && !Regex.IsMatch(q, "dégât|degat|cass|abîm|abim|endommag|touch|répar|repar"))
            {
                topics.Remove(Topic.Damage);
            }

            // « Meilleur tour théorique » : c'est le rythme (assemblage des meilleurs secteurs), pas la liste des tours.
            if (topics.Contains(Topic.Laps) && Regex.IsMatch(q, "théorique|theorique|idéal|ideal"))
            {
                topics.Remove(Topic.Laps);
            }

            // « C'est le dernier tour ? » : la fin de course.
            if (Regex.IsMatch(q, @"(c'est|c est|est-ce|est ce) (bien )?le dernier tour"))
            {
                topics.Remove(Topic.Laps);
                topics.Insert(0, Topic.Session);
            }

            // « Vitesse moyenne » : la vitesse, pas la moyenne des tours.
            if (topics.Contains(Topic.Speed) && topics.Contains(Topic.Laps) && !Regex.IsMatch(q, "meilleur tour|pire tour|dernier tour|mes tours"))
            {
                topics.Remove(Topic.Laps);
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

        /// <summary>
        /// Demande courte sans verbe (« Pneus », « Fure des pneus », « Et la balance des freins », « TC ? ») : c'est une question
        /// sur ce sujet, pas une remarque du pilote. Un symptôme (« ça glisse », « je bloque ») reste une remarque.
        /// </summary>
        public static bool IsShortRequest(string question)
        {
            string t = RadioCommands.Normalize(question);
            int words = t.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (words == 0 || words > 6 || SetupAdvisor.DetectSymptom(question) != null)
            {
                return false;
            }

            if (Regex.IsMatch(t, @"\b(glisse\w*|bloque\w*|patine\w*|vibre\w*|saute\w*|chauffe\w*|touche|casse\w*|crev\w*|abime\w*|mouill\w*|pleut|j ai|je suis|c est|ca)\b"))
            {
                return false;
            }

            return DetectTopics(question).Count > 0;
        }

        /// <summary>
        /// Question en minuscules, corrigée des erreurs courantes de la dictée (« fure des pneus » → « usure des pneus »,
        /// « la carto » → « la cartographie »). Un mot seul (« pneus », « météo ») reste une question.
        /// </summary>
        public static string Corrected(string question)
        {
            string q = (question ?? string.Empty).ToLowerInvariant().Trim().TrimEnd('.', '!', '?', '…').Trim();
            q = Regex.Replace(q, @"\b(fure|l'usur|usur)\b", "usure");
            q = Regex.Replace(q, @"\bcarto\b", "cartographie");
            q = Regex.Replace(q, @"\b(t\.?\s?c\.?|tisé|tissé|técé)\b", "tc");
            q = Regex.Replace(q, @"\b(a\.?\s?b\.?\s?s\.?)\b", "abs");
            q = Regex.Replace(q, @"\btrack(s)? limit(s|e|es)?\b", "track limits");
            return q;
        }

        private static int IndexOfWord(string q, string w)
        {
            Match m = Regex.Match(q, $@"\b{Regex.Escape(w)}\b");
            return m.Success ? m.Index : -1;
        }

        public static string Build(RaceSnapshot snap, string question = null, BriefingExtras extras = null) => Prepare(snap, question, extras).Facts;

        /// <summary>
        /// Question difficile : pas de réponse calculée, et une question longue, à plusieurs sujets, ou qui demande une analyse
        /// (« pourquoi », « qu'est-ce que je fais », « compare », « stratégie »…). Les questions simples restent sur le modèle habituel.
        /// </summary>
        public static bool IsHardQuestion(string question, BriefingResult brief)
        {
            if (brief?.Suggestion != null)
            {
                return false;
            }

            string t = RadioCommands.Normalize(question);
            int words = t.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            return words >= 14 || (brief?.Topics?.Count ?? 0) >= 2
                || Regex.IsMatch(t, @"\b(pourquoi|explique|expliquer|analyse|analyser|compare|comparer|strategie|que faire|qu est ce que je fais|tu conseilles|conseille moi|meilleure option|vaut le coup|quel plan|qu est ce qui est mieux)\b");
        }

        /// <summary>
        /// « Combien de temps prend un arrêt ? », « combien je perds au stand ? », « le temps de descendre aux stands » :
        /// le temps perdu par un arrêt (mesuré sur les arrêts vus, sinon estimé) et celui du dernier arrêt du pilote. Null sinon.
        /// </summary>
        public static string PitLossAnswer(string question, RaceSnapshot snap)
        {
            string t = RadioCommands.Normalize(question);
            bool pit = Regex.IsMatch(t, @"\b(arret|arrets|stand|stands|box|pit|pit stop|ravitaillement)\b");
            bool time = Regex.IsMatch(t, @"\b(combien de temps|temps perdu|temps que (ca|ça) (me |nous )?(fait|coute)|perte|perd|perds|perdre|coute|dure|duree|prend|descendre|passage)\b");
            if (!pit || !time || Regex.IsMatch(t, @"\b(quand|quel tour|dans combien de tours)\b"))
            {
                return null;
            }

            SecondMonitor.Hub.Core.Strategy.PitPlanDto plan = snap?.Player?.PitPlan;
            double loss = plan?.PitLoss ?? SecondMonitor.Hub.Core.Strategy.PitPlanner.DefaultPitLoss;
            string source = plan?.PitLossSource ?? "estimation, aucun arrêt vu pour l'instant";
            string text = $"Un arrêt te coûte environ {N(loss, 0)} secondes par rapport à un tour normal, entrée, arrêt et sortie des stands compris ({source}).";
            if (plan?.PlayerLastStopLoss is double mine && plan.PlayerLastStopLap is int lap)
            {
                text += $" Ton dernier arrêt, au tour {lap - 1} : {N(mine, 0)} secondes.";
            }

            return text;
        }

        public static BriefingResult Prepare(RaceSnapshot snap, string question, BriefingExtras extras = null)
        {
            if (snap == null || !snap.HasData)
            {
                return new BriefingResult { Facts = "Aucune donnée de course pour le moment (le jeu n'envoie rien).", Suggestion = "Je n'ai pas encore de données de course : le jeu n'envoie rien pour l'instant." };
            }

            extras ??= new BriefingExtras();
            var proposals = new List<(Topic Topic, Proposal Proposal)>();
            var sections = Collect(snap, extras, proposals);
            IReadOnlyList<Topic> topics = DetectTopics(question);
            string q = Corrected(question);

            string suggestion = null;
            Topic? main = topics.Where(sections.ContainsKey).Cast<Topic?>().FirstOrDefault();

            // Données que le jeu ne donne pas (essence ou setup d'un adversaire, réglages de la voiture) : on le dit tout de suite,
            // plutôt que de répondre à côté ou de laisser le modèle inventer.
            if (UnavailableData.IsMatch(q) && !(topics.Contains(Topic.Controls) && snap.Player?.Controls != null))
            {
                main = null;
                suggestion = Missing(question, snap, extras);
            }
            if (main.HasValue)
            {
                var lines = sections[main.Value];
                // « devant » / « derrière » : on privilégie la ligne qui en parle.
                bool behind = q.Contains("derrière") || q.Contains("derriere");
                bool ahead = q.Contains("devant");
                suggestion = Answer(lines, q, behind, ahead);

                // Plusieurs sujets dans la même question (« ma position, la météo et les prévisions ? ») : on répond à chacun.
                foreach (Topic other in topics.Where(t => t != main.Value && sections.ContainsKey(t)).Take(1))
                {
                    string more = Answer(sections[other], q, behind, ahead);
                    if (!string.IsNullOrWhiteSpace(more) && !suggestion.Contains(more.Trim(), StringComparison.Ordinal))
                    {
                        suggestion += " " + more;
                    }
                }

                suggestion = RadioFormat.WithoutRepeats(suggestion);
            }

            var sb = new StringBuilder();
            // Ce qui compte d'abord selon la session : la voiture en essais, le déroulé en course.
            Topic[] focus = snap.SessionType switch
            {
                "Practice" or "WarmUp" => new[] { Topic.Tyres, Topic.Controls, Topic.Car, Topic.Driving, Topic.Damage, Topic.Laps, Topic.Performance, Topic.Pace, Topic.Speed, Topic.Fuel },
                "Qualification" => new[] { Topic.Laps, Topic.Pace, Topic.Performance, Topic.Tyres, Topic.Speed, Topic.Session },
                _ => new[] { Topic.Position, Topic.Gap, Topic.Pace, Topic.Laps, Topic.Strategy, Topic.Pit, Topic.Fuel, Topic.Tyres, Topic.Damage },
            };
            var order = topics.Concat(focus.Except(topics)).Concat(Enum.GetValues<Topic>().Except(topics).Except(focus));
            sb.AppendLine("FICHE DE FAITS (chiffres exacts calculés par l'ordinateur) :");
            foreach (Topic topic in order.Where(sections.ContainsKey))
            {
                foreach (Fact f in sections[topic])
                {
                    sb.AppendLine("- " + f.Text);
                }
            }

            string carFacts = CarContext.From(snap, extras.WetThresholdPct).Describe();
            sb.AppendLine("- " + carFacts);

            string memory = extras.Memory?.Facts();
            if (memory != null)
            {
                sb.AppendLine().AppendLine(memory);
            }

            return new BriefingResult
            {
                Facts = sb.ToString().TrimEnd(),
                Suggestion = suggestion,
                Topics = topics.ToList(),
                Sections = sections.ToDictionary(kv => kv.Key, kv => kv.Value.Select(f => f.Text).ToList()),
                CarFacts = carFacts,
                MemoryFacts = memory,
                Proposal = main.HasValue ? proposals.FirstOrDefault(p => p.Topic == main.Value).Proposal : null,
            };
        }

        private static readonly Regex UnavailableData = new(
            @"(\b(num[ée]ro|n°|#)\s?\d+|\bdu \d+\b|\bson |\bsa |\bleurs? |adversaire|concurrent|voiture (devant|derri[èe]re)).*\b(essence|carburant|fuel|setup|r[ée]glages?|pression)|" +
            @"\b(essence|carburant|fuel|setup|r[ée]glages?)\b.*(\b(num[ée]ro|n°)\s?\d+|#\d+|\bdu \d+\b|adversaire|concurrent|de la voiture (devant|derri[èe]re))|" +
            @"\b(mon|ma|mes|quel|quelle|quels|quelles)\b.*\b(r[ée]glages?|setup)\b.*\b(aileron|carrossage|barres?|ressorts?|amortisseurs?|hauteurs?|diff[ée]rentiel|r[ée]partition|abs|tc)\b|" +
            @"\b(mon|ma|mes|quel|quelle)\b.*\b(aileron|carrossage|barres? antiroulis|ressorts?|amortisseurs?|hauteurs? de caisse|r[ée]partition de freinage|pr[ée]charge)\b.*\b(actuel|actuelle|actuels|r[ée]gl[ée]e?s?|mis|est (à|a) combien|combien)\b",
            RegexOptions.Compiled);

        private static readonly Regex ForecastWords = new(@"prévision|prevision|évolu|evolu|tendance|va (pleuvoir|changer|monter|baisser|sécher|secher)|vont|reste de la course|plus tard|dans \d+ min", RegexOptions.Compiled);

        // Sous-question précise dans un sujet (« mes pressions ? » dans les pneus) → étiquette de la ligne à donner.
        private static readonly (Regex Words, string Tag)[] SubTopics =
        {
            (new Regex(@"pression", RegexOptions.Compiled), "pression"),
            (new Regex(@"\busure|\bus[ée]s?\b|\bdur[ée]e de vie", RegexOptions.Compiled), "usure"),
            (new Regex(@"r[ée]partition|balance|migration|brake bias", RegexOptions.Compiled), "répartition"),
            (new Regex(@"\btc\b|traction|coupure", RegexOptions.Compiled), "tc"),
            (new Regex(@"\babs\b", RegexOptions.Compiled), "abs"),
            (new Regex(@"carto|mapping|\bmap\b", RegexOptions.Compiled), "cartographie"),
            (new Regex(@"mode|d[ée]ploy|d[ée]ploiement|regen|moteur [ée]lectrique", RegexOptions.Compiled), "mode"),
            (new Regex(@"ratio", RegexOptions.Compiled), "ratio"),
            (new Regex(@"frein", RegexOptions.Compiled), "freins"),
            (new Regex(@"moteur|\beau\b|huile", RegexOptions.Compiled), "moteur"),
            (new Regex(@"ressor|ressortir|sortie des stands", RegexOptions.Compiled), "ressortie"),
            (new Regex(@"carcasse|c(œ|oe)ur", RegexOptions.Compiled), "carcasse"),
            (new Regex(@"temp[ée]rature|chaud|froid|chauff", RegexOptions.Compiled), "température"),
            (new Regex(@"meilleur", RegexOptions.Compiled), "meilleur"),
            (new Regex(@"pire|plus mauvais|moins bon|plus lent", RegexOptions.Compiled), "pire"),
            (new Regex(@"dernier|précédent|precedent", RegexOptions.Compiled), "dernier"),
            (new Regex(@"r[ée]gul|constan", RegexOptions.Compiled), "régularité"),
            (new Regex(@"moyen", RegexOptions.Compiled), "moyenne"),
            (new Regex(@"\boù\b|\bou\b.*d[ée]ploi|o[uù] (je )?(d[ée]ploie|r[ée]colte|recharge)|quand (je )?d[ée]ploie", RegexOptions.Compiled), "où"),
        };

        /// <summary>Réponse à un sujet : la ligne la plus pertinente (devant / derrière / prévision / sous-question) et un complément au plus.</summary>
        private static string Answer(List<Fact> lines, string q, bool behind, bool ahead)
        {
            Fact forecast = ForecastWords.IsMatch(q) ? lines.FirstOrDefault(l => l.Tag == "prévision") : null;
            if (forecast != null)
            {
                // « La météo va évoluer ? » : la situation actuelle, puis la tendance.
                Fact current = lines.FirstOrDefault(l => !l.Companion);
                return current != null ? $"{current.Text} {forecast.Text}" : forecast.Text;
            }

            foreach (var (words, tag) in SubTopics)
            {
                Fact sub = words.IsMatch(q) ? lines.FirstOrDefault(l => l.Tag == tag) : null;
                if (sub != null)
                {
                    return sub.Text;
                }
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
                ?? lines.FirstOrDefault(l => !l.Companion && l.Tag == null)
                ?? lines.FirstOrDefault(l => !l.Companion)
                ?? lines.First();
            var parts = new List<string> { chosen.Text };
            parts.AddRange(lines.Where(l => l != chosen && l.Side == chosen.Side && l.Companion && l.Tag == null).Take(1).Select(l => l.Text));
            return string.Join(" ", parts);
        }

        private static Dictionary<Topic, List<Fact>> Collect(RaceSnapshot snap, BriefingExtras extras, List<(Topic, Proposal)> proposals)
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
                Add(Topic.Session, $"{me.Laps} tours bouclés, environ {N(Math.Ceiling(lr), 0)} restants.");
            }
            else if (me != null)
            {
                Add(Topic.Session, $"{me.Laps} tours bouclés.");
            }

            if (snap.LengthType == "Laps" && snap.TotalLaps > 0)
            {
                Add(Topic.Session, $"Course en {snap.TotalLaps} tours, le leader est dans le tour {snap.LeaderLap}.", companion: true);
            }
            else if (snap.TimeRemaining.HasValue)
            {
                Add(Topic.Session, $"Temps restant : {Duration(snap.TimeRemaining.Value)}.", companion: true);
            }

            if (snap.Flags.Count > 0)
            {
                Add(Topic.Session, "Drapeaux en cours : " + string.Join(", ", snap.Flags) + ".", companion: true);
            }

            if (!string.IsNullOrEmpty(snap.SafetyCar) && snap.SafetyCar != "None")
            {
                Add(Topic.Session, "Voiture de sécurité en piste.", companion: true);
            }

            bool alone = me != null && snap.Drivers.Count(d => d.ClassId == me.ClassId) <= 1 && snap.SessionType != "Race";
            if (me != null)
            {
                string gained = me.PositionsGained > 0 ? $", {me.PositionsGained} place{(me.PositionsGained > 1 ? "s" : string.Empty)} gagnée{(me.PositionsGained > 1 ? "s" : string.Empty)} depuis le départ"
                    : me.PositionsGained < 0 ? $", {-me.PositionsGained} place{(me.PositionsGained < -1 ? "s" : string.Empty)} perdue{(me.PositionsGained < -1 ? "s" : string.Empty)} depuis le départ" : string.Empty;
                Add(Topic.Position, alone ? "Tu es seul en piste dans ta catégorie : pas de classement à suivre."
                    : snap.IsMultiClass ? $"Tu es P{me.ClassPosition} dans ta catégorie ({me.ClassName}){gained}." : $"Tu es P{me.Position}{gained}.");

                var classmates = snap.Drivers.Where(d => d.ClassId == me.ClassId).ToList();
                int idx = classmates.IndexOf(me);
                DriverRow aheadInClass = idx > 0 ? classmates[idx - 1] : null;
                DriverRow behindInClass = idx >= 0 && idx < classmates.Count - 1 ? classmates[idx + 1] : null;

                if (alone)
                {
                    Add(Topic.Gap, "Tu es seul en piste dans ta catégorie : pas d'écart à donner.");
                }
                else if (aheadInClass != null)
                {
                    Add(Topic.Gap, $"Voiture devant : numéro {aheadInClass.Number} à {GapSecs(-(aheadInClass.GapToPlayer ?? double.NaN))}{Pace(me, aheadInClass, true)}{Contact(p, aheadInClass.Id)}.", Side.Ahead);
                }
                else
                {
                    Add(Topic.Gap, snap.IsMultiClass ? "Personne devant toi dans ta catégorie : tu es en tête." : "Tu es en tête, personne devant.", Side.Ahead);
                }

                if (alone)
                {
                }
                else if (behindInClass != null)
                {
                    Add(Topic.Gap, $"Voiture derrière : numéro {behindInClass.Number} à {GapSecs(behindInClass.GapToPlayer ?? double.NaN)}{Pace(me, behindInClass, false)}{Contact(p, behindInClass.Id)}.", Side.Behind);
                }
                else
                {
                    Add(Topic.Gap, "Personne derrière toi dans ta catégorie.", Side.Behind);
                }

                var onTrack = snap.Drivers.Where(d => !d.IsPlayer && !d.InPits && d.GapToPlayer.HasValue).ToList();
                DriverRow trackAhead = onTrack.Where(d => d.GapToPlayer < 0).OrderByDescending(d => d.GapToPlayer).FirstOrDefault();
                DriverRow trackBehind = onTrack.Where(d => d.GapToPlayer > 0).OrderBy(d => d.GapToPlayer).FirstOrDefault();
                // Autres catégories : seulement le trafic proche (moins de 5 secondes), le reste ne compte pas pour vous.
                if (trackAhead != null && trackAhead.ClassId != me.ClassId && -trackAhead.GapToPlayer <= 5)
                {
                    Add(Topic.Gap, $"Trafic : {trackAhead.ClassName} numéro {trackAhead.Number} juste devant, à {Sec(-trackAhead.GapToPlayer.Value)}.", Side.Ahead, companion: true);
                }

                if (trackBehind != null && trackBehind.ClassId != me.ClassId && trackBehind.GapToPlayer <= 5)
                {
                    Add(Topic.Gap, $"Trafic : {trackBehind.ClassName} numéro {trackBehind.Number} arrive derrière, à {Sec(trackBehind.GapToPlayer.Value)}.", Side.Behind, companion: true);
                }

                DriverRow classLeader = classmates.FirstOrDefault();
                if (classLeader != null && classLeader != me)
                {
                    Add(Topic.Position, $"Leader de ta catégorie : numéro {classLeader.Number} {classLeader.Name}, {GapText(me.GapToClassLeader, me.GapToClassLeaderLaps)} devant toi.");
                }

                if (snap.IsMultiClass)
                {
                    Add(Topic.Position, $"Au classement général (toutes catégories), tu es P{me.Position}.");
                }

                foreach (var (rival, side, label) in new[] { (aheadInClass, Side.Ahead, "devant"), (behindInClass, Side.Behind, "derrière") })
                {
                    if (rival == null)
                    {
                        Add(Topic.Opponents, side == Side.Ahead ? "Personne devant toi dans ta catégorie : tu es en tête." : "Personne derrière toi dans ta catégorie.", side);
                        continue;
                    }

                    string text = $"Voiture {label}, numéro {rival.Number} : pneus {Compound(rival.Tyres.Compound)} depuis {rival.Tyres.StintLaps} tours, {Stops(rival.PitStops)}";
                    text += rival.NextStop == null
                        ? ", prochain arrêt pas encore estimé."
                        : rival.NextStop.LapsUntil <= 1
                            ? ", arrêt attendu à tout moment (estimation)."
                            : $", prochain arrêt estimé au tour {rival.NextStop.Lap}, dans {rival.NextStop.LapsUntil} tours.";
                    Add(Topic.Opponents, text, side);
                }
            }

            if (p != null)
            {
                // Arrêt au stand (en premier : c'est la réponse à « je rentre quand ? »).
                int? planned = extras.Memory?.PlannedStopLap > me?.Laps ? extras.Memory.PlannedStopLap : null;
                if (extras.Memory?.NoStopPlanned == true && me != null)
                {
                    Add(Topic.Pit, "Pas d'arrêt prévu : on va au bout, comme décidé ensemble. Surveille les pneus.");
                }
                else if ((planned ?? p.RecommendedStopLap).HasValue && me != null)
                {
                    int stop = (planned ?? p.RecommendedStopLap).Value;
                    int inLaps = stop - me.Laps;
                    string when = inLaps <= 1 ? "ce tour-ci, box box" : $"dans {inLaps} tours";
                    string time = p.AverageLap is double avg && inLaps > 1 ? $", environ {Duration(Math.Max(0, inLaps - me.LapFraction) * avg)}" : string.Empty;
                    string why = planned.HasValue ? "comme décidé ensemble (relais allongé)" : Because(p.RecommendedStopReason);
                    Add(Topic.Pit, $"Rentre au stand au tour {stop}, {when}{time} : {why}.{Load(p)}");
                }
                else if (snap.SessionType == "Race" && p.Fuel?.Status == "ok" && (p.Energy == null || p.Energy.Status == "ok"))
                {
                    Add(Topic.Pit, "Pas besoin de rentrer au stand : tu as de quoi finir.");
                }
                else if (snap.SessionType == "Race")
                {
                    Add(Topic.Pit, "Arrêt pas encore calculable : il me faut un ou deux tours de consommation.");
                }

                if (p.Fuel != null)
                {
                    if (p.Fuel.PerLap.HasValue)
                    {
                        string fuel = $"Il te reste {N(p.Fuel.LapsLeft ?? 0, 1)} tours d'essence ({N(p.Fuel.Remaining, 1)} L, {N(p.Fuel.PerLap.Value, 2)} L/tour).";
                        if (p.Fuel.ToAdd <= 0)
                        {
                            fuel += " Assez pour finir.";
                        }
                        else if (p.Fuel.ToAdd.HasValue)
                        {
                            fuel += p.Fuel.StopsNeeded > 1
                                ? $" Il manque {N(p.Fuel.ToAdd.Value, 0)} L pour finir, soit {p.Fuel.StopsNeeded} arrêts."
                                : $" Il manque {N(p.Fuel.ToAdd.Value, 0)} L pour finir.";
                        }

                        Add(Topic.Fuel, fuel);
                    }
                    else
                    {
                        Add(Topic.Fuel, $"Il te reste {N(p.Fuel.Remaining, 1)} L ; consommation en cours de calcul, encore un tour.");
                    }
                }

                if (p.Energy != null)
                {
                    string e = p.Energy.PerLapPct.HasValue
                        ? $"Il te reste {N(p.Energy.LapsLeft ?? 0, 1)} tours d'énergie virtuelle ({N(p.Energy.RemainingPct, 0)} %, {N(p.Energy.PerLapPct.Value, 1)} %/tour)."
                        : $"Énergie virtuelle : {N(p.Energy.RemainingPct, 0)} % ; consommation en cours de calcul.";
                    Add(Topic.Energy, e);
                    if (p.Energy.LapsLeft is double veLaps && p.Fuel?.LapsLeft is double fuelLaps)
                    {
                        string limit = Math.Abs(veLaps - fuelLaps) < 0.5 ? "les deux sont équilibrés"
                            : veLaps < fuelLaps ? $"l'énergie virtuelle limite d'abord, {N(fuelLaps - veLaps, 1)} tour{(fuelLaps - veLaps >= 2 ? "s" : string.Empty)} d'essence en trop : on peut mettre moins d'essence à l'arrêt"
                            : $"l'essence limite d'abord, {N(veLaps - fuelLaps, 1)} tour{(veLaps - fuelLaps >= 2 ? "s" : string.Empty)} d'énergie en trop";
                        Add(Topic.Energy, $"Énergie virtuelle {N(veLaps, 1)} tours, essence {N(fuelLaps, 1)} tours : {limit}.", companion: true, tag: "ratio");
                    }
                }

                if (CarKnowledge.EnergyState(p.Controls, p.Hybrid) is string energyState)
                {
                    Add(Topic.Energy, energyState, companion: true, tag: "mode");
                    Add(Topic.Hybrid, energyState, companion: true, tag: "mode");
                }

                AddControlFacts(p.Controls, snap, (t, s2, tag) => Add(t, s2, tag: tag));
                if (extras.Memory?.PitPlan is PitOrder plan0 && !plan0.IsEmpty)
                {
                    Add(Topic.Pit, "Demandé pour l'arrêt : " + string.Join(", ", new[]
                    {
                        plan0.Tyres == null ? (plan0.NoTyres ? "pas de pneus" : null) : plan0.Tyres == "neufs" ? "pneus neufs" : $"{plan0.Tyres} neufs",
                        plan0.Repair ? "réparation" : null,
                        plan0.Fuel == null ? null : plan0.Fuel == "plein" ? "le plein" : "carburant jusqu'à la fin",
                    }.Where(x => x != null)) + ".", companion: true, tag: "demande");
                }

                if (p.Wheels.Count == 4)
                {
                    WheelStatus worst = p.Wheels.OrderByDescending(w => w.WearPct).First();
                    string t = $"{Cap(Compound(worst.Compound))}, {p.StintLaps} tour{(p.StintLaps > 1 ? "s" : string.Empty)}. Usure : {FourWheels(p.Wheels, w => w.WearPct, "%", 0)}.";
                    if (p.Wheels.All(w => w.TempMiddle.HasValue))
                    {
                        t += $" Températures : {FourWheels(p.Wheels, w => w.TempMiddle.Value, "degrés", 0)}.";
                    }

                    Add(Topic.Tyres, t);
                    Add(Topic.Tyres, $"Usure : {FourWheels(p.Wheels, w => w.WearPct, "%", 0)}." +
                        (worst.LapsToHeavyWear.HasValue ? $" Encore environ {N(worst.LapsToHeavyWear.Value, 0)} tours sur l'{Wheel(worst.Position)}." : string.Empty), tag: "usure");
                    var hot = p.Wheels.Where(w => w.TempMiddle.HasValue && w.IdealTemp.HasValue && w.TempMiddle > w.IdealTemp + 10).Select(w => Wheel(w.Position)).ToList();
                    var cold = p.Wheels.Where(w => w.TempMiddle.HasValue && w.IdealTemp.HasValue && w.TempMiddle < w.IdealTemp - 15).Select(w => Wheel(w.Position)).ToList();
                    Add(Topic.Tyres, hot.Count > 0 ? "Trop chauds : " + string.Join(", ", hot) + "." : cold.Count > 0 ? "Trop froids : " + string.Join(", ", cold) + "." : "Températures correctes.", companion: true, tag: "verdict");
                    if (p.Wheels.All(w => w.TempMiddle.HasValue))
                    {
                        Add(Topic.Tyres, Cap(FourWheels(p.Wheels, w => w.TempMiddle.Value, "degrés", 0)) + ".", tag: "température");
                    }

                    if (p.Wheels.All(w => w.Core.HasValue))
                    {
                        Add(Topic.Tyres, "Carcasse : " + FourWheels(p.Wheels, w => w.Core.Value, "degrés", 0) + ".", tag: "carcasse");
                    }

                    if (p.Wheels.All(w => w.Pressure.HasValue))
                    {
                        Add(Topic.Tyres, PressureLine(p.Wheels), tag: "pression");
                    }
                }

                if (p.Wheels.Count == 4 && p.Wheels.All(w => w.Brake.HasValue))
                {
                    WheelStatus hottest = p.Wheels.OrderByDescending(w => w.Brake).First();
                    Add(Topic.Car, "Freins : " + FourWheels(p.Wheels, w => w.Brake.Value, "degrés", 0) + ".", tag: "freins");
                }

                if (p.Engine is { } eng)
                {
                    var parts = new List<string>();
                    if (eng.WaterTemp.HasValue)
                    {
                        parts.Add($"eau {N(eng.WaterTemp.Value, 0)}°{Range(eng.WaterTemp.Value, eng.WaterIdeal, eng.WaterWindow)}");
                    }

                    if (eng.OilTemp.HasValue)
                    {
                        parts.Add($"huile {N(eng.OilTemp.Value, 0)}°{Range(eng.OilTemp.Value, eng.OilIdeal, eng.OilWindow)}");
                    }

                    if (parts.Count > 0)
                    {
                        Add(Topic.Car, "Moteur : " + string.Join(", ", parts) + ".", tag: "moteur");
                    }
                }

                if (p.Degradation.HasValue)
                {
                    Add(Topic.Tyres, p.Degradation.Value >= 0
                        ? $"Rythme en baisse de {N(p.Degradation.Value, 2)} s par tour sur ce relais."
                        : $"Rythme en hausse de {N(-p.Degradation.Value, 2)} s par tour sur ce relais (la voiture s'allège).", tag: "dégradation");
                }
            }

            if (me != null)
            {
                AddLapFacts(snap, me, extras, (t, s, tag) => Add(t, s, tag: tag));
                Proposal speedProposal = AddSpeedFacts(snap, me, extras.Speeds, (t, s, c) => Add(t, s, companion: c));
                if (speedProposal != null)
                {
                    proposals.Add((Topic.Speed, speedProposal));
                }

                AddPerformanceFacts(snap, me, extras, (t, s) => Add(t, s));
                AddDrivingFacts(extras, (t, s) => Add(t, s));
                AddRainFacts(snap, me, extras, (t, s) => Add(t, s));
            }

            foreach (var (text, tag) in HybridAdvisor.Facts(snap, extras.Speeds))
            {
                Add(Topic.Hybrid, text, tag: tag);
            }

            if (snap.Weather.Air.HasValue)
            {
                Add(Topic.Weather, $"Air {N(snap.Weather.Air.Value, 0)}°, piste {N(snap.Weather.TrackTemp ?? 0, 0)}°, {(snap.Weather.Rain > 0 ? $"pluie à {snap.Weather.Rain} %" : "pas de pluie")}{(snap.Weather.Wetness > 0 ? $", piste humide à {snap.Weather.Wetness} %" : string.Empty)}.");
            }

            if (!string.IsNullOrEmpty(snap.Weather.Trend))
            {
                Add(Topic.Weather, snap.Weather.Trend + " (tendance, pas la prévision officielle du jeu).", companion: true);
            }

            if (snap.Weather.Wind.HasValue)
            {
                Add(Topic.Weather, $"Vent {Kph(snap.Weather.Wind.Value)}.", companion: true);
            }

            AddForecastFacts(extras.Weather, (t, s, c, tag) => Add(t, s, companion: c, tag: tag));

            if (p?.PitPlan is SecondMonitor.Hub.Core.Strategy.PitPlanDto plan)
            {
                foreach (var cut in plan.Cuts.OrderByDescending(c => c.Feasible))
                {
                    Add(Topic.Strategy, cut.Text, cut.Kind == "menace-undercut" ? Side.Behind : Side.Ahead);
                }

                if (plan.RejoinNow?.Text is string rejoin)
                {
                    Add(Topic.Strategy, rejoin, companion: true);
                    Add(Topic.Pit, rejoin, companion: true, tag: "ressortie");
                }

                Add(Topic.Strategy, $"Un arrêt coûte environ {N(plan.PitLoss, 0)} s, {plan.PitLossSource}" +
                    (plan.FreshTyreGain is double fg ? $" ; les pneus neufs font gagner environ {N(fg, 1)} s par tour ({plan.FreshTyreGainSource})." : "."), companion: true);
            }

            if (p?.Damage is { Count: > 0 } damageParts)
            {
                var hurt = damageParts.Where(d => d.Percent >= 1).OrderByDescending(d => d.Percent).ToList();
                Add(Topic.Damage, hurt.Count == 0
                    ? "Aucun dégât sur la voiture."
                    : "Dégâts : " + string.Join(", ", hurt.Take(5).Select(d => $"{d.Label} {N(d.Percent, 0)} %")) + ".");
                if (p.DamageEvent is { LossPerLap: double loss } ev && loss > 0.1)
                {
                    double left = p.LapsRemaining ?? 0;
                    double pitLoss = p.PitPlan?.PitLoss ?? SecondMonitor.Hub.Core.Strategy.PitPlanner.DefaultPitLoss;
                    string verdict = left <= 0 ? string.Empty
                        : loss * left > pitLoss + 5
                            ? $" Sur {N(Math.Floor(left), 0)} tours : {N(loss * left, 0)} s, plus qu'un arrêt ({N(pitLoss, 0)} s) : réparer est rentable."
                            : $" Sur {N(Math.Floor(left), 0)} tours : {N(loss * left, 0)} s, moins qu'un arrêt ({N(pitLoss, 0)} s) : on continue.";
                    Add(Topic.Damage, $"Le dégât ({ev.Label}) coûte {N(loss, 2)} s par tour.{verdict}", companion: true);
                }
            }

            if (me != null)
            {
                AddPaceFacts(snap, me, p, extras, (t, s, side, tag) => Add(t, s, side, tag: tag));
            }

            if (p?.TyreThermal is { Count: > 0 } thermal)
            {
                var avg = thermal.Where(t => t.LastLapAverage.HasValue).ToList();
                if (avg.Count > 0)
                {
                    Add(Topic.Tyres, "Moyenne de surface sur le dernier tour : " + string.Join(", ", avg.Select(t => $"{Wheel(t.Position)} {N(t.LastLapAverage.Value, 0)}°")) +
                        (avg[0].Min.HasValue ? $", plage idéale {N(avg[0].Min.Value, 0)} à {N(avg[0].Max ?? 0, 0)}°." : "."), companion: true, tag: "moyenne-tour");
                }

                foreach (string alert in p.Alerts ?? new List<string>())
                {
                    Add(Topic.Tyres, alert.Replace("–", " à "), companion: true, tag: "alerte");
                }
            }

            return sections;
        }

        /// <summary>« avant gauche 80 degrés, avant droit 90, arrière gauche 50, arrière droit 55 » : l'unité une seule fois.</summary>
        private static string FourWheels(List<WheelStatus> wheels, Func<WheelStatus, double> value, string unit, int digits)
        {
            var ordered = new[] { "FL", "FR", "RL", "RR" }.Select(pos => wheels.FirstOrDefault(w => w.Position == pos)).Where(w => w != null).ToList();
            return string.Join(", ", ordered.Select((w, i) => $"{Wheel(w.Position)} {N(value(w), digits)}{(i == 0 ? (unit == "%" ? " %" : " " + unit) : string.Empty)}"));
        }

        /// <summary>Réglages en roulant donnés par le jeu (répartition, TC, ABS, cartographie, regen), limites de piste et pénalités.</summary>
        private static void AddControlFacts(ControlsDto c, RaceSnapshot snap, Action<Topic, string, string> add)
        {
            if (c == null)
            {
                add(Topic.Controls, "Le jeu n'envoie pas les réglages de la voiture (répartition, TC, cartographie) : regarde l'écran du volant.", null);
                return;
            }

            if (CarKnowledge.CurrentSettings(c) is string all)
            {
                add(Topic.Controls, all, null);
            }

            if (c.FrontBrakeBiasPct is double b)
            {
                add(Topic.Controls, $"Répartition {N(b, 1)} % sur l'avant" + (c.Migration is int mig ? $", migration {mig}{(c.MigrationMax is int mm ? $" sur {mm}" : string.Empty)}." : "."), "répartition");
            }

            if (c.Tc is int tc)
            {
                string text = tc == 0 ? "TC coupé" : $"TC {tc}{(c.TcMax is int tm ? $" sur {tm}" : string.Empty)}";
                text += c.TcCut is int cut ? $", coupure {cut}" : string.Empty;
                text += c.TcSlip is int slip ? $", glissement {slip}" : string.Empty;
                add(Topic.Controls, text + ".", "tc");
            }
            else
            {
                add(Topic.Controls, "Pas de TC réglable transmis pour cette voiture.", "tc");
            }

            add(Topic.Controls, c.Abs is int abs && c.AbsMax > 0 ? $"ABS {abs} sur {c.AbsMax}." : "Pas d'ABS sur cette voiture.", "abs");
            if (c.MotorMap is int map)
            {
                add(Topic.Controls, $"Cartographie {map}{(c.MotorMapMax is int mm2 ? $" sur {mm2}" : string.Empty)}.", "cartographie");
            }
            else
            {
                add(Topic.Controls, "Pas de cartographie réglable transmise pour cette voiture.", "cartographie");
            }

            if (CarKnowledge.EnergyState(c, snap.Player?.Hybrid) is string energy)
            {
                add(Topic.Controls, energy, "mode");
            }

            if (c.TrackLimitsPoints is double pts)
            {
                string limit = c.TrackLimitsPointsPerPenalty is double max && max > 0
                    ? $"Limites de piste : {N(pts, 0)} point{(pts >= 2 ? "s" : string.Empty)} sur {N(max, 0)} avant pénalité."
                    : $"Limites de piste : {N(pts, 0)} point{(pts >= 2 ? "s" : string.Empty)}.";
                add(Topic.Rules, limit + (c.Penalties is int pen && pen > 0 ? $" {pen} pénalité{(pen > 1 ? "s" : string.Empty)} à purger." : " Aucune pénalité."), null);
            }
            else if (c.Penalties is int pen2)
            {
                add(Topic.Rules, pen2 > 0 ? $"{pen2} pénalité{(pen2 > 1 ? "s" : string.Empty)} à purger." : "Aucune pénalité.", null);
            }

            if (c.LapInvalidated)
            {
                add(Topic.Rules, "Le tour en cours est annulé par le jeu.", null);
            }
        }

        /// <summary>Pilotage : pédales du dernier tour et usures brutales récentes.</summary>
        private static void AddDrivingFacts(BriefingExtras extras, Action<Topic, string> add)
        {
            if (extras.DrivingSummary != null)
            {
                add(Topic.Driving, extras.DrivingSummary);
            }

            var spikes = extras.WearSpikes?.TakeLast(3).ToList();
            if (spikes is { Count: > 0 })
            {
                add(Topic.Driving, "Usures brutales récentes : " + string.Join(" ; ", spikes.Select(x =>
                    $"tour {x.Lap}, {Wheel(x.Wheel)} +{N(x.DeltaPct, 1)} %{(x.Corner != null ? ", " + x.Corner.ToLowerInvariant() : string.Empty)}, en {x.Phase}")) + ".");
            }
            else if (extras.DrivingSummary == null)
            {
                add(Topic.Driving, "Pas encore de tour complet enregistré pour analyser le pilotage : encore un tour.");
            }
        }

        private static string PressureLine(List<WheelStatus> wheels)
        {
            string list = string.Join(", ", wheels.Select(w => $"{Wheel(w.Position)} {N(w.Pressure.Value / 100, 2)}"));
            var off = wheels.Where(w => w.IdealPressure.HasValue && Math.Abs(w.Pressure.Value - w.IdealPressure.Value) >= 5).ToList();
            string verdict;
            if (!wheels.Any(w => w.IdealPressure.HasValue))
            {
                verdict = string.Empty;
            }
            else if (off.Count == 0)
            {
                verdict = $" Dans la cible ({N(wheels.First(w => w.IdealPressure.HasValue).IdealPressure.Value / 100, 2)} bar).";
            }
            else
            {
                WheelStatus w = off.OrderByDescending(x => Math.Abs(x.Pressure.Value - x.IdealPressure.Value)).First();
                double d = w.Pressure.Value - w.IdealPressure.Value;
                verdict = $" {Cap(Wheel(w.Position))} trop {(d > 0 ? "haut" : "bas")} de {N(Math.Abs(d), 0)} kPa{(off.Count > 1 ? $" ({off.Count} pneus hors cible)" : string.Empty)}.";
            }

            return $"Pressions : {list} bar.{verdict}";
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
                add(Topic.Weather, "Prévision : pas encore assez d'historique, il faut quelques minutes.", true, "prévision");
                return;
            }

            WeatherSample last = w.Forecast[^1];
            WeatherSample first = w.History.Count > 0 ? w.History[^1] : w.Forecast[0];
            double minutes = Math.Max(1, Math.Round((last.T - first.T) / 60));
            var parts = new List<string>();
            if (last.Track.HasValue && first.Track.HasValue)
            {
                double d = last.Track.Value - first.Track.Value;
                parts.Add(Math.Abs(d) < 1 ? $"piste stable vers {N(last.Track.Value, 0)}°" : $"piste à {N(last.Track.Value, 0)}° ({(d > 0 ? "+" : "-")}{N(Math.Abs(d), 0)})");
            }

            parts.Add(last.Rain > 0 ? $"pluie vers {last.Rain} %" : "pas de pluie");
            if (last.Wetness > 0)
            {
                parts.Add($"piste humide à {last.Wetness} %");
            }

            add(Topic.Weather, $"Dans {N(minutes, 0)} min : {string.Join(", ", parts)} (tendance calculée).", true, "prévision");
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
                add(Topic.Pace, "Pas encore de tour complet pour calculer ton rythme : encore un tour.", Side.None, null);
                return;
            }

            string deg = p?.Degradation is double dg && Math.Abs(dg) >= 0.05 ? (dg > 0 ? $", en baisse de {N(dg, 2)} s par tour" : $", en hausse de {N(-dg, 2)} s par tour") : string.Empty;
            add(Topic.Pace, $"Ton rythme est de {Lap(myPace, 1)} (meilleur tour {Lap(me.BestLap)}){deg}.", Side.None, null);

            var cls = snap.Drivers.Where(d => d.ClassId == me.ClassId && d.FinishStatus is not ("Dnf" or "Dq")).OrderBy(d => d.ClassPosition).ToList();
            if (snap.SessionType != "Race")
            {
                DriverRow best = cls.Where(d => d.BestLap > 0).OrderBy(d => d.BestLap).FirstOrDefault();
                if (best != null && me.BestLap > 0 && best.Id != me.Id)
                {
                    add(Topic.Pace, $"Meilleur tour de la catégorie : {Lap(best.BestLap)} par le numéro {best.Number}, tu es à {Sec(me.BestLap.Value - best.BestLap.Value)}.", Side.None, "objectif-devant");
                }

                var ms = extras.MicroSectors?.Drivers.FirstOrDefault(d => d.Id == me.Id);
                if (ms?.TheoreticalBest is double theo && me.BestLap > theo)
                {
                    add(Topic.Pace, $"Meilleur tour théorique (tes meilleurs morceaux) : {Lap(theo)}, soit {Sec(me.BestLap.Value - theo)} à gagner.", Side.None, "objectif-gagner");
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
                    string keep = lapsLeft is double ll && ll >= 1 ? $" Pour rester devant, ne tourne pas plus lentement que {Lap(theirs + (g / ll), 1)}." : string.Empty;
                    add(Topic.Pace, $"Tu mènes de {Sec(g)} sur le numéro {next.Number}, qui tourne en {Lap(theirs, 1)}.{keep}", Side.None, "objectif-devant");
                }
                else
                {
                    add(Topic.Pace, "Tu mènes : garde ce rythme.", Side.None, "objectif-devant");
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
                    ? $" Tu lui reprends déjà {Sec(theirs - myPace)} par tour."
                    : theirs < myPace - 0.05 ? $" Pour l'instant il te reprend {Sec(myPace - theirs)} par tour." : " Même rythme pour l'instant.";
                if (lapsLeft is double ll && ll >= 1)
                {
                    double need = gap / ll;
                    add(Topic.Pace, $"{goal} (numéro {rival.Number}, {Sec(gap)}) en {N(Math.Floor(ll), 0)} tours : tourne en {Lap(theirs - need, 1)}, {Sec(need)} par tour plus vite que lui.{now}", Side.None, tag);
                }
                else
                {
                    add(Topic.Pace, $"{goal} : le numéro {rival.Number} est à {Sec(gap)} et tourne en {Lap(theirs, 1)}.{now}", Side.None, tag);
                }
            }

            // Deuxième : rattraper la voiture devant, c'est aussi gagner.
            Target(cls[idx - 1], idx == 1 ? "Pour gagner, rattrape le leader" : "Pour rattraper la voiture devant", idx == 1 ? "objectif-gagner" : "objectif-devant");
            if (idx > 1)
            {
                Target(cls[0], "Pour gagner", "objectif-gagner");
            }
        }

        private static string Load(PlayerStrategy p)
        {
            var parts = new List<string>();
            if (p.Fuel?.NextStopAdd > 0.5)
            {
                parts.Add($"{N(p.Fuel.NextStopAdd.Value, 0)} L");
            }

            if (p.Energy?.NextStopAddPct > 0.5)
            {
                parts.Add($"{N(p.Energy.NextStopAddPct.Value, 0)} % d'énergie");
            }

            return parts.Count == 0 ? string.Empty : " On mettra " + string.Join(" et ", parts) + ".";
        }

        private static string Because(string reason) => reason switch
        {
            "carburant" => "le carburant",
            "énergie virtuelle" => "l'énergie virtuelle",
            "usure des pneus" => "l'usure des pneus",
            _ => reason,
        };

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
            return b.Side == "ahead" ? $", dans ses roues {when}" : $", dans tes roues {when}";
        }

        private static string Stops(int n) => n == 0 ? "aucun arrêt" : n == 1 ? "un arrêt" : $"{n} arrêts";

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
                return ", même rythme";
            }

            if (otherAhead)
            {
                return diff > 0 ? $" ; tu lui reprends {Sec(diff)} par tour" : $" ; il s'échappe de {Sec(-diff)} par tour";
            }

            return diff < 0 ? $" ; il revient de {Sec(-diff)} par tour" : $" ; tu le distances de {Sec(diff)} par tour";
        }

        private static string GapText(double? seconds, int laps) => laps > 0 ? $"{laps} tour{(laps > 1 ? "s" : string.Empty)}" : GapSecs(seconds ?? double.NaN);

        private static string GapSecs(double s) => double.IsNaN(s) ? "un écart inconnu" : Sec(s);

        private static void AddPerformanceFacts(RaceSnapshot snap, DriverRow me, BriefingExtras extras, Action<Topic, string> add)
        {
            MicroSectorReportDto ms = extras.MicroSectors;
            DriverMicroSectorsDto mine = ms?.Drivers.FirstOrDefault(d => d.Id == me.Id);
            if (mine == null || !ms.ClassBest.TryGetValue(me.ClassId ?? string.Empty, out double?[] classBest))
            {
                add(Topic.Performance, "Pas encore assez de tours pour comparer tes secteurs : il en faut deux complets.");
                return;
            }

            // Seul dans sa catégorie : le meilleur de la catégorie, c'est soi. On compare le dernier tour à ses meilleurs morceaux.
            if (snap.Drivers.Count(d => d.ClassId == me.ClassId) <= 1 && mine.Last != null)
            {
                var own = Enumerable.Range(0, Math.Min(ms.Count, mine.Last.Length))
                    .Where(i => mine.Last[i].HasValue && mine.Best[i].HasValue && !(mine.LastPit?.ElementAtOrDefault(i) ?? false))
                    .Select(i => (Index: i, Loss: mine.Last[i].Value - mine.Best[i].Value))
                    .ToList();
                var worst = own.OrderByDescending(l => l.Loss).Take(2).Where(l => l.Loss > 0.05).ToList();
                add(Topic.Performance, worst.Count == 0
                    ? "Dernier tour au niveau de tes meilleurs micro-secteurs partout."
                    : $"Sur le dernier tour, tu perds le plus {string.Join(" et ", worst.Select(l => $"au micro-secteur {l.Index + 1}{Near(l.Index, ms, extras.Speeds)} ({Sec(l.Loss)})"))} par rapport à tes meilleurs passages.");
                if (mine.TheoreticalBest.HasValue && me.BestLap.HasValue && me.BestLap - mine.TheoreticalBest > 0.05)
                {
                    add(Topic.Performance, $"Tour idéal (tes meilleurs morceaux) : {Lap(mine.TheoreticalBest)}, {Sec(me.BestLap.Value - mine.TheoreticalBest.Value)} sous ton meilleur tour.");
                }

                return;
            }

            var losses = Enumerable.Range(0, ms.Count)
                .Where(i => mine.Best[i].HasValue && classBest[i].HasValue)
                .Select(i => (Index: i, Loss: mine.Best[i].Value - classBest[i].Value))
                .ToList();
            if (losses.Count < ms.Count / 2)
            {
                add(Topic.Performance, "Pas encore assez de tours pour comparer tes secteurs : il en faut deux complets.");
                return;
            }

            double total = losses.Sum(l => l.Loss);
            var top = losses.OrderByDescending(l => l.Loss).Take(2).Where(l => l.Loss > 0.03).ToList();
            if (top.Count == 0)
            {
                add(Topic.Performance, "Tu es au niveau des meilleurs de ta catégorie sur tout le tour.");
            }
            else
            {
                string where = string.Join(" et ", top.Select(l => $"au micro-secteur {l.Index + 1}{Near(l.Index, ms, extras.Speeds)} ({Sec(l.Loss)})"));
                add(Topic.Performance, $"Tu perds le plus {where}. Total : {Sec(total)} sur un tour idéal de la catégorie.");
            }

            if (mine.TheoreticalBest.HasValue && me.BestLap.HasValue && me.BestLap - mine.TheoreticalBest > 0.05)
            {
                add(Topic.Performance, $"En enchaînant tes meilleurs secteurs : {Sec(me.BestLap.Value - mine.TheoreticalBest.Value)} de mieux sur ton meilleur tour.");
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
                    ? $"La piste sèche ({wet} % d'humidité) : on peut repasser en pneus secs."
                    : $"Reste en pneus pluie : piste mouillée à {wet} %, pluie à {rain} %.";
            }
            else if (wet >= threshold)
            {
                text = $"Passe en pneus pluie : piste mouillée à {wet} %, au-dessus du seuil de {threshold} %.";
            }
            else if (rain == 0 && wet == 0)
            {
                text = "Pas de pluie : reste en pneus secs.";
            }
            else
            {
                text = $"Pas encore de pneus pluie : piste humide à {wet} %, pluie à {rain} % (seuil {threshold} %).";
                if (perHour > 0.5)
                {
                    double minutes = (threshold - wet) / perHour.Value * 60;
                    text += minutes <= 90 ? $" Au rythme actuel, ce serait dans environ {N(Math.Max(1, minutes), 0)} min." : " Pas avant longtemps au rythme actuel.";
                }
                else
                {
                    text += " L'humidité n'augmente pas.";
                }
            }

            add(Topic.Rain, text);
        }

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

        /// <summary>Tous les tours du joueur (meilleur / pire / dernier / moyenne / régularité). Null = les 12 derniers de l'instantané.</summary>
        public List<LapDto> Laps { get; set; }

        public int WetThresholdPct { get; set; } = 25;

        /// <summary>Résumé des pédales du dernier tour (DrivingWatch).</summary>
        public string DrivingSummary { get; set; }

        /// <summary>Usures brutales de la session (DrivingWatch).</summary>
        public List<WearSpike> WearSpikes { get; set; }
    }

    public sealed class BriefingResult
    {
        public string Facts { get; set; }

        /// <summary>Réponse préparée par le hub pour une question reconnue ; null pour une question libre.</summary>
        public string Suggestion { get; set; }

        public List<RaceBriefing.Topic> Topics { get; set; } = new();

        /// <summary>Faits par sujet (pour les spécialistes, qui ne reçoivent que les leurs).</summary>
        public Dictionary<RaceBriefing.Topic, List<string>> Sections { get; set; } = new();

        /// <summary>Comment marche la voiture et ce qui se règle en roulant.</summary>
        public string CarFacts { get; set; }

        public string MemoryFacts { get; set; }

        /// <summary>Proposition faite dans la réponse (ex. un cran d'aileron en moins), à relier aux « oui » / « non » du pilote.</summary>
        public Proposal Proposal { get; set; }
    }
}
