namespace SecondMonitor.Hub.Music;

using System.Diagnostics;

/// <summary>Ce que l'on sait du lecteur Deezer à un instant donné (null = inconnu sur ce système).</summary>
public sealed class PlayerState
{
    public bool? Playing { get; set; }

    public string? Title { get; set; }

    public string? Artist { get; set; }

    public string? Album { get; set; }

    /// <summary>Application qui joue (« Deezer », « Safari »…), si connue.</summary>
    public string? Source { get; set; }

    /// <summary>Volume de Deezer seul (0-100), si réglable sur ce système.</summary>
    public int? Volume { get; set; }

    /// <summary>D'où vient l'information (« MediaRemote », « CoreAudio », « SMTC », « MPRIS »…), pour le diagnostic.</summary>
    public string? Via { get; set; }
}

/// <summary>Commande de transport envoyée au lecteur (toujours explicite : jamais une bascule à l'aveugle).</summary>
public enum TransportCommand
{
    Play,
    Pause,
    Next,
    Previous,
}

/// <summary>
/// Pilotage du lecteur Deezer de l'ordinateur, propre à chaque système : Mac (MediaRemote / touches multimédia),
/// Windows (contrôles multimédia du système, volume par application), Linux (MPRIS via playerctl).
/// </summary>
public interface IMusicPlayer
{
    /// <summary>« mac », « windows », « linux ».</summary>
    string Platform { get; }

    /// <summary>Volume de Deezer seul réglable (sans toucher à la voix de l'ingénieur).</summary>
    bool CanSetVolume { get; }

    /// <summary>L'application Deezer officielle est installée (sinon : lecteur web).</summary>
    bool AppInstalled();

    bool AppRunning();

    Task<PlayerState> GetStateAsync(CancellationToken ct);

    /// <summary>Envoie la commande ; vrai si elle a été transmise (pas forcément suivie d'effet).</summary>
    Task<bool> SendAsync(TransportCommand command, PlayerState? known, CancellationToken ct);

    /// <summary>Règle le volume de Deezer seul (0-100). Null si impossible.</summary>
    Task<int?> SetVolumeAsync(int pct, CancellationToken ct);

    /// <summary>Ouvre un lien (deezer://… dans l'application, https://… dans le navigateur).</summary>
    Task OpenAsync(string link, CancellationToken ct);

    /// <summary>Autorisation système éventuelle (Mac : Accessibilité pour les touches multimédia).</summary>
    PermissionInfo Permission(bool mediaKeys);

    /// <summary>Demande l'autorisation (affiche la fenêtre du système). Vrai si déjà accordée.</summary>
    bool RequestPermission();

    /// <summary>Ouvre la page des réglages du système où accorder l'autorisation.</summary>
    void OpenPermissionSettings();
}

public sealed record PermissionInfo(bool Needed, bool Granted, string? Name, string? Why);

/// <summary>Petit lanceur de commandes système (osascript, powershell, playerctl) avec délai maximal.</summary>
public static class ProcessRunner
{
    public static async Task<(int Code, string Output)> RunAsync(string file, IEnumerable<string> args, CancellationToken ct, int timeoutMs = 4000)
    {
        var psi = new ProcessStartInfo(file)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (string a in args)
        {
            psi.ArgumentList.Add(a);
        }

        using var p = new Process { StartInfo = psi };
        try
        {
            if (!p.Start())
            {
                return (-1, string.Empty);
            }
        }
        catch (Exception)
        {
            return (-1, string.Empty); // programme absent (playerctl non installé…)
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeoutMs);
        try
        {
            Task<string> output = p.StandardOutput.ReadToEndAsync(cts.Token);
            _ = p.StandardError.ReadToEndAsync(cts.Token);
            await p.WaitForExitAsync(cts.Token).ConfigureAwait(false);
            return (p.ExitCode, (await output.ConfigureAwait(false)).Trim());
        }
        catch (OperationCanceledException)
        {
            try
            {
                p.Kill(true);
            }
            catch (Exception)
            {
                // déjà terminé
            }

            return (-1, string.Empty);
        }
    }

    /// <summary>Ouvre un lien avec le programme associé du système.</summary>
    public static void OpenWithSystem(string link)
    {
        if (OperatingSystem.IsMacOS())
        {
            Process.Start(new ProcessStartInfo("open") { ArgumentList = { link }, UseShellExecute = false })?.Dispose();
        }
        else if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo(link) { UseShellExecute = true })?.Dispose();
        }
        else
        {
            Process.Start(new ProcessStartInfo("xdg-open") { ArgumentList = { link }, UseShellExecute = false })?.Dispose();
        }
    }
}
