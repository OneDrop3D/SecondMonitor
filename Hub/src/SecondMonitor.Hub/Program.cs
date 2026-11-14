using System.Net;
using SecondMonitor.Hub;
using SecondMonitor.Hub.Core.Race;
using SecondMonitor.Hub.Services;

// SecondMonitor — l'application qui affiche les données (Mac, Windows ou Linux).
// Reçoit les données du collecteur, calcule la stratégie, affiche l'interface dans sa fenêtre et parle à Ollama.
// Les autres écrans du réseau (iPad, téléphone, PC de jeu) peuvent aussi l'ouvrir dans un navigateur.
//   (sans option)    fenêtre de l'application
//   --navigateur     interface dans le navigateur au lieu de la fenêtre
//   --sans-fenetre   serveur seul (machine sans écran, Raspberry Pi…)
//   --sans-gpu       fenêtre sans accélération graphique (Windows : fenêtre noire avec certains pilotes)
bool headless = args.Contains("--sans-fenetre") || args.Contains("--serveur");
bool useBrowser = args.Contains("--navigateur") || args.Contains("--ouvrir");
bool noGpu = args.Contains("--sans-gpu");
// Diagnostic : rejoue un enregistrement (dossier « enregistrements ») au lieu d'écouter le réseau.
int replayIndex = Array.IndexOf(args, "--rejouer");
if (replayIndex >= 0 && replayIndex + 1 < args.Length)
{
    DataSourceService.ReplayFile = Path.GetFullPath(args[replayIndex + 1]);
    int speedIndex = Array.IndexOf(args, "--vitesse");
    if (speedIndex >= 0 && speedIndex + 1 < args.Length && double.TryParse(args[speedIndex + 1], System.Globalization.CultureInfo.InvariantCulture, out double speed))
    {
        DataSourceService.ReplaySpeed = speed;
    }
}
string dataDir = Environment.GetEnvironmentVariable("SMHUB_DATA") ?? SettingsStore.DefaultDataDirectory();
Directory.CreateDirectory(dataDir);
var settings = new SettingsStore(dataDir);
if (args.Contains("--demo"))
{
    settings.Update(s => s.DemoMode = true);
}

// Application installée (Mac .app, Windows, Linux) : l'interface est à côté du programme, quel que soit le dossier de lancement.
string installedRoot = AppContext.BaseDirectory;
var builder = Directory.Exists(Path.Combine(installedRoot, "wwwroot"))
    ? WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ContentRootPath = installedRoot, WebRootPath = Path.Combine(installedRoot, "wwwroot") })
    : WebApplication.CreateBuilder(args);
int webPort = settings.Current.WebPort;
// Windows : 127.0.0.1 plutôt que « localhost » (Kestrel n'écoute qu'en IPv4 ; WebView2 peut essayer ::1 d'abord).
// Mac et Linux gardent « localhost » : les préférences de la fenêtre (disposition, bouton du volant…) dépendent de l'adresse.
string localUrl = OperatingSystem.IsWindows() ? $"http://127.0.0.1:{webPort}" : $"http://localhost:{webPort}";
noGpu |= settings.Current.FenetreSansGpu;
var (portFree, alreadyRunning) = AppWindow.ProbePort(webPort);
if (!portFree)
{
    if (alreadyRunning && !headless)
    {
        // SecondMonitor tourne déjà avec sa fenêtre : il ouvre une fenêtre de plus (muette, sa propre page), pour un autre écran.
        if (!useBrowser && AppWindow.AskRunningInstance(webPort))
        {
            return;
        }

        // Lancé sans fenêtre (ou ancienne version) : on ouvre simplement son interface ici.
        if (useBrowser || !AppWindow.Run(localUrl, "SecondMonitor", dataDir, noGpu, remember: false))
        {
            AppWindow.OpenBrowser(localUrl);
        }

        return;
    }

    AppWindow.Message(
        headless,
        "SecondMonitor ne peut pas démarrer",
        alreadyRunning
            ? $"SecondMonitor tourne déjà sur cet ordinateur : {localUrl}"
            : $"Le port {webPort} est déjà utilisé par un autre programme.\nFermez-le, ou changez « webPort » dans {Path.Combine(dataDir, "settings.json")}.");
    return;
}

