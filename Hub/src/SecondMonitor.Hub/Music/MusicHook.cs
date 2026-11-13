namespace SecondMonitor.Hub.Music;

/// <summary>
/// Point d'accroche du pipeline radio (EngineerService) : les phrases musicales (« pause musique », « mets Daft Punk »…)
/// sont traitées ici, sans passer par le modèle IA. Branché au démarrage par <see cref="MusicApi.Map"/>.
/// </summary>
public static class MusicHook
{
    public static MusicService? Service { get; set; }

    /// <summary>Réponse à dire à la radio, ou null si la phrase ne concerne pas la musique.</summary>
    public static async Task<string?> TryHandleAsync(string text, bool test, CancellationToken ct)
    {
        MusicService? service = Service;
        if (service == null || string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        try
        {
            return await service.HandleVoiceAsync(text, test, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return null; // la musique ne doit jamais empêcher l'ingénieur de répondre
        }
    }
}
