using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Photino.NET;
using SecondMonitor.Hub.Core.Display;

namespace SecondMonitor.Hub;

/// <summary>Réponse de « Nouvelle fenêtre » : ouverte par l'application (native) ou non (la page ouvre alors une fenêtre du navigateur).</summary>
public sealed record NewWindowResult(bool Native, int? Numero = null, bool AutreEcran = false, string? Raison = null);

/// <summary>
/// Fenêtres de l'application SecondMonitor : l'interface s'affiche dans la vue web du système
/// (WebKit sur Mac, WebView2 sur Windows, WebKitGTK sur Linux), sans navigateur.
/// Si la vue web manque (Linux sans WebKitGTK), on se replie sur le navigateur.
///
/// Plusieurs écrans : « Nouvelle fenêtre » en ouvre une de plus (adresse « ?fenetre=2 » : sa propre page et ses préférences
/// d'affichage, muette par défaut), sur un écran qui n'a pas encore de fenêtre de SecondMonitor. Les fenêtres ouvertes,
/// leur place et leur écran sont gardés dans fenetres.json et rouverts au lancement suivant.
/// Fermer une fenêtre ajoutée ne quitte pas l'application ; fermer la principale quitte tout.
///
/// Photino : une seule boucle de messages, sur le fil principal. Les fenêtres ajoutées sont créées sur ce fil
/// (WaitForClose n'y bloque pas), avec la principale pour parente : sans parente, Linux quitte l'application
/// quand on ferme la fenêtre (photino.NET #230).
/// </summary>
public static class AppWindow
{
    private static readonly object Sync = new();
    private static readonly Dictionary<int, PhotinoWindow> Extra = new();
    private static PhotinoWindow? _window;
    private static WindowOptions? _options;
    private static WindowLayoutStore? _store;
    private static WindowLayout _layout = new();
    private static IReadOnlyList<ScreenArea> _screens = Array.Empty<ScreenArea>();
    private static Timer? _saveTimer;
    private static bool _quitting;
    private static bool _reopened;

    /// <summary>Windows : la fenêtre est restée noire avec l'accélération graphique ; Program retient « sans GPU » pour le prochain lancement.</summary>
    public static Action? GpuSuspected { get; set; }

    /// <summary>Délai laissé à la page pour signaler qu'elle a démarré avant d'ouvrir l'interface dans le navigateur.</summary>
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(6); // une page locale démarre en 1 à 2 s ; 15 s, c'était long devant une fenêtre noire

    /// <summary>Délai avant d'écrire fenetres.json après un déplacement ou un changement de taille.</summary>
    private static readonly TimeSpan SaveDelay = TimeSpan.FromSeconds(1);

    private sealed record WindowOptions(string BaseUrl, string Title, string? DataDirectory, bool NoGpu);

    /// <summary>Réponse quand aucune fenêtre native ne peut être ouverte (appareil du réseau, interface dans le navigateur…).</summary>
    public static NewWindowResult NotOpened(string reason) => new(false, Raison: reason);

