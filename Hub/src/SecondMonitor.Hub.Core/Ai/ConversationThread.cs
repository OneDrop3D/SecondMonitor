namespace SecondMonitor.Hub.Core.Ai
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>Ce que l'ingénieur propose au pilote : un réglage en roulant, un réglage au stand, ou une façon de piloter.</summary>
    public sealed class Proposal
    {
        /// <summary>Identifiant stable (« bias-back », « wing-rear-less »…) : on ne repropose pas ce qui a été refusé.</summary>
        public string Key { get; set; }

        /// <summary>« course » (réglable en roulant), « setup » (au stand), « pilotage ».</summary>
        public string Kind { get; set; }

        /// <summary>« reculer la répartition de freinage d'un cran ».</summary>
        public string Infinitive { get; set; }

        /// <summary>« reculez la répartition de freinage d'un cran ».</summary>
        public string Imperative { get; set; }

        /// <summary>Pour le carnet de setup : « répartition de freinage reculée ».</summary>
        public string Note { get; set; }

        public Proposal Clone() => (Proposal)MemberwiseClone();
    }

    /// <summary>Décision du pilote sur une proposition (gardée pour toute la session et pour le carnet).</summary>
    public sealed class ProposalDecision
    {
        public DateTime At { get; set; } = DateTime.Now;

        public int? Lap { get; set; }

        public string Symptom { get; set; }

        public string Key { get; set; }

        public string Kind { get; set; }

        public string Text { get; set; }

        /// <summary>« accepté », « refusé », « efficace » (« c'est mieux »), « sans effet ».</summary>
        public string Decision { get; set; }
    }

    /// <summary>
    /// Fil de la conversation : la dernière proposition de l'ingénieur et celles qui restent, pour relier les réponses courtes
    /// du pilote (« oui », « non », « vas-y », « autre chose ? », « c'est en sortie », « c'est mieux ») à ce qui vient d'être dit.
    /// </summary>
    public sealed class ConversationThread
    {
        public const double WindowSeconds = 180;

        private readonly object _lock = new();
        private readonly List<ProposalDecision> _decisions = new();
        private OpenProposal _open;

        public enum FollowUpKind
        {
            None,
            Accept,
            Refuse,
            More,
            Better,
            Worse,
            Phase,
        }

        /// <summary>Dernière proposition ouverte (moins de <see cref="WindowSeconds"/> s), ou null.</summary>
        public OpenProposal Open
        {
            get
            {
                lock (_lock)
                {
                    return _open != null && (DateTime.UtcNow - _open.AtUtc).TotalSeconds <= WindowSeconds ? _open : null;
                }
            }
        }

        public IReadOnlyList<ProposalDecision> Decisions
        {
            get
            {
                lock (_lock)
                {
                    return _decisions.ToList();
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _open = null;
                _decisions.Clear();
            }
        }

        /// <summary>Ouvre une proposition (la précédente est remplacée).</summary>
        public void Propose(string symptom, string label, string phase, string corner, string category, Proposal current, IEnumerable<Proposal> rest)
        {
            lock (_lock)
            {
                _open = new OpenProposal
                {
                    Symptom = symptom,
                    Label = label,
                    Phase = phase,
                    Corner = corner,
                    Category = category,
                    Current = current,
                    Remaining = (rest ?? Enumerable.Empty<Proposal>()).Where(p => current == null || p.Key != current.Key).ToList(),
                    AtUtc = DateTime.UtcNow,
                };
            }
        }

        public void CloseOpen()
        {
            lock (_lock)
            {
                _open = null;
            }
        }

        /// <summary>La proposition courante passe à la suivante (« autre chose ? »). Retourne la nouvelle, ou null s'il n'y en a plus.</summary>
        public Proposal Next()
        {
            lock (_lock)
            {
                if (_open == null)
                {
                    return null;
                }

                Proposal next = _open.Remaining.FirstOrDefault(p => !IsRefusedInternal(p.Key));
                if (next != null)
                {
                    _open.Remaining.Remove(next);
                }

                _open.Current = next;
                _open.AtUtc = DateTime.UtcNow;
                return next;
            }
        }

        public void Decide(Proposal p, string symptom, string decision, int? lap)
        {
            if (p == null)
            {
                return;
            }

            lock (_lock)
            {
                _decisions.Add(new ProposalDecision { Key = p.Key, Kind = p.Kind, Text = p.Note ?? p.Infinitive, Symptom = symptom, Decision = decision, Lap = lap });
                if (_decisions.Count > 200)
                {
                    _decisions.RemoveAt(0);
                }
            }
        }

        public bool IsRefused(string key)
        {
            lock (_lock)
            {
                return IsRefusedInternal(key);
            }
        }

        public bool IsAccepted(string key)
        {
            lock (_lock)
            {
                return _decisions.LastOrDefault(d => d.Key == key)?.Decision is "accepté" or "efficace";
            }
        }

        private bool IsRefusedInternal(string key) => _decisions.LastOrDefault(d => d.Key == key)?.Decision == "refusé";

        /// <summary>Texte ajouté à la fiche de faits : propositions et décisions du pilote.</summary>
        public string Facts()
        {
            var list = Decisions.TakeLast(8).ToList();
            if (list.Count == 0)
            {
                return null;
            }

            return "Propositions de réglage et réponse du pilote : " + string.Join(" ; ", list.Select(d => $"{d.Text} ({d.Decision}{(d.Lap.HasValue ? $", tour {d.Lap}" : string.Empty)})")) + ".";
        }

        /// <summary>Réponse courte du pilote qui se rattache à la dernière proposition.</summary>
        public static FollowUpKind Classify(string text)
        {
            string t = RadioCommands.Normalize(text);
            if (t.Length == 0)
            {
                return FollowUpKind.None;
            }

            int words = t.Split(' ').Length;
            if (Regex.IsMatch(t, @"\b(autre chose|quoi d autre|sinon|une autre (idee|solution|piste)|autre (idee|solution|piste|reglage)|et apres|d autres? idees?|rien d autre)\b") && words <= 8)
            {
                return FollowUpKind.More;
            }

            if (words <= 8 && Regex.IsMatch(t, @"\b(c est mieux|ca va mieux|beaucoup mieux|nettement mieux|c est bon maintenant|ca marche bien|c est regle|ca a regle|probleme regle)\b"))
            {
                return FollowUpKind.Better;
            }

            if (words <= 8 && Regex.IsMatch(t, @"\b(toujours pareil|c est pareil|pas mieux|pas de changement|ca change rien|ca ne change rien|toujours|encore pire|c est pire|ca bloque encore|ca glisse encore|ca patine encore)\b"))
            {
                return FollowUpKind.Worse;
            }

            if (words <= 6 && Regex.IsMatch(t, @"^(c est |plutot |surtout |non |oui )?(en |a l |a la |au )?(entree|milieu|sortie|freinage|point de corde|corde|reaccel\w*|acceleration)\b"))
            {
                return FollowUpKind.Phase;
            }

            if (words > 7)
            {
                return FollowUpKind.None;
            }

            if (Regex.IsMatch(t, @"^(non|nan|nope|negatif|pas maintenant|pas besoin|pas la peine|laisse|laisse tomber|on laisse|on laisse comme ca|non merci|on garde|on garde comme ca|je prefere pas|surtout pas|on touche a rien|on ne touche a rien|on touche pas)\b"))
            {
                return FollowUpKind.Refuse;
            }

            if (Regex.IsMatch(t, @"^(oui|ouais|ouai|ok|okay|d accord|daccord|vas y|allez y|allez|go|fais le|faites le|on essaie|on essaye|on tente|on fait ca|affirmatif|ca marche|c est fait|fait|je le fais|je fais ca|bonne idee|top|parfait|recu|bien recu|copy|copie|entendu|valide|je teste|je vais essayer|on y va)\b"))
            {
                return FollowUpKind.Accept;
            }

            return FollowUpKind.None;
        }

        /// <summary>Phase du virage citée : « entry », « mid », « exit » ou null.</summary>
        public static string PhaseOf(string text)
        {
            string t = RadioCommands.Normalize(text);
            if (Regex.IsMatch(t, @"\b(sortie|en sortant|reaccel\w*|acceleration|j accelere|remettant les gaz|remise des gaz|remets les gaz|en remettant|sur les gaz)\b"))
            {
                return "exit";
            }

            if (Regex.IsMatch(t, @"\b(entree|en entrant|au freinage|en freinant|freinage|quand je freine|quand je tourne|turn in|mise en appui|a l attaque)\b"))
            {
                return "entry";
            }

            if (Regex.IsMatch(t, @"\b(milieu|point de corde|a la corde|en appui|en courbe|pendant le virage|au cours du virage|en plein virage)\b"))
            {
                return "mid";
            }

            return null;
        }
    }

    public sealed class OpenProposal
    {
        public string Symptom { get; set; }

        public string Label { get; set; }

        public string Phase { get; set; }

        public string Corner { get; set; }

        public string Category { get; set; }

        public Proposal Current { get; set; }

        public List<Proposal> Remaining { get; set; } = new();

        public DateTime AtUtc { get; set; }
    }
}
