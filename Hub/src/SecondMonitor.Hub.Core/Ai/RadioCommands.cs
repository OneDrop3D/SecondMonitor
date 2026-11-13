namespace SecondMonitor.Hub.Core.Ai
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Analyse des phrases radio qui ne sont pas des questions de course : changement de modèle d'IA à la voix,
    /// politesses, oui / non, retours du pilote à mémoriser, et réponses du modèle à écarter.
    /// </summary>
    public static class RadioCommands
    {
        private static readonly Regex SwitchVerb = new(@"\b(passe|passer|passons|bascule|basculer|change|changer|utilise|utiliser|prends|prendre|mets|mettre|switch)\b", RegexOptions.Compiled);

        private static readonly string[] StopWords =
        {
            "on", "je", "tu", "nous", "vous", "le", "la", "les", "l", "de", "du", "des", "un", "une", "a", "au", "aux", "sur", "vers", "en", "pour", "par",
            "modele", "model", "ia", "intelligence", "artificielle", "cerveau", "maintenant", "stp", "svp", "plutot", "s", "il", "te", "plait", "reflexion",
            "ecoute", "voix", "parole", "transcription", "machine", "passe", "passer", "passons", "bascule", "basculer", "change", "changer", "utilise",
            "utiliser", "prends", "prendre", "mets", "mettre", "switch", "est", "ce", "que", "qu",
        };

        /// <summary>Candidat pour un changement de modèle : identifiant réglable (« nom » ou « nom@machine »), nom, machine, capacités.</summary>
        public sealed record ModelCandidate(string Id, string Name, string Node, IReadOnlyCollection<string> Capabilities);

        public sealed record ModelSwitch(string Role, string ModelId, IReadOnlyList<string> Ambiguous);

        /// <summary>
        /// « On passe à mistral », « passe sur omni pour la voix », « utilise qwen sur le raspberry ».
        /// Rôle par défaut : réflexion. Retourne null si la phrase ne désigne aucun modèle connu (ex. « je passe au stand »).
        /// </summary>
        public static ModelSwitch ParseModelSwitch(string text, IReadOnlyList<ModelCandidate> models, IReadOnlyDictionary<string, string> aliases = null)
        {
            string t = Normalize(text);
            if (string.IsNullOrWhiteSpace(t) || models == null || models.Count == 0 || !SwitchVerb.IsMatch(t))
            {
                return null;
            }

            string role = Regex.IsMatch(t, @"\b(voix|parole|parler|synthese)\b") ? "speak"
                : Regex.IsMatch(t, @"\b(ecoute|ecouter|transcription|oreille)\b") ? "listen"
                : "think";
            string capability = role == "speak" ? "speech" : role == "listen" ? "audio" : "completion";

            Match verb = SwitchVerb.Match(t);
            var words = t[(verb.Index + verb.Length)..].Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(w => !StopWords.Contains(w)).ToList();
            if (words.Count == 0)
            {
                return null;
            }

            // Surnoms définis par l'utilisateur d'abord.
            if (aliases != null)
            {
                string joinedWords = string.Concat(words);
                foreach (var (alias, target) in aliases)
                {
                    string a = Compact(alias);
                    if (a.Length >= 3 && (joinedWords.Contains(a, StringComparison.Ordinal) || words.Any(w => w == a)))
                    {
                        return new ModelSwitch(role, target, Array.Empty<string>());
                    }
                }
            }

            // Machine éventuellement citée (« sur le raspberry »).
            string node = models.Select(m => m.Node).Distinct().FirstOrDefault(n => words.Any(w => w.Length >= 3 && Compact(n).Contains(w, StringComparison.Ordinal)));
            var nodeWords = node == null ? new HashSet<string>() : words.Where(w => Compact(node).Contains(w, StringComparison.Ordinal)).ToHashSet();
            string phrase = string.Concat(words.Where(w => !nodeWords.Contains(w)));
            if (phrase.Length < 3)
            {
                return null;
            }

            var scored = models
                .Where(m => node == null || m.Node == node)
                .Select(m => (Model: m, Score: LongestCommonSubstring(phrase, Compact(m.Name.Split(':')[0])) + (m.Capabilities.Contains(capability) ? 0.5 : 0)))
                .Where(x => x.Score >= 4)
                .OrderByDescending(x => x.Score)
                .ToList();
            if (scored.Count == 0)
            {
                return null;
            }

            double best = scored[0].Score;
            var close = scored.Where(x => best - x.Score <= 1).Select(x => x.Model.Id).Distinct().ToList();
            return new ModelSwitch(role, scored[0].Model.Id, close.Count > 1 ? close : Array.Empty<string>());
        }

        /// <summary>Interrupteur dit à la radio : « coupe les limites de piste », « remets les alertes », « active le menu des stands ».</summary>
        public sealed record Toggle(string Key, bool On);

        private static readonly Regex OffVerb = new(@"\b(coupe|couper|coupes|desactive|desactiver|desactives|arrete|arreter|stop|stoppe|enleve|enlever|retire|retirer|supprime|plus de|mute|eteins|eteindre)\b", RegexOptions.Compiled);
        private static readonly Regex OnVerb = new(@"\b(remets|remet|remettre|reactive|reactiver|active|activer|actives|rallume|rallumer|allume|allumer|redonne|relance)\b", RegexOptions.Compiled);

        /// <summary>
        /// « Coupe les alertes de limites de piste », « plus de track limits », « remets les limites de piste », « coupe les alertes de pilotage »,
        /// « active le menu des stands automatique », « désactive le menu des stands ». Null si ce n'est pas un interrupteur.
        /// </summary>
        public static Toggle ParseToggle(string text)
        {
            string t = Normalize(text);
            if (t.Split(' ').Length > 14)
            {
                return null;
            }

            // Mode « thinking » : « active le mode thinking », « coupe le raisonnement », « réfléchis plus », « réponds plus vite ».
            if (Regex.IsMatch(t, @"\breflechis (plus|davantage|a fond|bien|mieux)\b|\bprends le temps de reflechir\b"))
            {
                return new Toggle("thinking", true);
            }

            if (Regex.IsMatch(t, @"\b(reflechis moins|reponds plus vite|reponses? (plus )?rapides?|plus vite les reponses)\b"))
            {
                return new Toggle("thinking", false);
            }

            bool off = OffVerb.IsMatch(t);
            bool on = !off && OnVerb.IsMatch(t);
            if (!off && !on)
            {
                return null;
            }

            string key = Regex.IsMatch(t, @"\b(thinking|think|thinkin|raisonnement|mode (de )?reflexion|reflexion approfondie|mode reflechi)\b") ? "thinking"
                : Regex.IsMatch(t, @"\b(limites? de piste|limites?|tracks? limits?|track limit)\b") ? "limites"
                : Regex.IsMatch(t, @"\bmenu (des|du|de) stands?\b|\bstands? auto\w*|\bmenu stand\b") ? "stand"
                : Regex.IsMatch(t, @"\b(alertes?|annonces?)\b") ? "alertes"
                : null;
            return key == null ? null : new Toggle(key, on);
        }

        /// <summary>« Teste le menu des stands », « essaie l'API » : lecture seule du menu des stands par le collecteur.</summary>
        public static bool IsPitMenuTest(string text)
        {
            string t = Normalize(text);
            return Regex.IsMatch(t, @"\b(teste|tester|essaie|essaye|essayer|verifie|verifier|lis|lire)\b.*\b(api|menu (des|du) stands?)\b");
        }

        public static string RoleLabel(string role) => role switch
        {
            "speak" => "la voix",
            "listen" => "l'écoute",
            _ => "la réflexion",
        };

        /// <summary>« Merci », « reçu », « ok »… : pas une question, réponse courte du hub (le petit modèle répondait en anglais).</summary>
        public static string SmallTalk(string text)
        {
            string t = Normalize(text).Trim();
            if (t.Length == 0 || t.Split(' ').Length > 5)
            {
                return null;
            }

            if (Regex.IsMatch(t, @"^(merci|merci beaucoup|merci bien|thanks|top merci|super merci)\b"))
            {
                return "Copy, je reste à l'écoute.";
            }

            if (Regex.IsMatch(t, @"^(ok|okay|d accord|recu|bien recu|compris|c est note|parfait|super|top|nickel|ca marche)$"))
            {
                return "Copy.";
            }

            if (Regex.IsMatch(t, @"^(radio check|test radio|tu m entends|vous m entendez|allo)\b"))
            {
                return "Radio check : je te reçois cinq sur cinq.";
            }

            return null;
        }

        /// <summary>Réponse oui / non du pilote à une question de l'IA : true, false ou null (ni l'un ni l'autre).</summary>
        public static bool? YesNo(string text)
        {
            string t = Normalize(text);
            if (Regex.IsMatch(t, @"\b(non|pas du tout|negatif|impossible|trop use|trop usee|ca glisse|ils glissent|elles glissent|on rentre|je rentre|pas possible|j ai plus de grip|plus d adherence|c est mort|on s arrete)\b"))
            {
                return false;
            }

            if (Regex.IsMatch(t, @"\b(oui|ouais|ok|okay|d accord|affirmatif|ca va|ca tient|on y va|vas y|go|on peut|on continue|on tente|on allonge|aucun probleme|pas de probleme|ils sont bons|elles sont bonnes|nickel|ca passe)\b"))
            {
                return true;
            }

            return null;
        }

        /// <summary>Remarque du pilote sur la voiture ou la piste, à garder en mémoire (ce n'est pas une question).</summary>
        public static bool IsFeedback(string text)
        {
            string t = Normalize(text);
            if (IsQuestion(text))
            {
                return false;
            }

            return Regex.IsMatch(t, @"\b(glisse|glissent|survir|sous vir|sousvir|bloque|blocage|patine|grip|adherence|degrade|use|usee|uses|mouill|humide|seche|sec|aquaplan|flaque|vibr|touche|abime|degat|casse|frein|freins|pneu|pneus|gomme|equilibre|balance|trafic|drapeau|accident|debris|huile sur la piste|piste)\b");
        }

        public static bool IsQuestion(string text)
        {
            string t = Normalize(text);
            // La dictée oublie souvent le « ? » : on reconnaît aussi les tournures de question (« j'aurais quoi comme rythme »,
            // « la piste va évoluer », « je dois tourner en combien »).
            return (text ?? string.Empty).Contains('?')
                || Regex.IsMatch(t, @"^(est ce|quand|combien|comment|quel|quelle|quels|quelles|ou |pourquoi|qui|c est quoi|on fait quoi|tu penses|dis moi|donne moi|il reste|j ai combien|je rentre|je boxe|je box)")
                || Regex.IsMatch(t, @"\b(combien|quand|quel|quelle|est ce que|quoi|a peu pres|prevision|previsions|tendance)\b")
                || Regex.IsMatch(t, @"\b(va|vont|devrait|devraient) (evoluer|changer|pleuvoir|secher|monter|baisser|tenir|durer|chauffer|refroidir|s ameliorer|se degrader)\b")
                || Regex.IsMatch(t, @"\b(je dois|dois je|il faut|faut il|je peux|peut on|on peut) (faire|tourner|rouler|aller|gagner|rattraper|tenir|rentrer)\b");
        }

        /// <summary>
        /// Réponse du modèle à écarter : en anglais, ou qui parle de « document » / « texte » / « fiche » (le pilote n'en a pas),
        /// ou qui annonce ne pas savoir alors que le hub avait une réponse.
        /// </summary>
        public static bool IsUnusableAnswer(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
            {
                return true;
            }

            string t = Normalize(answer);
            int english = Regex.Matches(t, @"\b(you|your|the|is|are|welcome|feel|free|have|any|questions|sorry|please)\b").Count;
            return english >= 2 || Regex.IsMatch(t, @"\b(document|le texte|la fiche|fiche de faits|les donnees fournies|ne mentionne pas|n indique pas)\b");
        }

        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            string d = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(d.Length);
            foreach (char c in d)
            {
                UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
            }

            return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }

        private static string Compact(string text) => Normalize(text).Replace(" ", string.Empty, StringComparison.Ordinal);

        private static int LongestCommonSubstring(string a, string b)
        {
            int best = 0;
            var prev = new int[b.Length + 1];
            for (int i = 1; i <= a.Length; i++)
            {
                var cur = new int[b.Length + 1];
                for (int j = 1; j <= b.Length; j++)
                {
                    if (a[i - 1] == b[j - 1])
                    {
                        cur[j] = prev[j - 1] + 1;
                        best = Math.Max(best, cur[j]);
                    }
                }

                prev = cur;
            }

            return best;
        }
    }
}
