namespace SecondMonitor.Hub.Core.Ai
{
    using System.Linq;
    using System.Text.RegularExpressions;
    using SecondMonitor.Hub.Core.Race;
    using static SecondMonitor.Hub.Core.Ai.RadioFormat;

    /// <summary>Réponse du dialogue radio (sans modèle) : texte, type d'échange et catégorie du carnet.</summary>
    public sealed class DialogReply
    {
        public DialogReply(string text, string kind, string category, string symptom = null)
        {
            Text = text;
            Kind = kind;
            Category = category;
            Symptom = symptom;
        }

        public string Text { get; }

        /// <summary>« suite » (réponse à une proposition), « conseil » (base de réglages), « retour » (remarque du pilote).</summary>
        public string Kind { get; }

        public string Category { get; }

        public string Symptom { get; }
    }

    /// <summary>
    /// Dialogue radio sans modèle : les suites de conversation (« oui », « non », « autre chose ? », « c'est en sortie »)
    /// reliées à la dernière proposition, les réponses aux questions de l'IA, et les retours du pilote sur la voiture
    /// (base de connaissances <see cref="SetupAdvisor"/>). Un retour du pilote reçoit toujours une réponse utile.
    /// </summary>
    public static class RadioDialog
    {
        /// <summary>
        /// Suite de la conversation, à tester AVANT les politesses (« ok » répond à la proposition qui vient d'être faite).
        /// Retourne null si la phrase ne se rattache à rien.
        /// </summary>
        public static DialogReply FollowUp(string text, RaceMemory memory, CarContext ctx, int? lap)
        {
            if (memory == null || string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            ConversationThread thread = memory.Thread;
            ConversationThread.FollowUpKind kind = ConversationThread.Classify(text);
            OpenProposal open = thread.Open;
            PendingQuestion pending = memory.Pending();
            bool? yesNo = RadioCommands.YesNo(text);

            // La question la plus récente l'emporte : question spontanée de l'IA (relais, pluie) ou proposition de réglage.
            if (pending != null && (open == null || pending.AskedAtUtc >= open.AtUtc)
                && (yesNo != null || kind is ConversationThread.FollowUpKind.Accept or ConversationThread.FollowUpKind.Refuse))
            {
                memory.ClearPending();
                var (reply, note) = ProactiveEngineer.HandleAnswer(pending, text, memory);
                memory.Add("pilote", pending.Topic, note, lap);
                return new DialogReply(reply, "suite", ExchangeCategorizer.FromProactiveTopic(pending.Topic));
            }

            if (open == null)
            {
                return null;
            }

            // Nouveau symptôme différent : c'est un nouveau retour, pas une suite.
            string symptom = SetupAdvisor.DetectSymptom(text);
            if (symptom != null && symptom != open.Symptom && kind != ConversationThread.FollowUpKind.Phase)
            {
                return null;
            }

            if (kind == ConversationThread.FollowUpKind.None && yesNo != null && RadioCommands.Normalize(text).Split(' ').Length <= 10 && !RadioCommands.IsQuestion(text))
            {
                kind = yesNo == true ? ConversationThread.FollowUpKind.Accept : ConversationThread.FollowUpKind.Refuse;
            }

            string category = open.Category ?? ExchangeCategorizer.RaceInfo;
            Proposal current = open.Current;
            switch (kind)
            {
                case ConversationThread.FollowUpKind.Accept when current != null:
                    thread.Decide(current, open.Symptom, "accepté", lap);
                    memory.Add("pilote", "réglage", $"accepte : {current.Note ?? current.Infinitive}", lap, open.Symptom);
                    return new DialogReply(SetupAdvisor.Accepted(current), "suite", CategoryOf(current, category), open.Symptom);

                case ConversationThread.FollowUpKind.Refuse when current != null:
                    thread.Decide(current, open.Symptom, "refusé", lap);
                    memory.Add("pilote", "réglage", $"refuse : {current.Note ?? current.Infinitive}", lap, open.Symptom);
                    return new DialogReply("Copy, compris.", "suite", CategoryOf(current, category), open.Symptom);

                case ConversationThread.FollowUpKind.More:
                {
                    Proposal next = thread.Next();
                    return next == null
                        ? new DialogReply("C'est tout ce que je vois en roulant. On en reparle au prochain arrêt pour le setup.", "suite", category, open.Symptom)
                        : new DialogReply(SetupAdvisor.Offer(next, alternative: true), "suite", CategoryOf(next, category), open.Symptom);
                }

                case ConversationThread.FollowUpKind.Better:
                {
                    ProposalDecision last = thread.Decisions.LastOrDefault(d => d.Symptom == open.Symptom && d.Decision == "accepté");
                    if (last != null)
                    {
                        thread.Decide(new Proposal { Key = last.Key, Kind = last.Kind, Note = last.Text }, open.Symptom, "efficace", lap);
                    }

                    memory.Add("pilote", "réglage", $"mieux après : {last?.Text ?? "la correction"}", lap, open.Symptom);
                    thread.CloseOpen();
                    return new DialogReply("Parfait, on garde ça.", "suite", category, open.Symptom);
                }

                case ConversationThread.FollowUpKind.Worse:
                {
                    ProposalDecision last = thread.Decisions.LastOrDefault(d => d.Symptom == open.Symptom && d.Decision == "accepté");
                    if (last != null)
                    {
                        thread.Decide(new Proposal { Key = last.Key, Kind = last.Kind, Note = last.Text }, open.Symptom, "sans effet", lap);
                    }

                    Proposal next = thread.Next();
                    return next == null
                        ? new DialogReply("Compris. Plus rien à essayer en roulant : on revoit le setup au prochain arrêt.", "suite", category, open.Symptom)
                        : new DialogReply("Compris. " + SetupAdvisor.Offer(next, alternative: true), "suite", CategoryOf(next, category), open.Symptom);
                }

                case ConversationThread.FollowUpKind.Phase when open.Symptom is "understeer" or "oversteer":
                {
                    SetupAdvisor.Advice advice = SetupAdvisor.Advise(text, ctx, thread, open.Symptom, ConversationThread.PhaseOf(text), open.Corner);
                    if (advice == null)
                    {
                        return null;
                    }

                    Remember(memory, advice, text, lap, note: false); // précision du même retour, pas un nouveau
                    return new DialogReply(advice.Text, "conseil", advice.Category, advice.Symptom);
                }

                default:
                    return null;
            }
        }

        /// <summary>
        /// Retour du pilote sur la voiture ou la piste (ou réponse libre à une question de l'IA) : conseil immédiat, jamais un simple « reçu ».
        /// À tester après les commandes (politesses, changement de modèle). Retourne null si ce n'est pas un retour.
        /// </summary>
        public static DialogReply Feedback(string text, RaceMemory memory, CarContext ctx, RaceSnapshot snap, int? lap)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            SetupAdvisor.Advice advice = SetupAdvisor.Advise(text, ctx, memory?.Thread);
            if (advice != null)
            {
                Remember(memory, advice, text, lap);
                return new DialogReply(advice.Text, "conseil", advice.Category, advice.Symptom);
            }

            // Réponse libre à une question que l'IA vient de poser (« comment sont les pneus ? »).
            PendingQuestion pending = memory?.Pending();
            if (pending != null && (RadioCommands.IsFeedback(text) || !RadioCommands.IsQuestion(text)))
            {
                memory.ClearPending();
                var (reply, note) = ProactiveEngineer.HandleAnswer(pending, text, memory);
                memory.Add("pilote", pending.Topic, note, lap);
                return new DialogReply(reply, "suite", ExchangeCategorizer.FromProactiveTopic(pending.Topic));
            }

            if (!RadioCommands.IsFeedback(text))
            {
                return null;
            }

            string quote = Quote(text);
            string corner = SetupAdvisor.CornerOf(text);
            string at = corner == null || corner == "partout" ? string.Empty : corner.StartsWith("virage", System.StringComparison.Ordinal) ? $" au {corner}" : $" dans {corner}";
            string t = RadioCommands.Normalize(text);
            DialogReply result;
            if (Regex.IsMatch(t, @"\b(drapeau|accident|debris|huile|voiture arretee|crash|safety|carambolage)\b"))
            {
                result = new DialogReply($"Bien reçu, prudence{at}. Je garde l'info.", "retour", ExchangeCategorizer.RaceInfo);
            }
            else if (Regex.IsMatch(t, @"\b(seche|sechante|ca seche|s asseche|trajectoire seche)\b"))
            {
                result = new DialogReply($"Noté, la piste sèche de ton côté (humidité mesurée {snap?.Weather?.Wetness ?? 0} %). Je te dis quand repasser en pneus secs.", "retour", ExchangeCategorizer.Strategy);
            }
            else if (Regex.IsMatch(t, @"\b(mouill\w*|humide|il pleut|pleut|gouttes|averse)\b"))
            {
                result = new DialogReply($"Noté, piste mouillée de ton côté (humidité mesurée {snap?.Weather?.Wetness ?? 0} %). Je te dis quand passer en pneus pluie.", "retour", ExchangeCategorizer.Strategy);
            }
            else if (Regex.IsMatch(t, @"\b(touche|abime\w*|degat\w*|casse\w*|crev\w*)\b"))
            {
                var hurt = snap?.Player?.Damage?.Where(d => d.Percent >= 1).OrderByDescending(d => d.Percent).Take(2).ToList();
                string seen = hurt == null || hurt.Count == 0 ? "le jeu ne signale aucun dégât pour l'instant" : "le jeu signale " + string.Join(", ", hurt.Select(d => $"{d.Label} {N(d.Percent, 0)} %"));
                result = new DialogReply($"Noté : {seen}. Je mesure ce que ça coûte au tour.", "retour", ExchangeCategorizer.Strategy);
            }
            else if (Regex.IsMatch(t, @"\b(pneus?|gommes?|use|usee|uses|usees|degrade\w*)\b"))
            {
                WheelStatus worst = snap?.Player?.Wheels?.OrderByDescending(w => w.WearPct).FirstOrDefault();
                string wear = worst == null ? string.Empty : $" Usure mesurée : {N(worst.WearPct, 0)} % à l'{Wheel(worst.Position)}.";
                string box = snap?.Player?.RecommendedStopLap is int stop ? $" Box prévu au tour {stop}." : string.Empty;
                result = new DialogReply($"Noté pour les pneus.{wear}{box}".Trim(), "retour", ExchangeCategorizer.Strategy);
            }
            else if (Regex.IsMatch(t, @"\b(frein|freins)\b"))
            {
                result = new DialogReply("Noté pour les freins. Ça bloque, ça chauffe, ou la voiture est instable au freinage ?", "retour", ExchangeCategorizer.RaceSettings);
            }
            else if (Regex.IsMatch(t, @"\b(equilibre|balance|grip|adherence|glisse|glissent)\b"))
            {
                result = new DialogReply("Noté. C'est l'avant ou l'arrière, et en entrée, au milieu ou en sortie de virage ?", "retour", ExchangeCategorizer.CarSetup);
            }
            else if (Regex.IsMatch(t, @"\b(trafic|drapeau bleu|retardataire)\b"))
            {
                result = new DialogReply("Noté pour le trafic, je te préviens des voitures proches.", "retour", ExchangeCategorizer.RaceInfo);
            }
            else
            {
                result = new DialogReply($"Noté{at} : je le garde pour la suite.", "retour", ExchangeCategorizer.RaceInfo);
            }

            memory?.Add("pilote", "retour", quote, lap);
            return result;
        }

