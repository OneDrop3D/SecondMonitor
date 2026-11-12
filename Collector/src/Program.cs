namespace SecondMonitor.Collector
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using NLog;

    /// <summary>
    /// SecondMonitor Collecteur — à lancer sur le PC de jeu (Windows, Linux/SteamOS ; Mac pour les consoles).
    /// Détecte le jeu, lit ses données avec les connecteurs de SecondMonitor et les envoie à SecondMonitor sur le réseau local.
    /// Une petite fenêtre d'état, aucun calcul : le strict minimum pour ne pas gêner le jeu.
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            string baseDir = AppContext.BaseDirectory;
            Environment.CurrentDirectory = baseDir; // les connecteurs cherchent leurs plugins dans « Connectors\… »
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            bool console = args.Any(a => a is "--sans-fenetre" or "--console" or "-h" or "--help" or "/?" or "--aide") || Arg(args, "--autostart") != null;
            if (console)
            {
                Native.EnsureConsole();
                Console.OutputEncoding = Encoding.UTF8;
            }

            if (args.Any(a => a is "-h" or "--help" or "/?" or "--aide"))
            {
                PrintHelp();
                return 0;
            }

            string autostart = Arg(args, "--autostart") ?? Arg(args, "--demarrage-auto");
            if (autostart != null)
            {
                Console.WriteLine(Autostart.Set(autostart is "on" or "oui" or "1" or "true"));
                return 0;
            }

            using var mutex = new Mutex(true, "SecondMonitorCollecteur", out bool first);
            if (!first)
            {
                Say(console, "SecondMonitor Collecteur", "Le collecteur tourne déjà sur cet ordinateur.");
                return 1;
            }

            string dataDir = CollectorSettings.DataDirectory(baseDir);
            CollectorSettings settings = CollectorSettings.LoadOrCreate(dataDir);
            if (Arg(args, "--qualite") is string q)
            {
                settings.Qualite = q;
            }

            if (int.TryParse(Arg(args, "--port"), out int port))
            {
                settings.Port = port;
            }

            ConfigureLogging(dataDir);
            LiteNetLib.NetDebug.Logger = new QuietNetLogger(); // pas de messages techniques du réseau
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

            using var runtime = new CollectorRuntime(settings, dataDir);
            if (console)
            {
                runtime.LogAdded += Console.WriteLine;
                PrintHeader(runtime);
            }

            if (!runtime.Start())
            {
                Say(console, "SecondMonitor Collecteur", runtime.LogLines().LastOrDefault() ?? "Démarrage impossible.");
                return 2;
            }

            runtime.SetDemo(args.Contains("--demo"));

            if (!console && CollectorWindow.Run(runtime, minimized: args.Contains("--reduit")))
            {
                // Fenêtre fermée : on arrête (et on libère le port avant une éventuelle relance).
                runtime.Dispose();
                return CollectorWindow.RestartRequested ? Restart(mutex) : 0;
            }

            // Console (option « --sans-fenetre », ou pas de vue web sur ce système) : Ctrl+C pour quitter.
            if (!console)
            {
                Native.EnsureConsole();
                runtime.LogAdded += Console.WriteLine;
                PrintHeader(runtime);
            }

            using var quit = new ManualResetEventSlim();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                quit.Set();
            };
            var title = new Timer(_ => ConsoleTitle(runtime), null, 2000, 2000);
            quit.Wait();
            title.Dispose();
            runtime.Log("Arrêt du collecteur.");
            return 0;
        }

        private static int Restart(Mutex mutex)
        {
            mutex.ReleaseMutex();
            try
            {
                Process.Start(new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false });
            }
            catch (Exception)
            {
                // Relance impossible : l'utilisateur relancera lui-même.
            }

            return 0;
        }

        private static void ConsoleTitle(CollectorRuntime runtime)
        {
            runtime.UpdateStats();
            string game = runtime.ActiveGame ?? (runtime.Paused ? "en pause" : "aucun jeu");
            string hub = runtime.Hubs.Length == 0 ? "pas de SecondMonitor" : $"{runtime.Hubs.Length} SecondMonitor";
            try
            {
                Console.Title = $"SecondMonitor Collecteur — {game} — {hub} — {runtime.PackagesPerSecond:0} envois/s, {runtime.KilobytesPerSecond:0.0} Ko/s";
            }
            catch (Exception)
            {
                // Terminal sans titre.
            }
        }

        private static void Say(bool console, string title, string text)
        {
            if (console || !CollectorWindow.Message(title, text))
            {
                Native.EnsureConsole();
                Console.WriteLine(text);
            }
        }

        private static void PrintHeader(CollectorRuntime runtime)
        {
            CollectorSettings settings = runtime.Settings;
            var (tick, _, player, others) = settings.Rates;
            Console.WriteLine();
            Console.WriteLine("  ┌──────────────────────────────────────────────────────────┐");
            Console.WriteLine("  │  SecondMonitor Collecteur — envoie le jeu à SecondMonitor │");
            Console.WriteLine("  └──────────────────────────────────────────────────────────┘");
            Console.WriteLine($"  Ce PC : {string.Join(", ", CollectorRuntime.LocalAddresses())}   ·   port {settings.Port}");
            Console.WriteLine($"  Qualité « {settings.Qualite} » : jeu lu {1000 / tick} fois/s, vous {1000 / player}×/s, adversaires {1000.0 / others:0.#}×/s");
            Console.WriteLine($"  Jeux surveillés : {string.Join(", ", runtime.GameNames)}");
            Console.WriteLine($"  Ctrl+C pour quitter. Réglages : {settings.FilePath}");
            Console.WriteLine();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("SecondMonitor Collecteur — lit le jeu et envoie les données à SecondMonitor.");
            Console.WriteLine();
            Console.WriteLine("  (sans option)            démarre le collecteur avec sa fenêtre");
            Console.WriteLine("  --sans-fenetre           sans fenêtre, journal dans la console (Proton, serveur)");
            Console.WriteLine("  --reduit                 démarre fenêtre réduite");
            Console.WriteLine("  --qualite econome|equilibre|precis   moins de ressources ↔ données plus fines");
            Console.WriteLine("  --port 52642             port réseau");
            Console.WriteLine("  --autostart on|off       lancer (ou non) le collecteur à l'ouverture de session");
            Console.WriteLine("  --demo                   envoie une course fictive (tester la liaison sans jeu)");
        }

        private static string Arg(string[] args, string name)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1].ToLowerInvariant() : null;
        }

        /// <summary>Journal des connecteurs : seulement avertissements et erreurs, dans un petit fichier qui ne grossit pas.</summary>
        private static void ConfigureLogging(string dataDir)
        {
            try
            {
                LogManager.Setup().LoadConfiguration(b =>
                    b.ForLogger().FilterMinLevel(LogLevel.Warn).WriteToFile(
                        Path.Combine(dataDir, "collecteur.log"),
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
