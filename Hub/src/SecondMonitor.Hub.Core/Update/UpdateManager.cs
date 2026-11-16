namespace SecondMonitor.Hub.Core.Update
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>Préférences de mise à jour (« mise-a-jour.json » dans le dossier de données de l'application).</summary>
    public sealed class UpdatePreferences
    {
        /// <summary>Vérifier tout seul (au démarrage puis toutes les <see cref="IntervalHours"/> heures).</summary>
        public bool Automatic { get; set; } = true;

        public int IntervalHours { get; set; } = 6;

        /// <summary>Version dont le bandeau a été fermé (« Plus tard ») : elle reste dans les Réglages, sans bandeau.</summary>
        public string Dismissed { get; set; }
    }

    /// <summary>
    /// Vérification, téléchargement et installation, partagés par SecondMonitor (le hub) et le collecteur.
    /// Rien de lourd : une petite requête au démarrage puis toutes les 6 h ; en mode automatique, la nouvelle version
    /// est téléchargée et installée toute seule, jamais pendant une session de jeu ; sinon sur demande.
    /// </summary>
    public sealed class UpdateManager : IDisposable
    {
        private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private readonly object _lock = new();
        private readonly string _prefsPath;
        private readonly Action<string> _log;
        private readonly HttpClient _http;
        private readonly CancellationTokenSource _stop = new();
        private Timer _timer;
        private Task _download;
        private string _file;

        public UpdateManager(string app, string currentVersion, string dataDirectory, Action<string> log)
        {
            App = app;
            Current = UpdateVersion.Normalize(currentVersion);
            ProgramDirectory = AppContext.BaseDirectory;
            _prefsPath = Path.Combine(dataDirectory, "mise-a-jour.json");
            _log = log ?? (_ => { });
            _http = UpdateManifest.CreateClient(Current);
            Preferences = LoadPreferences();
            PreviousFailure = UpdatePaths.TakePreviousFailure();
            if (PreviousFailure != null)
            {
                _log($"La dernière mise à jour automatique n'a pas abouti : {PreviousFailure}");
            }
        }

        /// <summary>« hub » ou « collecteur ».</summary>
        public string App { get; }

        public string Current { get; }

        public string ProgramDirectory { get; set; }

        public UpdatePreferences Preferences { get; private set; }

        /// <summary>Raison du dernier retour arrière (affichée une fois).</summary>
        public string PreviousFailure { get; private set; }

        public UpdateOffer Offer { get; private set; }

        /// <summary>« attente », « verification », « telechargement », « pret », « installation », « redemarrage », « manuel », « erreur ».</summary>
        public string State { get; private set; } = "attente";

        public long Done { get; private set; }

        public long Total { get; private set; }

        public string Error { get; private set; }

        public string Message { get; private set; }

        public DateTime? LastCheck { get; private set; }

        public string LastCheckError { get; private set; }

        /// <summary>Faux pendant une session de jeu : la vérification et l'installation automatiques attendent.</summary>
        public Func<bool> CanCheckNow { get; set; } = () => true;

        /// <summary>Après une installation qui demande de quitter : l'application se ferme.</summary>
        public event Action QuitRequested;

        public event Action Changed;

        /// <summary>Plateforme (clés du manifeste) de ce poste.</summary>
        public IReadOnlyList<string> Keys => UpdatePlatform.CurrentKeys(ProgramDirectory);

        /// <summary>Démarre la vérification automatique : au démarrage (après <paramref name="firstDelay"/>), puis à intervalle.</summary>
        public void Start(TimeSpan firstDelay)
        {
            UpdatePaths.MarkStarted(App, Current);
            _timer = new Timer(_ => _ = TickAsync(), null, firstDelay, Timeout.InfiniteTimeSpan);
        }

        private async Task TickAsync()
        {
            TimeSpan next = TimeSpan.FromHours(Math.Clamp(Preferences.IntervalHours, 1, 168));
            try
            {
                if (!Preferences.Automatic)
                {
                    return;
                }

                if (!CanCheckNow())
                {
                    next = TimeSpan.FromMinutes(10); // en course : on réessaie plus tard, sans rien envoyer
                    return;
                }

                UpdateOffer offer = await CheckAsync().ConfigureAwait(false);

                // Mode automatique : installée toute seule, jamais pendant une session de jeu (CanCheckNow), sauf version mise de côté.
                if (offer != null && offer.Version != Preferences.Dismissed)
                {
                    InstallInBackground(automatic: true);
                }
            }
            catch (Exception)
            {
                // Hors ligne : on réessaiera à la prochaine échéance.
            }
            finally
            {
                if (!_stop.IsCancellationRequested)
                {
                    try
                    {
                        _timer?.Change(next, Timeout.InfiniteTimeSpan);
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }
        }

        public async Task<UpdateOffer> CheckAsync()
        {
            lock (_lock)
            {
                if (State is "telechargement" or "installation" or "redemarrage")
                {
                    return Offer;
                }

                if (State != "pret")
                {
                    State = "verification";
                }
            }

            Notify();
            try
            {
                UpdateOffer offer = await UpdateManifest.CheckAsync(_http, Current, Keys, _stop.Token).ConfigureAwait(false);
                lock (_lock)
                {
                    LastCheck = DateTime.Now;
                    LastCheckError = null;
                    if (offer?.Version != Offer?.Version)
                    {
                        _file = null; // une autre version : l'ancien téléchargement ne vaut plus
                        if (State == "pret")
                        {
                            State = "attente";
                        }
                    }

                    Offer = offer;
                    if (State == "verification")
                    {
                        State = "attente";
                    }
                }

                if (offer != null)
                {
                    _log($"Mise à jour disponible : {offer.Version} (vous avez {Current}).");
                }

                return offer;
            }
            catch (Exception ex)
            {
                lock (_lock)
                {
                    LastCheck = DateTime.Now;
                    LastCheckError = ex is TaskCanceledException ? "le serveur des mises à jour ne répond pas" : ex.Message;
                    if (State == "verification")
                    {
                        State = "attente";
                    }
                }

                throw;
            }
            finally
            {
                Notify();
            }
        }

        /// <summary>
        /// Télécharge (si besoin) puis installe : « Installer » en un clic. Ne bloque pas.
        /// <paramref name="automatic"/> : lancé par la vérification automatique ; si une session de jeu a commencé pendant
        /// le téléchargement, on garde la version prête et on installera à une prochaine vérification hors session.
        /// </summary>
        public void InstallInBackground(bool automatic = false)
        {
            lock (_lock)
            {
                if (State is "telechargement" or "installation" or "redemarrage")
                {
                    return;
                }

                _download = Task.Run(async () =>
                {
                    if (await DownloadAsync().ConfigureAwait(false) && (!automatic || CanCheckNow()))
                    {
                        Install();
                    }
                });
            }
        }

        /// <summary>Télécharge et vérifie la version proposée. Vrai si elle est prête à installer.</summary>
        public async Task<bool> DownloadAsync()
        {
            UpdateFile file;
            lock (_lock)
            {
                if (State == "pret" && _file != null && File.Exists(_file))
                {
                    return true;
                }

                file = Offer?.File;
                if (file == null)
                {
                    Error = Offer == null ? "Aucune mise à jour à télécharger." : "Pas de fichier pour ce système dans cette version : voir la page de téléchargement.";
                    State = "erreur";
                    Notify();
                    return false;
                }

                State = "telechargement";
                Error = null;
                Done = 0;
                Total = file.Size;
            }

            Notify();
            try
            {
                string path = await UpdateManifest.DownloadAsync(_http, file, UpdatePaths.Downloads(UpdatePaths.WorkDirectory), (done, total) =>
                {
                    Done = done;
                    Total = total > 0 ? total : file.Size;
                    Notify();
                }, _stop.Token).ConfigureAwait(false);
                lock (_lock)
                {
                    _file = path;
                    State = "pret";
                }

                _log($"Mise à jour {Offer?.Version} téléchargée et vérifiée (SHA-256).");
                return true;
            }
            catch (Exception ex)
            {
                lock (_lock)
                {
                    State = "erreur";
                    Error = ex is TaskCanceledException ? "Le téléchargement s'est interrompu (connexion perdue)." : ex.Message;
                }

                _log($"Téléchargement de la mise à jour impossible : {Error}");
                return false;
            }
            finally
            {
                Notify();
            }
        }

        /// <summary>
        /// « Redémarrer et installer » : prépare la nouvelle version pendant que l'application tourne, lance le script détaché,
        /// puis demande la fermeture (<see cref="QuitRequested"/>). Sinon : installation à la main (fichier ouvert).
        /// </summary>
        public void Install()
        {
            string file;
            lock (_lock)
            {
                if (State != "pret" || _file == null)
                {
                    return;
                }

                State = "installation";
                file = _file;
            }

            Notify();
            try
            {
                var context = new InstallContext
                {
                    File = file,
                    Version = Offer.Version,
                    Current = Current,
                    App = App,
                    Exe = Environment.ProcessPath,
                    Args = Environment.GetCommandLineArgs()[1..],
                    Work = UpdatePaths.WorkDirectory,
                    Pid = Environment.ProcessId,
                };
                InstallPlan plan = UpdateInstaller.Prepare(context);
                if (plan.Script != null)
                {
                    UpdateInstaller.LaunchDetached(plan.Script, context.Work);
                    lock (_lock)
                    {
                        State = "redemarrage";
                    }

                    _log($"Installation de la version {Offer.Version} : l'application se ferme et se rouvre toute seule.");
                    Notify();
                    QuitRequested?.Invoke();
                    return;
                }

                UpdateInstaller.Open(plan.OpenPath);
                lock (_lock)
                {
                    State = "manuel";
                    Message = plan.Manual;
                }
            }
            catch (Exception ex)
            {
                // Rien n'a été remplacé : la version actuelle est intacte.
                lock (_lock)
                {
                    State = "pret";
                    Error = ex.Message;
                }

                _log($"Installation impossible : {ex.Message}");
            }

            Notify();
        }

        public void SetPreferences(bool automatic, int intervalHours)
        {
            Preferences.Automatic = automatic;
            Preferences.IntervalHours = Math.Clamp(intervalHours, 1, 168);
            SavePreferences();
            if (automatic)
            {
                try
                {
                    _timer?.Change(TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
                }
                catch (ObjectDisposedException)
                {
                }
            }

            Notify();
        }

        public void Dismiss(string version)
        {
            Preferences.Dismissed = version;
            SavePreferences();
            Notify();
        }

        /// <summary>La raison du dernier retour arrière a été montrée : on ne la répète pas.</summary>
        public void AcknowledgeFailure()
        {
            PreviousFailure = null;
            Notify();
        }

        /// <summary>État pour l'interface (sérialisé en JSON tel quel).</summary>
        public object Describe() => new
        {
            current = Current,
            platform = string.Join(", ", Keys),
            automatic = Preferences.Automatic,
            intervalHours = Preferences.IntervalHours,
            dismissed = Preferences.Dismissed,
            lastCheck = LastCheck?.ToString("yyyy-MM-ddTHH:mm:ss"),
            lastCheckError = LastCheckError,
            state = State,
            done = Done,
            total = Total,
            error = Error,
            message = Message,
            previousFailure = PreviousFailure,
            testMode = UpdateSource.TestMode,
            offer = Offer == null ? null : new
            {
                version = Offer.Version,
                date = Offer.Date,
                notes = Offer.Notes,
                page = Offer.Page,
                file = Offer.File == null ? null : new { name = Offer.File.Name, size = Offer.File.Size, key = Offer.File.Key },
            },
        };

        public void Dispose()
        {
            _stop.Cancel();
            _timer?.Dispose();
            _http.Dispose();
        }

        private void Notify()
        {
            try
            {
                Changed?.Invoke();
            }
            catch (Exception)
            {
                // Une interface fermée ne doit pas arrêter la mise à jour.
            }
        }

        private UpdatePreferences LoadPreferences()
        {
            try
            {
                if (File.Exists(_prefsPath))
                {
                    return JsonSerializer.Deserialize<UpdatePreferences>(File.ReadAllText(_prefsPath), Json) ?? new UpdatePreferences();
                }
            }
            catch (Exception)
            {
                // Fichier abîmé : valeurs par défaut.
            }

            return new UpdatePreferences();
        }

        private void SavePreferences()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_prefsPath)!);
                File.WriteAllText(_prefsPath, JsonSerializer.Serialize(Preferences, Json));
            }
            catch (Exception)
            {
                // Lecture seule : le réglage vaut jusqu'à la fermeture.
            }
        }
    }
}
