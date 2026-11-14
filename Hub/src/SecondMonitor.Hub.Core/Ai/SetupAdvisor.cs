namespace SecondMonitor.Hub.Core.Ai
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Base de connaissances « symptôme → réglages » : sous-virage et survirage selon la phase du virage, blocages, roue intérieure,
    /// patinage, instabilité au freinage, rebonds, pneus trop chauds ou froids, freins, pluie. La réponse est immédiate (pas de modèle) :
    /// d'abord ce qui se règle en roulant sur cette voiture, puis le pilotage, puis le setup au stand.
    /// </summary>
    public static class SetupAdvisor
    {
        // ───── Propositions ─────
        private static readonly Dictionary<string, Proposal> P = new[]
        {
            Course("bias-back", "reculer la répartition de freinage d'un cran", "recule la répartition de freinage d'un cran", "répartition de freinage reculée"),
            Course("bias-fwd", "avancer la répartition de freinage d'un cran", "avance la répartition de freinage d'un cran", "répartition de freinage avancée"),
            Course("abs-up", "monter l'ABS d'un cran", "monte l'ABS d'un cran", "ABS plus haut"),
            Course("tc-up", "monter le TC d'un cran", "monte le TC d'un cran", "TC plus haut"),
            Course("map-soft", "passer sur une cartographie moteur plus douce", "passe sur une cartographie moteur plus douce", "cartographie moteur plus douce"),
            Course("diff-open", "baisser le différentiel à l'accélération de quelques points", "baisse le différentiel à l'accélération de quelques points", "différentiel à l'accélération plus ouvert"),
            Course("diff-lock", "monter le différentiel à l'accélération de quelques points", "monte le différentiel à l'accélération de quelques points", "différentiel à l'accélération plus bloqué"),
            Drive("trail-brake", "relâcher le frein plus progressivement en tournant", "relâche le frein plus progressivement en tournant", "freinage dégressif en entrée"),
            Drive("inside-release", "relâcher le frein plus tôt quand tu braques, la roue intérieure se déleste", "relâche le frein plus tôt quand tu braques, la roue intérieure se déleste", "relâcher le frein plus tôt en braquant"),
            Drive("brake-straight", "freiner un peu plus tôt et plus droit", "freine un peu plus tôt et plus droit", "freiner plus tôt et en ligne droite"),
            Drive("entry-slower", "entrer un peu moins vite pour garder l'avant", "entre un peu moins vite pour garder l'avant", "vitesse d'entrée plus basse"),
            Drive("throttle-smooth", "accélérer plus progressivement, volant plus ouvert", "accélère plus progressivement, volant plus ouvert", "accélération plus progressive"),
            Drive("throttle-later", "attendre que la voiture soit en ligne pour accélérer à fond", "attends que la voiture soit en ligne pour accélérer à fond", "accélérer plus tard"),
            Drive("downshift-slow", "rétrograder moins vite au freinage", "rétrograde moins vite au freinage", "rétrogradages plus lents"),
            Drive("short-shift", "passer un rapport plus haut en sortie des virages lents", "passe un rapport plus haut en sortie des virages lents", "rapport plus haut en sortie"),
            Drive("kerbs-avoid", "éviter les vibreurs hauts", "évite les vibreurs hauts", "éviter les vibreurs"),
            Drive("less-slide", "moins faire glisser la voiture, surtout en entrée", "faites moins glisser la voiture, surtout en entrée", "moins de glisse"),
            Drive("warm-tyres", "attaquer les freinages et faire des zigzags pour chauffer les pneus", "attaque les freinages et faites des zigzags pour chauffer les pneus", "chauffer les pneus"),
            Drive("brake-cool", "freiner un peu plus tôt et sortir de l'aspiration pour refroidir les freins", "freine un peu plus tôt et sors de l'aspiration pour refroidir les freins", "freins à refroidir"),
            Drive("wet-line", "quitter la trajectoire sèche : sous la pluie, l'extérieur accroche mieux", "quitte la trajectoire sèche : sous la pluie, l'extérieur accroche mieux", "trajectoire pluie"),
            Setup("arb-front-soft", "barre antiroulis avant plus souple"),
            Setup("arb-front-stiff", "barre antiroulis avant plus raide"),
            Setup("arb-rear-soft", "barre antiroulis arrière plus souple"),
            Setup("arb-rear-stiff", "barre antiroulis arrière plus raide"),
            Setup("springs-front-soft", "ressorts avant plus souples"),
            Setup("springs-rear-soft", "ressorts arrière plus souples"),
            Setup("springs-soft", "ressorts et barres plus souples"),
            Setup("wing-rear-more", "un cran d'aileron arrière en plus"),
            Setup("wing-rear-less", "un cran d'aileron arrière en moins"),
            Setup("wing-front-more", "un cran d'aileron avant en plus"),
            Setup("camber-front-more", "un peu plus de carrossage négatif à l'avant"),
            Setup("diff-preload-less", "moins de précharge au différentiel"),
            Setup("diff-coast-more", "différentiel au lever de pied plus bloqué"),
            Setup("diff-power-less", "différentiel à l'accélération moins bloqué"),
            Setup("brake-pressure-less", "pression de freinage un peu plus basse"),
            Setup("pressures-lower", "pressions à froid un peu plus basses"),
            Setup("ducts-open", "écopes de freins plus ouvertes"),
            Setup("ducts-close", "écopes de freins plus fermées"),
            Setup("ride-height-up", "hauteur de caisse un peu plus haute"),
            Setup("bump-soft", "amortisseurs plus souples en compression rapide"),
            Setup("front-pressure-lower", "pression des pneus avant un peu plus basse"),
        }.ToDictionary(p => p.Key);

        private sealed record Symptom(string Key, string Label, Regex Pattern, string Category);

        // Ordre = priorité de détection (le plus précis d'abord).
        private static readonly Symptom[] Symptoms =
        {
            new("lock-inside", "Blocage de la roue intérieure", R(@"\b(bloqu\w*|blocage|meplat)\b.*\binterieur\w*|\binterieur\w*.*\b(bloqu\w*|blocage)"), ExchangeCategorizer.RaceSettings),
            new("lock-rear", "Blocage de l'arrière", R(@"\b(bloqu\w*|blocage)\b.*\b(arriere|roues ar)\b|\b(arriere)\b.*\b(bloqu\w*|blocage)\b"), ExchangeCategorizer.RaceSettings),
            new("lock-front", "Blocage de l'avant", R(@"\b(bloqu\w*|blocage|meplat|flat spot|plat sur le pneu)\b"), ExchangeCategorizer.RaceSettings),
            new("brake-unstable", "Voiture instable au freinage", R(@"\b(instable|louvoie|se dandine|se tortille|bouge|danse)\b.*\b(freinage|freine|freins)\b|\b(freinage|freine)\b.*\b(instable|louvoie|bouge)\b|\barriere\b.*\b(se derobe|derobe|decroche|chasse)\b.*\bfrein"), ExchangeCategorizer.RaceSettings),
            new("wheelspin", "Patinage en sortie", R(@"\b(patin\w*|wheelspin|ca cire|cire|motricite|traction|les roues tournent dans le vide)\b"), ExchangeCategorizer.RaceSettings),
            new("understeer", "Sous-virage", R(@"\bsous ?vir\w*|\bunder ?steer|\bl avant (glisse|decroche|pousse|ne tourne pas|tourne pas|part|ne veut pas tourner|n accroche pas|accroche pas)|\bca pousse\b|\bpousse (de l avant|tout droit)|\btire tout droit|\bne tourne pas\b|\btourne pas\b|\bpas de grip a l avant|\bmanque de grip a l avant|\bmanque d avant"), ExchangeCategorizer.CarSetup),
            new("oversteer", "Survirage", R(@"\bsurvir\w*|\bover ?steer|\bl arriere (glisse|decroche|part|chasse|se derobe|est instable|bouge|est vivant|est nerveux)|\bca chasse\b|\btete a queue|\btoupie\b|\bpas de grip a l arriere|\bmanque de grip a l arriere"), ExchangeCategorizer.CarSetup),
            new("bouncing", "Rebonds", R(@"\b(rebond\w*|saute|sautille|talonn\w*|tape le sol|touche le sol|porpois\w*|marsouin\w*|trop raide sur les vibreurs|bosses?)\b"), ExchangeCategorizer.CarSetup),
            new("brakes-hot", "Freins trop chauds", R(@"\bfrein\w*\b.*\b(chaud|chauffe\w*|surchauf\w*|fading|fade|spongieux|pedale longue)\b|\b(surchauf\w*|chauffe\w*)\b.*\bfrein"), ExchangeCategorizer.RaceSettings),
            new("tyres-cold", "Pneus pas assez chauds", R(@"\bpneus?\b.*\b(froids?|pas chauds?|chauffent pas|chauffe pas|montent pas|monte pas)\b|\b(froids?)\b.*\bpneus?\b"), ExchangeCategorizer.CarSetup),
            new("tyres-hot", "Pneus trop chauds", R(@"\bpneus?\b.*\b(trop chauds?|chauffent|chauffe|surchauf\w*|brul\w*|cuits?)\b|\b(surchauf\w*)\b.*\bpneus?\b"), ExchangeCategorizer.CarSetup),
            new("rain", "Manque de grip sous la pluie", R(@"\baquaplan\w*|\bflaques?\b|\b(glisse|patine|grip|accroche)\b.*\b(pluie|mouill\w*|humide)\b|\b(pluie|mouill\w*)\b.*\b(glisse|grip|accroche pas)\b"), ExchangeCategorizer.RaceSettings),
            new("no-grip", "Manque de grip", R(@"\b(pas de grip|aucun grip|zero grip|plus de grip|ca glisse partout|glisse de partout|glisse partout|pas d adherence|plus d adherence|ca glisse|elle glisse|la voiture glisse)\b"), ExchangeCategorizer.Strategy),
        };

        // Propositions par symptôme et par phase (filtrées ensuite selon la voiture).
        private static readonly Dictionary<string, string[]> Plans = new()
        {
            ["understeer:entry"] = new[] { "bias-back", "trail-brake", "arb-front-soft", "springs-front-soft", "camber-front-more" },
            ["understeer:mid"] = new[] { "arb-front-soft", "entry-slower", "arb-rear-stiff", "camber-front-more", "wing-front-more" },
            ["understeer:exit"] = new[] { "diff-open", "throttle-later", "diff-preload-less", "arb-rear-stiff" },
            ["understeer:"] = new[] { "bias-back", "trail-brake", "arb-front-soft", "camber-front-more" },
            ["oversteer:entry"] = new[] { "bias-fwd", "abs-up", "brake-straight", "diff-coast-more", "arb-rear-soft" },
            ["oversteer:mid"] = new[] { "arb-rear-soft", "less-slide", "wing-rear-more", "arb-front-stiff" },
            ["oversteer:exit"] = new[] { "tc-up", "map-soft", "diff-lock", "throttle-smooth", "springs-rear-soft", "wing-rear-more", "diff-power-less" },
            ["oversteer:"] = new[] { "tc-up", "bias-fwd", "throttle-smooth", "arb-rear-soft", "wing-rear-more" },
            ["lock-front:"] = new[] { "bias-back", "abs-up", "trail-brake", "brake-pressure-less" },
            ["lock-inside:"] = new[] { "bias-back", "abs-up", "arb-front-soft", "brake-pressure-less" },
            ["lock-rear:"] = new[] { "bias-fwd", "abs-up", "downshift-slow", "diff-coast-more" },
            ["brake-unstable:"] = new[] { "bias-fwd", "abs-up", "brake-straight", "diff-coast-more", "wing-rear-more" },
            ["wheelspin:"] = new[] { "tc-up", "map-soft", "diff-lock", "throttle-smooth", "short-shift", "springs-rear-soft", "wing-rear-more" },
            ["bouncing:"] = new[] { "kerbs-avoid", "ride-height-up", "bump-soft", "springs-soft" },
            ["brakes-hot:"] = new[] { "brake-cool", "bias-back", "ducts-open" },
            ["tyres-hot:"] = new[] { "less-slide", "pressures-lower", "ducts-open" },
            ["tyres-cold:"] = new[] { "warm-tyres", "ducts-close" },
            ["rain:"] = new[] { "tc-up", "abs-up", "map-soft", "wet-line", "springs-soft", "wing-rear-more" },
            ["no-grip:"] = new[] { "less-slide", "pressures-lower" },
        };

        private static readonly string[] NumberWords = { "zero", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf", "dix", "onze", "douze", "treize", "quatorze", "quinze", "seize", "dix sept", "dix huit", "dix neuf", "vingt" };

        public sealed class Advice
        {
            public string Symptom { get; set; }

            public string Label { get; set; }

            public string Phase { get; set; }

            public string Corner { get; set; }

            public string Category { get; set; }

            public string Context { get; set; }

            public Proposal Main { get; set; }

            public List<Proposal> Proposals { get; set; } = new();

            /// <summary>Phrase radio complète (courte).</summary>
            public string Text { get; set; }

            /// <summary>« neufs », « usés », « mi-usure » : état des pneus au moment du retour (pour le carnet).</summary>
            public string Tyres { get; set; }
        }

        /// <summary>Symptôme reconnu dans la phrase du pilote (clé) ou null.</summary>
        public static string DetectSymptom(string text)
        {
            string t = RadioCommands.Normalize(text);
            if (t.Length == 0)
            {
                return null;
            }

            // Une vraie question sur l'état (« comment chauffent mes pneus ? ») va aux réponses chiffrées ;
            // une plainte ou une demande de conseil (« j'ai du sous-virage, je règle quoi ? ») vient ici.
            if (RadioCommands.IsQuestion(text) && !Regex.IsMatch(t, @"\b(j ai|je|ca|la voiture|elle|il y a|regl\w*|corrig\w*|evit\w*|conseil|que faire|quoi faire|comment faire|comment on)\b"))
            {
                return null;
            }

            foreach (Symptom s in Symptoms)
            {
                if (s.Pattern.IsMatch(t))
                {
                    return s.Key;
                }
            }

            return null;
        }

        /// <summary>Virage cité : « virage 1 », « V3 », « premier virage », « l'épingle », « partout ».</summary>
        public static string CornerOf(string text)
        {
            string t = RadioCommands.Normalize(text);
            Match m = Regex.Match(t, @"\b(?:virage|v|t|turn)\s?(\d{1,2})\b");
            if (m.Success)
            {
                return $"virage {m.Groups[1].Value}";
            }

            m = Regex.Match(t, @"\bvirage (" + string.Join("|", NumberWords.Skip(1).Reverse()) + @")\b");
            if (m.Success)
            {
                return $"virage {Array.IndexOf(NumberWords, m.Groups[1].Value)}";
            }

            m = Regex.Match(t, @"\b(\d{1,2})\s?(?:er|e|eme|ieme)? virage\b");
            if (m.Success)
            {
                return $"virage {m.Groups[1].Value}";
            }

            if (Regex.IsMatch(t, @"\bpremier virage\b"))
            {
                return "virage 1";
            }

            if (Regex.IsMatch(t, @"\b(partout|tous les virages|chaque virage|tous les freinages|chaque freinage)\b"))
            {
                return "partout";
            }

            m = Regex.Match(t, @"\b(epingle|chicane|virages lents|virages rapides)\b");
            return m.Success ? m.Value switch { "epingle" => "l'épingle", "chicane" => "la chicane", _ => "les " + m.Value } : null;
        }

        /// <summary>
        /// Conseil pour un retour du pilote. <paramref name="symptomOverride"/> et <paramref name="phaseOverride"/> servent aux suites
        /// (« c'est en sortie » après « j'ai du sous-virage »).
        /// </summary>
        public static Advice Advise(string text, CarContext ctx, ConversationThread thread = null, string symptomOverride = null, string phaseOverride = null, string cornerOverride = null)
        {
            ctx ??= new CarContext();
            string key = symptomOverride ?? DetectSymptom(text);
            Symptom s = Symptoms.FirstOrDefault(x => x.Key == key);
            if (s == null)
            {
                return null;
            }

            string phase = phaseOverride ?? ConversationThread.PhaseOf(text);
            if (key == "brake-unstable")
            {
                phase = "entry";
            }

            string corner = cornerOverride ?? CornerOf(text);
            string planKey = (key is "understeer" or "oversteer") ? $"{key}:{phase}" : $"{key}:";
            if (!Plans.TryGetValue(planKey, out string[] plan))
            {
                plan = Plans[$"{key}:"];
            }

            // Pluie : on privilégie les aides (TC, ABS) avant le reste.
            IEnumerable<string> keys = plan;
            if (ctx.Wet && key is "oversteer" or "wheelspin" or "lock-front" or "lock-rear" or "brake-unstable")
            {
                keys = new[] { "tc-up", "abs-up" }.Where(k => key != "lock-front" || k == "abs-up").Concat(plan).Distinct();
            }

            var proposals = new List<Proposal>();
            foreach (string k in keys)
            {
                if (!P.TryGetValue(k, out Proposal p) || !Applies(p, ctx) || thread?.IsRefused(k) == true)
                {
                    continue;
                }

                Proposal copy = p.Clone();
                if (thread?.IsAccepted(k) == true && copy.Infinitive.Contains("d'un cran", StringComparison.Ordinal))
                {
                    copy.Infinitive = copy.Infinitive.Replace("d'un cran", "d'un cran de plus", StringComparison.Ordinal);
                    copy.Imperative = copy.Imperative.Replace("d'un cran", "d'un cran de plus", StringComparison.Ordinal);
                }

                proposals.Add(copy);
            }

            var advice = new Advice
            {
                Symptom = key,
                Label = Label(s.Label, phase),
                Phase = phase,
                Corner = corner,
                Proposals = proposals,
                Tyres = ctx.NewTyres ? "neufs" : ctx.WornTyres ? "usés" : "mi-usure",
            };

            advice.Context = ContextNote(key, ctx);

            // Roue intérieure à un virage précis : le geste de pilotage d'abord, le réglage ensuite.
            string tip = null;
            if (key == "lock-inside")
            {
                tip = "relâche le frein plus tôt quand tu braques, la roue intérieure se déleste";
            }
            else if (key == "lock-front" && corner != null && corner != "partout")
            {
                tip = "freine un peu plus tôt et relâche progressivement en tournant";
            }

            // Manque de grip général : la cause est souvent ailleurs (pneus usés, froids, pluie).
            if (key == "no-grip")
            {
                if (ctx.Wet)
                {
                    return Advise(text, ctx, thread, "rain", phase, corner);
                }

                if (ctx.TyresCold || ctx.NewTyres)
                {
                    return Advise(text, ctx, thread, "tyres-cold", phase, corner);
                }

                if (ctx.WornTyres)
                {
                    advice.Category = ExchangeCategorizer.Strategy;
                    advice.Text = $"Pneus usés à {RadioFormat.N(ctx.WorstWearPct ?? 0, 0)} % ({RadioFormat.Wheel(ctx.WorstWearWheel)}) : c'est l'usure. Ménage-les, je regarde pour avancer l'arrêt.";
                    advice.Main = null;
                    return advice;
                }
            }

            Proposal main = proposals.FirstOrDefault();
            advice.Main = main;
            // Catégorie du symptôme : l'équilibre (sous-virage, survirage, rebonds) relève du setup, les blocages et le patinage des réglages en roulant.
            advice.Category = s.Category;
            advice.Text = Compose(advice, tip, ctx, askPhase: (key is "understeer" or "oversteer") && phase == null);
            return advice;
        }

        /// <summary>Premier réglage de setup (au stand) contre un symptôme, hors propositions refusées ; null s'il n'y en a pas.</summary>
        public static string SetupIdea(string symptom, IEnumerable<string> refused = null)
        {
            var no = new HashSet<string>(refused ?? Enumerable.Empty<string>());
            return Plans.TryGetValue($"{symptom}:", out string[] plan)
                ? plan.Select(k => P.TryGetValue(k, out Proposal p) ? p : null).FirstOrDefault(p => p?.Kind == "setup" && !no.Contains(p.Key))?.Note
                : null;
        }

        /// <summary>Phrase pour la proposition suivante (« autre chose ? »).</summary>
        public static string Offer(Proposal p, bool alternative)
        {
            if (p == null)
            {
                return null;
            }

            string head = alternative ? "Sinon" : "Je propose";
            return p.Kind switch
            {
                "setup" => alternative ? $"Sinon, au stand : {p.Note}. Je le note pour le setup ?" : $"Au stand : {p.Note}. Je le note pour le setup ?",
                "pilotage" => alternative ? $"Sinon, {p.Infinitive}. Tu essaies ?" : $"Essaie {De(p.Infinitive)}.",
                _ => alternative ? $"Sinon : {p.Infinitive}. On essaie ?" : $"{head} {De(p.Infinitive)}. On essaie ?",
            };
        }

        /// <summary>Réponse quand le pilote accepte la proposition.</summary>
        public static string Accepted(Proposal p) => p?.Kind switch
        {
            "setup" => $"Noté pour le prochain arrêt : {p.Note}.",
            "pilotage" => "Bien reçu. Dis-moi si ça va mieux.",
            null => "Bien reçu.",
            _ => $"Bien reçu : {p.Imperative}. Dis-moi si ça va mieux.",
        };

        private static string Compose(Advice a, string tip, CarContext ctx, bool askPhase)
        {
            string where = a.Corner == null ? string.Empty : a.Corner == "partout" ? " partout" : a.Corner.StartsWith("virage", StringComparison.Ordinal) ? $" au {a.Corner}" : $" dans {a.Corner}";
            string head = $"{a.Label}{where}";
            var parts = new List<string>();
            if (a.Context != null)
            {
                parts.Add(a.Context);
            }

            Proposal p = a.Main;
            if (tip != null)
            {
                parts.Add($"{head} : {tip}.");
                if (p != null)
                {
                    parts.Add(p.Kind == "setup" ? $"Si ça continue, au stand : {p.Note} ?" : $"Si ça continue, on va {p.Infinitive} ?");
                }
            }
            else if (p == null)
            {
                parts.Add($"{head} : noté, je n'ai pas de réglage à proposer en roulant.");
            }
            else if (p.Kind == "course")
            {
                parts.Add(askPhase
                    ? $"{head} : si c'est en entrée, {p.Imperative}. C'est en entrée, au milieu ou en sortie ?"
                    : $"{head} : je propose {De(p.Infinitive)}. On essaie ?");
            }
            else if (p.Kind == "setup")
            {
                parts.Add(askPhase
                    ? $"{head} : au stand, {p.Note}. C'est en entrée, au milieu ou en sortie ?"
                    : $"{head} : rien à régler en roulant ; au stand, {p.Note}. Je le note pour le setup ?");
            }
            else
            {
                parts.Add($"{head} : {p.Imperative}.");
                Proposal next = a.Proposals.Skip(1).FirstOrDefault(x => x.Kind != "pilotage");
                if (next != null)
                {
                    parts.Add(next.Kind == "setup" ? $"Au stand : {next.Note}, je le note ?" : $"Sinon on peut {next.Infinitive}, on essaie ?");
                    a.Main = next;
                }
            }

            return string.Join(" ", parts);
        }

        private static string ContextNote(string key, CarContext ctx)
        {
            if (ctx.Wet && key != "rain")
            {
                return $"Piste mouillée à {ctx.WetnessPct} % :";
            }

            if (ctx.TyresCold && ctx.NewTyres && key is "understeer" or "oversteer" or "lock-front" or "lock-rear" or "lock-inside" or "no-grip" or "wheelspin")
            {
                return "Pneus encore froids, ça ira mieux dans un tour.";
            }

            if (ctx.WornTyres && key is "understeer" or "oversteer" or "wheelspin" or "no-grip")
            {
                return $"Pneus usés à {RadioFormat.N(ctx.WorstWearPct ?? 0, 0)} %, c'est en partie l'usure.";
            }

            if (ctx.FrontHot && key is "understeer" or "lock-front" or "tyres-hot")
            {
                return $"L'avant est trop chaud ({RadioFormat.N(ctx.HottestTemp ?? 0, 0)}°).";
            }

            if (ctx.RearHot && key is "oversteer" or "wheelspin" or "tyres-hot")
            {
                return $"L'arrière est trop chaud ({RadioFormat.N(ctx.HottestTemp ?? 0, 0)}°).";
            }

            return null;
        }

        private static string Label(string label, string phase) => phase switch
        {
            "entry" when label is "Sous-virage" or "Survirage" => label + " en entrée",
            "mid" when label is "Sous-virage" or "Survirage" => label + " au milieu du virage",
            "exit" when label is "Sous-virage" or "Survirage" => label + " en sortie",
            _ => label,
        };

        private static bool Applies(Proposal p, CarContext ctx) => p.Key switch
        {
            "abs-up" => ctx.HasAbs,
            "tc-up" => ctx.HasTc,
            "map-soft" => ctx.HasEngineMap,
            "diff-open" or "diff-lock" => ctx.HasDiffInRace,
            "wing-front-more" => ctx.CarClass is "F1" or "F2",
            "ducts-open" or "ducts-close" => ctx.Game is "ACC" or "LMU" or "rF2" or "AMS2" or "Demo" or "autre",
            _ => true,
        };

        /// <summary>« de reculer », « d'avancer ».</summary>
        private static string De(string infinitive) => Regex.IsMatch(infinitive ?? string.Empty, "^[aeiouyéèêàâh]", RegexOptions.IgnoreCase) ? "d'" + infinitive : "de " + infinitive;

        private static Regex R(string pattern) => new(pattern, RegexOptions.Compiled);

        private static Proposal Course(string key, string inf, string imp, string note) => new() { Key = key, Kind = "course", Infinitive = inf, Imperative = imp, Note = note };

        private static Proposal Drive(string key, string inf, string imp, string note) => new() { Key = key, Kind = "pilotage", Infinitive = inf, Imperative = imp, Note = note };

        private static Proposal Setup(string key, string noun) => new() { Key = key, Kind = "setup", Infinitive = "mettre " + noun, Imperative = "mets " + noun, Note = noun };
    }
}
