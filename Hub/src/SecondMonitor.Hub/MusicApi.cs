using System.Net;
using SecondMonitor.Hub.Core.Music;
using SecondMonitor.Hub.Core.Race;
using SecondMonitor.Hub.Music;
using SecondMonitor.Hub.Services;

namespace SecondMonitor.Hub;

/// <summary>
/// Musique Deezer : /api/music/*. La lecture reste dans l'application Deezer de l'ordinateur (pleine qualité, compte de
/// l'utilisateur) ; SecondMonitor cherche, ouvre et commande. Branché par une seule ligne dans Program.cs.
/// </summary>
public static class MusicApi
{
    public static void Map(WebApplication app, string dataDir)
    {
        var engine = app.Services.GetRequiredService<RaceEngine>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Musique");
        var music = new MusicService(dataDir, engine, logger, app.Services.GetRequiredService<LiveHub>());
        MusicHook.Service = music;
        app.Lifetime.ApplicationStarted.Register(() => _ = Task.Run(() => music.RunAsync(app.Lifetime.ApplicationStopping)));

        app.MapGet("/api/music", async (CancellationToken ct) => Results.Json(await music.StatusAsync(ct), LiveHub.Json));

        app.MapPut("/api/music/settings", (MusicSettings incoming) =>
        {
            music.Settings.Update(s =>
            {
                s.Enabled = incoming.Enabled;
                s.Player = incoming.Player;
                s.SessionPolicy = incoming.SessionPolicy;
                s.DuckMode = incoming.DuckMode;
                s.DuckVolumePct = incoming.DuckVolumePct;
                s.VolumeStepPct = incoming.VolumeStepPct;
                s.MediaKeys = incoming.MediaKeys;
                s.Output = incoming.Output;
                s.FavoriteAutoPlay = incoming.FavoriteAutoPlay;
            });
            return Results.Json(music.Settings.Current, LiveHub.Json);
        });

        app.MapPost("/api/music/command", async (MusicCommandRequest req, CancellationToken ct) =>
        {
            if (!Enum.TryParse(req.Action, ignoreCase: true, out MusicAction action) || action == MusicAction.Open)
            {
                return Results.BadRequest(new { error = "Commande inconnue" });
            }

            return Results.Json(new { message = await music.CommandAsync(action, ct) });
        });

        app.MapGet("/api/music/search", async (string? q, string? kind, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Results.Json(Array.Empty<object>());
            }

            DeezerCatalog.TryParseKind(kind ?? "playlist", out MusicKind k);
            try
            {
                return Results.Json(await music.SearchAsync(k == MusicKind.Any ? MusicKind.Playlist : k, q, ct), LiveHub.Json);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                return Results.Json(new { error = "Deezer ne répond pas." }, statusCode: 502);
            }
        });

        app.MapPost("/api/music/open", async (MusicOpenRequest req, CancellationToken ct) =>
        {
            // Seulement des identifiants Deezer (jamais une adresse quelconque) : le lien est construit ici.
            if (!DeezerCatalog.TryParseKind(req.Kind, out MusicKind kind) || kind == MusicKind.Any || req.Id <= 0)
            {
                return Results.BadRequest(new { error = "Élément inconnu" });
            }

            return Results.Json(new { message = await music.OpenItemAsync(kind, req.Id, req.Title, ct) });
        });

        // Playlist préférée : l'étoile d'un résultat de recherche (seulement des identifiants Deezer).
        app.MapPost("/api/music/favorite", (MusicFavoriteRequest req) =>
        {
            if (!DeezerCatalog.TryParseKind(req.Kind, out MusicKind kind) || kind == MusicKind.Any || req.Id <= 0)
            {
                return Results.BadRequest(new { error = "Élément inconnu" });
            }

            string? picture = req.Picture != null && req.Picture.StartsWith("https://", StringComparison.Ordinal) && req.Picture.Length < 400 ? req.Picture : null;
            music.Settings.Update(s => s.Favorite = new MusicFavorite { Kind = DeezerCatalog.KindPath(kind), Id = req.Id, Title = (req.Title ?? "ma sélection").Trim()[..Math.Min(120, (req.Title ?? "ma sélection").Trim().Length)], Picture = picture });
            return Results.Json(music.Settings.Current, LiveHub.Json);
        });

        app.MapDelete("/api/music/favorite", () =>
        {
            music.Settings.Update(s => s.Favorite = null);
            return Results.Json(music.Settings.Current, LiveHub.Json);
        });

        // La voix de l'ingénieur commence / s'arrête sur le poste radio : la musique s'efface puis revient.
        app.MapPost("/api/music/voix", (MusicVoiceRequest req) =>
        {
            music.SetVoice(req.Playing);
            return Results.Ok();
        });

        // Autorisation macOS (Accessibilité) : seulement depuis cet ordinateur.
        app.MapPost("/api/music/permission", (HttpContext ctx, string? action) =>
        {
            if (ctx.Connection.RemoteIpAddress is { } ip && !IPAddress.IsLoopback(ip))
            {
                return Results.StatusCode(403);
            }

            if (action == "reglages")
            {
                music.Player.OpenPermissionSettings();
                return Results.Ok();
            }

            music.Settings.Update(s => s.AccessibilityAsked = true);
            bool granted = music.Player.RequestPermission();
            return Results.Json(new { granted });
        });
    }
}

public sealed record MusicCommandRequest(string Action);

public sealed record MusicOpenRequest(string Kind, long Id, string? Title);

public sealed record MusicFavoriteRequest(string Kind, long Id, string? Title, string? Picture);

public sealed record MusicVoiceRequest(bool Playing);
