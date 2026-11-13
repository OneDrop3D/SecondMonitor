using SecondMonitor.Hub.Core.Race;
using SecondMonitor.Hub.Services;

namespace SecondMonitor.Hub;

/// <summary>
/// Accueil (tableau de bord) : GET /api/race/events?after=N → { session, events } (événements de course, les plus anciens d'abord).
/// Le dernier identifiant est aussi dans chaque instantané (lastEventId) : l'interface ne relit que s'il a changé.
/// </summary>
public static class DashboardApi
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/race/events", (RaceEngine engine, long? after) =>
        {
            var (session, events) = engine.RaceEvents(after ?? 0);
            return Results.Json(new { session, events }, LiveHub.Json);
        });
    }
}
