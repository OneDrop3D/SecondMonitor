using System.Net;
using System.Net.Sockets;
using Photino.NET;

namespace SecondMonitor.Hub;

/// <summary>
/// Fenêtre de l'application SecondMonitor : l'interface s'affiche dans la vue web du système
/// (WebKit sur Mac, WebView2 sur Windows, WebKitGTK sur Linux), sans navigateur.
/// Si la vue web manque (Linux sans WebKitGTK), on se replie sur le navigateur.
/// </summary>
public static class AppWindow
{
    private static PhotinoWindow? _window;

    /// <summary>Délai laissé à la page pour signaler qu'elle a démarré avant d'ouvrir l'interface dans le navigateur.</summary>
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Affiche la fenêtre et bloque jusqu'à sa fermeture. Faux si aucune fenêtre n'a pu être créée.
    /// Si la page ne signale pas qu'elle est prête (vue web restée noire), la raison va dans journal.txt
    /// (<paramref name="dataDirectory"/>) et l'interface s'ouvre dans le navigateur ; la fenêtre reste ouverte.
    /// <paramref name="noGpu"/> : WebView2 sans accélération graphique (option « --sans-gpu », pilote graphique capricieux).
    /// </summary>
    public static bool Run(string url, string title, string? dataDirectory = null, bool noGpu = false)
    {
        if (!LinuxDisplayAvailable())
        {
            Console.WriteLine("Aucun écran graphique (DISPLAY / WAYLAND_DISPLAY) : l'interface s'ouvre dans le navigateur.");
            return false;
        }

        try
        {
            var window = new PhotinoWindow()
                .SetLogVerbosity(0)
                .SetTitle(title)
                .SetUseOsDefaultSize(false)
                .SetSize(1500, 940)
                .SetMinSize(720, 480)
                .Center()
                .SetResizable(true)
                .SetGrantBrowserPermissions(true)   // micro (radio) sans question à chaque fois sous Windows
                .SetMediaStreamEnabled(true)
                .SetMediaAutoplayEnabled(true)       // voix de l'ingénieur
                .SetContextMenuEnabled(false)
                .SetDevToolsEnabled(false)
                .SetLogVerbosity(0);

            // Windows : dossier de travail de WebView2 dans le profil de l'utilisateur (le dossier du programme peut être
            // en lecture seule ; WebView2 ne peut alors pas démarrer et la fenêtre reste noire).
            // Un dossier à part pour SecondMonitor : WebView2 refuse deux réglages différents (micro, lecture automatique…)
            // sur un même dossier, et celui du collecteur peut être encore utilisé (processus msedgewebview2 restés ouverts).
            string? webViewFolder = null;
            if (OperatingSystem.IsWindows())
            {
                webViewFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SecondMonitor", "WebView2-Affichage");
                window.SetTemporaryFilesPath(webViewFolder);
                if (noGpu)
                {
                    window.SetBrowserControlInitParameters("--disable-gpu");
                }
            }

            string icon = Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? "SecondMonitor.ico" : "icone.png");
            if (!OperatingSystem.IsMacOS() && File.Exists(icon))
            {
                window.SetIconFile(icon);
            }

            // Filet de sécurité : la page envoie « pret » dès qu'elle tourne (main.tsx). Sans nouvelles, la vue web est
            // sans doute restée noire : on le note dans le journal et on ouvre l'interface dans le navigateur.
            int ready = 0;
            Timer? watchdog = null;
            window.RegisterWebMessageReceivedHandler((_, message) =>
            {
                if (message != null && message.Contains("pret", StringComparison.Ordinal))
                {
                    Interlocked.Exchange(ref ready, 1);
                }
            });
            window.RegisterWindowCreatedHandler((_, _) => watchdog = new Timer(_ => NotReady(), null, ReadyTimeout, Timeout.InfiniteTimeSpan));

            void NotReady()
            {
                if (Volatile.Read(ref ready) != 0 || _window == null)
                {
                    return;
                }

                string where = webViewFolder != null ? $", dossier {webViewFolder}" : string.Empty;
                string hint = noGpu || !OperatingSystem.IsWindows() ? string.Empty : " Si elle reste noire, essayez l'option --sans-gpu (ou \"fenetreSansGpu\": true dans settings.json).";
                string reason = $"La fenêtre n'a pas affiché l'interface en {ReadyTimeout.TotalSeconds:0} s ({WebViewDescription()}{where}{(noGpu ? ", sans GPU" : string.Empty)}) : "
                    + $"interface ouverte dans le navigateur ({url}).{hint}";
                Console.WriteLine(reason);
                if (dataDirectory != null)
                {
                    HubJournal.Write(dataDirectory, "fenêtre", reason);
                }

                OpenBrowser(url);
            }

            window.Load(new Uri(url));
            _window = window;
            try
            {
                window.WaitForClose();
            }
            finally
            {
                _window = null;
                watchdog?.Dispose();
            }

            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or TypeInitializationException or EntryPointNotFoundException or BadImageFormatException or InvalidOperationException)
        {
            Console.WriteLine($"Fenêtre indisponible ({ex.Message}) : l'interface s'ouvre dans le navigateur.");
            return false;
        }
    }

    /// <summary>Vue web utilisée, pour le journal (sous Windows : version de WebView2 installée, si on la trouve).</summary>
    private static string WebViewDescription()
    {
        if (!OperatingSystem.IsWindows())
        {
            return OperatingSystem.IsMacOS() ? "WebKit" : "WebKitGTK";
        }

        try
        {
            // « WebView2 Runtime » (Evergreen), installé pour la machine ou pour l'utilisateur.
            const string client = @"Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}";
            foreach (string key in new[] { $@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\{client}", $@"HKEY_LOCAL_MACHINE\SOFTWARE\{client}", $@"HKEY_CURRENT_USER\Software\{client}" })
            {
                if (Microsoft.Win32.Registry.GetValue(key, "pv", null) is string v && v.Length > 0 && v != "0.0.0.0")
                {
                    return $"WebView2 {v}";
                }
            }

            return "WebView2 introuvable : à installer depuis https://go.microsoft.com/fwlink/p/?LinkId=2124703";
        }
        catch (Exception)
        {
            return "WebView2";
        }
    }

    /// <summary>
    /// Linux : sans session graphique (SSH, serveur), GTK s'arrêterait net au lieu de lever une exception.
    /// Avec une session : WebKitGTK sans le rendu DMA-BUF, qui donne une fenêtre blanche avec certains pilotes (NVIDIA).
    /// </summary>
    public static bool LinuxDisplayAvailable()
    {
        if (!OperatingSystem.IsLinux())
        {
            return true;
        }

        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")))
        {
            return false;
        }

        if (Environment.GetEnvironmentVariable("WEBKIT_DISABLE_DMABUF_RENDERER") == null)
        {
            Environment.SetEnvironmentVariable("WEBKIT_DISABLE_DMABUF_RENDERER", "1");
        }

        return true;
    }

    /// <summary>Ferme la fenêtre depuis n'importe quel fil (bouton « Quitter » des réglages).</summary>
    public static void Close()
    {
        PhotinoWindow? w = _window;
        if (w == null)
        {
            return;
        }

        try
        {
            w.Invoke(w.Close);
        }
        catch (Exception)
        {
            // Déjà fermée.
        }
    }

    /// <summary>Message simple dans une petite fenêtre (ou la console si aucune fenêtre n'est possible).</summary>
    public static void Message(bool consoleOnly, string title, string text)
    {
        Console.WriteLine($"{title} : {text}");
        if (consoleOnly || !LinuxDisplayAvailable())
        {
            return; // serveur sans fenêtre : pas de fenêtre surprise sur l'écran
        }

        try
        {
            string html = $"""
                <!doctype html><html lang="fr"><meta charset="utf-8"><title>{WebUtility.HtmlEncode(title)}</title>
                <body style="margin:0;background:#1b1b1b;color:#eee;font:15px/1.5 -apple-system,'Segoe UI',sans-serif;padding:28px">
                <h2 style="margin:0 0 12px;font-size:18px">{WebUtility.HtmlEncode(title)}</h2>
                <p style="white-space:pre-wrap">{WebUtility.HtmlEncode(text)}</p></body></html>
                """;
            new PhotinoWindow().SetLogVerbosity(0).SetTitle(title).SetUseOsDefaultSize(false).SetSize(560, 300).Center().SetLogVerbosity(0)
                .LoadRawString(html).WaitForClose();
        }
        catch (Exception)
        {
            // Pas de fenêtre possible : le message est dans la console.
        }
    }

    public static void OpenBrowser(string url)
    {
        try
        {
            if (OperatingSystem.IsMacOS())
            {
                System.Diagnostics.Process.Start("open", url);
            }
            else if (OperatingSystem.IsWindows())
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
            else
            {
                System.Diagnostics.Process.Start("xdg-open", url);
            }
        }
        catch (Exception)
        {
            Console.WriteLine($"Ouvrez {url} dans votre navigateur.");
        }
    }

    /// <summary>Le port est-il libre ? Sinon : est-ce un SecondMonitor déjà lancé (vrai) ou un autre programme (faux) ?</summary>
    public static (bool Free, bool OtherInstance) ProbePort(int port)
    {
        try
        {
            var probe = new TcpListener(IPAddress.Any, port);
            probe.Start();
            probe.Stop();
            return (true, false);
        }
        catch (SocketException)
        {
        }

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            string body = http.GetStringAsync($"http://127.0.0.1:{port}/api/status").GetAwaiter().GetResult();
            return (false, body.Contains("\"dataDir\"", StringComparison.Ordinal));
        }
        catch (Exception)
        {
            return (false, false);
        }
    }
}
