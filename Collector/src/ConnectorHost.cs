namespace SecondMonitor.Collector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using SecondMonitor.Foundation.Connectors;
    using SecondMonitor.PluginsConfiguration.Common.Controller;

    /// <summary>
    /// Cherche le jeu lancé, branche le bon connecteur SecondMonitor et transmet ses données au serveur de diffusion.
    /// Pour ménager le PC de jeu : rien n'est lu tant qu'aucun hub n'écoute, et la lecture s'arrête quand le hub part.
    /// </summary>
    internal sealed record PendingDecision(string Game, string Message, Action Action);

    internal sealed class ConnectorHost
    {
        private readonly List<(string Name, IRawTelemetryConnector Connector)> _connectors;
        private readonly BroadcastServer _server;
        private readonly CollectorSettings _settings;
        private IRawTelemetryConnector _active;
        private DateTime _noPeerSince = DateTime.MinValue;

        public ConnectorHost(IPluginSettingsProvider provider, BroadcastServer server, CollectorSettings settings, Action<string> log)
        {
            _server = server;
            _settings = settings;
            Log = log;
            _connectors = Create(provider, log)
                .Where(c => !settings.JeuxDesactives.Any(d => string.Equals(d, c.Name, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            foreach (var (name, connector) in _connectors)
            {
                if (connector is AbstractGameConnector game)
                {
                    game.TickTime = settings.Rates.TickMs;
                }

                // Abonnement dès le départ : certains jeux (Assetto Corsa, rFactor) demandent l'installation de leur plugin
                // AVANT d'avoir pu se connecter. Sans cela, la demande était perdue et le jeu n'était jamais détecté.
                string gameName = name;
                connector.DisplayMessage += (_, e) => OnMessage(gameName, e);
            }
        }

        /// <summary>Une action proposée par un jeu (installer son plugin), en attente d'un clic.</summary>
        public PendingDecision Decision { get; private set; }

        /// <summary>Windows a refusé l'installation : il faut relancer le collecteur en administrateur.</summary>
        public bool NeedsAdmin { get; private set; }

        /// <summary>Jeu lancé mais pas encore lisible (plugin absent, télémétrie désactivée…).</summary>
        public string WaitingGame { get; private set; }

        public DateTime WaitingSinceUtc { get; private set; }

        /// <summary>Dernières vraies données de course reçues du jeu (pas les signes de vie).</summary>
        public DateTime LastGameDataUtc { get; private set; } = DateTime.MinValue;

        public int LastDriverCount { get; private set; }

        public DateTime ActiveSinceUtc { get; private set; }

        /// <summary>Exécute l'action proposée par le jeu (installation du plugin).</summary>
        public void RunDecision()
        {
            PendingDecision d = Decision;
            if (d == null)
            {
                return;
            }

            Decision = null;
            NeedsAdmin = false;
            Log($"{d.Game} : installation du plugin…");
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    d.Action();
                }
                catch (Exception ex)
                {
                    OnInstallFailure(d.Game, ex.Message);
                }
            });
        }

        public void DismissDecision() => Decision = null;

        public Action<string> Log { get; }

        public IReadOnlyList<string> GameNames => _connectors.Select(c => c.Name).ToList();

        public string ActiveGame { get; private set; }

        public bool Paused { get; private set; } = true;

        public async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    Tick();
                }
                catch (Exception ex)
                {
                    Log($"Erreur : {ex.Message}");
                }

                // Recherche du jeu toutes les 3 s ; vérification de présence du hub toutes les secondes.
                await Task.Delay(_active == null ? 3000 : 1000, token).ConfigureAwait(false);
            }

            await StopActiveAsync();
        }

        private void Tick()
        {
            bool hubListening = _server.PeerCount > 0;
            if (hubListening)
            {
                _noPeerSince = DateTime.MinValue;
            }
            else if (_noPeerSince == DateTime.MinValue)
            {
                _noPeerSince = DateTime.UtcNow;
            }

            int pause = _settings.PauseSansHubSecondes;
            bool shouldPause = !hubListening && pause > 0 && (DateTime.UtcNow - _noPeerSince).TotalSeconds >= pause;
            Paused = shouldPause || (!hubListening && _active == null && pause > 0);

            if (_active != null)
            {
                if (shouldPause)
                {
                    Log($"Aucun hub depuis {pause} s : lecture de {ActiveGame} en pause (aucune charge sur le PC).");
                    _ = StopActiveAsync();
                }

                return;
            }

            if (Paused)
            {
                return;
            }

            string waiting = null;
            foreach (var (name, connector) in _connectors)
            {
                if (connector is AbstractGameConnector g && !g.IsProcessRunning())
                {
                    continue;
                }

                bool connected;
                try
                {
                    connected = connector.TryConnect();
                }
                catch (Exception ex)
                {
                    Log($"{name} : connexion impossible ({ex.Message})");
                    continue;
                }

                if (!connected)
                {
                    // Le jeu tourne (processus vu) mais ne se laisse pas lire : plugin absent ou désactivé, jeu encore au chargement.
                    // Les connecteurs qui ne savent pas voir le processus du jeu ne sont jamais signalés.
                    if (connector is AbstractGameConnector)
                    {
                        waiting ??= name;
                    }

                    continue;
                }

                _active = connector;
                ActiveGame = name;
                connector.DataLoaded += OnData;
                connector.SessionStarted += OnSession;
                connector.Disconnected += OnDisconnected;
                connector.StartConnectorLoop();
                WaitingGame = null;
                ActiveSinceUtc = DateTime.UtcNow;
                LastGameDataUtc = DateTime.MinValue;
                Log($"Jeu détecté : {name}. Les données partent vers SecondMonitor.");
                return;
            }

            if (waiting != WaitingGame)
            {
                WaitingGame = waiting;
                WaitingSinceUtc = DateTime.UtcNow;
                if (waiting != null)
                {
                    Log($"{waiting} est lancé, mais ses données ne sont pas encore lisibles.");
                }
            }
        }

        private async Task StopActiveAsync()
        {
            IRawTelemetryConnector c = _active;
            if (c == null)
            {
                return;
            }

            _active = null;
            c.DataLoaded -= OnData;
            c.SessionStarted -= OnSession;
            c.Disconnected -= OnDisconnected;
            try
            {
                await c.FinnishConnectorAsync();
            }
            catch (Exception)
            {
                // Connecteur déjà arrêté.
            }

            ActiveGame = null;
        }

        private void OnData(object sender, DataEventArgs e)
        {
            int drivers = e.Data?.DriversInfo?.Length ?? 0;
            if (drivers > 0)
            {
                LastGameDataUtc = DateTime.UtcNow;
                LastDriverCount = drivers;
            }

            _server.SendData(e.Data);
        }

        private void OnSession(object sender, DataEventArgs e)
        {
            Log($"Nouvelle session : {e.Data?.SessionInfo?.SessionType} · {e.Data?.SessionInfo?.TrackInfo?.TrackFullName}");
            _server.SendSessionStarted(e.Data);
        }

        private void OnDisconnected(object sender, EventArgs e)
        {
            Log($"{ActiveGame} fermé. En attente d'un jeu…");
            _ = StopActiveAsync();
        }

        private void OnMessage(string game, MessageArgs e)
        {
            string text = e.Message ?? string.Empty;
            if (e.IsDecision)
            {
                Decision = new PendingDecision(game, $"{game} a besoin du petit plugin SecondMonitor dans le dossier du jeu pour envoyer ses données. Installez-le, puis redémarrez le jeu.", e.Action);
                Log($"{game} : plugin SecondMonitor à installer (bouton dans la fenêtre).");
                if (AutoInstall)
                {
                    RunDecision();
                }

                return;
            }

            if (text.StartsWith("Operation Completed", StringComparison.OrdinalIgnoreCase))
            {
                Log($"{game} : plugin installé. Redémarrez le jeu.");
            }
            else if (text.StartsWith("Unable to install", StringComparison.OrdinalIgnoreCase))
            {
                OnInstallFailure(game, text);
            }
            else
            {
                Log($"{game} : {text}");
            }
        }

        /// <summary>Console (Proton, --sans-fenetre) : pas de bouton, on installe directement.</summary>
        public bool AutoInstall { get; set; }

        private void OnInstallFailure(string game, string detail)
        {
            bool denied = detail.Contains("denied", StringComparison.OrdinalIgnoreCase) || detail.Contains("refus", StringComparison.OrdinalIgnoreCase)
                || detail.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase);
            NeedsAdmin = denied && OperatingSystem.IsWindows();
            Log(NeedsAdmin
                ? $"{game} : Windows refuse d'écrire dans le dossier du jeu. Cliquez « Relancer en administrateur » une seule fois pour installer le plugin."
                : $"{game} : installation du plugin impossible ({detail}).");
        }


        /// <summary>Tous les jeux pris en charge par SecondMonitor. Un connecteur qui ne peut pas démarrer est simplement ignoré.</summary>
        private static IEnumerable<(string Name, IRawTelemetryConnector Connector)> Create(IPluginSettingsProvider p, Action<string> log)
        {
            var factories = new (string Name, Func<IRawTelemetryConnector> Make)[]
            {
                ("Le Mans Ultimate", () => new SecondMonitor.LmUConnector.LmUConnector()),
                ("Assetto Corsa Competizione", () => new SecondMonitor.AccConnector.AccConnector(p, new SecondMonitor.AccConnector.Repository.AccSettingsRepository())),
                ("rFactor 2", () => new SecondMonitor.RF2Connector.Rf2Connector()),
                ("Assetto Corsa", () => new SecondMonitor.AssettoCorsaConnector.AssettoCorsaConnector()),
                ("Automobilista 2", () => new SecondMonitor.AMS2Connector.Ams2Connector(p)),
                ("Project CARS 2", () => new SecondMonitor.PCars2Connector.PCars2Connector(p)),
                ("Project CARS", () => new SecondMonitor.PCarsConnector.PCarsConnector()),
                ("RaceRoom", () => new SecondMonitor.R3EConnector.R3EConnector()),
                ("rFactor / Automobilista 1", () => new SecondMonitor.RFactorConnector.RFConnector()),
                ("F1 24", () => new SecondMonitor.F12024Connector.F12024Connector(p)),
                ("F1 2021", () => new SecondMonitor.F12021Connector.F12021Connector(p)),
                ("F1 2020", () => new SecondMonitor.F12020Connector.F12020Connector(p)),
                ("F1 2019", () => new SecondMonitor.F12019Connector.F12019Connector(p)),
                ("Forza Motorsport", () => new SecondMonitor.ForzaMotorsportConnector.ForzaMotorsportConnector(p)),
            };

            foreach (var (name, make) in factories)
            {
                IRawTelemetryConnector c = null;
                try
                {
                    c = make();
                }
                catch (Exception ex)
                {
                    log($"{name} : indisponible sur ce système ({ex.GetType().Name}).");
                    NLog.LogManager.GetCurrentClassLogger().Warn(ex, $"Connecteur {name} indisponible");
                }

                if (c != null)
                {
                    yield return (name, c);
                }
            }
        }
    }
}
