using SecondMonitor.Hub.Services;

namespace SecondMonitor.Hub;

/// <summary>
/// Carnet de session (écran « Carnet ») :
///   GET    /api/carnets                 liste des sessions (la session en cours d'abord, « current »)
///   GET    /api/carnets/current         carnet de la session en cours (404 s'il n'y en a pas encore)
///   GET    /api/carnets/{id}            un carnet complet (radios, notes, réglages, tours, résultat, récapitulatif)
///   POST   /api/carnets/{id}/recap      (re)générer le récapitulatif (IA si disponible, sinon calculé)
///   POST   /api/carnets/{id}/notes      ajouter une note { text, source }
///   DELETE /api/carnets/{id}            supprimer un carnet
/// </summary>
public static class CarnetApi
{
    public static void Register(IServiceCollection services)
    {
        services.AddSingleton<CarnetService>();
        services.AddHostedService(sp => sp.GetRequiredService<CarnetService>());
    }

    public static void Map(WebApplication app)
    {
        app.MapGet("/api/carnets", (CarnetService carnets) => Results.Json(carnets.List(), LiveHub.Json));
        app.MapGet("/api/carnets/current", (CarnetService carnets) =>
            carnets.CurrentId is string id && carnets.Get(id) is { } c ? Results.Json(c, LiveHub.Json) : Results.NotFound());
        app.MapGet("/api/carnets/{id}", (CarnetService carnets, string id) =>
            carnets.Get(id) is { } c ? Results.Json(c, LiveHub.Json) : Results.NotFound());
        app.MapPost("/api/carnets/{id}/recap", async (CarnetService carnets, string id, CancellationToken ct) =>
            await carnets.RecapAsync(id, ct) is { } c ? Results.Json(c, LiveHub.Json) : Results.NotFound());
        app.MapPost("/api/carnets/{id}/notes", (CarnetService carnets, string id, CarnetNoteRequest note) =>
            string.IsNullOrWhiteSpace(note.Text) ? Results.BadRequest()
                : carnets.AddNote(id, note.Text.Trim(), note.Source ?? "ingénieur") is { } c ? Results.Json(c, LiveHub.Json) : Results.NotFound());
        app.MapDelete("/api/carnets/{id}", (CarnetService carnets, string id) => carnets.Delete(id) ? Results.Ok() : Results.NotFound());
    }
}

public sealed record CarnetNoteRequest(string? Text, string? Source = null);
