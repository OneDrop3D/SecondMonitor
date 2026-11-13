namespace SecondMonitor.Hub.Services;

using System.Diagnostics;
using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Demo;
using SecondMonitor.Hub.Core.Race;

/// <summary>
/// Ingénieur de course IA : écoute (modèle « écoute »), réfléchit (modèle « réflexion ») avec une fiche de faits
/// calculée par le hub, puis répond à la voix (modèle « voix »). Chaque rôle peut utiliser un modèle différent.
/// </summary>
public sealed class EngineerService
{
    private const string SystemPrompt =
        "Tu es l'ingénieur de course du pilote et tu lui parles à la radio pendant la course. " +
        "Réponds TOUJOURS en français, en une ou deux phrases courtes, faciles à entendre en pilotant, en TUTOYANT le pilote comme un vrai ingénieur à la radio (« tu », « ton », « ta »). " +
        "Réponds SEULEMENT à ce qui est demandé : pas de récapitulatif de la course, pas de météo ni de position si on ne te les demande pas. " +
        "Si le pilote fait une remarque sans poser de question, accuse réception en commençant par « Copy » et ajoute au plus un conseil utile. " +
        "Utilise UNIQUEMENT les chiffres des informations ci-dessous, recopiés tels quels ; ne fais aucun calcul. " +
        "Ne parle jamais de « document », de « texte » ni de « fiche » : le pilote ne les voit pas. " +
        "Si une information manque vraiment, dis en une phrase laquelle et pourquoi (le jeu ne l'envoie pas, ou il faut encore un tour). " +
        "Pour un problème d'équilibre ou de freinage, propose d'abord ce qui se règle en roulant (répartition de freinage, ABS, TC, cartographie), puis le setup au stand. " +
        "Le classement qui compte est celui de la catégorie du pilote ; les autres catégories ne sont que du trafic. " +
        "« Rentrer », « box » ou « stand » signifient l'arrêt au stand, pas la fin de la course. " +
        "Tiens compte de la mémoire de course (retours du pilote). Pas de liste, pas d'emoji, pas de markdown. " +
        "Vocabulaire du sport auto à comprendre et à employer : undercut (s'arrêter avant l'adversaire pour le passer grâce aux pneus neufs), " +
        "overcut (rester en piste plus longtemps que lui pendant qu'il perd du temps sur pneus froids), relais (stint), box, tour de sortie (out lap), " +
        "tour d'entrée (in lap), perte au stand, delta, rythme, dégradation, graining, blistering, aspiration, air sale, carrossage, pressions à chaud, " +
        "sous-virage, survirage, fenêtre d'arrêt, drapeau jaune, voiture de sécurité, VSC, trafic, voiture à un tour. " +
        "Les calculs d'undercut, d'overcut et de position à la sortie des stands sont déjà faits dans les informations : recopie-les.";

    /// <summary>Ce qui compte selon la session : la voiture en essais, le tour lancé en qualification, le déroulé en course.</summary>
    private static string SessionFocus(string? sessionType) => sessionType switch
    {
        "Practice" or "WarmUp" => "SESSION : ESSAIS. Le pilote travaille la voiture : privilégie les températures et pressions des pneus, l'équilibre, les freins, les secteurs où il perd du temps et son meilleur tour théorique.",
        "Qualification" => "SESSION : QUALIFICATION. Privilégie le tour lancé : écart au meilleur tour, secteurs, température des pneus pour attaquer, piste qui s'améliore.",
        "Race" => "SESSION : COURSE. Privilégie le déroulé : position, écarts, rythme, arrêts, carburant, undercut et overcut, menaces derrière.",
        _ => string.Empty,
    };

    private readonly RaceMemory _testMemory = new();

    private readonly OllamaClient _ollama;
    private readonly SettingsStore _settings;
    private readonly RaceEngine _engine;
    private readonly LiveHub _hub;
    private readonly ILogger<EngineerService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<(string Domain, string Role, string Text)> _history = new();
    private readonly SemaphoreSlim _thinkQueue = new(1, 1);
    private int _thinking;
    private (DateTime At, List<OllamaModel> Models) _modelCache = (DateTime.MinValue, new());
    private DateTime _stopRequestedUtc = DateTime.MinValue;

