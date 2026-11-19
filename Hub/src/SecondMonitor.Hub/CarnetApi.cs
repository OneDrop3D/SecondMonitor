using SecondMonitor.Hub.Services;

namespace SecondMonitor.Hub;

/// <summary>
/// Carnet de session (écran « Carnet ») :
///   GET    /api/carnets                 liste des sessions (la session en cours d'abord, « current »)
///   GET    /api/carnets/current         carnet de la session en cours (404 s'il n'y en a pas encore)
///   GET    /api/carnets/{id}            un carnet complet (radios, notes, réglages, tours, résultat, récapitulatif)
///   POST   /api/carnets/{id}/recap      (re)générer le récapitulatif (IA si disponible, sinon calculé)
///   POST   /api/carnets/{id}/notes      ajouter une note { text, source }
///   GET    /api/carnets/{id}/bilan      tours (temps, essence, énergie, usure) et moyennes par relais et de la course
///   GET    /api/carnets/{id}/traces      tours dont le tracé est enregistré ; ?laps=3,4 : leur tracé (vitesse, pédales, rapport…)
///   POST   /api/carnets/{id}/bilan/tours compter ou retirer un tour des moyennes { tour, compte } ; renvoie le bilan
///   DELETE /api/carnets/{id}            supprimer un carnet
///   POST   /api/session/end             terminer la session en cours (carnet fermé et récapitulé)
///   POST   /api/session/new             nouvelle session (carnet, mémoire et calculs repartent de zéro)
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
        app.MapGet("/api/carnets/{id}/bilan", (CarnetService carnets, string id) =>
            carnets.Bilan(id) is { } b ? Results.Json(b, LiveHub.Json) : Results.NotFound());
        // Tracés des tours (analyse après la course) : sans « laps », la liste des tours enregistrés ; avec « laps=3,4,5 », leur détail.
        app.MapGet("/api/carnets/{id}/traces", (CarnetService carnets, string id, string? laps) =>
            string.IsNullOrEmpty(laps)
                ? Results.Json(new { step = SecondMonitor.Hub.Core.Race.DrivingWatch.BinMeters, laps = carnets.TraceLaps(id) }, LiveHub.Json)
                : Results.Json(carnets.Traces(id, laps.Split(',').Select(t => int.TryParse(t, out int n) ? n : 0).Where(n => n > 0)), LiveHub.Json));
        app.MapPost("/api/carnets/{id}/bilan/tours", (CarnetService carnets, string id, CarnetTourRequest r) =>
            carnets.CompterTour(id, r.Tour, r.Compte) is { } b ? Results.Json(b, LiveHub.Json) : Results.NotFound());
        // Boutons « Terminer la session » / « Nouvelle session » (écrans Ingénieur IA et Carnet).
        app.MapPost("/api/session/end", (CarnetService carnets) => Results.Json(new { id = carnets.EndCurrent() }));
        app.MapPost("/api/session/new", (CarnetService carnets, SecondMonitor.Hub.Core.Race.RaceEngine engine) =>
        {
            carnets.StartNew();
            engine.StartNewSession(); // mémoire de course, conversation et calculs repartent de zéro
            return Results.Ok();
        });
        app.MapDelete("/api/carnets/{id}", (CarnetService carnets, string id) => carnets.Delete(id) ? Results.Ok() : Results.NotFound());
    }
}

public sealed record CarnetNoteRequest(string? Text, string? Source = null);

public sealed record CarnetTourRequest(int Tour, bool Compte);