// HTTPS facultatif : si son port est pris, on démarre quand même (sans micro depuis les autres appareils).
var httpsCertificate = settings.Current.EnableHttps && AppWindow.ProbePort(settings.Current.HttpsPort).Free ? HttpsCertificate.LoadOrCreate(dataDir) : null;
builder.WebHost.ConfigureKestrel(k =>
{
    k.Listen(IPAddress.Any, settings.Current.WebPort); // accessible depuis l'iPad/téléphone du réseau local
    if (httpsCertificate != null)
    {
        // HTTPS : le navigateur n'autorise le micro et le volant (Gamepad API) qu'en HTTPS hors de « localhost ».
        k.Listen(IPAddress.Any, settings.Current.HttpsPort, o => o.UseHttps(httpsCertificate));
    }
});
builder.Services.AddSingleton(settings);
builder.Services.AddSingleton(new RaceEngine(dataDir) { ThermalProfiles = () => settings.Current.ThermalProfiles });
builder.Services.AddSingleton<SecondMonitor.Hub.Core.Ai.RaceMemory>();
builder.Services.AddSingleton<DataSourceService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DataSourceService>());
builder.Services.AddSingleton<LiveHub>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<LiveHub>());
builder.Services.AddHttpClient<OllamaClient>();
builder.Services.AddSingleton<EngineerService>();
builder.Services.AddHostedService<ProactiveService>();
CarnetApi.Register(builder.Services); // carnet de session (IA)
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    FiniteNumbers.Register(o.SerializerOptions); // NaN / infini → null, jamais « "NaN" » (faisait planter l'interface)
});

var app = builder.Build();
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(15) });
app.UseDefaultFiles();
app.UseStaticFiles();

app.Map("/ws", async (HttpContext ctx, LiveHub hub) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = 400;
        return;
    }

    using var socket = await ctx.WebSockets.AcceptWebSocketAsync();
    // « ?poste=1 » : collecteur du PC de jeu qui joue la voix de l'ingénieur ou enregistre le micro (sans l'état de course).
    await hub.AcceptAsync(socket, ctx.RequestAborted, ctx.Request.Query["poste"] == "1", ctx.Connection.RemoteIpAddress?.ToString());
});

app.MapGet("/api/snapshot", (RaceEngine engine) => Results.Json(engine.BuildSnapshot(), LiveHub.Json));
app.MapGet("/api/drivers/{id}/laps", (string id, RaceEngine engine) => engine.Read((trackers, _) =>
    trackers.TryGetValue(id, out DriverTracker? t)
        ? Results.Json(t.Laps.Select(l => new { lap = l.Lap, time = l.Time, pit = l.IsPitLap, outLap = l.IsOutLap, valid = l.Valid, compound = l.Compound, fuel = l.FuelUsed, energy = l.EnergyUsedPct }).ToList(), LiveHub.Json)
        : Results.NotFound()));
app.MapGet("/api/status", (DataSourceService source, LiveHub hub) => Results.Json(new
{
    source = source.GetStatus(),
    clients = hub.ClientCount,
    version = typeof(Program).Assembly.GetName().Version?.ToString(),
    dataDir,
    webPort = settings.Current.WebPort,
    httpsPort = httpsCertificate != null ? settings.Current.HttpsPort : (int?)null,
    stations = hub.Stations, // postes audio du PC de jeu (voix de l'ingénieur, micro)
    voiceOutput = settings.Current.VoiceOutput,
}));