    public EngineerService(OllamaClient ollama, SettingsStore settings, RaceEngine engine, LiveHub hub, RaceMemory memory, ILogger<EngineerService> logger)
    {
        _ollama = ollama;
        _settings = settings;
        _engine = engine;
        _hub = hub;
        Memory = memory;
        _logger = logger;
        engine.SessionReset += () =>
        {
            lock (_history)
            {
                _history.Clear();
            }

            memory.Clear();
        };
    }

    public RaceMemory Memory { get; }

    /// <summary>Chaque échange radio diffusé (question / réponse, message spontané) : le carnet de session s'y abonne.</summary>
    public event Action<EngineerReply>? Replied;

    /// <summary>
    /// Le pilote coupe la radio (appui sur le bouton) : on arrête la voix sur tous les écrans et on ne fera pas parler
    /// une réponse encore en préparation (son texte reste affiché).
    /// </summary>
    public Task StopSpeakingAsync()
    {
        _stopRequestedUtc = DateTime.UtcNow;
        return _hub.BroadcastAsync("radioStop", new { at = DateTime.Now });
    }

    /// <summary>
    /// Pose une question à l'ingénieur IA (ou lui transmet une consigne, une remarque, une réponse).
    /// Ce qui se calcule (chiffres, réglages, suites de conversation, demandes d'arrêt) répond tout de suite.
    /// Ce qui demande le modèle passe par une file (une réflexion à la fois) : si elle dure plus de quelques secondes,
    /// l'ingénieur répond « Copy, je regarde ça et je reviens vers vous », le pilote peut poser d'autres questions,
    /// et la réponse arrive ensuite à la radio (« Pour la stratégie : … »).
    /// En mode test, la réponse n'est envoyée qu'à l'écran qui a posé la question (pas de diffusion radio, pas d'historique, pas de mémoire)
    /// et, s'il n'y a pas de course en cours, l'IA répond sur une situation de course fictive.
    /// </summary>
    public async Task<EngineerReply> AskAsync(string question, bool speak, string from, CancellationToken ct, bool test = false)
    {
        AiSettings ai = _settings.Current.Ai;
        var started = DateTime.UtcNow;
        Prepared prep;
        await _gate.WaitAsync(ct); // court : état de la conversation et réponses calculées, jamais pendant la réflexion du modèle
        try
        {
            prep = await PrepareAsync(question, test, ct);
        }
        finally
        {
            _gate.Release();
        }

        if (prep.Answer != null)
        {
            return await FinishAsync(prep, prep.Answer, prep.Kind, speak, from, started, ct);
        }

        if (string.IsNullOrWhiteSpace(ModelFor(prep.Domain)))
        {
            throw new InvalidOperationException("Choisissez d'abord un modèle « réflexion » dans l'onglet Ingénieur IA.");
        }

        bool queued = Volatile.Read(ref _thinking) > 0;
        Task<string> thinking = ThinkQueuedAsync(prep);
        if (test)
        {
            string answer = await thinking;
            return await FinishAsync(prep, answer, answer == prep.Brief.Suggestion ? "calculée" : "modèle", speak, from, started, ct);
        }

        Task delay = Task.Delay(TimeSpan.FromSeconds(Math.Clamp(ai.AckAfterSeconds, 0.5, 15)), ct);
        if (!queued && await Task.WhenAny(thinking, delay) == thinking)
        {
            string answer = await thinking;
            return await FinishAsync(prep, answer, answer == prep.Brief.Suggestion ? "calculée" : "modèle", speak, from, started, ct);
        }

        // Réflexion longue : accusé de réception tout de suite, réponse plus tard. Le pilote n'est pas bloqué.
        EngineerReply ack = await FinishAsync(prep, RadioStyle.Thinking(queued, ai.EnglishCalls), "accusé", speak, from, started, ct, remember: false);
        _ = Task.Run(async () =>
        {
            string later;
            string kind;
            try
            {
                later = await thinking;
                kind = later == prep.Brief.Suggestion ? "calculée" : "modèle";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Réflexion de l'IA impossible");
                later = prep.Brief.Suggestion ?? RaceBriefing.Missing(question, prep.Snapshot);
                kind = "calculée";
            }

            try
            {
                string intro = $"Pour {Specialists.Label(prep.Domain)} : ";
                await FinishAsync(prep, intro + RadioFormat.Cap(later.TrimStart()), kind, speak, from, started, CancellationToken.None, followUp: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Réponse différée impossible");
            }
        });
        return ack;
    }

    /// <summary>Question préparée : réponse immédiate (Answer) ou de quoi faire réfléchir le modèle.</summary>
    private sealed class Prepared
    {
        public string Question = "";
        public bool Test;
        public RaceSnapshot Snapshot = new();
        public string Scenario = "course en cours";
        public int? Lap;
        public RaceMemory Memory = new();
        public string? Answer;
        public string Kind = "modèle";
        public string? Category;
        public string CategorySource = "règles";
        public string? Facts;
        public BriefingResult Brief = new();
        public string Domain = Specialists.Car;
    }

    private async Task<Prepared> PrepareAsync(string question, bool test, CancellationToken ct)
    {
        AiSettings ai = _settings.Current.Ai;
        var prep = new Prepared { Question = question, Test = test, Snapshot = _engine.BuildSnapshot() };
        if (test && !prep.Snapshot.HasData)
        {
            prep.Snapshot = await Task.Run(TestScenario.Get, ct);
            prep.Scenario = "course fictive de test (aucune donnée du jeu)";
        }

        RaceSnapshot snapshot = prep.Snapshot;
        prep.Lap = snapshot.Drivers.FirstOrDefault(d => d.IsPlayer)?.Laps;
        // Mode test : mémoire à part (le fil de conversation marche, sans toucher à la vraie course).
        prep.Memory = test ? _testMemory : Memory;
        RaceMemory memory = prep.Memory;
        CarContext car = CarContext.From(snapshot, ai.WetThresholdPct);

        // Suites de conversation (« oui », « non », « autre chose ? ») avant les politesses : « ok » répond à la proposition.
        DialogReply? dialog = RadioDialog.FollowUp(question, memory, car, prep.Lap);
        string? answer = dialog?.Text ?? await HandleCommandAsync(question, test, prep.Lap, ct);
        if (dialog == null && answer == null && PitOrders.Parse(question) is PitOrder order)
        {
            // « Je boxe », « je veux des softs neufs », « la réparation aussi » : confirmé tout de suite et gardé pour l'arrêt.
            prep.Answer = PitOrders.Confirm(order, memory, snapshot, prep.Lap);
            prep.Kind = "commande";
            prep.Category = ExchangeCategorizer.Strategy;
            return prep;
        }

        if (dialog == null && answer == null && !RaceBriefing.IsShortRequest(question))
        {
            // Retour du pilote sur la voiture : base de réglages, réponse immédiate (jamais un simple « reçu »).
            dialog = RadioDialog.Feedback(question, memory, car, snapshot, prep.Lap);
        }

        if (dialog != null)
        {
            prep.Answer = ai.EnglishCalls && dialog.Kind is "retour" or "suite" ? RadioStyle.WithCopy(dialog.Text) : dialog.Text;
            prep.Kind = dialog.Kind;
            prep.Category = dialog.Category;
            return prep;
        }

        if (answer != null)
        {
            prep.Answer = answer;
            prep.Kind = "commande";
            prep.Category = ExchangeCategorizer.FromText(question) ?? ExchangeCategorizer.RaceInfo;
            return prep;
        }

        bool live = snapshot.HasData && !test;
        var extras = new BriefingExtras
        {
            Speeds = live ? _engine.BuildSpeedAnalysis() : null,
            MicroSectors = live ? _engine.BuildMicroSectors() : null,
            Weather = live ? _engine.BuildWeather() : null,
            Laps = live ? _engine.PlayerLaps() : null,
            DrivingSummary = live ? _engine.DrivingSummary() : null,
            WearSpikes = live ? _engine.WearSpikes() : null,
            Memory = memory,
            WetThresholdPct = ai.WetThresholdPct,
        };
        BriefingResult brief = RaceBriefing.Prepare(snapshot, question, extras);
        prep.Brief = brief;
        prep.Facts = brief.Facts;
        prep.Domain = Specialists.For(brief.Topics);
        prep.Category = ExchangeCategorizer.Categorize(question, brief.Topics);
        if (prep.Category == null)
        {
            prep.Category = ExchangeCategorizer.RaceInfo;
            prep.CategorySource = "défaut"; // le carnet demandera au modèle de classer l'échange
        }

        if (brief.Suggestion != null && (ai.StrictAnswers || string.IsNullOrWhiteSpace(ai.ThinkModel)))
        {
            // Réponse calculée par le hub : dite telle quelle (fiable, et sans attendre le modèle).
            prep.Answer = brief.Suggestion;
            prep.Kind = "calculée";
            if (brief.Proposal != null)
            {
                // La réponse propose quelque chose (un cran d'aileron, une sortie de virage) : « oui » / « non » s'y rattachent.
                RadioDialog.Offered(memory, brief.Proposal, "vitesse", "Vitesse de pointe", prep.Category);
            }
        }

        return prep;
    }

    /// <summary>Diffuse la réponse (voix comprise), la garde dans l'historique et le carnet.</summary>
    private async Task<EngineerReply> FinishAsync(Prepared prep, string answer, string kind, bool speak, string from, DateTime started, CancellationToken ct, bool remember = true, bool followUp = false)
    {
        if (!prep.Test && remember)
        {
            lock (_history)
            {
                _history.Add((prep.Domain, "user", prep.Question));
                _history.Add((prep.Domain, "assistant", answer));
                if (_history.Count > 20)
                {
                    _history.RemoveRange(0, 2);
                }
            }
        }

        DateTime voiceStart = DateTime.UtcNow;
        int thinkMs = (int)(voiceStart - started).TotalMilliseconds;
        string? audio = speak && _stopRequestedUtc < voiceStart ? await TrySpeakAsync(answer, ct) : null;
        if (_stopRequestedUtc > voiceStart)
        {
            audio = null; // coupé pendant la préparation de la voix
        }

        var reply = new EngineerReply
        {
            Question = followUp ? null : prep.Question,
            Answer = answer,
            AudioBase64 = audio,
            From = followUp ? "ingénieur IA" : from,
            Facts = prep.Facts,
            Milliseconds = (int)(DateTime.UtcNow - started).TotalMilliseconds,
            ThinkMilliseconds = thinkMs,
            Test = prep.Test,
            Scenario = prep.Test ? prep.Scenario : null,
            UsedPreparedAnswer = kind == "calculée",
            Kind = kind,
            Category = prep.Category,
            CategorySource = prep.CategorySource,
            FollowUp = followUp,
            InReplyTo = followUp ? prep.Question : null,
            Specialist = prep.Brief.Topics.Count > 0 || kind == "modèle" ? prep.Domain : null,
        };

        // Tous les écrans (Mac, iPad, poste du pilote) voient et peuvent entendre l'échange (sauf en mode test).
        if (!prep.Test)
        {
            await _hub.BroadcastAsync("radio", reply);
            Replied?.Invoke(reply);
        }

        return reply;
    }

    /// <summary>Phrases qui ne demandent pas de réflexion : politesses, changement de modèle, réponse à une question de l'IA, remarque à mémoriser.</summary>
    private async Task<string?> HandleCommandAsync(string text, bool test, int? lap, CancellationToken ct)
    {
        string? small = RadioCommands.SmallTalk(text);
        if (small != null)
        {
            return small;
        }

        // « On passe à mistral », « passe sur omni pour la voix »…
        if (RadioCommands.Normalize(text).Split(' ').Length <= 14)
        {
            List<OllamaModel> models = await CachedModelsAsync(ct);
            var candidates = models.Select(m => new RadioCommands.ModelCandidate(m.Id, m.Name, m.Node, m.Capabilities)).ToList();
            RadioCommands.ModelSwitch? sw = RadioCommands.ParseModelSwitch(text, candidates, _settings.Current.Ai.Aliases);
            if (sw != null)
            {
                if (sw.Ambiguous.Count > 1)
                {
                    return $"J'hésite entre {string.Join(" et ", sw.Ambiguous.Select(Pretty))}. Redites le nom plus précisément.";
                }

                if (!test)
                {
                    _settings.Update(s =>
                    {
                        switch (sw.Role)
                        {
                            case "speak": s.Ai.SpeakModel = sw.ModelId; break;
                            case "listen": s.Ai.ListenModel = sw.ModelId; break;
                            default: s.Ai.ThinkModel = sw.ModelId; break;
                        }
                    });
                }

                return $"C'est fait : {RadioCommands.RoleLabel(sw.Role)} passe sur {Pretty(sw.ModelId)}{(test ? " (mode test : rien n'est changé)" : string.Empty)}.";
            }
        }

        // ───── Musique Deezer (chantier musique) : « pause musique », « mets Daft Punk »… sans le modèle ─────
        if (await Music.MusicHook.TryHandleAsync(text, test, ct) is string music)
        {
            return music;
        }

        if (test)
        {
            return null;
        }

        // Réponses aux questions de l'IA et retours du pilote : voir RadioDialog (appelé par AskAsync).
        return null;
    }

    private string ModelFor(string domain)
    {
        AiSettings ai = _settings.Current.Ai;
        return ai.Specialists != null && ai.Specialists.TryGetValue(domain, out string? model) && !string.IsNullOrWhiteSpace(model) ? model : ai.ThinkModel;
    }

    /// <summary>File de réflexion : une question au modèle à la fois, dans l'ordre d'arrivée.</summary>
    private async Task<string> ThinkQueuedAsync(Prepared prep)
    {
        Interlocked.Increment(ref _thinking);
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            await _thinkQueue.WaitAsync(timeout.Token);
            try
            {
                return await ThinkAsync(prep, timeout.Token);
            }
            finally
            {
                _thinkQueue.Release();
            }
        }
        finally
        {
            Interlocked.Decrement(ref _thinking);
        }
    }

    /// <summary>
    /// Spécialiste du domaine de la question : consigne commune + rôle du spécialiste + faits de son domaine seulement,
    /// et seulement le dernier échange du même domaine (le contexte est vidé entre deux sujets).
    /// </summary>
    private async Task<string> ThinkAsync(Prepared prep, CancellationToken ct)
    {
        BriefingResult brief = prep.Brief;
        string system = SystemPrompt + "\n\n" + Specialists.Role(prep.Domain) + "\n\n" + SessionFocus(prep.Snapshot.SessionType) + "\n\n" + Specialists.Facts(prep.Domain, brief);
        if (brief.Suggestion != null)
        {
            system += "\n\nRÉPONSE PRÉPARÉE POUR CETTE QUESTION (à dire au pilote, presque mot pour mot) :\n" + brief.Suggestion;
        }

        var messages = new List<object> { new { role = "system", content = system } };
        if (!prep.Test)
        {
            lock (_history)
            {
                foreach (var (_, role, text) in _history.Where(h => h.Domain == prep.Domain).TakeLast(2))
                {
                    messages.Add(new { role, content = text });
                }
            }
        }

        messages.Add(new { role = "user", content = prep.Question });
        string answer = Shorten(Clean(await _ollama.ChatAsync(ModelFor(prep.Domain), messages, ct)));

        // Garde-fous : chiffre inventé, réponse en anglais ou qui parle du « document » → réponse du hub.
        bool invented = brief.Suggestion != null && HasUnknownNumber(answer, brief.Facts + " " + prep.Question);
        if (invented || RadioCommands.IsUnusableAnswer(answer))
        {
            _logger.LogInformation("Réponse IA écartée : {Answer}", answer);
            return brief.Suggestion ?? RaceBriefing.Missing(prep.Question, prep.Snapshot);
        }

        // « Je n'ai pas cette information » : on dit plutôt quelle donnée manque et pourquoi.
        if (RadioCommands.Normalize(answer).Contains("pas cette information"))
        {
            return brief.Suggestion ?? RaceBriefing.Missing(prep.Question, prep.Snapshot);
        }

        return answer;
    }

    /// <summary>Radio : deux phrases au plus. Le modèle a tendance à tout réciter.</summary>
    private static string Shorten(string text)
    {
        var sentences = System.Text.RegularExpressions.Regex.Split(text.Trim(), @"(?<=[.!?])\s+").Where(x => x.Length > 0).ToList();
        return sentences.Count <= 2 ? text.Trim() : string.Join(" ", sentences.Take(2));
    }

    private async Task<List<OllamaModel>> CachedModelsAsync(CancellationToken ct)
    {
        if ((DateTime.UtcNow - _modelCache.At).TotalSeconds < 60 && _modelCache.Models.Count > 0)
        {
            return _modelCache.Models;
        }

        try
        {
            var (models, _) = await _ollama.ListAllAsync(ct);
            _modelCache = (DateTime.UtcNow, models);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Liste des modèles indisponible");
        }

        return _modelCache.Models;
    }

    private static string Pretty(string id)
    {
        int at = id.LastIndexOf('@');
        string name = at > 0 ? id[..at] : id;
        name = name.Split(':')[0];
        return at > 0 ? $"{name} sur {id[(at + 1)..]}" : name;
    }

    public async Task<EngineerReply?> ListenAndAskAsync(byte[] audio, string fileName, bool speak, string from, CancellationToken ct, bool test = false)
    {
        AiSettings ai = _settings.Current.Ai;
        string listenModel = string.IsNullOrWhiteSpace(ai.ListenModel) ? ai.ThinkModel : ai.ListenModel;
        if (string.IsNullOrWhiteSpace(listenModel))
        {
            throw new InvalidOperationException("Choisissez un modèle « écoute » dans l'onglet Ingénieur IA.");
        }

        var sw = Stopwatch.StartNew();
        string transcript = await _ollama.TranscribeAsync(listenModel, audio, fileName, ai.Language, ct);
        int listenMs = (int)sw.ElapsedMilliseconds;
        if (string.IsNullOrWhiteSpace(transcript) || transcript.Trim().Trim('.', '…', ' ').Length < 2)
        {
            // Rien à analyser (appui court pour faire taire la radio) : on ne répond pas.
            return null;
        }

        EngineerReply reply = await AskAsync(transcript, speak, from, ct, test);
        reply.Transcript = transcript;
        reply.ListenMilliseconds = listenMs;
        return reply;
    }

    /// <summary>Transcription seule (test du micro et du modèle « écoute »).</summary>
    public async Task<DiagnosticResult> TestListenAsync(byte[] audio, string fileName, CancellationToken ct)
    {
        AiSettings ai = _settings.Current.Ai;
        string listenModel = string.IsNullOrWhiteSpace(ai.ListenModel) ? ai.ThinkModel : ai.ListenModel;
        if (string.IsNullOrWhiteSpace(listenModel))
        {
            return new DiagnosticResult { Step = "listen", Ok = false, Detail = "Aucun modèle « écoute » choisi." };
        }

        var sw = Stopwatch.StartNew();
        try
        {
            string text = await _ollama.TranscribeAsync(listenModel, audio, fileName, ai.Language, ct);
            return new DiagnosticResult { Step = "listen", Ok = !string.IsNullOrWhiteSpace(text), Milliseconds = (int)sw.ElapsedMilliseconds, Detail = string.IsNullOrWhiteSpace(text) ? "Rien entendu (micro coupé ou trop loin ?)" : $"« {text.Trim()} »", Model = listenModel };
        }
        catch (Exception ex)
        {
            return new DiagnosticResult { Step = "listen", Ok = false, Milliseconds = (int)sw.ElapsedMilliseconds, Detail = ex.Message, Model = listenModel };
        }
    }

    /// <summary>Vérifie une étape de la chaîne IA : connexion à Ollama, réflexion, voix.</summary>
    public async Task<DiagnosticResult> DiagnoseAsync(string step, CancellationToken ct)
    {
        AiSettings ai = _settings.Current.Ai;
        var sw = Stopwatch.StartNew();
        try
        {
            switch (step)
            {
                case "ollama":
                {
                    var (models, nodes) = await _ollama.ListAllAsync(ct);
                    string detail = string.Join(" · ", nodes.Select(n => n.Ok ? $"{n.Name} : {n.ModelCount} modèle(s) en {n.Milliseconds} ms" : $"{n.Name} : injoignable ({n.Error})"));
                    bool localOk = nodes.FirstOrDefault(n => n.Local)?.Ok == true;
                    return new DiagnosticResult { Step = step, Ok = localOk && models.Count > 0, Milliseconds = (int)sw.ElapsedMilliseconds, Detail = models.Count == 0 && localOk ? "Ollama répond mais aucun modèle n'est installé (ollama pull …)." : detail };
                }

                case "think":
                {
                    if (string.IsNullOrWhiteSpace(ai.ThinkModel))
                    {
                        return new DiagnosticResult { Step = step, Ok = false, Detail = "Aucun modèle « réflexion » choisi." };
                    }

                    string answer = await _ollama.ChatAsync(ai.ThinkModel, new object[]
                    {
                        new { role = "system", content = "Tu es un ingénieur de course. Réponds en français, en une phrase très courte." },
                        new { role = "user", content = "Test radio, tu m'entends ?" },
                    }, ct);
                    return new DiagnosticResult { Step = step, Ok = !string.IsNullOrWhiteSpace(answer), Milliseconds = (int)sw.ElapsedMilliseconds, Detail = Clean(answer), Model = ai.ThinkModel };
                }

                case "speak":
                {
                    string speakModel = string.IsNullOrWhiteSpace(ai.SpeakModel) ? ai.ThinkModel : ai.SpeakModel;
                    if (string.IsNullOrWhiteSpace(speakModel))
                    {
                        return new DiagnosticResult { Step = step, Ok = false, Detail = "Aucun modèle « voix » choisi." };
                    }

                    byte[] wav = await _ollama.SpeakAsync(speakModel, "Test radio. Je te reçois cinq sur cinq.", ai.Voice, ct);
                    return new DiagnosticResult { Step = step, Ok = wav.Length > 100, Milliseconds = (int)sw.ElapsedMilliseconds, Detail = $"Voix « {ai.Voice} » générée ({wav.Length / 1024} Ko).", AudioBase64 = Convert.ToBase64String(wav), Model = speakModel };
                }

                default:
                    return new DiagnosticResult { Step = step, Ok = false, Detail = "Étape inconnue" };
            }
        }
        catch (Exception ex)
        {
            return new DiagnosticResult { Step = step, Ok = false, Milliseconds = (int)sw.ElapsedMilliseconds, Detail = step == "ollama" ? $"Ollama injoignable ({ex.Message}). Est-il lancé sur ce Mac ?" : ex.Message };
        }
    }

    /// <summary>Message radio sans question (alerte du hub, consigne de l'ingénieur humain, intervention spontanée de l'IA).</summary>
    public async Task<EngineerReply> SayAsync(string text, string from, CancellationToken ct, bool proactive = false, bool awaitingAnswer = false, string? category = null, bool urgent = false)
    {
        var started = DateTime.UtcNow;
        string? audio = await TrySpeakAsync(text, ct);
        var reply = new EngineerReply { Answer = text, From = from, AudioBase64 = _stopRequestedUtc > started ? null : audio, Proactive = proactive, AwaitingAnswer = awaitingAnswer, Urgent = urgent, Kind = proactive ? (urgent ? "alerte" : "spontanée") : "message", Category = category ?? ExchangeCategorizer.FromText(text) ?? ExchangeCategorizer.RaceInfo };
        await _hub.BroadcastAsync("radio", reply);
        Replied?.Invoke(reply);
        return reply;
    }

    private async Task<string?> TrySpeakAsync(string text, CancellationToken ct)
    {
        AiSettings ai = _settings.Current.Ai;
        string speakModel = string.IsNullOrWhiteSpace(ai.SpeakModel) ? ai.ThinkModel : ai.SpeakModel;
        if (string.IsNullOrWhiteSpace(speakModel) || string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        try
        {
            // Les modèles de voix lisent mal les chiffres : tout est écrit en lettres pour la synthèse.
            byte[] wav = await _ollama.SpeakAsync(speakModel, FrenchSpeech.ToSpeech(text), ai.Voice, ct);
            return Convert.ToBase64String(wav);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Synthèse vocale impossible");
            return null;
        }
    }

    private static bool HasUnknownNumber(string answer, string source)
    {
        static IEnumerable<string> Numbers(string text) =>
            System.Text.RegularExpressions.Regex.Matches(text, @"\d+(?:[.,]\d+)?").Select(m => m.Value.Replace('.', ','));
        var known = Numbers(source).ToHashSet();
        return Numbers(answer).Any(n => !known.Contains(n));
    }

    private static string Clean(string text) => text.Replace("**", string.Empty).Replace("#", string.Empty).Trim();
}

public sealed class EngineerReply
{
    public string? Question { get; set; }

    public string? Transcript { get; set; }

    public string Answer { get; set; } = "";

    public string? AudioBase64 { get; set; }

    public string From { get; set; } = "";

    public string? Facts { get; set; }

    public int Milliseconds { get; set; }

    /// <summary>Temps de transcription de la voix (ms), si la question était orale.</summary>
    public int? ListenMilliseconds { get; set; }

    /// <summary>Temps de réflexion du modèle (ms), hors synthèse vocale.</summary>
    public int? ThinkMilliseconds { get; set; }

    public bool Test { get; set; }

    public string? Scenario { get; set; }

    /// <summary>Vrai si le hub a remplacé (ou validé mot pour mot) la réponse du modèle par sa réponse préparée.</summary>
    public bool UsedPreparedAnswer { get; set; }

    /// <summary>« calculée » (par le hub), « modèle » (formulée par l'IA), « commande », « accusé » (réponse à venir), « spontanée », « alerte », « message ».</summary>
    public string? Kind { get; set; }

    /// <summary>L'IA a pris la parole d'elle-même.</summary>
    public bool Proactive { get; set; }

    /// <summary>L'IA attend une réponse du pilote.</summary>
    public bool AwaitingAnswer { get; set; }

    /// <summary>Catégorie du carnet : « Stratégie », « Réglages de course », « Réglages de la voiture », « Informations course ».</summary>
    public string? Category { get; set; }

    /// <summary>« règles » ou « défaut » (le carnet fait alors classer l'échange par le modèle).</summary>
    public string? CategorySource { get; set; }

    /// <summary>Alerte de pilotage (limites de piste, usure brutale…) : dite tout de suite.</summary>
    public bool Urgent { get; set; }

    /// <summary>Réponse arrivée après « Copy, je reviens vers vous » ; InReplyTo rappelle la question.</summary>
    public bool FollowUp { get; set; }

    public string? InReplyTo { get; set; }

    /// <summary>Spécialiste qui a répondu : « pneus », « stratégie », « voiture », « course ».</summary>
    public string? Specialist { get; set; }

    public DateTime At { get; set; } = DateTime.Now;
}

public sealed class DiagnosticResult
{
    public string Step { get; set; } = "";

    public bool Ok { get; set; }

    public int Milliseconds { get; set; }

    public string Detail { get; set; } = "";

    public string? Model { get; set; }

    public string? AudioBase64 { get; set; }
}
