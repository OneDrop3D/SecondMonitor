namespace SecondMonitor.Hub.Services;

using SecondMonitor.Hub.Core.Ai;
using SecondMonitor.Hub.Core.Race;

/// <summary>
/// Carnet de session tenu automatiquement : chaque radio (avec sa catégorie), les notes et retours du pilote, les réglages
/// acceptés ou refusés, les tours et le résultat. Un fichier JSON par session dans &lt;dossier de données&gt;/carnets.
/// À la fin de la session (arrivée de la course, ou changement de session), un récapitulatif est généré :
/// calculé sans IA, plus un résumé rédigé par le modèle « réflexion » s'il répond.
/// </summary>
public sealed class CarnetService : BackgroundService
{
    private readonly RaceEngine _engine;
    private readonly EngineerService _engineer;
    private readonly OllamaClient _ollama;
    private readonly SettingsStore _settings;
    private readonly ILogger<CarnetService> _logger;
    private readonly RaceResumeKeeper _resume;
    private readonly object _lock = new();
    private CarnetSession? _current;
    private string? _currentKey;
    private DateTime _lastSave = DateTime.MinValue;
    private DateTime? _finishedSince;
    private bool _dirty;

    public CarnetService(RaceEngine engine, EngineerService engineer, OllamaClient ollama, SettingsStore settings, RaceResumeKeeper resume, ILogger<CarnetService> logger)
    {
        _engine = engine;
        _resume = resume;
        resume.CarnetId = () => CurrentId; // gardé dans la sauvegarde de reprise : le carnet continue après un redémarrage
        _engineer = engineer;
        _ollama = ollama;
        _settings = settings;
        _logger = logger;
        Store = new CarnetStore(Path.Combine(settings.DataDirectory, "carnets"));
        engineer.Replied += OnReply;
        engine.SessionReset += () => Close("changement de session");
    }

    public CarnetStore Store { get; }

    public string? CurrentId
    {
        get
        {
            lock (_lock)
            {
                return _current?.Id;
            }
        }
    }

    /// <summary>Copie du carnet en cours (à jour) ou carnet enregistré (pour l'API).</summary>
    public CarnetSession? Get(string id)
    {
        lock (_lock)
        {
            if (_current != null && _current.Id == id)
            {
                Refresh(_current, _engine.BuildSnapshot());
                return System.Text.Json.JsonSerializer.Deserialize<CarnetSession>(System.Text.Json.JsonSerializer.Serialize(_current));
            }
        }

        return Store.Load(id);
    }

    private CarnetSession? Live(string id)
    {
        lock (_lock)
        {
            if (_current != null && _current.Id == id)
            {
                Refresh(_current, _engine.BuildSnapshot());
                return _current;
            }
        }

        return Store.Load(id);
    }

    public List<CarnetSummary> List()
    {
        var list = Store.List();
        lock (_lock)
        {
            if (_current != null && _current.HasContent)
            {
                list.RemoveAll(s => s.Id == _current.Id);
                CarnetSummary cur = CarnetStore.Summarize(_current, current: true);
                cur.Current = true;
                list.Insert(0, cur);
            }
        }

        return list;
    }

    public CarnetSession? AddNote(string id, string text, string source)
    {
        lock (_lock)
        {
            if (_current != null && _current.Id == id)
            {
                // Note de la session en cours : dans la mémoire de course (l'IA s'en sert), puis copiée dans le carnet.
                int? lap = _engine.BuildSnapshot().Drivers.FirstOrDefault(d => d.IsPlayer)?.Laps;
                _engineer.Memory.Add(source, "note", text, lap);
                Refresh(_current, _engine.BuildSnapshot());
                Save(_current);
                return _current;
            }
        }

        CarnetSession? c = Store.Load(id);
        if (c == null)
        {
            return null;
        }

        c.Notes.Add(new CarnetNote { Id = -(c.Notes.Count + 1), At = DateTime.Now, Source = source, Topic = "note", Text = text });
        Store.Save(c);
        return c;
    }

    public CarnetBilan? Bilan(string id) => Get(id) is { } c ? CarnetBuilder.Bilan(c) : null;

    /// <summary>Le pilote compte ou retire un tour des moyennes du bilan ; le choix est enregistré dans le carnet.</summary>
    public CarnetBilan? CompterTour(string id, int tour, bool compte)
    {
        lock (_lock)
        {
            if (_current != null && _current.Id == id)
            {
                CarnetBuilder.CompterTour(_current, tour, compte);
                Save(_current);
                return CarnetBuilder.Bilan(_current);
            }
        }

        CarnetSession? c = Store.Load(id);
        if (c == null)
        {
            return null;
        }

        CarnetBuilder.CompterTour(c, tour, compte);
        Store.Save(c);
        return CarnetBuilder.Bilan(c);
    }