    /// <summary>
    /// Affiche la fenêtre principale et bloque jusqu'à sa fermeture. Faux si aucune fenêtre n'a pu être créée.
    /// Si la page ne signale pas qu'elle est prête (vue web restée noire), la raison va dans journal.txt
    /// (<paramref name="dataDirectory"/>) et l'interface s'ouvre dans le navigateur ; la fenêtre reste ouverte.
    /// <paramref name="noGpu"/> : WebView2 sans accélération graphique (option « --sans-gpu », pilote graphique capricieux).
    /// <paramref name="remember"/> : rouvrir les fenêtres du lancement précédent et retenir celles-ci (faux pour une fenêtre
    /// ouverte par un second lancement, quand un SecondMonitor sans fenêtre tourne déjà).
    /// </summary>
    public static bool Run(string url, string title, string? dataDirectory = null, bool noGpu = false, bool remember = true)
    {
        if (!LinuxDisplayAvailable())
        {
            Console.WriteLine("Aucun écran graphique (DISPLAY / WAYLAND_DISPLAY) : l'interface s'ouvre dans le navigateur.");
            return false;
        }

        try
        {
            _options = new WindowOptions(url, title, dataDirectory, noGpu);
            _store = remember && dataDirectory != null ? new WindowLayoutStore(dataDirectory) : null;
            _layout = _store?.Load() ?? new WindowLayout();
            ScreenRect? saved = _layout.Main?.Rect;

            // « ?fenetre=1 » : la page sait qu'elle est la fenêtre principale de l'application et reprend sa dernière page.
            string mainUrl = remember ? WindowPlacement.WindowUrl(url, 1) : url;
            PhotinoWindow window = Create(mainUrl, title, null, saved);
            WatchReady(window, mainUrl, remember ? () => Later(ReopenSavedWindows) : null);
            window.RegisterWindowCreatedHandler((_, _) => OnMainCreated(window, saved));
            window.RegisterWindowClosingHandler((_, _) =>
            {
                OnMainClosing();
                return false;
            });
            Track(window, 1);
            lock (Sync)
            {
                _window = window;
            }

            try
            {
                window.WaitForClose();
            }
            finally
            {
                lock (Sync)
                {
                    _window = null;
                }
            }

            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or TypeInitializationException or EntryPointNotFoundException or BadImageFormatException or InvalidOperationException)
        {
            Console.WriteLine($"Fenêtre indisponible ({ex.Message}) : l'interface s'ouvre dans le navigateur.");
            return false;
        }
    }

    /// <summary>
    /// « Nouvelle fenêtre » (depuis n'importe quel fil) : ouvre une fenêtre de plus, sur un écran qui n'en a pas encore.
    /// <paramref name="tab"/> : page affichée au départ (celle de la fenêtre d'où l'on vient).
    /// </summary>
    public static NewWindowResult OpenNew(string? tab)
    {
        PhotinoWindow? main;
        lock (Sync)
        {
            main = _quitting ? null : _window;
            if (main != null && Extra.Count >= WindowLayoutStore.MaxWindows)
            {
                return NotOpened($"{WindowLayoutStore.MaxWindows + 1} fenêtres au plus.");
            }
        }

        if (main == null)
        {
            return NotOpened("Pas de fenêtre d'application (interface dans le navigateur, ou sans écran).");
        }

        NewWindowResult result = NotOpened("La fenêtre n'a pas pu s'ouvrir.");
        try
        {
            main.Invoke(() =>
            {
                int number;
                lock (Sync)
                {
                    if (_quitting || _window == null)
                    {
                        return;
                    }

                    number = WindowPlacement.NextNumber(Extra.Keys);
                }

                if (OpenExtra(number, tab, null, null, out bool otherScreen))
                {
                    result = new NewWindowResult(true, number, otherScreen);
                }
            });
        }
        catch (Exception ex)
        {
            Journal($"Nouvelle fenêtre impossible : {ex.Message}");
            return NotOpened(ex.Message);
        }

        return result;
    }

    /// <summary>État des fenêtres (GET /api/fenetres) : fenêtres natives possibles, nombre d'écrans, fenêtres ajoutées ouvertes.</summary>
    public static object Describe()
    {
        lock (Sync)
        {
            return new
            {
                native = _window != null && !_quitting,
                ecrans = _screens.Count,
                fenetres = Extra.Keys.OrderBy(n => n).ToArray(),
            };
        }
    }

