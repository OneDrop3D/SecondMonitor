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

    /// <summary>Affiche la fenêtre et bloque jusqu'à sa fermeture. Faux si aucune fenêtre n'a pu être créée.</summary>
    public static bool Run(string url, string title, Action? onCreated = null)
    {
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
            string icon = Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? "SecondMonitor.ico" : "icone.png");
            if (!OperatingSystem.IsMacOS() && File.Exists(icon))
            {
                window.SetIconFile(icon);
            }

            window.RegisterWindowCreatedHandler((_, _) => onCreated?.Invoke());
            window.Load(new Uri(url));
            _window = window;
            window.WaitForClose();
            _window = null;
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or TypeInitializationException or EntryPointNotFoundException or BadImageFormatException or InvalidOperationException)
        {
            Console.WriteLine($"Fenêtre indisponible ({ex.Message}) : l'interface s'ouvre dans le navigateur.");
            return false;
        }
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
    public static void Message(string title, string text)
    {
        Console.WriteLine($"{title} : {text}");
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