        /// <summary>Une réponse calculée vient de proposer quelque chose (ex. un cran d'aileron en moins) : on ouvre le fil.</summary>
        public static void Offered(RaceMemory memory, Proposal proposal, string symptom, string label, string category)
        {
            if (memory == null || proposal == null)
            {
                return;
            }

            memory.Thread.Propose(symptom, label, null, null, category, proposal, null);
        }

        private static void Remember(RaceMemory memory, SetupAdvisor.Advice advice, string text, int? lap, bool note = true)
        {
            if (memory == null)
            {
                return;
            }

            if (note)
            {
                memory.Add("pilote", "retour", Quote(text), lap, advice.Symptom, advice.Tyres);
            }
            else
            {
                memory.Add("pilote", "précision", Quote(text), lap);
            }
            if (advice.Main != null)
            {
                // Les propositions déjà dites avant la principale (geste de pilotage) ne sont pas reproposées.
                var rest = advice.Proposals.SkipWhile(p => p.Key != advice.Main.Key).Skip(1).ToList();
                memory.Thread.Propose(advice.Symptom, advice.Label, advice.Phase, advice.Corner, advice.Category, advice.Main, rest);
            }
        }

        private static string CategoryOf(Proposal p, string fallback) => p?.Kind switch
        {
            "course" => ExchangeCategorizer.RaceSettings,
            "setup" => ExchangeCategorizer.CarSetup,
            _ => fallback,
        };

        private static string Quote(string text) => $"« {text.Trim().TrimEnd('.', '!')} »";
    }
}
