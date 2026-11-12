namespace SecondMonitor.Hub;

/// <summary>
/// Journal sur disque (journal.txt dans le dossier de données) : réception, erreurs de l'interface, erreurs du serveur.
/// Petit et tournant (1 Mo, un fichier précédent gardé) ; sert à comprendre un problème après coup.
/// </summary>
public static class HubJournal
{
    private const long MaxBytes = 1_000_000;
    private static readonly object Lock = new();

    public static void Write(string dataDirectory, string source, string message)
    {
        try
        {
            lock (Lock)
            {
                string path = Path.Combine(dataDirectory, "journal.txt");
                if (File.Exists(path) && new FileInfo(path).Length > MaxBytes)
                {
                    File.Move(path, Path.Combine(dataDirectory, "journal.precedent.txt"), overwrite: true);
                }

                File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{source}] {message}{Environment.NewLine}");
            }
        }
        catch (Exception)
        {
            // Journal impossible (disque plein) : sans importance pour la course.
        }
    }
}
