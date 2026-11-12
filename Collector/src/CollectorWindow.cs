namespace SecondMonitor.Collector
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text.Json;
    using System.Threading;
    using Photino.NET;

    /// <summary>
    /// Petite fenêtre d'état du collecteur : SecondMonitor connecté, jeu détecté, débit, qualité, démarrage automatique, journal.
    /// Elle utilise la vue web du système (rien à installer) et se met à jour une fois par seconde, sans charge pour le jeu.
    /// </summary>
    internal static class CollectorWindow
    {
        private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public static bool RestartRequested { get; private set; }

        /// <summary>Relancer avec les droits administrateur (installation d'un plugin dans Program Files).</summary>
        public static bool RestartAsAdmin { get; private set; }

        /// <summary>Affiche la fenêtre et bloque jusqu'à sa fermeture. Faux si la vue web manque (on passe alors en console).</summary>
        public static bool Run(CollectorRuntime runtime, bool minimized)
        {
            PhotinoWindow window;
            try
            {
                window = new PhotinoWindow()
                    .SetLogVerbosity(0)
                    .SetTitle("SecondMonitor Collecteur")
                    .SetUseOsDefaultSize(false)
                    .SetSize(540, 720)
                    .SetMinSize(420, 520)
                    .Center()
                    .SetResizable(true)
                    .SetContextMenuEnabled(false)
                    .SetDevToolsEnabled(false)
                    .SetLogVerbosity(0)
                    .SetMinimized(minimized);
                // Windows : dossier de travail de WebView2 dans le profil de l'utilisateur (le dossier du programme peut être
                // en lecture seule ; WebView2 ne peut alors pas démarrer et la fenêtre reste noire).
                if (OperatingSystem.IsWindows())
                {
                    window.SetTemporaryFilesPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SecondMonitor", "WebView2"));
                }

                string icon = Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? "SecondMonitor.ico" : "icone.png");
                if (!OperatingSystem.IsMacOS() && File.Exists(icon))
                {
                    window.SetIconFile(icon);
                }
            }
            catch (Exception ex) when (IsMissingWebView(ex))
            {
                return false;
            }

            Timer timer = null;
            void Send(object message)
            {
                string text = JsonSerializer.Serialize(message, Json);
                try
                {
                    window.Invoke(() => window.SendWebMessage(text));
                }
                catch (Exception)
                {
                    // Fenêtre en cours de fermeture.
                }
            }

            void SendState()
            {
                runtime.UpdateStats();
                var s = runtime.Settings;
                var (tick, _, player, others) = s.Rates;
                Send(new
                {
                    type = "etat",
                    hubs = runtime.Hubs,
                    jeu = runtime.ActiveGame,
                    pause = runtime.Paused,
                    demo = runtime.Demo,
                    envois = Math.Round(runtime.PackagesPerSecond),
                    ko = Math.Round(runtime.KilobytesPerSecond, 1),
                    adresses = CollectorRuntime.LocalAddresses(),
                    port = s.Port,
                    qualite = NormalizeQuality(s.Qualite),
                    lectures = 1000 / tick,
                    joueurHz = 1000 / player,
                    adversairesHz = Math.Round(1000.0 / others, 1),
                    jeux = runtime.GameNames,
                    autostart = Autostart.IsEnabled(),
                    version = typeof(CollectorWindow).Assembly.GetName().Version?.ToString(3),
                    dossier = runtime.DataDirectory,
                    systeme = OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "mac" : "linux",
                    visualiseur = ViewerPath() != null || OperatingSystem.IsMacOS(),
                    donnees = runtime.GameDataFlowing,
                    voitures = runtime.Host.LastDriverCount,
                    conseil = runtime.Advice() is { } a ? new { titre = a.Title, texte = a.Text, action = a.Action } : null,
                });
            }

            string startQuality = NormalizeQuality(runtime.Settings.Qualite);
            void OnLog(string line) => Send(new { type = "journal", ligne = line });
            runtime.LogAdded += OnLog;

            window.RegisterWebMessageReceivedHandler((_, message) =>
            {
                try
                {
                    using var doc = JsonDocument.Parse(message);
                    JsonElement root = doc.RootElement;
                    string cmd = root.GetProperty("cmd").GetString();
                    switch (cmd)
                    {
                        case "pret":
                            Send(new { type = "journalComplet", lignes = runtime.LogLines() });
                            SendState();
                            timer ??= new Timer(_ => SendState(), null, 1000, 1000);
                            break;
                        case "qualite":
                            runtime.Settings.Qualite = root.GetProperty("valeur").GetString();
                            runtime.Settings.Save();
                            Send(new { type = "relancer", necessaire = NormalizeQuality(runtime.Settings.Qualite) != startQuality });
                            SendState();
                            break;
                        case "demo":
                            runtime.SetDemo(root.GetProperty("valeur").GetBoolean());
                            SendState();
                            break;
                        case "autostart":
                            runtime.Log(Autostart.Set(root.GetProperty("valeur").GetBoolean()));
                            SendState();
                            break;
                        case "relancer":
                            RestartRequested = true;
                            window.Close();
                            break;
                        case "installer":
                            runtime.Host.RunDecision();
                            SendState();
                            break;
                        case "admin":
                            RestartRequested = true;
                            RestartAsAdmin = true;
                            window.Close();
                            break;
                        case "dossier":
                            Open(runtime.DataDirectory);
                            break;
                        case "visualiseur":
                            OpenViewer(runtime);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    runtime.Log($"Commande de la fenêtre ignorée : {ex.Message}");
                }
            });

            try
            {
                window.LoadRawString(Page());
                window.WaitForClose();
            }
            catch (Exception ex) when (IsMissingWebView(ex))
            {
                runtime.LogAdded -= OnLog;
                return false;
            }
            finally
            {
                timer?.Dispose();
            }

            runtime.LogAdded -= OnLog;
            return true;
        }

        /// <summary>Message simple dans une petite fenêtre. Faux si aucune fenêtre n'est possible.</summary>
        public static bool Message(string title, string text)
        {
            try
            {
                string html = $"""
                    <!doctype html><html lang="fr"><meta charset="utf-8">
                    <body style="margin:0;background:#1b1b1b;color:#eee;font:15px/1.5 -apple-system,'Segoe UI',sans-serif;padding:28px">
                    <h2 style="margin:0 0 12px;font-size:18px">{WebUtility.HtmlEncode(title)}</h2>
                    <p style="white-space:pre-wrap">{WebUtility.HtmlEncode(text)}</p></body></html>
                    """;
                new PhotinoWindow().SetTitle(title).SetUseOsDefaultSize(false).SetSize(520, 260).Center().SetLogVerbosity(0)
                    .LoadRawString(html).WaitForClose();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsMissingWebView(Exception ex) =>
            ex is DllNotFoundException or TypeInitializationException or EntryPointNotFoundException or BadImageFormatException or InvalidOperationException;

        private static string NormalizeQuality(string q) => q?.ToLowerInvariant() switch
        {
            "econome" or "économe" => "econome",
            "precis" or "précis" => "precis",
            _ => "equilibre",
        };

        private static string Page()
        {
            using Stream stream = typeof(CollectorWindow).Assembly.GetManifestResourceStream("SecondMonitor.Collector.interface.html");
            using var reader = new StreamReader(stream!);
            return reader.ReadToEnd();
        }

        /// <summary>SecondMonitor (l'application qui affiche) installée à côté du collecteur.</summary>
        private static string ViewerPath()
        {
            string dir = AppContext.BaseDirectory;
            foreach (string name in new[] { "SecondMonitorHub.exe", "SecondMonitorHub", Path.Combine("..", "SecondMonitorHub.exe"), Path.Combine("..", "SecondMonitorHub") })
            {
                string path = Path.GetFullPath(Path.Combine(dir, name));
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private static void OpenViewer(CollectorRuntime runtime)
        {
            try
            {
                if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", new[] { "-b", "fr.onedrop3d.secondmonitor" });
                    return;
                }

                string path = ViewerPath();
                if (path == null)
                {
                    runtime.Log("SecondMonitor n'est pas installé à côté du collecteur.");
                    return;
                }

                Process.Start(new ProcessStartInfo(path) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(path)! });
            }
            catch (Exception ex)
            {
                runtime.Log($"Impossible d'ouvrir SecondMonitor : {ex.Message}");
            }
        }

        private static void Open(string folder)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
                }
                else
                {
                    Process.Start(OperatingSystem.IsMacOS() ? "open" : "xdg-open", folder);
                }
            }
            catch (Exception)
            {
                // Pas de gestionnaire de fichiers.
            }
        }
    }
}