// ───── Analyses détaillées (à la demande, hors flux temps réel) ─────
static string[] Ids(string? ids) => (ids ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
app.MapGet("/api/analysis/laps", (RaceEngine engine) => Results.Json(engine.AllLaps(), LiveHub.Json));
app.MapGet("/api/analysis/microsectors", (RaceEngine engine) => Results.Json(engine.BuildMicroSectors(), LiveHub.Json));
app.MapGet("/api/analysis/microsectors/laps", (RaceEngine engine, string? ids) => Results.Json(engine.BuildMicroSectorLaps(Ids(ids)), LiveHub.Json));
app.MapGet("/api/analysis/lapcompare", (RaceEngine engine, string? laps) => Results.Json(engine.BuildLapComparison(Ids(laps ?? "best,last")), LiveHub.Json));
app.MapGet("/api/analysis/speeds", (RaceEngine engine) => Results.Json(engine.BuildSpeedAnalysis(), LiveHub.Json));
app.MapGet("/api/analysis/speedtrace", (RaceEngine engine, string? ids, string? kind) => Results.Json(engine.BuildSpeedTraces(Ids(ids), kind != "last"), LiveHub.Json));
app.MapGet("/api/analysis/trajectories", (RaceEngine engine, string? ids, string? seg, string? kind) => Results.Json(engine.BuildTrajectories(Ids(ids), seg, kind != "last"), LiveHub.Json));
app.MapGet("/api/analysis/inputs", (RaceEngine engine, string? lap) =>
    engine.InputTrace(lap ?? "last") is { } trace ? Results.Json(trace, LiveHub.Json) : Results.Json(new { laps = Array.Empty<int>() }, LiveHub.Json));
app.MapGet("/api/player/laps", (RaceEngine engine) => Results.Json(engine.PlayerLaps(), LiveHub.Json));
app.MapGet("/api/telemetry", (RaceEngine engine, double? since) => Results.Json(engine.ReadTelemetry(since ?? double.NegativeInfinity), LiveHub.Json));
app.MapGet("/api/weather", (RaceEngine engine) => Results.Json(engine.BuildWeather(), LiveHub.Json));
app.MapGet("/api/settings", (SettingsStore store) => store.Current);
app.MapPut("/api/settings", (SettingsStore store, HubSettings incoming) =>
{
    store.Update(s =>
    {
        s.GameHosts = incoming.GameHosts ?? new List<string>();
        s.DemoMode = incoming.DemoMode;
        s.DemoSpeed = incoming.DemoSpeed;
        s.Ai = incoming.Ai ?? new AiSettings();
        // Lexique du pilote : seulement des corrections complètes et courtes ; morceaux d'audio entre 5 s et 2 min.
        s.Ai.Lexicon = (s.Ai.Lexicon ?? new()).Where(SecondMonitor.Hub.Core.Ai.RadioLexicon.IsValid)
            .Select(e => new SecondMonitor.Hub.Core.Ai.LexiconEntry { Heard = e.Heard.Trim(), Meant = e.Meant.Trim() }).Take(200).ToList();
        s.Ai.ListenChunkSeconds = Math.Clamp(s.Ai.ListenChunkSeconds <= 0 ? 28 : s.Ai.ListenChunkSeconds, 5, 120);
        s.ThermalProfiles = incoming.ThermalProfiles ?? new List<SecondMonitor.Hub.Core.Race.ThermalProfile>();
        s.VoiceOutput = incoming.VoiceOutput is "pc" or "ecrans" or "deux" ? incoming.VoiceOutput : s.VoiceOutput;
    });
    return Results.Ok(store.Current);
});

// ───── Ingénieur IA (Ollama, sur cet ordinateur uniquement) ─────
// Lexique de piste : vocabulaire compris et corrections d'office de la dictée (les corrections du pilote sont dans les réglages).
app.MapGet("/api/ai/lexique", () => Results.Json(new
{
    vocabulary = SecondMonitor.Hub.Core.Ai.RadioLexicon.Vocabulary.Select(v => new { word = v.Word, meaning = v.Meaning }),
    builtin = SecondMonitor.Hub.Core.Ai.RadioLexicon.BuiltinExamples,
}));
app.MapGet("/api/ai/models", async (OllamaClient ollama, CancellationToken ct) =>
{
    try
    {
        var (models, nodes) = await ollama.ListAllAsync(ct);
        return Results.Json(new { ok = nodes.Any(n => n.Local && n.Ok), error = nodes.FirstOrDefault(n => n.Local && !n.Ok)?.Error is string e ? $"Ollama injoignable sur ce Mac ({e}). Est-il lancé ?" : null, models, nodes });
    }
    catch (Exception ex)
    {
        return Results.Json(new { ok = false, error = $"Ollama injoignable ({ex.Message}). Est-il lancé ?", models = Array.Empty<object>() });
    }
});
app.MapGet("/api/ai/briefing", (RaceEngine engine, string? q) => Results.Text(SecondMonitor.Hub.Core.Ai.RaceBriefing.Build(engine.BuildSnapshot(), q), "text/plain; charset=utf-8"));
app.MapPost("/api/ai/ask", async (EngineerService engineer, AskRequest req, CancellationToken ct) =>
{
    try
    {
        return Results.Json(await engineer.AskAsync(req.Text, req.Speak, req.From ?? "ingénieur", ct, req.Test));
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: 400);
    }
});
app.MapPost("/api/ai/listen", async (EngineerService engineer, HttpRequest request, CancellationToken ct) =>
{
    try
    {
        IFormCollection form = await request.ReadFormAsync(ct);
        IFormFile? file = form.Files.GetFile("audio");
        if (file == null)
        {
            return Results.Json(new { error = "Aucun audio reçu" }, statusCode: 400);
        }

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        bool speak = form["speak"] != "false";
        bool test = form["test"] == "true";
        if (form["transcribeOnly"] == "true")
        {
            return Results.Json(await engineer.TestListenAsync(ms.ToArray(), file.FileName, ct));
        }

        EngineerReply? reply = await engineer.ListenAndAskAsync(ms.ToArray(), file.FileName, speak, form["from"].FirstOrDefault() ?? "ingénieur", ct, test);
        return reply == null ? Results.Json(new { silent = true }) : Results.Json(reply);
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: 400);
    }
}).DisableAntiforgery();
app.MapGet("/api/ai/thinking", async (EngineerService engineer, CancellationToken ct) => Results.Json(await engineer.ThinkingStatusAsync(ct)));
app.MapPost("/api/pitmenu/test", async (EngineerService engineer, CancellationToken ct) =>
    Results.Json(new { sent = await engineer.TestPitMenuAsync(ct) }));
