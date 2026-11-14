namespace SecondMonitor.Collector
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.WebSockets;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Radio de l'ingénieur sur le PC de jeu : le collecteur se déclare « poste audio » auprès de SecondMonitor
    /// (WebSocket /ws?poste=1), reçoit la voix de l'ingénieur et l'ordre de la couper, et envoie ce que dit le pilote
    /// (POST /api/ai/listen). Le son lui-même (sortie choisie, micro choisi) passe par la vue web de la fenêtre.
    /// Rien ne tourne tant que ni la voix ni le micro de ce PC ne sont demandés.
    /// </summary>
    internal sealed class EngineerLink : IDisposable
    {
        /// <summary>Nom donné aux questions du pilote dans la radio et le carnet.</summary>
        public const string From = "pilote (PC de jeu)";

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(90) }; // transcription + réflexion

        private readonly CollectorRuntime _runtime;
        private readonly CollectorMusic _music;
        private readonly CancellationTokenSource _stop = new();
        private readonly SemaphoreSlim _wake = new(0);
        private volatile bool _dirty = true;
        private volatile bool _pageAttached;
        private volatile bool _micAvailable;
        private string _lastError;

        public EngineerLink(CollectorRuntime runtime)
        {
            _runtime = runtime;
            _music = new CollectorMusic(runtime.DataDirectory);
            // Menu des stands de LMU : chaque échange avec le jeu dans le journal (tronqué) et en entier dans un fichier du dossier du collecteur.
            LmuPitMenu.Log = runtime.Log;
            LmuPitMenu.DumpFile = Path.Combine(runtime.DataDirectory, "menu-stands-lmu.txt");
            _ = Task.Run(LoopAsync);
        }

        /// <summary>Message à transmettre à la page (voix à jouer, ordre de couper, réglage du hub…).</summary>
        public event Action<object> ToPage;

        /// <summary>Appui radio à traiter par le micro de ce PC (bouton du PC de jeu, ou ClickDeck via SecondMonitor).</summary>
        public event Action PressRequested;

        /// <summary>État de la liaison, pour la fenêtre.</summary>
        public string Status { get; private set; } = "Désactivée : la voix et le micro restent sur l'ordinateur de SecondMonitor.";

        /// <summary>Réglage « Voix de l'ingénieur sur » de SecondMonitor : « pc », « ecrans » ou « deux ».</summary>
        public string VoiceOutput { get; private set; } = "pc";

        /// <summary>SecondMonitor auquel on est relié (null si aucun).</summary>
        public string Host { get; private set; }

        private RadioIngenieur Settings => _runtime.Settings.RadioIngenieur;

        private bool WantsVoice => _pageAttached && Settings.Entendre;

        private bool WantsMic => _pageAttached && _micAvailable && Settings.Parler;

        /// <summary>Le pilote laisse SecondMonitor régler le menu des stands de Le Mans Ultimate depuis ce PC (réglage du collecteur).</summary>
        private bool WantsPit => _runtime.Settings.MenuStandsLmu;

        /// <summary>SecondMonitor peut faire jouer Deezer sur ce PC (case du collecteur, Windows seulement).</summary>
        private bool WantsMusic => _runtime.Settings.MusiqueDeezer && _music.Available;

        /// <summary>Deezer pilotable sur ce système.</summary>
        public bool MusicAvailable => _music.Available;

        /// <summary>
        /// La voix de l'ingénieur commence ou s'arrête dans le casque : SecondMonitor met la musique en pause puis la relance.
        /// </summary>
        public Task ReportVoiceAsync(bool playing) => WantsMusic ? SendAsync(_socket, new { type = "voice", playing }, _stop.Token) : Task.CompletedTask;

        private ClientWebSocket _socket;
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        /// <summary>La fenêtre est ouverte et dit ce que sa vue web sait faire (le micro exige une page « sécurisée »).</summary>
        public void AttachPage(bool micAvailable)
        {
            _pageAttached = true;
            _micAvailable = micAvailable;
            SettingsChanged();
        }

        public void DetachPage()
        {
            _pageAttached = false;
            SettingsChanged();
        }

        /// <summary>Réglage changé : on se déclare de nouveau (ou on se déconnecte).</summary>
        public void SettingsChanged()
        {
            _dirty = true;
            _wake.Release();
        }

        /// <summary>Appui sur le bouton radio du PC de jeu : vrai si le micro de ce PC s'en charge (sinon, l'écran de SecondMonitor).</summary>
        public bool TryHandlePress()
        {
            Action press = PressRequested;
            if (!WantsMic || press == null)
            {
                return false;
            }

            press();
            return true;
        }

        /// <summary>Envoie ce qu'a dit le pilote (WAV) à l'ingénieur. Sa réponse revient par la radio.</summary>
        public async Task<string> SendQuestionAsync(byte[] wav)
        {
            string host = Host ?? FirstHub();
            if (host == null)
            {
                return "aucun SecondMonitor connecté";
            }

            try
            {
                using var form = new MultipartFormDataContent();
                var audio = new ByteArrayContent(wav);
                audio.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                form.Add(audio, "audio", "question.wav");
                form.Add(new StringContent(From), "from");
                form.Add(new StringContent("true"), "speak");
                using HttpResponseMessage r = await Http.PostAsync($"{BaseUrl(host)}/api/ai/listen", form, _stop.Token).ConfigureAwait(false);
                string body = await r.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!r.IsSuccessStatusCode || body.Contains("\"error\"", StringComparison.Ordinal))
                {
                    string error = ErrorOf(body) ?? $"réponse {(int)r.StatusCode}";
                    _runtime.Log($"Radio : l'ingénieur n'a pas pu répondre ({error}).");
                    return error;
                }

                return null;
            }
            catch (Exception ex)
            {
                _runtime.Log($"Radio : SecondMonitor ({host}) injoignable ({ex.Message}).");
                return ex.Message;
            }
        }

        /// <summary>Le pilote coupe l'ingénieur : sa voix s'arrête partout et une réponse en préparation n'est pas dite.</summary>
        public Task StopSpeakingAsync() => PostAsync("/api/radio/stop", null);

        /// <summary>État du micro de ce PC (« idle », « recording », « thinking ») : affiché sur ClickDeck.</summary>
        public Task ReportPhaseAsync(string phase) =>
            PostAsync("/api/radio/state", new StringContent(JsonSerializer.Serialize(new { phase }), Encoding.UTF8, "application/json"));

        public void Dispose()
        {
            _stop.Cancel();
            _wake.Release();
        }

        /// <summary>Adresse d'un SecondMonitor connecté, utilisable dans une URL.</summary>
        internal static string UrlHost(string peer)
        {
            if (string.IsNullOrEmpty(peer))
            {
                return null;
            }

            if (IPAddress.TryParse(peer, out IPAddress ip))
            {
                if (ip.IsIPv4MappedToIPv6)
                {
                    ip = ip.MapToIPv4();
                }

                return ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? $"[{ip}]" : ip.ToString();
            }

            // « adresse:port » : on garde l'adresse.
            return peer.Count(c => c == ':') == 1 ? peer.Split(':')[0] : peer;
        }

        private static string ErrorOf(string body)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(body);
                return doc.RootElement.TryGetProperty("error", out JsonElement e) ? e.GetString() : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private string FirstHub() => UrlHost(_runtime.Hubs.FirstOrDefault());

        private string BaseUrl(string host) => $"http://{host}:{_runtime.Settings.PortSecondMonitor}";

        private async Task PostAsync(string path, HttpContent content)
        {
            string host = Host ?? FirstHub();
            if (host == null)
            {
                return;
            }

            try
            {
                using HttpResponseMessage r = await Http.PostAsync($"{BaseUrl(host)}{path}", content, _stop.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // SecondMonitor injoignable : la liaison le signale déjà.
            }
            finally
            {
                content?.Dispose();
            }
        }

        private async Task LoopAsync()
        {
            CancellationToken stop = _stop.Token;
            while (!stop.IsCancellationRequested)
            {
                string host = FirstHub();
                if (!WantsVoice && !WantsMic && !WantsPit && !WantsMusic)
                {
                    Status = _pageAttached && Settings.Parler && !_micAvailable
                        ? "Micro indisponible dans cette fenêtre : la radio reste sur l'ordinateur de SecondMonitor."
                        : "Désactivée : la voix et le micro restent sur l'ordinateur de SecondMonitor.";
                    await WaitAsync(TimeSpan.FromSeconds(5), stop).ConfigureAwait(false);
                    continue;
                }

                if (host == null)
                {
                    Status = "En attente de SecondMonitor (il se connecte tout seul au collecteur).";
                    await WaitAsync(TimeSpan.FromSeconds(2), stop).ConfigureAwait(false);
                    continue;
                }

                try
                {
                    await RunConnectionAsync(host, stop).ConfigureAwait(false);
                    _lastError = null;
                }
                catch (Exception ex) when (!stop.IsCancellationRequested)
                {
                    string message = ex is WebSocketException or HttpRequestException or OperationCanceledException or IOException ? ex.Message : ex.GetType().Name + " : " + ex.Message;
                    Status = $"SecondMonitor ({host}:{_runtime.Settings.PortSecondMonitor}) injoignable pour la radio : {message}. Nouvel essai dans quelques secondes.";
                    if (message != _lastError)
                    {
                        _lastError = message;
                        _runtime.Log($"Radio de l'ingénieur : {Status}");
                    }
                }
                catch (Exception)
                {
                    break; // arrêt du collecteur
                }
                finally
                {
                    Host = null;
                    _socket = null;
                }

                await WaitAsync(TimeSpan.FromSeconds(3), stop).ConfigureAwait(false);
            }
        }

        private async Task RunConnectionAsync(string host, CancellationToken stop)
        {
            using var socket = new ClientWebSocket();
            socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            socket.Options.KeepAliveTimeout = TimeSpan.FromSeconds(20); // SecondMonitor disparu sans prévenir : on le voit
            using (var connect = CancellationTokenSource.CreateLinkedTokenSource(stop))
            {
                connect.CancelAfter(TimeSpan.FromSeconds(5));
                await socket.ConnectAsync(new Uri($"ws://{host}:{_runtime.Settings.PortSecondMonitor}/ws?poste=1"), connect.Token).ConfigureAwait(false);
            }

            Host = host;
            _socket = socket;
            Status = WantsVoice || WantsMic ? $"Reliée à SecondMonitor ({host})." : $"Reliée à SecondMonitor ({host}) pour le menu des stands ; la voix et le micro restent sur l'ordinateur de SecondMonitor.";
            _runtime.Log($"Radio de l'ingénieur reliée à SecondMonitor ({host}).");
            _dirty = true;
            bool leaving = false;
            Task receive = ReceiveAsync(socket, stop);
            while (socket.State == WebSocketState.Open && !stop.IsCancellationRequested)
            {
                if (!WantsVoice && !WantsMic && !WantsPit && !WantsMusic || FirstHub() != host)
                {
                    // Plus rien à faire sur ce PC (ou autre SecondMonitor) : on se retire proprement.
                    leaving = true;
                    await SendStationAsync(socket, stop).ConfigureAwait(false);
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "fin", stop).ConfigureAwait(false);
                    _runtime.Log("Radio de l'ingénieur : liaison avec SecondMonitor fermée.");
                    break;
                }

                if (_dirty)
                {
                    _dirty = false;
                    await SendStationAsync(socket, stop).ConfigureAwait(false);
                }

                Task wake = _wake.WaitAsync(TimeSpan.FromSeconds(2), stop);
                if (await Task.WhenAny(receive, wake).ConfigureAwait(false) == receive)
                {
                    break;
                }
            }

            if (!leaving && !stop.IsCancellationRequested)
            {
                throw new WebSocketException("liaison coupée par SecondMonitor");
            }
        }

        private Task SendStationAsync(ClientWebSocket socket, CancellationToken stop)
        {
            string name = $"PC de jeu ({Environment.MachineName})";
            return SendAsync(socket, new { type = "station", voice = WantsVoice, mic = WantsMic, pit = WantsPit, music = WantsMusic, name }, stop);
        }

        /// <summary>Un seul envoi à la fois sur la liaison (le WebSocket ne supporte pas deux envois simultanés).</summary>
        private async Task SendAsync(ClientWebSocket socket, object message, CancellationToken stop)
        {
            if (socket == null || socket.State != WebSocketState.Open)
            {
                return;
            }

            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(message);
            await _sendLock.WaitAsync(stop).ConfigureAwait(false);
            try
            {
                await socket.SendAsync(payload, WebSocketMessageType.Text, true, stop).ConfigureAwait(false);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        /// <summary>
        /// Demande de SecondMonitor pour le menu des stands de Le Mans Ultimate : lecture seule (« test ») ou réglage.
        /// Le résultat repart vers SecondMonitor (« pitMenuResult »), qui le dit à la radio.
        /// </summary>
        private void HandlePitMenu(JsonElement data, bool test)
        {
            string id = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("id", out JsonElement i) ? i.ToString() : string.Empty;
            JsonElement order = data.ValueKind == JsonValueKind.Object ? data.Clone() : default;
            ClientWebSocket socket = _socket;
            _ = Task.Run(async () =>
            {
                bool ok;
                string text;
                string shown = null; // ce que la fenêtre affiche, quand c'est plus détaillé que la phrase radio
                if (!WantsPit)
                {
                    (ok, text) = (false, "Le collecteur n'autorise pas le réglage du menu des stands (case à cocher dans sa fenêtre).");
                    _runtime.Log($"Menu des stands LMU : demande de SecondMonitor refusée (case non cochée) : {(order.ValueKind == JsonValueKind.Undefined ? "(vide)" : order.GetRawText())}");
                }
                else if (_runtime.ActiveGame != "Le Mans Ultimate" && !test)
                {
                    (ok, text) = (false, "Le Mans Ultimate n'est pas lancé sur le PC de jeu.");
                    _runtime.Log($"Menu des stands LMU : demande de SecondMonitor ignorée, jeu lu : {_runtime.ActiveGame ?? "aucun"}.");
                }
                else if (test)
                {
                    // À la radio : la phrase courte ; dans la fenêtre et le journal : le détail.
                    LmuPitMenu.Inspection r = await LmuPitMenu.InspectAsync(_stop.Token, CarNow()).ConfigureAwait(false);
                    (ok, text, shown) = (r.Ok, r.Text, r.Detail);
                }
                else
                {
                    (ok, text) = await LmuPitMenu.ApplyAsync(order, _stop.Token, CarNow()).ConfigureAwait(false);
                }

                _runtime.Log($"Menu des stands LMU : {text}");
                LastPitMenu = shown ?? text;
                ToPage?.Invoke(new { type = "stand-resultat", ok, texte = shown ?? text });
                try
                {
                    await SendAsync(socket, new { type = "pitMenuResult", id, ok, test, text }, _stop.Token).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Liaison coupée entre-temps : le résultat reste dans le journal du collecteur.
                }
            });
        }

        /// <summary>Commande musique de SecondMonitor (Deezer de ce PC) : exécutée à part, la réponse repart sur la liaison.</summary>
        private void HandleMusic(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            JsonElement order = data.Clone();
            ClientWebSocket socket = _socket;
            _ = Task.Run(async () =>
            {
                object result = WantsMusic
                    ? await _music.HandleAsync(order, _stop.Token).ConfigureAwait(false)
                    : new { type = "musicResult", id = order.TryGetProperty("id", out JsonElement i) ? i.ToString() : string.Empty, ok = false, error = "musique non autorisée sur ce PC" };
                try
                {
                    await SendAsync(socket, result, _stop.Token).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Liaison coupée entre-temps.
                }
            });
        }

        /// <summary>Dernier résultat du menu des stands (affiché dans la fenêtre).</summary>
        public string LastPitMenu { get; private set; }

        /// <summary>
        /// Bouton « Tester l'API du menu des stands » de la fenêtre : lecture seule, sans passer par SecondMonitor.
        /// Rend le détail (lignes du menu et leurs choix, état du jeu, voiture) ; les réponses brutes sont dans le journal et le fichier.
        /// </summary>
        public async Task<(bool Ok, string Text)> TestPitMenuAsync()
        {
            LmuPitMenu.Inspection r = await LmuPitMenu.InspectAsync(_stop.Token, CarNow()).ConfigureAwait(false);
            LastPitMenu = r.Detail;
            _runtime.Log($"Menu des stands LMU (test) : {r.Text}");
            return (r.Ok, r.Detail);
        }

        /// <summary>La voiture du pilote d'après la télémétrie lue par le collecteur (Le Mans Ultimate seulement), ou null.</summary>
        private LmuPitMenu.CarNow CarNow()
        {
            if (_runtime.ActiveGame != "Le Mans Ultimate" || _runtime.Host.LastPlayer?.CarInfo is not { } car)
            {
                return null;
            }

            static string Clean(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            string front = Clean(car.WheelsInfo?.FrontLeft?.TyreType);
            string rear = Clean(car.WheelsInfo?.RearLeft?.TyreType) ?? front;
            double? fuel = car.FuelSystemInfo?.FuelRemaining is { InLiters: >= 0 } v ? v.InLiters : null;
            return front == null && fuel == null ? null : new LmuPitMenu.CarNow(front, rear, fuel);
        }

        /// <summary>Lit les messages de SecondMonitor jusqu'à la fermeture de la liaison.</summary>
        private async Task ReceiveAsync(ClientWebSocket socket, CancellationToken stop)
        {
            var buffer = new byte[64 * 1024];
            using var message = new MemoryStream();
            try
            {
                while (socket.State is WebSocketState.Open or WebSocketState.CloseSent)
                {
                    WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, stop).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }

                    if (message.Length + result.Count > 32 * 1024 * 1024)
                    {
                        message.SetLength(0); // message démesuré : ignoré
                        continue;
                    }

                    message.Write(buffer, 0, result.Count);
                    if (result.EndOfMessage)
                    {
                        Handle(message.ToArray());
                        message.SetLength(0);
                    }
                }
            }
            catch (Exception) when (!stop.IsCancellationRequested)
            {
                // Liaison coupée : la boucle principale le voit et se reconnecte.
            }
        }

        private void Handle(byte[] payload)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(payload);
                JsonElement root = doc.RootElement;
                string type = root.TryGetProperty("type", out JsonElement t) ? t.GetString() : null;
                root.TryGetProperty("data", out JsonElement data);
                switch (type)
                {
                    case "radio":
                        // Voix de l'ingénieur (les réponses du mode test restent sur l'écran qui a posé la question).
                        if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("audioBase64", out JsonElement audio) && audio.ValueKind == JsonValueKind.String
                            && !(data.TryGetProperty("test", out JsonElement test) && test.ValueKind == JsonValueKind.True))
                        {
                            string text = data.TryGetProperty("answer", out JsonElement a) ? a.GetString() : null;
                            ToPage?.Invoke(new { type = "radio-voix", audio = audio.GetString(), texte = text });
                        }

                        break;
                    case "radioStop":
                        ToPage?.Invoke(new { type = "radio-stop" });
                        break;
                    case "radioPress":
                        // Appui venu d'ailleurs (ClickDeck) : c'est le micro de ce PC qui écoute.
                        if (WantsMic)
                        {
                            PressRequested?.Invoke();
                        }

                        break;
                    case "pitMenu":
                        HandlePitMenu(data, test: false);
                        break;
                    case "pitMenuTest":
                        HandlePitMenu(data, test: true);
                        break;
                    case "music":
                        HandleMusic(data);
                        break;
                    case "stations":
                        if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("voiceOutput", out JsonElement v) && v.ValueKind == JsonValueKind.String)
                        {
                            VoiceOutput = v.GetString();
                            ToPage?.Invoke(new { type = "radio-hub", voixSur = VoiceOutput });
                        }

                        break;
                }
            }
            catch (JsonException)
            {
                // Message illisible : ignoré.
            }
        }

        private async Task WaitAsync(TimeSpan delay, CancellationToken stop)
        {
            try
            {
                await _wake.WaitAsync(delay, stop).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Arrêt du collecteur.
            }
        }
    }
}
