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
        "Réponds TOUJOURS en français, en une ou deux phrases courtes, faciles à entendre en pilotant, en vouvoyant le pilote. " +
        "Utilise UNIQUEMENT les chiffres des informations ci-dessous, recopiés tels quels ; ne fais aucun calcul. " +
        "Ne parle jamais de « document », de « texte » ni de « fiche » : le pilote ne les voit pas. " +
        "Si l'information manque, dis simplement « Je n'ai pas cette information pour le moment ». " +
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

    private const string Unknown = "Je n'ai pas cette information pour le moment.";

    private readonly OllamaClient _ollama;
    private readonly SettingsStore _settings;
    private readonly RaceEngine _engine;
    private readonly LiveHub _hub;
    private readonly ILogger<EngineerService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<(string Role, string Text)> _history = new();
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
    /// En mode test, la réponse n'est envoyée qu'à l'écran qui a posé la question (pas de diffusion radio, pas d'historique, pas de mémoire)
    /// et, s'il n'y a pas de course en cours, l'IA répond sur une situation de course fictive.
    /// </summary>
    public async Task<EngineerReply> AskAsync(string question, bool speak, string from, CancellationToken ct, bool test = false)
    {
        AiSettings ai = _settings.Current.Ai;
        await _gate.WaitAsync(ct);
        try
        {
            var started = DateTime.UtcNow;
            RaceSnapshot snapshot = _engine.BuildSnapshot();
            string scenario = "course en cours";
            if (test && !snapshot.HasData)
            {
                snapshot = await Task.Run(TestScenario.Get, ct);
                scenario = "course fictive de test (aucune donnée du jeu)";
            }

            int? lap = snapshot.Drivers.FirstOrDefault(d => d.IsPlayer)?.Laps;
            string? facts = null;
            string kind;
            string? answer = await HandleCommandAsync(question, test, lap, ct);
            if (answer != null)
            {
                kind = "commande";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(ai.ThinkModel))
                {
                    throw new InvalidOperationException("Choisissez d'abord un modèle « réflexion » dans l'onglet Ingénieur IA.");
                }

                var extras = new BriefingExtras
                {
                    Speeds = snapshot.HasData && !test ? _engine.BuildSpeedAnalysis() : null,
                    MicroSectors = snapshot.HasData && !test ? _engine.BuildMicroSectors() : null,
                    Weather = snapshot.HasData && !test ? _engine.BuildWeather() : null,
                    Memory = test ? null : Memory,
                    WetThresholdPct = ai.WetThresholdPct,
                };
                BriefingResult brief = RaceBriefing.Prepare(snapshot, question, extras);
                facts = brief.Facts;
                if (brief.Suggestion != null && ai.StrictAnswers)
                {
                    // Réponse calculée par le hub : dite telle quelle (fiable, et sans attendre le modèle).
                    answer = brief.Suggestion;
                    kind = "calculée";
                }
                else
                {
                    answer = await ThinkAsync(question, brief, test, ct);
                    kind = answer == brief.Suggestion ? "calculée" : "modèle";
                }
            }

            if (!test)
            {
                lock (_history)
                {
                    _history.Add(("user", question));
                    _history.Add(("assistant", answer));
                    if (_history.Count > 20)
                    {
                        _history.RemoveRange(0, 2);
                    }
                }
            }

            int thinkMs = (int)(DateTime.UtcNow - started).TotalMilliseconds;
            string? audio = speak && _stopRequestedUtc < started ? await TrySpeakAsync(answer, ct) : null;
            if (_stopRequestedUtc > started)
            {
                audio = null; // coupé pendant la préparation de la voix
            }
            var reply = new EngineerReply
            {
                Question = question,
                Answer = answer,
                AudioBase64 = audio,
                From = from,
                Facts = facts,
                Milliseconds = (int)(DateTime.UtcNow - started).TotalMilliseconds,
                ThinkMilliseconds = thinkMs,
                Test = test,
                Scenario = test ? scenario : null,
                UsedPreparedAnswer = kind == "calculée",
                Kind = kind,
            };

            // Tous les écrans (Mac, iPad, poste du pilote) voient et peuvent entendre l'échange (sauf en mode test).
            if (!test)
            {
                await _hub.BroadcastAsync("radio", reply);
            }

            return reply;
        }
        finally
        {
            _gate.Release();
        }
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

        // Réponse du pilote à une question que l'IA vient de lui poser.
        PendingQuestion? pending = Memory.Pending();
        if (pending != null && (RadioCommands.YesNo(text) != null || RadioCommands.IsFeedback(text) || !RadioCommands.IsQuestion(text)))
        {
            Memory.ClearPending();
            var (reply, note) = ProactiveEngineer.HandleAnswer(pending, text, Memory);
            Memory.Add("pilote", pending.Topic, note, lap);
            return reply;
        }

        // Remarque spontanée sur la voiture ou la piste : on la garde pour la suite de la course.
        if (RadioCommands.IsFeedback(text))
        {
            Memory.Add("pilote", "retour", $"« {text.Trim().TrimEnd('.', '!')} »", lap);
            return "Reçu, c'est noté. J'en tiens compte pour la suite.";
        }

        return null;
    }

    private async Task<string> ThinkAsync(string question, BriefingResult brief, bool test, CancellationToken ct)
    {
        AiSettings ai = _settings.Current.Ai;
        string system = SystemPrompt + "\n\n" + SessionFocus(_engine.BuildSnapshot().SessionType) + "\n\nINFORMATIONS (chiffres exacts calculés par l'ordinateur) :\n" + brief.Facts;
        if (brief.Suggestion != null)
        {
            system += "\n\nRÉPONSE PRÉPARÉE POUR CETTE QUESTION (à dire au pilote, presque mot pour mot) :\n" + brief.Suggestion;
        }

        var messages = new List<object> { new { role = "system", content = system } };
        if (!test)
        {
            // Un peu de contexte de conversation (questions/réponses récentes), sans les anciens chiffres.
            lock (_history)
            {
                foreach (var (role, text) in _history.TakeLast(4))
                {
                    messages.Add(new { role, content = text });
                }
            }
        }

        messages.Add(new { role = "user", content = question });
        string answer = Clean(await _ollama.ChatAsync(ai.ThinkModel, messages, ct));

        // Garde-fous : chiffre inventé, réponse en anglais ou qui parle du « document » → réponse du hub.
        bool invented = brief.Suggestion != null && HasUnknownNumber(answer, brief.Facts + " " + question);
        if (invented || RadioCommands.IsUnusableAnswer(answer))
        {
            _logger.LogInformation("Réponse IA écartée : {Answer}", answer);
            return brief.Suggestion ?? Unknown;
        }

        return answer;
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
    public async Task<EngineerReply> SayAsync(string text, string from, CancellationToken ct, bool proactive = false, bool awaitingAnswer = false)
    {
        var started = DateTime.UtcNow;
        string? audio = await TrySpeakAsync(text, ct);
        var reply = new EngineerReply { Answer = text, From = from, AudioBase64 = _stopRequestedUtc > started ? null : audio, Proactive = proactive, AwaitingAnswer = awaitingAnswer, Kind = proactive ? "spontanée" : "message" };
        await _hub.BroadcastAsync("radio", reply);
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

    /// <summary>« calculée » (par le hub), « modèle » (formulée par l'IA), « commande », « spontanée », « message ».</summary>
    public string? Kind { get; set; }

    /// <summary>L'IA a pris la parole d'elle-même.</summary>
    public bool Proactive { get; set; }

    /// <summary>L'IA attend une réponse du pilote.</summary>
    public bool AwaitingAnswer { get; set; }

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
