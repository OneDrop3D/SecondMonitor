namespace SecondMonitor.Collector
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using NLog;

    /// <summary>
    /// SecondMonitor Collecteur — à lancer sur le PC de jeu (Windows, ou Linux/SteamOS).
    /// Détecte le jeu, lit ses données avec les connecteurs de SecondMonitor et les envoie au hub (Mac) sur le réseau local.
    /// Aucun écran, aucun calcul : le strict minimum pour ne pas gêner le jeu.
    /// </summary>
    public static class Program
    {
        private static readonly object ConsoleLock = new();

        public static async Task<int> Main(string[] args)
        {
            string baseDir = AppContext.BaseDirectory;
            Environment.CurrentDirectory = baseDir; // les connecteurs cherchent leurs plugins dans « Connectors\… »
            Console.OutputEncoding = Encoding.UTF8;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (args.Any(a => a is "-h" or "--help" or "/?" or "--aide"))
            {
                PrintHelp();
                return 0;
            }

            string autostart = Arg(args, "--autostart") ?? Arg(args, "--demarrage-auto");
            if (autostart != null)
            {
                bool on = autostart is "on" or "oui" or "1" or "true";
                Console.WriteLine(Autostart.Set(on));
                return 0;
            }

            using var mutex = new Mutex(true, "SecondMonitorCollecteur", out bool first);
            if (!first)
            {
                Console.WriteLine("Le collecteur tourne déjà sur ce PC.");
                Pause(args);
                return 1;
            }

            if (args.Contains("--reduit"))
            {
                Native.MinimizeConsole();
            }

            CollectorSettings settings = CollectorSettings.LoadOrCreate(baseDir);
            if (Arg(args, "--qualite") is string q)
            {
                settings.Qualite = q;
            }

            if (int.TryParse(Arg(args, "--port"), out int port))
            {
                settings.Port = port;
            }

            ConfigureLogging(baseDir);
            LiteNetLib.NetDebug.Logger = new QuietNetLogger(); // pas de messages techniques du réseau dans la fenêtre
            if (settings.PrioriteBasse)
            {
                try
                {
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
                }
                catch (Exception)
                {
                    // Sans droit pour changer la priorité : on continue.
                }
            }

            var provider = new CollectorSettingsProvider(settings);
            using var server = new BroadcastServer(provider);
            var host = new ConnectorHost(provider, server, settings, Log);

            PrintHeader(settings, host);
            if (!server.Start(settings.Port))
            {
                Log($"Impossible d'ouvrir le port {settings.Port} : SecondMonitor (plugin « Data Broadcast Server ») est peut-être déjà lancé. Fermez-le, ou changez le port dans collecteur.json.");
                Pause(args);
                return 2;
            }

            server.PeerConnected += ip => Log($"Hub connecté : {ip}");
            server.PeerDisconnected += (ip, why) => Log($"Hub déconnecté : {ip} ({Reason(why)})");
            Log("Prêt. En attente du hub (lancez « SecondMonitor Hub » sur le Mac)…");

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            Task status = StatusLoop(server, host, cts.Token);
            try
            {
                if (args.Contains("--demo"))
                {
                    await RunDemoAsync(server, settings, cts.Token);
                }
                else
                {
                    await host.RunAsync(cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }

            Log("Arrêt du collecteur.");
            await status.ContinueWith(_ => { });
            return 0;
        }

        /// <summary>Course fictive envoyée au hub : pour vérifier la liaison PC ↔ Mac sans lancer de jeu.</summary>
        private static async Task RunDemoAsync(BroadcastServer server, CollectorSettings settings, CancellationToken token)
        {
            Log("Mode démonstration : une course fictive est envoyée au hub (aucun jeu lu).");
            var sim = new SecondMonitor.Hub.Core.Demo.DemoRaceSimulator(seed: Environment.TickCount, raceDurationSeconds: 3600);
            double dt = settings.Rates.TickMs / 1000.0;
            bool started = false;
            while (!token.IsCancellationRequested)
            {
                var data = sim.Step(dt);
                if (server.PeerCount > 0)
                {
                    if (!started)
                    {
                        server.SendSessionStarted(data);
                        started = true;
                    }

                    server.SendData(data);
                }
                else
                {
                    started = false;
                }

                await Task.Delay(settings.Rates.TickMs, token).ConfigureAwait(false);
            }
        }

        private static void Log(string message)
        {
            lock (ConsoleLock)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss}  {message}");
            }
        }

        private static async Task StatusLoop(BroadcastServer server, ConnectorHost host, CancellationToken token)
        {
            long lastPackages = 0;
            long lastBytes = 0;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(2000, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                long p = server.PackagesSent;
                long b = server.BytesSent;
                double rate = (p - lastPackages) / 2.0;
                double kb = (b - lastBytes) / 2.0 / 1024;
                lastPackages = p;
                lastBytes = b;
                string game = host.ActiveGame ?? (host.Paused ? "en pause" : "aucun jeu");
                string hub = server.PeerCount == 0 ? "pas de hub" : $"{server.PeerCount} hub(s)";
                try
                {
                    Console.Title = $"SecondMonitor Collecteur — {game} — {hub} — {rate:0} envois/s, {kb:0.0} Ko/s";
                }
                catch (Exception)
                {
                    // Terminal sans titre.
                }
            }
        }

        private static void PrintHeader(CollectorSettings settings, ConnectorHost host)
        {
            var (tick, _, player, others) = settings.Rates;
            Console.WriteLine();
            Console.WriteLine("  ┌──────────────────────────────────────────────────────────┐");
            Console.WriteLine("  │  SecondMonitor Collecteur — envoie le jeu au hub (Mac)    │");
            Console.WriteLine("  └──────────────────────────────────────────────────────────┘");
            Console.WriteLine($"  Ce PC : {string.Join(", ", LocalAddresses())}   ·   port {settings.Port}");
            Console.WriteLine($"  Qualité « {settings.Qualite} » : jeu lu {1000 / tick} fois/s, vous {1000 / player}×/s, adversaires {1000.0 / others:0.#}×/s");
            Console.WriteLine($"  Jeux surveillés : {string.Join(", ", host.GameNames)}");
            Console.WriteLine("  Laissez cette fenêtre ouverte (ou réduite). Ctrl+C pour quitter. Réglages : collecteur.json");
            Console.WriteLine();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("SecondMonitor Collecteur — lit le jeu et envoie les données au SecondMonitor Hub.");
            Console.WriteLine();
            Console.WriteLine("  (sans option)            démarre le collecteur");
            Console.WriteLine("  --qualite econome|equilibre|precis   moins de ressources ↔ données plus fines");
            Console.WriteLine("  --port 52642             port réseau (celui du hub)");
            Console.WriteLine("  --reduit                 démarre fenêtre réduite");
            Console.WriteLine("  --autostart on|off       lancer (ou non) le collecteur à l'ouverture de session");
            Console.WriteLine("  --demo                   envoie une course fictive (tester la liaison avec le Mac sans jeu)");
        }

        private static string Arg(string[] args, string name)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1].ToLowerInvariant() : null;
        }

        private static void Pause(string[] args)
        {
            if (!args.Contains("--reduit") && !Console.IsInputRedirected)
            {
                Console.WriteLine("Appuyez sur Entrée pour fermer.");
                Console.ReadLine();
            }
        }

        private static string Reason(string why) => why switch
        {
            "Timeout" => "plus de réponse",
            "RemoteConnectionClose" => "hub fermé",
            "DisconnectPeerCalled" => "fermé",
            _ => why,
        };

        private static string[] LocalAddresses()
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

        /// <summary>Journal des connecteurs : seulement avertissements et erreurs, dans un petit fichier qui ne grossit pas.</summary>
        private static void ConfigureLogging(string baseDir)
        {
            try
            {
                LogManager.Setup().LoadConfiguration(b =>
                    b.ForLogger().FilterMinLevel(LogLevel.Warn).WriteToFile(
                        Path.Combine(baseDir, "collecteur.log"),
                        layout: "${longdate} ${level} ${logger:shortName=true} ${message} ${exception}",
                        archiveAboveSize: 1_000_000,
                        maxArchiveFiles: 1));
            }
            catch (Exception)
            {
                // Pas de journal possible (dossier en lecture seule) : sans importance.
            }
        }
    }

    /// <summary>LiteNetLib écrit ses avertissements techniques dans la console : on les envoie dans le journal à la place.</summary>
    internal sealed class QuietNetLogger : LiteNetLib.INetLogger
    {
        private static readonly Logger Logger = LogManager.GetLogger("Réseau");

        public void WriteNet(LiteNetLib.NetLogLevel level, string str, params object[] args)
        {
            if (level == LiteNetLib.NetLogLevel.Error && !str.Contains("Decoded data size", StringComparison.Ordinal))
            {
                Logger.Warn(str, args);
            }
        }
    }
}
