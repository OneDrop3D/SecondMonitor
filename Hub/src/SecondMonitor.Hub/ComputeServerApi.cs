using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using SecondMonitor.Hub.Core.Compute;
using SecondMonitor.Hub.Services;

namespace SecondMonitor.Hub;

/// <summary>
/// Utilisation à trois ordinateurs : PC de jeu (collecteur) → ordinateur 2 (SecondMonitor qui affiche la course)
/// → ordinateur 3 (« serveur de calcul » : SecondMonitor qui prête l'Ollama de sa grosse carte graphique).
///
/// Sur le serveur de calcul, sous « /calcul » (seules routes ouvertes au réseau dans ce mode, voir <see cref="UseGuard"/>) :
///   POST /calcul/appairer       { code, id, nom } → { jeton, id, nom }   sans jeton ; erreurs limitées (ComputePairing)
///   GET  /calcul/ping           signe de vie de l'ordinateur 2 → { id, nom, modeles, ollama }
///   GET  /calcul/api/tags   POST /calcul/api/show   POST /calcul/api/chat
///   POST /calcul/v1/audio/transcriptions   POST /calcul/v1/audio/speech
///                               relayées telles quelles vers l'Ollama de cet ordinateur, jeton exigé
///                               (« Authorization: Bearer … ») ; rien d'autre d'Ollama n'est joignable (ni pull, ni delete…).
/// Écran d'état du serveur (cet ordinateur seulement pour le code et les actions) :
///   GET /api/calcul/etat   POST /api/calcul/code   PUT /api/calcul/nom   DELETE /api/calcul/clients/{id}
/// Sur l'ordinateur 2 :
///   GET  /api/calcul/decouverts   serveurs entendus sur le réseau, machines reliées et leur dernier signe de vie
///   POST /api/calcul/relier       { adresse, port, code } : la machine rejoint « Machines IA » avec son jeton
/// Partout : POST /api/mode { mode: "affichage" | "calcul" }.
/// </summary>
public static class ComputeServerApi
{
    public const string Prefix = "/calcul";

    /// <summary>Routes d'Ollama relayées : lister et décrire les modèles, converser, transcrire, parler.</summary>
    public static readonly (string Method, string Path)[] RelayedRoutes =
    {
        ("GET", "/api/tags"),
        ("POST", "/api/show"),
        ("POST", "/api/chat"),
        ("POST", "/v1/audio/transcriptions"),
        ("POST", "/v1/audio/speech"),
    };

