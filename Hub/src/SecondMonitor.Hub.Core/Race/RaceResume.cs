namespace SecondMonitor.Hub.Core.Race
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using SecondMonitor.Hub.Core.Ai;
    using SecondMonitor.Hub.Core.Strategy;

    /// <summary>
    /// Ce qu'il faut pour reprendre une course après un redémarrage de SecondMonitor : tours, arrêts et relais de chaque pilote,
    /// consommation du joueur tour par tour, usures brutales, mémoire de l'ingénieur et carnet en cours.
    /// Les traces lourdes (télémétrie, micro-secteurs, trace de distance, météo) ne sont pas gardées : elles se refont en roulant.
    /// </summary>
    public sealed class RaceResumeState
    {
        public const int CurrentVersion = 1;

        public int Version { get; set; } = CurrentVersion;

        /// <summary>Clé de session du moteur (jeu|circuit|type de session).</summary>
        public string SessionKey { get; set; }

        public string Game { get; set; }

        public string Track { get; set; }

        public string SessionType { get; set; }

        /// <summary>Temps de session du jeu au moment de la sauvegarde (s).</summary>
        public double SessionTime { get; set; }

        public DateTime SavedAtUtc { get; set; }

        public string PlayerId { get; set; }

        public string PlayerName { get; set; }

        /// <summary>Tours bouclés par le joueur (dans le jeu) au moment de la sauvegarde.</summary>
        public int PlayerLaps { get; set; }

        public double MaxLapDistance { get; set; }

        public List<DriverTrackerState> Drivers { get; set; } = new();

        public PlayerStrategyState Player { get; set; }

        public DrivingWatchState Driving { get; set; }

        public RaceMemoryState Memory { get; set; }

        /// <summary>Carnet de session en cours, continué après la reprise.</summary>
        public string CarnetId { get; set; }

        [JsonIgnore]
        public DriverTrackerState PlayerTracker => PlayerId == null ? null : Drivers?.FirstOrDefault(d => d?.Id == PlayerId);

        /// <summary>« 12 tours, 1 arrêt » : ce qui a été repris pour le joueur.</summary>
        public string Summary()
        {
            DriverTrackerState me = PlayerTracker;
            int laps = me?.Laps?.Count ?? 0;
            int stops = me?.PitStops?.Count ?? 0;
            string lapText = laps <= 1 ? $"{laps} tour" : $"{laps} tours";
            string stopText = stops == 0 ? "aucun arrêt" : stops == 1 ? "1 arrêt" : $"{stops} arrêts";
            return $"{lapText}, {stopText}";
        }
    }

    /// <summary>Fichier de reprise (&lt;dossier de données&gt;/reprise/session.json), écrit d'un coup (fichier temporaire puis renommage).</summary>
    public sealed class RaceResumeStore
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals, // NaN ou infini possibles dans les mesures
        };

        private readonly object _lock = new();

        public RaceResumeStore(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public bool Exists => File.Exists(Path);

        public void Save(RaceResumeState state)
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(state, Json); // hors du verrou : la copie appartient déjà à l'appelant
            lock (_lock)
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
                string tmp = Path + ".tmp";
                File.WriteAllBytes(tmp, bytes);
                File.Move(tmp, Path, overwrite: true);
            }
        }

        /// <summary>Sauvegarde lue, ou null (absente, illisible ou d'une autre version : elle est alors effacée).</summary>
        public RaceResumeState Load()
        {
            lock (_lock)
            {
                if (!File.Exists(Path))
                {
                    return null;
                }

                try
                {
                    RaceResumeState state = JsonSerializer.Deserialize<RaceResumeState>(File.ReadAllBytes(Path), Json);
                    if (state?.Version == RaceResumeState.CurrentVersion && !string.IsNullOrEmpty(state.SessionKey))
                    {
                        return state;
                    }
                }
                catch (Exception)
                {
                    // Fichier abîmé (coupure pendant l'écriture d'une ancienne version…) : ignoré.
                }

                DeleteInternal();
                return null;
            }
        }

        public void Delete()
        {
            lock (_lock)
            {
                DeleteInternal();
            }
        }

        private void DeleteInternal()
        {
            try
            {
                File.Delete(Path);
            }
            catch (Exception)
            {
                // Déjà absent ou disque en lecture seule : sans importance.
            }
        }
    }

    /// <summary>
    /// Reprise de course : propose la sauvegarde au moteur au lancement, sauvegarde l'état à chaque tour bouclé du joueur
    /// (et au moins toutes les 30 s), l'efface à chaque vraie nouvelle session ou quand le réglage est désactivé.
    /// </summary>
    public sealed class RaceResumeKeeper
    {
        /// <summary>Au-delà, la sauvegarde n'est plus reprise (une course de 24 h peut être mise en pause quelques heures).</summary>
        public static readonly TimeSpan MaxAge = TimeSpan.FromHours(12);

        public static readonly TimeSpan SaveInterval = TimeSpan.FromSeconds(30);

        private readonly RaceEngine _engine;
        private readonly RaceMemory _memory;
        private readonly RaceResumeStore _store;
        private readonly Func<bool> _enabled;
        private readonly object _lock = new();
        private string _savedKey;
        private int _savedLaps = -1;
        private DateTime _lastSaveUtc = DateTime.MinValue;
        private string _resumedCarnetId;
        private bool _clearedWhileDisabled;

        public RaceResumeKeeper(RaceEngine engine, RaceMemory memory, RaceResumeStore store, Func<bool> enabled)
        {
            _engine = engine;
            _memory = memory;
            _store = store;
            _enabled = enabled ?? (() => true);
            engine.SessionReset += OnSessionReset;
            engine.SessionResumed += OnResumed;
            engine.ResumeDeclined += reason => Declined?.Invoke(reason);
        }

        /// <summary>Carnet de session en cours (gardé dans la sauvegarde pour être continué après la reprise).</summary>
        public Func<string> CarnetId { get; set; }

        public RaceResumeStore Store => _store;

        /// <summary>Course reprise : l'état du moteur et la mémoire de l'ingénieur sont rechargés.</summary>
        public event Action<RaceResumeState> Resumed;

        /// <summary>Sauvegarde non reprise (autre session, trop ancienne…), avec la raison.</summary>
        public event Action<string> Declined;

        /// <summary>Au lancement : propose la sauvegarde au moteur, qui décidera au premier paquet du jeu.</summary>
        public RaceResumeState Start(DateTime nowUtc)
        {
            if (!_enabled())
            {
                _store.Delete();
                return null;
            }

            RaceResumeState state = _store.Load();
            if (state == null)
            {
                return null;
            }

            if (nowUtc - state.SavedAtUtc > MaxAge)
            {
                _store.Delete();
                Declined?.Invoke($"sauvegarde trop ancienne ({state.SavedAtUtc.ToLocalTime():dd/MM HH:mm})");
                return null;
            }

            _engine.OfferResume(state);
            return state;
        }

        /// <summary>Appelé régulièrement : sauvegarde si le joueur a bouclé un tour ou si la dernière date de 30 s.</summary>
        public bool Tick(DateTime nowUtc)
        {
            if (!_enabled())
            {
                if (!_clearedWhileDisabled)
                {
                    _clearedWhileDisabled = true;
                    _engine.CancelResume();
                    lock (_lock)
                    {
                        _store.Delete();
                        _savedKey = null;
                        _savedLaps = -1;
                    }
                }

                return false;
            }

            _clearedWhileDisabled = false;
            (string key, int laps)? progress = _engine.ResumeProgress();
            if (progress == null)
            {
                return false;
            }

            bool newLap;
            lock (_lock)
            {
                newLap = progress.Value.key != _savedKey || progress.Value.laps != _savedLaps;
                if (!newLap && nowUtc - _lastSaveUtc < SaveInterval)
                {
                    return false;
                }
            }

            return SaveNow(nowUtc);
        }

        /// <summary>Sauvegarde tout de suite (fermeture de SecondMonitor).</summary>
        public bool SaveNow(DateTime nowUtc)
        {
            if (!_enabled())
            {
                return false;
            }

            RaceResumeState state = _engine.ExportState(nowUtc, out long generation);
            if (state == null)
            {
                return false;
            }

            state.Memory = _memory.Export();
            state.CarnetId = CarnetId?.Invoke();
            lock (_lock)
            {
                // Session remise à zéro pendant la copie : cette sauvegarde ne vaut plus rien.
                if (generation != _engine.SessionGeneration)
                {
                    return false;
                }

                _store.Save(state);
                _savedKey = state.SessionKey;
                _savedLaps = state.PlayerLaps;
                _lastSaveUtc = nowUtc;
            }

            return true;
        }

        /// <summary>Carnet de la course reprise, donné une seule fois (au carnet de session qui le rouvre).</summary>
        public string TakeResumedCarnetId()
        {
            lock (_lock)
            {
                string id = _resumedCarnetId;
                _resumedCarnetId = null;
                return id;
            }
        }

        private void OnSessionReset()
        {
            // Vraie nouvelle session (ou « Nouvelle session ») : l'ancienne sauvegarde ne doit plus jamais être reprise.
            lock (_lock)
            {
                _store.Delete();
                _savedKey = null;
                _savedLaps = -1;
                _lastSaveUtc = DateTime.MinValue;
                _resumedCarnetId = null;
            }
        }

        private void OnResumed(RaceResumeState state)
        {
            _memory.Import(state.Memory);
            lock (_lock)
            {
                _resumedCarnetId = state.CarnetId;
                _savedKey = state.SessionKey;
                _savedLaps = state.PlayerLaps;
            }

            Resumed?.Invoke(state);
        }
    }
}
