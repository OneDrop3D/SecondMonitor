namespace SecondMonitor.Collector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Le collecteur proprement dit, sans affichage : serveur de diffusion, lecture du jeu (ou course de démonstration),
    /// journal et chiffres de débit. La fenêtre et la console ne font que le présenter.
    /// </summary>
    internal sealed class CollectorRuntime : IDisposable
    {
        private const int MaxLogLines = 300;

        private readonly object _lock = new();
        private readonly List<string> _log = new();
        private readonly BroadcastServer _server;
        private readonly ConnectorHost _host;
        private CancellationTokenSource _modeCts;
        private Task _modeTask = Task.CompletedTask;
        private long _lastPackages;
        private long _lastBytes;
        private DateTime _lastStats = DateTime.UtcNow;
        private long _sourceReads;
        private CollectorDiagnostics _diagnostics;

        public CollectorRuntime(CollectorSettings settings, string dataDirectory)
        {
            Settings = settings;
            DataDirectory = dataDirectory;
            var provider = new CollectorSettingsProvider(settings);
            _server = new BroadcastServer(provider);
            _host = new ConnectorHost(provider, _server, settings, Log);
            _server.PeerConnected += ip => Log($"SecondMonitor connecté : {ip}");
            _server.PeerDisconnected += (ip, why) => Log($"Hub déconnecté : {ip} ({Reason(why)})");
        }

        /// <summary>Une ligne de journal (déjà horodatée).</summary>
        public event Action<string> LogAdded;

        public CollectorSettings Settings { get; }

        public string DataDirectory { get; }

        public bool Demo { get; private set; }

        public double PackagesPerSecond { get; private set; }

        public double KilobytesPerSecond { get; private set; }

        public IReadOnlyList<string> GameNames => _host.GameNames;

        public string ActiveGame => Demo ? "Course de démonstration" : ReplayFile != null ? "Rejeu d'un enregistrement" : _host.ActiveGame;

        /// <summary>Enregistrement du hub (.smrec) à renvoyer au lieu de lire le jeu (diagnostic, option « --rejouer »).</summary>
        public string ReplayFile { get; set; }

        /// <summary>Vitesse du rejeu (1 = temps réel).</summary>
        public double ReplaySpeed { get; set; } = 1;

        /// <summary>Lectures du jeu (ou de la démonstration / du rejeu) depuis le démarrage.</summary>
        public long GameReads => _host.GameReads + Interlocked.Read(ref _sourceReads);

        public long PackagesSent => _server.PackagesSent;

        public long WireBytesSent => _server.WireBytesSent;

        public long DatagramsSent => _server.DatagramsSent;

        /// <summary>Mesures du mode diagnostic (créées à la première demande).</summary>
        public CollectorDiagnostics Diagnostics => _diagnostics ??= new CollectorDiagnostics(this);

        public bool Paused => !Demo && _host.Paused;

        public string[] Hubs => _server.Peers;

        public ConnectorHost Host => _host;

        /// <summary>Bouton radio du PC de jeu (Windows), créé au démarrage.</summary>
        public RadioButton Radio { get; private set; }

        /// <summary>Le jeu envoie-t-il de vraies données de course (pas seulement des signes de vie) ?</summary>
        public bool GameDataFlowing => Demo || ReplayFile != null || (_host.ActiveGame != null && (DateTime.UtcNow - _host.LastGameDataUtc).TotalSeconds < 5);

        /// <summary>
        /// Ce qui bloque, en clair, et quoi faire : plugin à installer, droits administrateur, télémétrie du jeu désactivée…
        /// Null quand tout va bien.
        /// </summary>
        public (string Title, string Text, string Action)? Advice()
        {
            if (Demo)
            {
                return null;
            }

            if (_host.Decision is PendingDecision d)
            {
                return ($"{d.Game} : plugin à installer", d.Message, "installer");
            }

            if (_host.NeedsAdmin)
            {
                return ("Droits administrateur nécessaires", "Le jeu est installé dans un dossier protégé. Relancez le collecteur en administrateur une seule fois : le plugin s'installera, puis vous pourrez le relancer normalement.", "admin");
            }

            if (_host.WaitingGame is string w && (DateTime.UtcNow - _host.WaitingSinceUtc).TotalSeconds > 8)
            {
                return ($"{w} est lancé, mais pas encore lisible", GameHint(w, connected: false), null);
            }

            if (_host.ActiveGame is string g && !GameDataFlowing && (DateTime.UtcNow - _host.ActiveSinceUtc).TotalSeconds > 10)
            {
                return ($"{g} est détecté, mais n'envoie pas de données", GameHint(g, connected: true), null);
            }

            return null;
        }

        private static string GameHint(string game, bool connected)
        {
            if (game.StartsWith("F1", StringComparison.Ordinal))
            {
                string year = game.Contains("24", StringComparison.Ordinal) ? "2024" : game.Replace("F1 ", string.Empty, StringComparison.Ordinal);
                return $"Activez la télémétrie dans le jeu : Options › Paramètres › Télémétrie. Télémétrie UDP : Activée · Mode diffusion : Désactivé · "
                    + $"Adresse IP : 127.0.0.1 · Port : 20777 · Format UDP : {year}. Les données arrivent dès que vous êtes en piste (tour de formation compris). "
                    + "Fréquence d'envoi UDP : 20 Hz suffit (30 Hz en qualité « Précis ») ; 60 Hz fait travailler le PC pour rien.";
            }

            if (game == "Assetto Corsa")
            {
                return "Assetto Corsa ne partage ses données qu'avec le plugin SecondMonitor actif. S'il n'est pas installé, le bouton « Installer le plugin » apparaît ici. "
                    + "Ensuite, activez l'application Python « SecondMonitor » (Content Manager : réglages d'Assetto Corsa, applications Python ; ou dans le jeu : Options › Général › Modules d'interface), puis relancez la session.";
            }

            if (game is "Le Mans Ultimate" or "rFactor 2")
            {
                return "Le plugin de mémoire partagée du jeu doit être activé (rFactor2SharedMemoryMapPlugin64, dans UserData\\player\\CustomPluginVariables.JSON : \" Enabled\":1). "
                    + "Les données arrivent une fois la session chargée.";
            }

            if (game == "Forza Motorsport")
            {
                return "Activez « Data Out » dans les réglages du jeu (HUD et jouabilité) : adresse IP 127.0.0.1, port 20127.";
            }

            return connected
                ? "Les données arrivent une fois en piste (pas dans les menus ou au chargement)."
                : "Le jeu est peut-être encore en train de charger. Si cela dure, fermez-le et relancez-le après le collecteur.";
        }

        public bool Start()
        {
            if (!_server.Start(Settings.Port))
            {
                Log($"Impossible d'ouvrir le port {Settings.Port} : SecondMonitor (plugin « Data Broadcast Server ») ou un autre collecteur est peut-être déjà lancé. Fermez-le, ou changez le port.");
                return false;
            }

            Log("Prêt. En attente de SecondMonitor (l'application qui affiche les données) sur le réseau…");
            Radio = new RadioButton(this);
            return true;
        }

        /// <summary>Lit le jeu, ou envoie la course de démonstration (pour tester la liaison sans jeu).</summary>
        public void SetDemo(bool demo)
        {
            CancellationTokenSource old;
            Task oldTask;
            lock (_lock)
            {
                if (demo == Demo && _modeCts != null)
                {
                    return;
                }

                Demo = demo;
                old = _modeCts;
                oldTask = _modeTask;
                _modeCts = new CancellationTokenSource();
                CancellationToken token = _modeCts.Token;
                _modeTask = Task.Run(async () =>
                {
                    old?.Cancel();
                    await oldTask.ContinueWith(_ => { }).ConfigureAwait(false);
                    try
                    {
                        if (demo)
                        {
                            await RunDemoAsync(token).ConfigureAwait(false);
                        }
                        else if (ReplayFile != null)
                        {
                            await RunReplayAsync(token).ConfigureAwait(false);
                        }
                        else
                        {
                            await _host.RunAsync(token).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        Log($"Erreur : {ex.Message}");
                    }
                });
            }
        }

        /// <summary>Recalcule le débit (appelé environ une fois par seconde par l'affichage).</summary>
        public void UpdateStats()
        {
            DateTime now = DateTime.UtcNow;
            double dt = Math.Max(0.2, (now - _lastStats).TotalSeconds);
            long p = _server.PackagesSent;
            long b = _server.WireBytesSent; // ce qui part vraiment sur le réseau (compressé)
            PackagesPerSecond = (p - _lastPackages) / dt;
            KilobytesPerSecond = (b - _lastBytes) / dt / 1024;
            _lastPackages = p;
            _lastBytes = b;
            _lastStats = now;
        }

        public string[] LogLines()
        {
            lock (_log)
            {
                return _log.ToArray();
            }
        }

        public void Log(string message)
        {
            string line = $"{DateTime.Now:HH:mm:ss}  {message}";
            lock (_log)
            {
                _log.Add(line);
                if (_log.Count > MaxLogLines)
                {
                    _log.RemoveRange(0, _log.Count - MaxLogLines);
                }
            }

            LogAdded?.Invoke(line);
        }

        public void Stop()
        {
            Task task;
            lock (_lock)
            {
                _modeCts?.Cancel();
                task = _modeTask;
            }

            try
            {
                task.Wait(TimeSpan.FromSeconds(3));
            }
            catch (Exception)
            {
                // Arrêt : sans importance.
            }
        }

        public void Dispose()
        {
            Stop();
            Radio?.Dispose();
            _server.Dispose();
        }

        public static string[] LocalAddresses()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address))
                    .Select(a => a.Address.ToString())
                    .Distinct()
                    .ToArray();
            }
            catch (Exception)
            {
                return new[] { "?" };
            }
        }

        private async Task RunDemoAsync(CancellationToken token)
        {
            Log("Mode démonstration : une course fictive est envoyée (aucun jeu lu).");
            var sim = new SecondMonitor.Hub.Core.Demo.DemoRaceSimulator(seed: Environment.TickCount, raceDurationSeconds: 3600);
            double dt = Settings.Rates.TickMs / 1000.0;
            bool started = false;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var data = sim.Step(dt);
                    Interlocked.Increment(ref _sourceReads);
                    if (_server.PeerCount > 0)
                    {
                        if (!started)
                        {
                            _server.SendSessionStarted(data);
                            started = true;
                        }

                        _server.SendData(data);
                    }
                    else
                    {
                        started = false;
                    }

                    await Task.Delay(Settings.Rates.TickMs, token).ConfigureAwait(false);
                }
            }
            finally
            {
                Log("Démonstration arrêtée.");
            }
        }

        /// <summary>
        /// Rejoue un enregistrement du hub comme si le jeu tournait : le dernier état enregistré est renvoyé au rythme de lecture
        /// du jeu ; les moments où le jeu se taisait (signes de vie dans l'enregistrement) restent silencieux. Mesure du coût réel
        /// du collecteur sur une vraie course, sans le jeu.
        /// </summary>
        private async Task RunReplayAsync(CancellationToken token)
        {
            Log($"Rejeu de {System.IO.Path.GetFileName(ReplayFile)} (vitesse ×{ReplaySpeed:0.##}) : aucun jeu lu.");
            int tick = Settings.Rates.TickMs;
            while (!token.IsCancellationRequested)
            {
                var unpacker = new SecondMonitor.Remote.Adapter.DatagramPayloadUnPacker();
                SecondMonitor.DataModel.Snapshot.SimulatorDataSet current = null;
                DateTime clock = DateTime.MinValue;
                foreach (var (time, bytes) in SecondMonitor.Hub.Core.Network.CaptureReader.Read(ReplayFile))
                {
                    var payload = ProtoBuf.Serializer.Deserialize<SecondMonitor.Remote.Models.DatagramPayload>(new System.IO.MemoryStream(bytes));
                    var data = unpacker.UnpackDatagramPayload(payload);
                    if (clock == DateTime.MinValue)
                    {
                        clock = time;
                    }

                    // Jusqu'au paquet suivant : on renvoie l'état courant à chaque lecture du jeu.
                    while (clock < time && !token.IsCancellationRequested)
                    {
                        if (current != null && _server.PeerCount > 0)
                        {
                            Interlocked.Increment(ref _sourceReads);
                            _server.SendData(current);
                        }

                        await Task.Delay(Math.Max(1, (int)(tick / ReplaySpeed)), token).ConfigureAwait(false);
                        clock = clock.AddMilliseconds(tick);
                    }

                    switch (payload.PayloadKind)
                    {
                        case SecondMonitor.Remote.Models.DatagramPayloadKind.SessionStart:
                            current = data;
                            _server.SendSessionStarted(data);
                            break;
                        case SecondMonitor.Remote.Models.DatagramPayloadKind.Normal:
                            current = data;
                            break;
                        default:
                            current = null; // le jeu ne transmettait rien (menus, pause) : silence
                            break;
                    }
                }

                Log("Fin de l'enregistrement : rejeu depuis le début.");
            }
        }

        private static string Reason(string why) => why switch
        {
            "Timeout" => "plus de réponse",
            "RemoteConnectionClose" => "SecondMonitor fermé",
            "DisconnectPeerCalled" => "fermé",
            _ => why,
        };
    }
}