    /// <summary>
    /// En mode serveur de calcul, les autres appareils du réseau n'atteignent que « /calcul » (appairage, puis relais avec jeton) :
    /// ni l'interface, ni le code d'appairage, ni les réglages de cet ordinateur.
    /// </summary>
    public static void UseGuard(WebApplication app, SettingsStore settings)
    {
        app.Use(async (ctx, next) =>
        {
            if (settings.Current.IsComputeServer && !IsLocal(ctx) && !ctx.Request.Path.StartsWithSegments(Prefix))
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                ctx.Response.ContentType = "text/plain; charset=utf-8";
                await ctx.Response.WriteAsync("Serveur de calcul SecondMonitor : son écran ne s'affiche que sur l'ordinateur lui-même.");
                return;
            }

            await next();
        });
    }

    public static void Map(WebApplication app)
    {
        string dataDir = app.Services.GetRequiredService<SettingsStore>().DataDirectory;

        // ───── Serveur de calcul : appairage, signe de vie, relais ─────
        app.MapPost($"{Prefix}/appairer", async (HttpContext ctx, ComputePairing pairing, ComputeServerService server, SettingsStore settings, ComputePairRequest req) =>
        {
            if (!settings.Current.IsComputeServer)
            {
                return NotComputeServer();
            }

            PairingResult result = pairing.TryPair(req.Code, req.Id, req.Nom, Remote(ctx));
            switch (result.Status)
            {
                case PairingStatus.Paired:
                    HubJournal.Write(dataDir, "serveur de calcul", $"« {result.ClientName} » ({Remote(ctx)}) est relié");
                    return Results.Json(new { jeton = result.Token, id = server.ServerId, nom = server.ServerName });
                case PairingStatus.Locked:
                    return Results.Json(new { error = $"Trop d'essais de code : réessayez dans {Math.Max(1, (int)Math.Ceiling(result.RetryAfter.TotalMinutes))} min." }, statusCode: StatusCodes.Status429TooManyRequests);
                default:
                    HubJournal.Write(dataDir, "serveur de calcul", $"Code d'appairage incorrect reçu de {Remote(ctx)}");
                    await Task.Delay(TimeSpan.FromSeconds(1)); // freine les essais au hasard
                    return Results.Json(new { error = "Code incorrect : recopiez le code affiché sur l'écran du serveur de calcul." }, statusCode: StatusCodes.Status403Forbidden);
            }
        });

        app.MapGet($"{Prefix}/ping", (HttpContext ctx, ComputePairing pairing, ComputeServerService server, SettingsStore settings) =>
        {
            if (!settings.Current.IsComputeServer)
            {
                return NotComputeServer();
            }

            PairedClient? client = pairing.Authenticate(ctx.Request.Headers.Authorization.ToString());
            if (client == null)
            {
                return Refused(ctx);
            }

            pairing.Touch(client, Remote(ctx), "signe de vie", countsAsRequest: false);
            var state = server.State();
            return Results.Json(new { id = server.ServerId, nom = server.ServerName, modeles = state.Models.Count, ollama = state.Ok });
        });

        foreach (var (method, path) in RelayedRoutes)
        {
            string ollamaPath = path;
            app.MapMethods(Prefix + path, new[] { method }, (HttpContext ctx, ComputePairing pairing, SettingsStore settings, IHttpClientFactory http) =>
                RelayAsync(ctx, ollamaPath, pairing, settings, http));
        }

        // Tout le reste sous « /calcul » : introuvable (jamais relayé, jamais l'interface par la route de repli).
        app.Map(Prefix + "/{**rest}", () => Results.Json(new { error = "Route inconnue du serveur de calcul." }, statusCode: StatusCodes.Status404NotFound));

        // ───── Écran d'état du serveur de calcul ─────
        app.MapGet("/api/calcul/etat", (HttpContext ctx, ComputePairing pairing, ComputeServerService server, SettingsStore settings) =>
        {
            bool local = IsLocal(ctx);
            var state = server.State();
            DateTime now = DateTime.UtcNow;
            return Results.Json(new
            {
                mode = settings.Current.Mode,
                id = server.ServerId,
                nom = server.ServerName,
                nomChoisi = settings.Current.Calcul.Name,
                port = settings.Current.WebPort,
                portAnnonces = settings.Current.Calcul.DiscoveryPort,
                code = local ? ComputePairing.FormatCode(pairing.Code) : null,
                adresses = LocalAddresses(),
                ollama = new
                {
                    url = ComputeServerService.OllamaBase(settings.Current.Ai.OllamaUrl),
                    ok = state.Ok,
                    error = state.Error,
                    verifie = state.CheckedUtc != DateTime.MinValue,
                    modeles = state.Models.Select(m => new { nom = m.Name, taille = m.SizeBytes }),
                },
                annonce = new
                {
                    ilYA = state.AnnouncedUtc == DateTime.MinValue ? (double?)null : Math.Round((now - state.AnnouncedUtc).TotalSeconds, 1),
                    destinations = state.Targets,
                },
                clients = local
                    ? pairing.Clients().Select(c => new
                    {
                        id = c.Id,
                        nom = c.Name,
                        adresse = c.Address,
                        relieLe = c.PairedUtc,
                        enLigne = c.Online,
                        vuIlYA = c.LastSeenUtc is DateTime seen ? Math.Round((now - seen).TotalSeconds) : (double?)null,
                        requetes = c.Requests,
                        derniereRequete = c.LastRequest,
                    }).ToList<object>()
                    : new List<object>(),
            });
        });
        app.MapPost("/api/calcul/code", (HttpContext ctx, ComputePairing pairing) =>
            !IsLocal(ctx) ? LocalOnly() : Results.Json(new { code = ComputePairing.FormatCode(pairing.RenewCode()) }));
        app.MapPost("/api/calcul/actualiser", async (ComputeServerService server, CancellationToken ct) =>
        {
            await server.RefreshModelsAsync(ct);
            return Results.Ok();
        });
        app.MapPut("/api/calcul/nom", (HttpContext ctx, SettingsStore settings, ComputeNameRequest req) =>
        {
            if (!IsLocal(ctx))
            {
                return LocalOnly();
            }

            settings.Update(s => s.Calcul.Name = ComputeAnnouncement.CleanName(req.Nom));
            return Results.Ok();
        });
        app.MapDelete("/api/calcul/clients/{id}", (HttpContext ctx, ComputePairing pairing, string id) =>
        {
            if (!IsLocal(ctx))
            {
                return LocalOnly();
            }

            bool removed = pairing.Revoke(id);
            if (removed)
            {
                HubJournal.Write(dataDir, "serveur de calcul", $"SecondMonitor {id} retiré : son jeton est refusé");
            }

            return removed ? Results.Ok() : Results.NotFound();
        });

        // ───── Ordinateur 2 : serveurs trouvés, liaison ─────
        app.MapGet("/api/calcul/decouverts", (ComputeLinkService link, SettingsStore settings) =>
        {
            DateTime now = DateTime.UtcNow;
            List<AiNode> nodes = settings.Current.Ai.Nodes;
            return Results.Json(new
            {
                port = settings.Current.Calcul.DiscoveryPort,
                erreur = link.DiscoveryError,
                serveurs = link.Discovered().Select(s => new
                {
                    id = s.Announcement.Id,
                    nom = s.Announcement.Name,
                    adresse = s.Address,
                    port = s.Announcement.Port,
                    modeles = s.Announcement.Models,
                    ollama = s.Announcement.OllamaOk,
                    vuIlYA = Math.Round((now - s.LastSeenUtc).TotalSeconds, 1),
                    machine = nodes.FirstOrDefault(n => n.ServerId == s.Announcement.Id && !string.IsNullOrEmpty(n.Token))?.Name,
                }),
                liens = link.Links.ToDictionary(l => l.Key, l => new
                {
                    ok = l.Value.Ok,
                    refuse = l.Value.Refused,
                    error = l.Value.Error,
                    ilYA = Math.Round((now - l.Value.CheckedUtc).TotalSeconds),
                }),
            });
        });
        app.MapPost("/api/calcul/relier", async (ComputeLinkService link, ComputeLinkRequest req, CancellationToken ct) =>
        {
            string code = ComputePairing.NormalizeCode(req.Code);
            if (code.Length != 6)
            {
                return Results.Json(new { error = "Le code d'appairage a 6 chiffres (affichés sur l'écran du serveur de calcul)." }, statusCode: StatusCodes.Status400BadRequest);
            }

            if (!TryParseAddress(req.Adresse, req.Port, out string host, out int port))
            {
                return Results.Json(new { error = "Adresse incomprise : par exemple 192.168.1.40 ou pc-calcul.local (port 8686 par défaut)." }, statusCode: StatusCodes.Status400BadRequest);
            }

            ComputeLinkService.PairOutcome outcome = await link.PairAsync(host, port, code, ct);
            return outcome.Ok
                ? Results.Json(new { ok = true, machine = outcome.Node })
                : Results.Json(new { error = outcome.Error }, statusCode: StatusCodes.Status400BadRequest);
        });

        // ───── Usage de cet ordinateur ─────
        app.MapPost("/api/mode", (SettingsStore settings, ModeRequest req) =>
        {
            if (req.Mode is not (HubSettings.ModeDisplay or HubSettings.ModeCompute))
            {
                return Results.Json(new { error = "Usage inconnu (« affichage » ou « calcul »)." }, statusCode: StatusCodes.Status400BadRequest);
            }

            settings.Update(s => s.Mode = req.Mode);
            HubJournal.Write(dataDir, "démarrage", req.Mode == HubSettings.ModeCompute ? "Cet ordinateur devient serveur de calcul" : "Cet ordinateur affiche la course");
            return Results.Json(new { mode = req.Mode });
        });
    }

    /// <summary>Relaie une requête d'un SecondMonitor relié vers l'Ollama de cet ordinateur, réponse comprise (en flux).</summary>
    private static async Task RelayAsync(HttpContext ctx, string path, ComputePairing pairing, SettingsStore settings, IHttpClientFactory http)
    {
        if (!settings.Current.IsComputeServer)
        {
            await NotComputeServer().ExecuteAsync(ctx);
            return;
        }

        PairedClient? client = pairing.Authenticate(ctx.Request.Headers.Authorization.ToString());
        if (client == null)
        {
            await Refused(ctx).ExecuteAsync(ctx);
            return;
        }

        bool hasBody = HttpMethods.IsPost(ctx.Request.Method);
        byte[] body = Array.Empty<byte>();
        if (hasBody)
        {
            using var buffer = new MemoryStream();
            await ctx.Request.Body.CopyToAsync(buffer, ctx.RequestAborted);
            body = buffer.ToArray();
        }

        bool work = path is "/api/chat" or "/v1/audio/transcriptions" or "/v1/audio/speech";
        pairing.Touch(client, Remote(ctx), Describe(path, body), countsAsRequest: work);

        string target = ComputeServerService.OllamaBase(settings.Current.Ai.OllamaUrl) + path;
        using var request = new HttpRequestMessage(new HttpMethod(ctx.Request.Method), target);
        if (hasBody)
        {
            request.Content = new ByteArrayContent(body);
            if (!string.IsNullOrEmpty(ctx.Request.ContentType))
            {
                request.Content.Headers.TryAddWithoutValidation("Content-Type", ctx.Request.ContentType);
            }
        }

        // Le jeton reste ici : Ollama ne reçoit que la requête elle-même.
        HttpResponseMessage response;
        try
        {
            response = await http.CreateClient("relais").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ctx.RequestAborted);
        }
        catch (Exception ex) when (!ctx.RequestAborted.IsCancellationRequested)
        {
            await Results.Json(new { error = $"L'Ollama du serveur de calcul ne répond pas ({ex.Message}). Est-il lancé sur cet ordinateur ?" }, statusCode: StatusCodes.Status502BadGateway).ExecuteAsync(ctx);
            return;
        }

        using (response)
        {
            ctx.Response.StatusCode = (int)response.StatusCode;
            if (response.Content.Headers.ContentType is { } type)
            {
                ctx.Response.ContentType = type.ToString();
            }

            if (response.Content.Headers.ContentEncoding.Count > 0)
            {
                ctx.Response.Headers.ContentEncoding = string.Join(", ", response.Content.Headers.ContentEncoding);
            }

            await response.Content.CopyToAsync(ctx.Response.Body, ctx.RequestAborted);
        }
    }

    /// <summary>Ce que demande le SecondMonitor relié, pour l'écran d'état (« conversation · qwen3:32b »).</summary>
    private static string Describe(string path, byte[] body)
    {
        string what = path switch
        {
            "/api/chat" => "conversation",
            "/v1/audio/transcriptions" => "écoute",
            "/v1/audio/speech" => "voix",
            "/api/show" => "fiche de modèle",
            _ => "liste des modèles",
        };
        if (path is "/api/chat" or "/v1/audio/speech" or "/api/show" && body.Length is > 0 and < 4_000_000)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("model", out JsonElement model) && model.ValueKind == JsonValueKind.String)
                {
                    return $"{what} · {model.GetString()}";
                }
            }
            catch (JsonException)
            {
                // Corps illisible : Ollama le dira lui-même.
            }
        }

        return what;
    }

    private static bool TryParseAddress(string? text, int? port, out string host, out int portNumber)
    {
        host = "";
        portNumber = port is > 0 and < 65536 ? port.Value : 8686;
        text = (text ?? "").Trim();
        if (text.Length == 0)
        {
            return false;
        }

        if (!text.Contains("://"))
        {
            text = "http://" + text;
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out Uri? uri) || uri.HostNameType is UriHostNameType.Unknown or UriHostNameType.Basic)
        {
            return false;
        }

        host = uri.HostNameType == UriHostNameType.IPv6 ? uri.Host.Trim('[', ']') : uri.Host;
        if (!uri.IsDefaultPort)
        {
            portNumber = uri.Port;
        }

        return true;
    }

    /// <summary>Adresses de cet ordinateur sur le réseau local (pour relier à la main quand la découverte ne passe pas).</summary>
    private static List<string> LocalAddresses()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address))
                .Select(a => a.Address.ToString())
                .Distinct()
                .ToList();
        }
        catch (Exception)
        {
            return new List<string>();
        }
    }

    private static bool IsLocal(HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress is { } ip && IPAddress.IsLoopback(ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip);

    private static string Remote(HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress is { } ip ? (ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip).ToString() : "?";

    private static IResult NotComputeServer() =>
        Results.Json(new { error = "Cet ordinateur n'est pas en mode « serveur de calcul » (Réglages de SecondMonitor sur cet ordinateur)." }, statusCode: StatusCodes.Status503ServiceUnavailable);

    private static IResult Refused(HttpContext ctx)
    {
        ctx.Response.Headers.WWWAuthenticate = "Bearer";
        return Results.Json(new { error = "Jeton refusé : reliez de nouveau ce SecondMonitor au serveur de calcul (Ingénieur IA › Machines IA)." }, statusCode: StatusCodes.Status401Unauthorized);
    }

    private static IResult LocalOnly() =>
        Results.Json(new { error = "Seulement depuis le serveur de calcul lui-même." }, statusCode: StatusCodes.Status403Forbidden);
}

public sealed record ComputePairRequest(string? Code, string? Id, string? Nom);

public sealed record ComputeLinkRequest(string? Adresse, int? Port, string? Code);

public sealed record ComputeNameRequest(string? Nom);

public sealed record ModeRequest(string? Mode);