    public bool Delete(string id)
    {
        lock (_lock)
        {
            if (_current?.Id == id)
            {
                _current = null;
                _currentKey = null;
            }
        }

        return Store.Delete(id);
    }

    /// <summary>(Re)génère le récapitulatif : calculé, puis rédigé par l'IA si elle répond.</summary>
    public async Task<CarnetSession?> RecapAsync(string id, CancellationToken ct)
    {
        CarnetSession? c = Live(id);
        if (c == null)
        {
            return null;
        }

        CarnetRecap recap;
        lock (_lock)
        {
            recap = CarnetBuilder.Recap(c);
        }

        string model = _settings.Current.Ai.ThinkModel;
        if (string.IsNullOrWhiteSpace(model))
        {
            recap.Note = "Aucun modèle « réflexion » choisi : récapitulatif calculé seulement.";
        }
        else
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(90));
                string prompt;
                lock (_lock)
                {
                    prompt = CarnetBuilder.AiPrompt(c, recap);
                }

                string text = await _ollama.ChatAsync(model, new object[] { new { role = "user", content = prompt } }, timeout.Token, maxTokens: 600);
                text = text.Replace("**", string.Empty).Replace("#", string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(text) && !RadioCommands.IsUnusableAnswer(text))
                {
                    recap.Ai = text;
                    recap.Model = model;
                }
                else
                {
                    recap.Note = "Le résumé de l'IA était inutilisable : récapitulatif calculé seulement.";
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                _logger.LogInformation(ex, "Résumé IA du carnet impossible");
                recap.Note = "IA indisponible (Ollama ne répond pas) : récapitulatif calculé seulement.";
            }
        }

        lock (_lock)
        {
            c.Recap = recap;
            Save(c);
        }

        return Get(id) ?? c;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                string? finishedId = Tick();
                if (finishedId != null)
                {
                    await RecapAsync(finishedId, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Carnet de session : mise à jour impossible");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_current?.HasContent == true)
            {
                Save(_current);
            }
        }

        await base.StopAsync(cancellationToken);
    }

    /// <summary>Mise à jour périodique ; retourne l'identifiant d'un carnet dont la course vient de finir (récapitulatif à faire).</summary>
    private string? Tick()
    {
        if ((DateTime.UtcNow - _engine.LastUpdateUtc).TotalSeconds > 10)
        {
            return null;
        }

        RaceSnapshot snap = _engine.BuildSnapshot();
        if (!snap.HasData)
        {
            return null;
        }

        lock (_lock)
        {
            if (Ended(snap))
            {
                return null;
            }

            _endedKey = null;
            CarnetSession c = Ensure(snap);
            Refresh(c, snap);
            if ((DateTime.UtcNow - _lastSave).TotalSeconds >= 15 || _dirty)
            {
                Save(c);
            }

            // Course finie : récapitulatif 20 s après l'arrivée (le temps de la radio d'arrivée).
            if (c.Finished && c.Recap == null)
            {
                _finishedSince ??= DateTime.UtcNow;
                if ((DateTime.UtcNow - _finishedSince.Value).TotalSeconds >= 20)
                {
                    c.Recap = new CarnetRecap { Note = "Génération en cours…" };
                    return c.Id;
                }
            }
        }

        return null;
    }

    private void OnReply(EngineerReply r)
    {
        RaceSnapshot snap = _engine.BuildSnapshot();
        if (!snap.HasData)
        {
            return;
        }

        CarnetRadio entry;
        lock (_lock)
        {
            if (Ended(snap))
            {
                return;
            }

            CarnetSession c = Ensure(snap);
            entry = new CarnetRadio
            {
                At = r.At,
                Lap = snap.Drivers.FirstOrDefault(d => d.IsPlayer)?.Laps,
                From = r.Proactive ? "ingénieur IA" : r.From,
                Question = r.Transcript ?? r.Question,
                Answer = r.Answer,
                Kind = r.Kind,
                Category = r.Category ?? ExchangeCategorizer.RaceInfo,
                CategorySource = r.CategorySource ?? "règles",
                Proactive = r.Proactive,
            };
            c.Radios.Add(entry);
            Refresh(c, snap);
            _dirty = true;
        }

        // Aucune règle n'a tranché : on demande au modèle (en tâche de fond, la radio n'attend pas).
        if (entry.CategorySource == "défaut" && !string.IsNullOrWhiteSpace(_settings.Current.Ai.ThinkModel))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    string raw = await _ollama.ChatAsync(_settings.Current.Ai.ThinkModel, new object[] { new { role = "user", content = ExchangeCategorizer.Prompt(entry.Question ?? string.Empty, entry.Answer) } }, cts.Token, maxTokens: 12);
                    if (ExchangeCategorizer.Parse(raw) is string cat)
                    {
                        lock (_lock)
                        {
                            entry.Category = cat;
                            entry.CategorySource = "modèle";
                            _dirty = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Catégorie par le modèle impossible");
                }
            });
        }
    }

    private string? _endedKey;

    /// <summary>« Terminer la session » : le carnet en cours est fermé et récapitulé ; plus rien n'y est ajouté jusqu'à une nouvelle session.</summary>
    public string? EndCurrent()
    {
        string? id;
        lock (_lock)
        {
            id = _current?.Id;
            _endedKey = _currentKey ?? (_engine.BuildSnapshot() is { HasData: true } snap ? CarnetBuilder.SessionKey(snap) : null);
        }

        Close("fin de session demandée par le pilote");
        return id;
    }

    /// <summary>« Nouvelle session » : on repart d'un carnet vide (le précédent est fermé et récapitulé).</summary>
    public void StartNew()
    {
        Close("nouvelle session demandée par le pilote");
        lock (_lock)
        {
            _endedKey = null;
        }
    }

    /// <summary>La session terminée à la main continue dans le jeu : on n'ouvre pas de nouveau carnet tant qu'elle dure.</summary>
    private bool Ended(RaceSnapshot snap) => _endedKey != null && CarnetBuilder.SessionKey(snap) == _endedKey;

    /// <summary>Fin de session (changement de session) : on ferme le carnet et on génère son récapitulatif.</summary>
    private void Close(string reason)
    {
        CarnetSession? closing;
        lock (_lock)
        {
            closing = _current;
            _current = null;
            _currentKey = null;
            _finishedSince = null;
            if (closing == null || !closing.HasContent)
            {
                return;
            }

            closing.EndedAt = DateTime.Now;
            Save(closing);
        }

        _logger.LogInformation("Carnet {Id} fermé ({Reason})", closing.Id, reason);
        if (closing.Recap == null || closing.Recap.Ai == null)
        {
            _ = Task.Run(() => RecapAsync(closing.Id, CancellationToken.None));
        }
    }

    private CarnetSession Ensure(RaceSnapshot snap)
    {
        string key = CarnetBuilder.SessionKey(snap);
        if (_current != null && _currentKey != key)
        {
            // Session différente sans remise à zéro signalée : on ferme l'ancienne proprement.
            CarnetSession old = _current;
            old.EndedAt = DateTime.Now;
            if (old.HasContent)
            {
                Save(old);
                if (old.Recap == null)
                {
                    _ = Task.Run(() => RecapAsync(old.Id, CancellationToken.None));
                }
            }

            _current = null;
        }

        if (_current == null)
        {
            _current = Resumed(snap) ?? CarnetBuilder.Create(snap, DateTime.Now);
            _currentKey = key;
            _finishedSince = null;
        }

        return _current;
    }

    /// <summary>
    /// Course reprise après un redémarrage de SecondMonitor : on continue son carnet (même fichier, radios et tours ajoutés
    /// à la suite) au lieu d'en ouvrir un nouveau.
    /// </summary>
    private CarnetSession? Resumed(RaceSnapshot snap)
    {
        string? id = _resume.TakeResumedCarnetId();
        CarnetSession? c = id == null ? null : Store.Load(id);
        if (c == null || c.Track != snap.Track?.Name || c.SessionType != snap.SessionType || c.Game != CarnetBuilder.GameName(snap.Source))
        {
            return null;
        }

        c.EndedAt = null;
        _logger.LogInformation("Carnet {Id} continué (course reprise)", c.Id);
        return c;
    }

    private void Refresh(CarnetSession c, RaceSnapshot snap)
    {
        if (snap.HasData && CarnetBuilder.SessionKey(snap) == _currentKey)
        {
            CarnetBuilder.Update(c, snap, _engine.PlayerLaps(), _engineer.Memory);
        }
    }

    private void Save(CarnetSession c)
    {
        if (!c.HasContent)
        {
            return;
        }

        try
        {
            Store.Save(c);
            _lastSave = DateTime.UtcNow;
            _dirty = false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Carnet {Id} : enregistrement impossible", c.Id);
        }
    }
}
