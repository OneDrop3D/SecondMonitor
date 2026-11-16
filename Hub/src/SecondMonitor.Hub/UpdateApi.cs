using System.Net;
using SecondMonitor.Hub.Core.Race;
using SecondMonitor.Hub.Core.Update;

namespace SecondMonitor.Hub;

/// <summary>
/// Mises à jour de SecondMonitor (et du collecteur installé avec lui) : voir Hub.Core/Update.
///   GET  /api/update            état (version proposée, avancement, réglages, échec précédent)
///   POST /api/update/check      vérifier maintenant
///   POST /api/update/install    télécharger (vérifié) puis « Redémarrer et installer » — depuis cet ordinateur seulement
///   PUT  /api/update/settings   { automatic, intervalHours }
///   POST /api/update/dismiss    { version } : plus de bandeau pour cette version
///   POST /api/update/ack        la raison du dernier retour arrière a été lue
/// </summary>
public static class UpdateApi
{
    public static UpdateManager? Manager { get; private set; }

    public static void Map(WebApplication app, string dataDir, bool headless)
    {
        string version = typeof(UpdateApi).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        var manager = new UpdateManager("hub", version, dataDir, m => HubJournal.Write(dataDir, "mise à jour", m));
        Manager = manager;
        // Jamais en course : le jeu a envoyé des données dans les 2 dernières minutes.
        RaceEngine engine = app.Services.GetRequiredService<RaceEngine>();
        manager.CanCheckNow = () => (DateTime.UtcNow - engine.LastUpdateUtc).TotalMinutes > 2;
        IHostApplicationLifetime life = app.Lifetime;
        manager.QuitRequested += () => _ = Task.Run(async () =>
        {
            // Le script attend la fin de ce processus pour remplacer l'application.
            await Task.Delay(800);
            life.StopApplication();
            AppWindow.Close();
        });
        // Première vérification une fois l'interface affichée, puis toutes les 6 h (réglable).
        life.ApplicationStarted.Register(() => manager.Start(TimeSpan.FromSeconds(headless ? 30 : 8)));
        life.ApplicationStopping.Register(manager.Dispose);

        app.MapGet("/api/update", (HttpContext ctx) => Results.Json(new { local = IsLocal(ctx), status = manager.Describe() }));
        app.MapPost("/api/update/check", async () =>
        {
            try
            {
                await manager.CheckAsync();
            }
            catch (Exception)
            {
                // L'erreur est dans l'état (lastCheckError).
            }

            return Results.Json(manager.Describe());
        });
        app.MapPost("/api/update/install", (HttpContext ctx) =>
        {
            if (!IsLocal(ctx))
            {
                return Results.Json(new { error = "La mise à jour se lance depuis l'ordinateur où SecondMonitor est installé." }, statusCode: 403);
            }

            manager.InstallInBackground();
            return Results.Json(manager.Describe());
        });
        app.MapPut("/api/update/settings", (UpdateSettingsRequest req) =>
        {
            manager.SetPreferences(req.Automatic, req.IntervalHours <= 0 ? 6 : req.IntervalHours);
            return Results.Json(manager.Describe());
        });
        app.MapPost("/api/update/dismiss", (UpdateDismissRequest req) =>
        {
            manager.Dismiss(req.Version);
            return Results.Json(manager.Describe());
        });
        app.MapPost("/api/update/ack", () =>
        {
            manager.AcknowledgeFailure();
            return Results.Ok();
        });
    }

    private static bool IsLocal(HttpContext ctx) => ctx.Connection.RemoteIpAddress is not { } ip || IPAddress.IsLoopback(ip);
}

public sealed record UpdateSettingsRequest(bool Automatic, int IntervalHours = 6);

public sealed record UpdateDismissRequest(string? Version);
