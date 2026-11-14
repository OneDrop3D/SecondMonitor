namespace SecondMonitor.Hub.Core.Ai
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>Correction ajoutée par le pilote : ce que la dictée entend (« books ») et ce qu'il a dit (« box »).</summary>
    public sealed class LexiconEntry
    {
        public string Heard { get; set; }

        public string Meant { get; set; }
    }

    /// <summary>Terme de piste : le mot employé à la radio et ce qu'il veut dire.</summary>
    public sealed record LexiconTerm(string Word, string Meaning);

    /// <summary>
    /// Lexique de piste : le vocabulaire du sim racing (box box, delta, stint, undercut, brake bias…), compris tel quel par
    /// l'ingénieur ; les erreurs courantes de la dictée corrigées avant d'analyser la phrase (« books books » → « box box »,
    /// « passion d'énergie » → « gestion d'énergie ») ; et les mots anglais traduits pour les règles du hub, qui raisonnent en français.
    /// Le pilote peut ajouter ses propres corrections (écran Ingénieur IA).
    /// </summary>
    public static class RadioLexicon
    {
        /// <summary>Vocabulaire de piste (affiché dans l'écran Ingénieur IA, donné au modèle d'écoute et au modèle de réflexion).</summary>
        public static readonly LexiconTerm[] Vocabulary =
        {
            new("box box", "rentre au stand à la fin de ce tour"),
            new("pit", "le stand (pit stop : l'arrêt, pit lane : la voie des stands)"),
            new("delta", "écart de temps avec un tour de référence ou un adversaire"),
            new("fuel", "carburant"),
            new("stint", "relais entre deux arrêts"),
            new("undercut", "s'arrêter avant l'adversaire pour le passer grâce aux pneus neufs"),
            new("overcut", "rester en piste plus longtemps que lui pendant qu'il perd du temps sur pneus froids"),
            new("soft", "pneus tendres"),
            new("medium", "pneus médiums"),
            new("hard", "pneus durs"),
            new("wet", "pneus pluie"),
            new("inter", "pneus intermédiaires"),
            new("DRS", "aileron arrière mobile (F1)"),
            new("ERS", "système hybride : récupération et déploiement de la batterie"),
            new("brake bias", "répartition de freinage (aussi « BB », « balance de frein »)"),
            new("TC", "antipatinage (traction control)"),
            new("ABS", "antiblocage des roues"),
            new("VE", "énergie virtuelle (Le Mans Ultimate, WEC)"),
            new("lift and coast", "lever le pied avant le freinage pour économiser"),
            new("out lap / in lap", "tour de sortie des stands / tour d'entrée aux stands"),
            new("push", "attaquer"),
            new("track limits", "limites de piste"),
            new("blue flag", "drapeau bleu : laisser passer"),
            new("safety car / VSC / FCY", "voiture de sécurité, voiture de sécurité virtuelle, neutralisation"),
        };

        /// <summary>Erreurs de dictée corrigées d'office (motif, correction, exemple affiché).</summary>
        private static readonly (Regex Pattern, string Replacement, string Example)[] Builtin =
        {
            (Rx(@"\bbooks?\b"), "box", "books → box"),
            (Rx(@"\bpassion d['’ ]?[ée]nergie\b"), "gestion d'énergie", "passion d'énergie → gestion d'énergie"),
            (Rx(@"\b(?:br[eèé]que?|break|braque|brick|brek)[- ]+(?:bia[iy]?s+e?|baillasse|ba[iï]asse|biaisse|byas)\b"), "brake bias", "brèque baïasse → brake bias"),
            (Rx(@"\bbalance (?:de |du )?(?:faim|fin|fun|frin|frais)\b"), "balance de frein", "balance de faim → balance de frein"),
            (Rx(@"\bpr[ée][- ]?g?u?arance\b"), "précharge", "pré-guarance → précharge"),
            (Rx(@"\bstin(?:t|n)es\b|\bstinte\b|\bstinne\b"), "stint", "stinte → stint"),
            (Rx(@"\b(?:under|ander|onder)[- ]?(?:cut|cat|kat|kut)\b"), "undercut", "under cat → undercut"),
            (Rx(@"\bover[- ]?(?:cut|cat|kat|kut)\b"), "overcut", "over cat → overcut"),
            (Rx(@"(?<![\p{L}\p{N}])d\. ?r\. ?s\.?(?![\p{L}\p{N}])"), "DRS", null),
            (Rx(@"(?<![\p{L}\p{N}])e\. ?r\. ?s\.?(?![\p{L}\p{N}])"), "ERS", null),
            (Rx(@"\b(les|ces|mes|tes|des|nos|vos|aux) (?:clous|plumes|neus|pneux|peneus)\b"), "$1 pneus", "ces plumes → ces pneus"),
            (Rx(@"\bpneux\b"), "pneus", null),
            (Rx(@"\b(mets|lance|ouvre|relance|coupe|arr[eê]te|ferme|pause)( le| la)? (?:10|dix) ?heures?\b"), "$1 Deezer", "lance 10 heures → lance Deezer"),
            (Rx(@"\blift (?:and|&|end|n|en) coa?st\b"), "lift and coast", null),
            (Rx(@"\b(la|ma|ta) b[ée] ?b[ée]\b"), "$1 BB", "ma bébé → ma BB"),
            (Rx(@"\bsafe?t?y ?car\b"), "safety car", null),
        };

        /// <summary>Mots de piste (souvent anglais) et l'équivalent français compris par les règles du hub.</summary>
        private static readonly (Regex Pattern, string French)[] Jargon =
        {
            (Rx(@"\b(?:brake bias|brake balance|bb|bias)\b"), "répartition de freinage"),
            (Rx(@"\bstints?\b"), "relais"),
            (Rx(@"\bdelta\b"), "écart"),
            (Rx(@"\bfuel\b"), "carburant"),
            (Rx(@"\b(?:pit ?stop|pit lane|pits?)\b"), "stand"),
            (Rx(@"\b(?:softs?|mediums?|hards?|wets?|inters?|tyres?|tires?)\b"), "pneus"),
            (Rx(@"\b(?:ers|deploy(?:ment)?|harvest(?:ing)?)\b"), "batterie hybride"),
            (Rx(@"\b(?:ve|virtual energy)\b"), "énergie virtuelle"),
            (Rx(@"\blift and coast\b"), "carburant"),
            (Rx(@"\bout ?lap\b"), "tour de sortie"),
            (Rx(@"\bin ?lap\b"), "tour d'entrée"),
            (Rx(@"\bpace\b"), "rythme"),
            (Rx(@"\bbrakes?\b"), "freins"),
            (Rx(@"\bengine map\b"), "cartographie"),
        };

        private static readonly Regex AsrLanguage = new(@"^\s*(?:language\s+[A-Za-z]+\s*)?<asr_text>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex AsrTags = new(@"</?asr_text>|<\|[^|>]{1,40}\|>|^\s*language\s+(?:None|French|English|Français)\b\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Sortie brute de certains modèles d'écoute (Qwen3-ASR : « language French&lt;asr_text&gt;Box box. ») : seulement la phrase dite.
        /// Sans cela, les commandes qui commencent la phrase (« mets de la musique ») n'étaient plus reconnues.
        /// </summary>
        public static string CleanTranscript(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            string t = AsrLanguage.Replace(text, string.Empty);
            t = AsrTags.Replace(t, " ");
            return Regex.Replace(t, @"\s{2,}", " ").Trim();
        }

        /// <summary>Exemples des corrections d'office (affichés dans l'écran Ingénieur IA).</summary>
        public static IReadOnlyList<string> BuiltinExamples => Builtin.Where(b => b.Example != null).Select(b => b.Example).ToList();

        /// <summary>
        /// Corrige la phrase transcrite : erreurs connues de la dictée, puis corrections du pilote (« ce qu'elle entend » → « ce que j'ai dit »).
        /// Majuscules et accents ne comptent pas dans la recherche.
        /// </summary>
        public static string Correct(string text, IEnumerable<LexiconEntry> custom = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            string result = text;
            foreach (var (pattern, replacement, _) in Builtin)
            {
                result = pattern.Replace(result, replacement);
            }

            foreach (LexiconEntry e in custom ?? Enumerable.Empty<LexiconEntry>())
            {
                if (CustomPattern(e) is Regex r)
                {
                    try
                    {
                        result = r.Replace(result, e.Meant.Trim().Replace("$", "$$", StringComparison.Ordinal));
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        // motif trop coûteux sur cette phrase : ignoré
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Phrase « comprise » par les règles du hub : la phrase, suivie entre parenthèses des équivalents français des mots
        /// de piste anglais qu'elle contient (« what's my brake bias » → « … (répartition de freinage) »).
        /// </summary>
        public static string Understand(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            string normalized = RadioCommands.Normalize(text);
            var extra = Jargon.Where(j => j.Pattern.IsMatch(text) && !normalized.Contains(RadioCommands.Normalize(j.French), StringComparison.Ordinal))
                .Select(j => j.French)
                .Distinct()
                .ToList();
            return extra.Count == 0 ? text : $"{text.TrimEnd()} ({string.Join(", ", extra)})";
        }

        /// <summary>Indice donné au modèle d'écoute (paramètre « prompt » des transcriptions) : il reconnaît mieux ces mots.</summary>
        public static string TranscriptionHint(IEnumerable<LexiconEntry> custom = null)
        {
            var words = new List<string> { "Radio de course automobile en français." };
            words.AddRange(Vocabulary.Select(v => v.Word));
            words.AddRange(new[] { "pneus", "relais", "stand", "répartition de freinage", "énergie virtuelle", "cartographie", "précharge du différentiel", "Deezer" });
            words.AddRange((custom ?? Enumerable.Empty<LexiconEntry>()).Where(IsValid).Select(e => e.Meant.Trim()));
            return string.Join(", ", words.Distinct(StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>Glossaire pour la consigne du modèle de réflexion.</summary>
        public static string ModelGlossary() =>
            "LEXIQUE DE PISTE (le pilote emploie ces mots, toi aussi) : " + string.Join(" ; ", Vocabulary.Select(v => $"{v.Word} = {v.Meaning}")) + ".";

        /// <summary>Correction du pilote utilisable (deux côtés remplis, pas trop longue).</summary>
        public static bool IsValid(LexiconEntry e) =>
            e != null && !string.IsNullOrWhiteSpace(e.Heard) && !string.IsNullOrWhiteSpace(e.Meant) && e.Heard.Trim().Length <= 60 && e.Meant.Trim().Length <= 60;

        private static Regex CustomPattern(LexiconEntry e)
        {
            if (!IsValid(e))
            {
                return null;
            }

            // Recherche sans accents ni majuscules : chaque voyelle accepte ses variantes accentuées, espaces et apostrophes sont souples.
            string heard = RadioCommands.Normalize(e.Heard).Trim();
            if (heard.Length == 0)
            {
                return null;
            }

            var sb = new System.Text.StringBuilder();
            foreach (char c in heard)
            {
                sb.Append(c switch
                {
                    'a' => "[aàâä]",
                    'e' => "[eéèêë]",
                    'i' => "[iîï]",
                    'o' => "[oôö]",
                    'u' => "[uùûü]",
                    'c' => "[cç]",
                    ' ' => @"[\s'’-]+",
                    _ => Regex.Escape(c.ToString()),
                });
            }

            try
            {
                return new Regex($@"(?<![\p{{L}}\p{{N}}]){sb}(?![\p{{L}}\p{{N}}])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static Regex Rx(string pattern) => new(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }
}