app.MapPost("/api/ai/test", async (EngineerService engineer, DiagnosticRequest req, CancellationToken ct) =>
    Results.Json(await engineer.DiagnoseAsync(req.Step, ct)));
app.MapGet("/api/ai/memory", (EngineerService engineer) => Results.Json(new
{
    entries = engineer.Memory.Entries,
    plannedStopLap = engineer.Memory.PlannedStopLap,
    noStopPlanned = engineer.Memory.NoStopPlanned,
    pending = engineer.Memory.Pending()?.Text,
}));
app.MapPost("/api/ai/memory", (EngineerService engineer, RaceEngine engine, MemoryNote note) =>
{
    int? lap = engine.BuildSnapshot().Drivers.FirstOrDefault(d => d.IsPlayer)?.Laps;
    return string.IsNullOrWhiteSpace(note.Text) ? Results.BadRequest() : Results.Json(engineer.Memory.Add(note.Source ?? "ingénieur", "note", note.Text, lap));
});
app.MapDelete("/api/ai/memory/{id:int}", (EngineerService engineer, int id) => engineer.Memory.Remove(id) ? Results.Ok() : Results.NotFound());
app.MapDelete("/api/ai/memory/plan", (EngineerService engineer) =>
{
    engineer.Memory.PlannedStopLap = null;
    engineer.Memory.NoStopPlanned = false;
    return Results.Ok();
});
app.MapPost("/api/quit", (IHostApplicationLifetime life, HttpContext ctx) =>
{
    // Arrêt demandé depuis Réglages (application installée sans fenêtre) : seulement depuis cet ordinateur.
    if (ctx.Connection.RemoteIpAddress is { } ip && !IPAddress.IsLoopback(ip))
    {
        return Results.StatusCode(403);
    }

    _ = Task.Run(async () => { await Task.Delay(300); life.StopApplication(); AppWindow.Close(); });
    return Results.Ok();
});
// ───── Fenêtres de l'application (une par écran : AppWindow.cs) ─────
app.MapGet("/api/fenetres", () => Results.Json(AppWindow.Describe()));
app.MapPost("/api/fenetres", (HttpContext ctx, NewWindowRequest? req) =>
{
    // Fenêtre native seulement à la demande de cet ordinateur : un iPad ou un téléphone ouvre plutôt une fenêtre de son navigateur.
    // (Corps JSON obligatoire : une page d'un autre site ne peut pas l'envoyer sans autorisation CORS.)
    bool local = ctx.Connection.RemoteIpAddress is { } ip && IPAddress.IsLoopback(ip);
    return Results.Json(local ? AppWindow.OpenNew(req?.Onglet) : AppWindow.NotOpened("Fenêtre demandée depuis un autre appareil."));
});
app.MapPost("/api/client-log", (ClientLog entry) =>
{
    // Erreurs de l'interface (écran noir, plantage d'un écran) : gardées dans journal.txt pour comprendre après coup.
    HubJournal.Write(dataDir, "interface", $"{entry.Message} {entry.Detail}".Trim());
    return Results.Ok();
});
// ───── Petits écrans et boutons externes (ClickDeck, touche du clavier ou bouton du volant du PC de jeu) ─────
DashboardApi.Map(app); // accueil : événements de course
app.MapGet("/api/deck", (RaceEngine engine, LiveHub hub) => Results.Json(DeckApi.Build(engine.BuildSnapshot(), hub.LastRadioText), LiveHub.Json));
app.MapPost("/api/radio/press", async (LiveHub hub, HttpContext ctx) =>
{
    // Un appui « radio » venu d'ailleurs : le poste radio (la fenêtre de SecondMonitor) démarre ou envoie la dictée,
    // ou coupe l'ingénieur s'il parle. Même comportement que le bouton à l'écran.
    // Si le collecteur enregistre le micro du PC de jeu (« poste »), c'est lui qui répond à l'appui, pas les écrans.
    string from = ctx.Request.Query["de"].FirstOrDefault() ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "?";
    await hub.BroadcastAsync("radioPress", new { from, at = DateTime.Now, poste = hub.MicStation?.Name });
    return Results.Json(new { ok = true, postes = hub.ClientCount, phase = DeckApi.RadioPhase });
});
app.MapPost("/api/radio/state", (RadioState state) =>
{
    DeckApi.RadioPhase = state.Phase ?? "idle";
    return Results.Ok();
});
app.MapPost("/api/radio/stop", async (EngineerService engineer) =>
{
    await engineer.StopSpeakingAsync();
    return Results.Ok();
});
app.MapPost("/api/ai/say", async (EngineerService engineer, AskRequest req, CancellationToken ct) =>
    Results.Json(await engineer.SayAsync(req.Text, req.From ?? "ingénieur", ct)));

