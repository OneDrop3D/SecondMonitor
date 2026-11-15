namespace SecondMonitor.Hub.Core.Race
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Drivers;

    /// <summary>Reprise de course après un redémarrage de SecondMonitor (voir <see cref="RaceResumeKeeper"/>).</summary>
    public sealed partial class RaceEngine
    {
        /// <summary>Recul toléré (s) du temps de session par rapport à la sauvegarde (dernier état envoyé par le collecteur).</summary>
        public const double ResumeTimeMargin = 10;

        /// <summary>Pilotes de la course reprise, rendus à leur suivi dès qu'ils réapparaissent dans les données.</summary>
        private readonly Dictionary<string, DriverTrackerState> _resumedDrivers = new();
        private RaceResumeState _resumeCandidate;
        private long _generation;

        /// <summary>Course reprise au premier paquet du jeu (pas de <see cref="SessionReset"/> dans ce cas).</summary>
        public event Action<RaceResumeState> SessionResumed;

        /// <summary>Sauvegarde proposée mais refusée (autre session, temps revenu en arrière…), avec la raison.</summary>
        public event Action<string> ResumeDeclined;

        /// <summary>Change à chaque remise à zéro : une sauvegarde copiée avant n'est plus écrite.</summary>
        public long SessionGeneration
        {
            get
            {
                lock (_lock)
                {
                    return _generation;
                }
            }
        }

        /// <summary>Une sauvegarde attend le premier paquet du jeu pour être reprise ou refusée.</summary>
        public bool ResumePending
        {
            get
            {
                lock (_lock)
                {
                    return _resumeCandidate != null;
                }
            }
        }

        /// <summary>Propose un état sauvegardé : il sera repris au premier paquet si c'est la même session du jeu.</summary>
        public void OfferResume(RaceResumeState state)
        {
            lock (_lock)
            {
                _resumeCandidate = state;
            }
        }

        public void CancelResume()
        {
            lock (_lock)
            {
                _resumeCandidate = null;
            }
        }

        /// <summary>Clé de session et tours bouclés du joueur (pour décider d'une sauvegarde sans rien copier), ou null.</summary>
        public (string Key, int PlayerLaps)? ResumeProgress()
        {
            lock (_lock)
            {
                if (_sessionKey == null || _resumeCandidate != null || _last == null)
                {
                    return null;
                }

                return (_sessionKey, PlayerOf(_last)?.CompletedLaps ?? 0);
            }
        }

        /// <summary>Copie de l'état de course à sauvegarder (sérialisée ensuite hors du verrou), ou null sans session en cours.</summary>
        public RaceResumeState ExportState(DateTime nowUtc, out long generation)
        {
            lock (_lock)
            {
                generation = _generation;
                if (_sessionKey == null || _resumeCandidate != null || _last?.SessionInfo == null || _trackers.Count == 0 && _resumedDrivers.Count == 0)
                {
                    return null;
                }

                DriverInfo player = PlayerOf(_last);
                var drivers = _trackers.Values.Select(t => t.Export()).ToList();
                drivers.AddRange(_resumedDrivers.Values.Where(d => !_trackers.ContainsKey(d.Id))); // pas encore revus depuis la reprise
                return new RaceResumeState
                {
                    SessionKey = _sessionKey,
                    Game = _last.Source,
                    Track = _last.SessionInfo.TrackInfo?.TrackFullName,
                    SessionType = _last.SessionInfo.SessionType.ToString(),
                    SessionTime = _lastSessionTime,
                    SavedAtUtc = nowUtc,
                    PlayerId = player != null ? DriverId(player) : null,
                    PlayerName = player?.DriverLongName,
                    PlayerLaps = player?.CompletedLaps ?? 0,
                    MaxLapDistance = _maxLapDistanceSeen,
                    Drivers = drivers,
                    Player = _player.Export(),
                    Driving = _driving.Export(),
                };
            }
        }

        /// <summary>Pourquoi la sauvegarde ne peut pas être reprise avec ces données, ou null si c'est la même session.</summary>
        public static string DeclineReason(RaceResumeState saved, SimulatorDataSet data)
        {
            if (saved == null || data?.SessionInfo == null)
            {
                return "pas de données";
            }

            if (saved.Version != RaceResumeState.CurrentVersion)
            {
                return "sauvegarde d'une autre version";
            }

            if (!string.Equals(saved.Game, data.Source, StringComparison.Ordinal))
            {
                return $"autre jeu ({data.Source})";
            }

            if (!string.Equals(saved.Track, data.SessionInfo.TrackInfo?.TrackFullName, StringComparison.Ordinal))
            {
                return $"autre circuit ({data.SessionInfo.TrackInfo?.TrackFullName})";
            }

            if (!string.Equals(saved.SessionType, data.SessionInfo.SessionType.ToString(), StringComparison.Ordinal))
            {
                return $"autre type de session ({data.SessionInfo.SessionType})";
            }

            double now = data.SessionInfo.SessionTime.TotalSeconds;
            if (now < saved.SessionTime - ResumeTimeMargin)
            {
                return $"temps de session revenu en arrière ({now:0} s au lieu de {saved.SessionTime:0} s) : nouvelle session";
            }

            string name = PlayerOf(data)?.DriverLongName;
            if (!string.IsNullOrWhiteSpace(saved.PlayerName) && !string.IsNullOrWhiteSpace(name) && !string.Equals(saved.PlayerName.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return $"autre pilote ({name})";
            }

            return null;
        }

        /// <summary>
        /// Premier paquet de course depuis le lancement, avec une sauvegarde en attente : reprise si c'est la même session,
        /// sinon la sauvegarde est oubliée (la remise à zéro habituelle suit). Appelé sous le verrou.
        /// </summary>
        private RaceResumeState TryResumeInternal(SimulatorDataSet data, string key, double now, out string declined)
        {
            RaceResumeState saved = _resumeCandidate;
            _resumeCandidate = null;
            declined = saved.SessionKey != key ? DeclineReason(saved, data) ?? "autre session" : DeclineReason(saved, data);
            if (declined != null)
            {
                return null;
            }

            ResetInternal();
            _sessionKey = key;
            _trackMap.SetTrack(data.Source, data.SessionInfo.TrackInfo?.TrackFullName ?? "inconnu");
            _lastSessionTime = now;
            _maxLapDistanceSeen = Math.Max(0, saved.MaxLapDistance);
            foreach (DriverTrackerState d in saved.Drivers ?? new List<DriverTrackerState>())
            {
                if (!string.IsNullOrEmpty(d?.Id))
                {
                    _resumedDrivers[d.Id] = d;
                }
            }

            _player.Import(saved.Player);
            _driving.Import(saved.Driving);
            return saved;
        }

        /// <summary>Suivi d'un pilote vu pour la première fois : celui de la course reprise s'il y en a un.</summary>
        private DriverTracker NewTracker(string id)
        {
            var tracker = new DriverTracker(id);
            if (_resumedDrivers.Remove(id, out DriverTrackerState saved))
            {
                tracker.Import(saved);
            }

            return tracker;
        }
    }
}
