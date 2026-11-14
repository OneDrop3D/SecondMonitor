namespace SecondMonitor.Collector
{
    using System;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using SecondMonitor.Hub.Music;

    /// <summary>
    /// Musique Deezer sur le PC de jeu, commandée par SecondMonitor : la recherche et les décisions restent sur l'ordinateur
    /// de SecondMonitor ; ici, on ouvre la playlist dans l'application Deezer (ou le lecteur web) et on la commande par les
    /// contrôles multimédia de Windows, pour qu'elle joue dans le casque du pilote. Même code que SecondMonitor sous Windows.
    /// </summary>
    internal sealed class CollectorMusic
    {
        private readonly IMusicPlayer _player;
        private volatile bool _web;

        public CollectorMusic(string dataDirectory)
        {
            if (OperatingSystem.IsWindows())
            {
                _player = new WindowsMusicPlayer(dataDirectory, () => _web);
            }
        }

        /// <summary>Deezer pilotable sur ce système (Windows seulement pour l'instant).</summary>
        public bool Available => _player != null;

        /// <summary>Commande reçue de SecondMonitor ; la réponse (« musicResult ») donne l'état de Deezer.</summary>
        public async Task<object> HandleAsync(JsonElement data, CancellationToken ct)
        {
            string id = data.TryGetProperty("id", out JsonElement i) ? i.ToString() : string.Empty;
            if (_player == null)
            {
                return new { type = "musicResult", id, ok = false, error = "Musique Deezer disponible seulement sur Windows." };
            }

            string action = data.TryGetProperty("action", out JsonElement a) && a.ValueKind == JsonValueKind.String ? a.GetString() : "state";
            _web = data.TryGetProperty("web", out JsonElement w) && w.ValueKind == JsonValueKind.True;
            try
            {
                switch (action)
                {
                    case "open":
                        string link = data.TryGetProperty("link", out JsonElement l) && l.ValueKind == JsonValueKind.String ? l.GetString() : null;
                        // Seulement des liens Deezer (jamais une adresse quelconque envoyée par le réseau).
                        if (link == null || !(link.StartsWith("deezer://www.deezer.com/", StringComparison.Ordinal) || link.StartsWith("https://www.deezer.com/", StringComparison.Ordinal)))
                        {
                            return new { type = "musicResult", id, ok = false, error = "lien refusé" };
                        }

                        await _player.OpenAsync(link, ct).ConfigureAwait(false);
                        break;
                    case "play":
                    case "pause":
                    case "next":
                    case "previous":
                        PlayerState known = await _player.GetStateAsync(ct).ConfigureAwait(false);
                        TransportCommand command = action switch
                        {
                            "play" => TransportCommand.Play,
                            "pause" => TransportCommand.Pause,
                            "next" => TransportCommand.Next,
                            _ => TransportCommand.Previous,
                        };
                        await _player.SendAsync(command, known, ct).ConfigureAwait(false);
                        await Task.Delay(400, ct).ConfigureAwait(false);
                        break;
                    case "volume":
                        if (data.TryGetProperty("volume", out JsonElement v) && v.TryGetInt32(out int pct))
                        {
                            await _player.SetVolumeAsync(Math.Clamp(pct, 0, 100), ct).ConfigureAwait(false);
                        }

                        break;
                }

                PlayerState s = await _player.GetStateAsync(ct).ConfigureAwait(false);
                return new
                {
                    type = "musicResult",
                    id,
                    ok = true,
                    installed = _player.AppInstalled(),
                    running = _player.AppRunning(),
                    canSetVolume = _player.CanSetVolume,
                    state = new { playing = s.Playing, title = s.Title, artist = s.Artist, album = s.Album, source = s.Source, volume = s.Volume, via = s.Via },
                };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return new { type = "musicResult", id, ok = false, error = ex.Message };
            }
        }
    }
}