MusicApi.Map(app, dataDir); // Musique Deezer (/api/music/*, commandes vocales, priorité radio)
UpdateApi.Map(app, dataDir, headless); // mises à jour (UpdateApi.cs)
CarnetApi.Map(app); // carnet de session : /api/carnets

// Toute autre route → l'application web (navigation côté client).
app.MapFallbackToFile("index.html");

try
{
    app.Start();
}
catch (Exception ex)
{
    AppWindow.Message(headless, "SecondMonitor ne peut pas démarrer", ex.Message);
    return;
}

Console.WriteLine($"SecondMonitor prêt : {localUrl}  (données : {dataDir})");
HubJournal.Write(dataDir, "démarrage", $"SecondMonitor {typeof(Program).Assembly.GetName().Version} sur {Environment.OSVersion} : {localUrl}");
if (httpsCertificate != null)
{
    Console.WriteLine($"Depuis le PC de jeu, un iPad ou un téléphone (micro et volant) : https://<adresse-de-cet-ordinateur>:{settings.Current.HttpsPort}  — acceptez l'avertissement de certificat la première fois.");
}

if (headless)
{
    app.WaitForShutdown();
    return;
}

if (useBrowser || !AppWindow.Run(localUrl, "SecondMonitor", dataDir, noGpu))
{
    // Pas de fenêtre : interface dans le navigateur. Pour arrêter : Réglages › Quitter.
    AppWindow.OpenBrowser(localUrl);
    app.WaitForShutdown();
    return;
}

// Fenêtre fermée : on arrête tout proprement.
app.StopAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();

public sealed record AskRequest(string Text, bool Speak = true, string? From = null, bool Test = false);

public sealed record DiagnosticRequest(string Step);

public sealed record MemoryNote(string Text, string? Source = null);

public sealed record ClientLog(string Message, string? Detail = null);

public sealed record RadioState(string? Phase);

/// <summary>« Nouvelle fenêtre » : page affichée au départ (onglet de la fenêtre d'où l'on vient).</summary>
public sealed record NewWindowRequest(string? Onglet);
