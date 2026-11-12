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
            foreach (var (_, connector) in _connectors)
            {
                if (connector is AbstractGameConnector game)
                {
                    game.TickTime = settings.Rates.TickMs;
                }
            }
        }

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

            foreach (var (name, connector) in _connectors)
            {
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
                    continue;
                }

                _active = connector;
                ActiveGame = name;
                connector.DataLoaded += OnData;
                connector.SessionStarted += OnSession;
                connector.Disconnected += OnDisconnected;
                connector.DisplayMessage += OnMessage;
                connector.StartConnectorLoop();
                Log($"Jeu détecté : {name}. Les données partent vers le hub.");
                return;
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
            c.DisplayMessage -= OnMessage;
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

        private void OnData(object sender, DataEventArgs e) => _server.SendData(e.Data);

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

        private void OnMessage(object sender, MessageArgs e) => Log($"{ActiveGame} : {e.Message}");

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