    /// <summary>
    /// Second lancement alors que SecondMonitor tourne déjà avec sa fenêtre : on lui demande d'ouvrir une fenêtre de plus
    /// (muette, avec sa propre page) plutôt que d'ouvrir une seconde fenêtre principale qui ferait entendre la voix deux fois.
    /// </summary>
    public static bool AskRunningInstance(int port)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var content = new StringContent("{}", Encoding.UTF8, "application/json");
            using HttpResponseMessage response = http.PostAsync($"http://127.0.0.1:{port}/api/fenetres", content).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                return false; // ancienne version sans fenêtres multiples
            }

            using JsonDocument json = JsonDocument.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            return json.RootElement.TryGetProperty("native", out JsonElement native) && native.ValueKind == JsonValueKind.True;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // ───── Création des fenêtres ─────

    /// <summary>
    /// Même réglages pour toutes les fenêtres. Windows : WebView2 refuse deux réglages différents (micro, lecture
    /// automatique, GPU…) sur un même dossier de travail, et les fenêtres doivent partager ce dossier (préférences communes).
    /// </summary>
    private static PhotinoWindow Create(string url, string title, PhotinoWindow? parent, ScreenRect? place)
    {
        WindowOptions o = _options ?? throw new InvalidOperationException("Fenêtre non préparée");
        var window = new PhotinoWindow(parent)
            .SetLogVerbosity(0)
            .SetTitle(title)
            .SetUseOsDefaultSize(false)
            .SetMinSize(WindowPlacement.MinWidth, WindowPlacement.MinHeight)
            .SetResizable(true)
            .SetGrantBrowserPermissions(true)   // micro (radio) sans question à chaque fois sous Windows
            .SetMediaStreamEnabled(true)
            .SetMediaAutoplayEnabled(true)       // voix de l'ingénieur
            .SetContextMenuEnabled(false)
            .SetDevToolsEnabled(false)
            .SetLogVerbosity(0);

        if (place is { } p)
        {
            // Position avant création : la fenêtre apparaît directement à sa place (Mac : haut de la fenêtre, depuis le haut de l'écran principal).
            window.SetUseOsDefaultLocation(false).SetSize(p.Width, p.Height).SetLocation(new Point(p.X, p.Y));
        }
        else
        {
            window.SetSize(WindowPlacement.DefaultWidth, WindowPlacement.DefaultHeight).Center();
        }

        // Windows : dossier de travail de WebView2 dans le profil de l'utilisateur (le dossier du programme peut être
        // en lecture seule ; WebView2 ne peut alors pas démarrer et la fenêtre reste noire).
        // Un dossier à part pour SecondMonitor : WebView2 refuse deux réglages différents (micro, lecture automatique…)
        // sur un même dossier, et celui du collecteur peut être encore utilisé (processus msedgewebview2 restés ouverts).
        if (OperatingSystem.IsWindows())
        {
            window.SetTemporaryFilesPath(WebViewFolder());
            if (o.NoGpu)
            {
                window.SetBrowserControlInitParameters("--disable-gpu");
            }
        }

        string icon = Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? "SecondMonitor.ico" : "icone.png");
        if (!OperatingSystem.IsMacOS() && File.Exists(icon))
        {
            window.SetIconFile(icon);
        }

        window.Load(new Uri(url));
        return window;
    }

    private static string WebViewFolder()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SecondMonitor", "WebView2-Affichage");

    /// <summary>
    /// Filet de sécurité : la page envoie « pret » dès qu'elle tourne (main.tsx). Sans nouvelles, la vue web est
    /// sans doute restée noire : on le note dans le journal et on ouvre cette page dans le navigateur.
    /// <paramref name="onReady"/> : appelé une fois, sur le fil des fenêtres, quand la page est prête.
    /// </summary>
    private static void WatchReady(PhotinoWindow window, string url, Action? onReady)
    {
        int ready = 0;
        int closed = 0;
        Timer? watchdog = null;
        window.RegisterWebMessageReceivedHandler((_, message) =>
        {
            if (message != null && message.Contains("pret", StringComparison.Ordinal) && Interlocked.Exchange(ref ready, 1) == 0)
            {
                onReady?.Invoke();
            }
        });
        window.RegisterWindowCreatedHandler((_, _) => watchdog = new Timer(_ => NotReady(), null, ReadyTimeout, Timeout.InfiniteTimeSpan));
        window.RegisterWindowClosingHandler((_, _) =>
        {
            Interlocked.Exchange(ref closed, 1);
            watchdog?.Dispose();
            return false;
        });

        void NotReady()
        {
            if (Volatile.Read(ref ready) != 0 || Volatile.Read(ref closed) != 0 || _quitting)
            {
                return;
            }

            bool noGpu = _options?.NoGpu ?? false;
            string where = OperatingSystem.IsWindows() ? $", dossier {WebViewFolder()}" : string.Empty;
            // Cause la plus fréquente d'une WebView2 noire : le pilote graphique. Le prochain lancement se fera sans GPU.
            bool retryWithoutGpu = !noGpu && OperatingSystem.IsWindows() && GpuSuspected != null;
            if (retryWithoutGpu)
            {
                GpuSuspected!.Invoke();
            }

            string hint = retryWithoutGpu ? " Au prochain lancement, la fenêtre s'ouvrira sans accélération graphique (\"fenetreSansGpu\": true dans settings.json, à remettre à false si ça n'aide pas)." : string.Empty;
            string reason = $"La fenêtre n'a pas affiché l'interface en {ReadyTimeout.TotalSeconds:0} s ({WebViewDescription()}{where}{(noGpu ? ", sans GPU" : string.Empty)}) : "
                + $"interface ouverte dans le navigateur ({url}).{hint}";
            Console.WriteLine(reason);
            Journal(reason);
            OpenBrowser(url);
        }
    }

    /// <summary>Fenêtre principale créée : place retrouvée (ou recentrée si son écran a disparu), écrans connus.</summary>
    private static void OnMainCreated(PhotinoWindow window, ScreenRect? saved)
    {
        try
        {
            RefreshScreens(window);
            if (saved is { } s)
            {
                ScreenRect? place = WindowPlacement.Restore(s, _layout.Main?.Screen, _screens);
                if (place == null)
                {
                    window.SetSize(WindowPlacement.DefaultWidth, WindowPlacement.DefaultHeight);
                    window.Center();
                }
                else if (place != s)
                {
                    window.SetSize(place.Value.Width, place.Value.Height);
                    window.SetLocation(new Point(place.Value.X, place.Value.Y));
                }
            }

            lock (Sync)
            {
                _layout.Main ??= new SavedWindow { Number = 1 };
                _layout.Main.Rect = CurrentRect(window);
                UpdateScreen(_layout.Main);
            }
        }
        catch (Exception ex)
        {
            Journal($"Place de la fenêtre inconnue : {ex.Message}");
        }
    }

    /// <summary>
    /// Ouvre la fenêtre n° <paramref name="number"/>, sur le fil des fenêtres (boucle de messages déjà lancée).
    /// Place mémorisée si son écran est toujours là, sinon un écran libre (ou décalée sur le même écran).
    /// </summary>
    private static bool OpenExtra(int number, string? tab, ScreenRect? saved, ScreenRect? savedScreen, out bool otherScreen)
    {
        otherScreen = false;
        PhotinoWindow? main = _window;
        WindowOptions? o = _options;
        if (main == null || o == null || _quitting)
        {
            return false;
        }

        RefreshScreens(main);
        ScreenRect mainRect = CurrentRect(main);
        ScreenRect? place = saved is { } s ? WindowPlacement.Restore(s, savedScreen, _screens) : null;
        IReadOnlyList<ScreenArea> screens;
        lock (Sync)
        {
            screens = _screens;
            if (place == null)
            {
                var others = _layout.Windows.Where(w => Extra.ContainsKey(w.Number)).Select(w => w.Rect).ToList();
                place = WindowPlacement.PlaceNew(mainRect, others, screens, out otherScreen);
            }
            else
            {
                otherScreen = WindowPlacement.ScreenIndexOf(place.Value, screens) != WindowPlacement.ScreenIndexOf(mainRect, screens);
            }
        }

        string url = WindowPlacement.WindowUrl(o.BaseUrl, number, tab);
        PhotinoWindow window = Create(url, $"{o.Title} · fenêtre {number}", main, place);
        WatchReady(window, url, null);
        window.RegisterWindowClosingHandler((_, _) =>
        {
            OnExtraClosing(number);
            return false;
        });
        Track(window, number);
        lock (Sync)
        {
            Extra[number] = window;
            SavedWindow entry = _layout.Windows.FirstOrDefault(w => w.Number == number) ?? AddEntry(number);
            entry.Rect = place.Value;
            UpdateScreen(entry);
        }

        window.WaitForClose(); // ne bloque pas : la boucle de messages tourne déjà (fenêtre principale)
        try
        {
            Moved(number, CurrentRect(window));
        }
        catch (Exception)
        {
            // Place réelle inconnue : celle demandée reste retenue.
        }

        ScheduleSave();
        return true;
    }

    private static SavedWindow AddEntry(int number)
    {
        var entry = new SavedWindow { Number = number };
        _layout.Windows.Add(entry);
        return entry;
    }

    /// <summary>
    /// Exécute <paramref name="action"/> sur le fil des fenêtres, après l'évènement en cours : on ne crée pas de fenêtre
    /// pendant le traitement d'un message de la page (WebView2 et WebKit n'aiment pas la réentrance).
    /// </summary>
    private static void Later(Action action)
    {
        ThreadPool.QueueUserWorkItem(_ =>
        {
            PhotinoWindow? main;
            lock (Sync)
            {
                main = _quitting ? null : _window;
            }

            try
            {
                main?.Invoke(action);
            }
            catch (Exception ex)
            {
                Journal($"Fenêtres non rouvertes : {ex.Message}");
            }
        });
    }

    /// <summary>La fenêtre principale a affiché sa page : on rouvre les fenêtres ajoutées du lancement précédent.</summary>
    private static void ReopenSavedWindows()
    {
        List<SavedWindow> saved;
        lock (Sync)
        {
            if (_reopened)
            {
                return;
            }

            _reopened = true;
            saved = _layout.Windows.Where(w => !Extra.ContainsKey(w.Number)).ToList();
        }

        foreach (SavedWindow w in saved)
        {
            try
            {
                if (!OpenExtra(w.Number, null, w.Rect, w.Screen, out _))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                Journal($"Fenêtre {w.Number} non rouverte : {ex.Message}");
                lock (Sync)
                {
                    _layout.Windows.RemoveAll(x => x.Number == w.Number);
                }
            }
        }
    }

    private static void OnExtraClosing(int number)
    {
        lock (Sync)
        {
            Extra.Remove(number);
            if (_quitting)
            {
                return; // l'application quitte : la fenêtre sera rouverte au prochain lancement
            }

            _layout.Windows.RemoveAll(w => w.Number == number);
        }

        ScheduleSave();
    }

    /// <summary>
    /// Fermer la fenêtre principale quitte tout : les fenêtres ouvertes sont retenues (rouvertes au prochain lancement)
    /// puis fermées. Sur Mac, l'application s'arrête dès la dernière fenêtre fermée : on enregistre tout de suite.
    /// </summary>
    private static void OnMainClosing()
    {
        List<PhotinoWindow> others;
        lock (Sync)
        {
            if (_quitting)
            {
                return;
            }

            _quitting = true;
            others = Extra.Values.ToList();
            _saveTimer?.Dispose();
            _saveTimer = null;
        }

        SaveNow();
        foreach (PhotinoWindow w in others)
        {
            try
            {
                w.Close();
            }
            catch (Exception)
            {
                // Déjà fermée.
            }
        }
    }

    // ───── Place des fenêtres ─────

    private static void Track(PhotinoWindow window, int number)
    {
        window.RegisterLocationChangedHandler((_, p) => Moved(number, x: p.X, y: p.Y));
        window.RegisterSizeChangedHandler((_, s) => Moved(number, width: s.Width, height: s.Height));
    }

    private static void Moved(int number, ScreenRect rect) => Moved(number, rect.X, rect.Y, rect.Width, rect.Height);

    /// <summary>Déplacement ou nouvelle taille (valeurs données par l'évènement : la fenêtre n'est pas interrogée pendant sa création).</summary>
    private static void Moved(int number, int? x = null, int? y = null, int? width = null, int? height = null)
    {
        // Fenêtre réduite (Windows la met à -32000 et la rétrécit) : on garde la place d'avant.
        if (x <= -10000 || y <= -10000 || width < 200 || height < 100)
        {
            return;
        }

        lock (Sync)
        {
            SavedWindow? entry = number == 1 ? _layout.Main : _layout.Windows.FirstOrDefault(w => w.Number == number);
            if (_quitting || entry == null)
            {
                return;
            }

            ScreenRect r = entry.Rect;
            entry.Rect = new ScreenRect(x ?? r.X, y ?? r.Y, width ?? r.Width, height ?? r.Height);
            UpdateScreen(entry);
        }

        ScheduleSave();
    }

    /// <summary>Écran où se trouve la fenêtre (le plus grand morceau), d'après la dernière liste des écrans.</summary>
    private static void UpdateScreen(SavedWindow entry)
    {
        int index = WindowPlacement.ScreenIndexOf(entry.Rect, _screens);
        if (index >= 0)
        {
            entry.Screen = _screens[index].Area;
        }
    }

    private static ScreenRect CurrentRect(PhotinoWindow window)
    {
        Point location = window.Location;
        Size size = window.Size;
        return new ScreenRect(location.X, location.Y, size.Width, size.Height);
    }

    /// <summary>Écrans branchés (sur le fil des fenêtres). Mac : convertis depuis les coordonnées Cocoa.</summary>
    private static void RefreshScreens(PhotinoWindow window)
    {
        try
        {
            var monitors = window.Monitors;
            if (monitors.Count == 0)
            {
                return;
            }

            int primaryHeight = monitors[0].MonitorArea.Height; // Mac : l'écran 0 est celui de la barre des menus
            ScreenRect Convert(Rectangle r)
            {
                var rect = new ScreenRect(r.X, r.Y, r.Width, r.Height);
                return OperatingSystem.IsMacOS() ? WindowPlacement.FromCocoa(rect, primaryHeight) : rect;
            }

            var list = monitors.Select(m => new ScreenArea(Convert(m.MonitorArea), Convert(m.WorkArea))).ToList();
            lock (Sync)
            {
                _screens = list;
            }
        }
        catch (Exception)
        {
            // Écrans inconnus : on garde la liste précédente.
        }
    }

    private static void ScheduleSave()
    {
        if (_store == null)
        {
            return;
        }

        lock (Sync)
        {
            if (_quitting)
            {
                return;
            }

            _saveTimer ??= new Timer(_ => SaveNow(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _saveTimer.Change(SaveDelay, Timeout.InfiniteTimeSpan);
        }
    }

    private static void SaveNow()
    {
        WindowLayoutStore? store = _store;
        if (store == null)
        {
            return;
        }

        WindowLayout copy;
        lock (Sync)
        {
            copy = WindowLayoutStore.Clean(_layout);
        }

        try
        {
            store.Save(copy);
        }
        catch (Exception ex)
        {
            Journal($"Fenêtres non enregistrées ({store.FilePath}) : {ex.Message}");
        }
    }

    private static void Journal(string text)
    {
        if (_options?.DataDirectory is { } dir)
        {
            HubJournal.Write(dir, "fenêtre", text);
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

    /// <summary>Ferme la fenêtre principale (donc toutes) depuis n'importe quel fil (bouton « Quitter » des réglages).</summary>
    public static void Close()
    {
        PhotinoWindow? w;
        lock (Sync)
        {
            w = _window;
        }

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
