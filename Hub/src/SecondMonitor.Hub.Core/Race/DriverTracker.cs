namespace SecondMonitor.Hub.Core.Race
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.DataModel.Snapshot.Systems;

    /// <summary>Suivi dans le temps d'un pilote : tours, relais, arrêts au stand, gommes, rythme.</summary>
    public sealed class DriverTracker
    {
        private const double LapTimeConfirmWindow = 5.0;

        private int _lastCompletedLaps = -1;
        private double? _lapStartTime;
        private bool _wasInPits;
        private bool _pitSeenThisLap;
        private bool _pitExitThisLap;
        private double? _pitEntryTime;
        private LapRecord _pendingLap;
        private double _pendingSince;
        private TimeSpan _lastSeenLastLapTime = TimeSpan.Zero;
        private bool _lapValidSoFar = true;
        private bool _anyValidLapSeen;

        public DriverTracker(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public DriverInfo Latest { get; private set; }

        public DistanceTimeline Timeline { get; } = new();

        public List<LapRecord> Laps { get; } = new();

        public List<PitStopRecord> PitStops { get; } = new();

        public List<StintRecord> Stints { get; } = new();

        public StintRecord CurrentStint => Stints.Count == 0 ? null : Stints[^1];

        public int StartPosition { get; private set; }

        public double Distance { get; private set; }

        public string CurrentCompound { get; private set; }

        /// <summary>Micro-secteurs, traces de vitesse et trajectoires.</summary>
        public DriverLapAnalysis Analysis { get; } = new();

        /// <summary>
        /// Met à jour le suivi. Le PC de jeu n'envoie les adversaires qu'à intervalle (1 s par défaut) et le hub reçoit
        /// entre-temps le même objet : on l'ignore, sinon la voiture semblerait immobile puis téléportée (écarts faussés).
        /// </summary>
        public void Update(DriverInfo driver, double now, double lapLength, bool worldValid = true)
        {
            if (ReferenceEquals(driver, Latest))
            {
                return;
            }

            Latest = driver;
            if (StartPosition == 0 && driver.Position > 0)
            {
                StartPosition = driver.Position;
            }

            Distance = driver.TotalDistance > 0 ? driver.TotalDistance : Math.Max(0, (driver.CompletedLaps * lapLength) + driver.LapDistance);
            Timeline.Add(now, Distance);

            string compound = ReadCompound(driver.CarInfo);
            if (!string.IsNullOrWhiteSpace(compound))
            {
                CurrentCompound = compound;
            }

            if (Stints.Count == 0)
            {
                Stints.Add(new StintRecord { Index = 1, StartLap = driver.CompletedLaps, Compound = CurrentCompound });
            }
            else if (CurrentStint.Compound == null && CurrentCompound != null)
            {
                CurrentStint.Compound = CurrentCompound;
            }

            TrackPits(driver, now);
            TrackLaps(driver, now);
            TrackSectors(driver);

            bool hasPosition = worldValid && driver.WorldPosition != null && !(driver.WorldPosition.X.InMeters == 0 && driver.WorldPosition.Z.InMeters == 0);
            Analysis.Update(
                now,
                driver.CompletedLaps,
                driver.LapDistance,
                lapLength,
                driver.Speed?.InKph ?? 0,
                hasPosition ? driver.WorldPosition.X.InMeters : null,
                hasPosition ? driver.WorldPosition.Z.InMeters : null,
                driver.InPits);
        }

        /// <summary>Meilleur temps de chaque secteur (S1, S2, S3) sur la session, et le tour où il a été fait.</summary>
        public double?[] BestSectors { get; } = new double?[3];

        public int?[] BestSectorLaps { get; } = new int?[3];

        private void TrackSectors(DriverInfo driver)
        {
            var timing = driver.Timing;
            if (timing == null || driver.InPits)
            {
                return;
            }

            TimeSpan?[] sectors = { timing.LastSector1Time, timing.LastSector2Time, timing.LastSector3Time };
            for (int i = 0; i < 3; i++)
            {
                if (sectors[i] is not TimeSpan ts || ts <= TimeSpan.Zero)
                {
                    continue;
                }

                double v = Math.Round(ts.TotalSeconds, 3);
                if (BestSectors[i] == null || v < BestSectors[i] - 0.0005)
                {
                    BestSectors[i] = v;
                    // S1 et S2 appartiennent au tour en cours ; S3 se termine sur la ligne (tour qui vient d'être bouclé).
                    BestSectorLaps[i] = i < 2 ? driver.CompletedLaps + 1 : driver.CompletedLaps;
                }
            }
        }

        /// <summary>Régression linéaire des temps du relais en cours (pour la projection des prochains tours).</summary>
        public (double Slope, double Intercept, int Count)? CurrentStintFit()
        {
            StintRecord stint = CurrentStint;
            if (stint == null)
            {
                return null;
            }

            return Strategy.StrategyMath.LinearFit(Laps.Where(l => l.Lap > stint.StartLap && l.IsClean && l.Time.HasValue).Select(l => (l.Lap, l.Time.Value)).ToList());
        }

        public double? AverageOfLastCleanLaps(int count)
        {
            var clean = Laps.Where(l => l.IsClean && l.Time.HasValue).TakeLast(count).Select(l => l.Time.Value).ToList();
            return clean.Count == 0 ? null : clean.Average();
        }

        /// <summary>Pente (s/tour) des temps sur le relais en cours, hors tours de stand et tours anormaux.</summary>
        public double? CurrentStintDegradation()
        {
            StintRecord stint = CurrentStint;
            if (stint == null)
            {
                return null;
            }

            return Strategy.StrategyMath.DegradationSlope(Laps.Where(l => l.Lap > stint.StartLap && l.IsClean && l.Time.HasValue).Select(l => (l.Lap, l.Time.Value)).ToList());
        }

        private static string ReadCompound(CarInfo carInfo)
        {
            string type = carInfo?.WheelsInfo?.FrontLeft?.TyreType;
            if (string.IsNullOrWhiteSpace(type))
            {
                type = carInfo?.WheelsInfo?.RearLeft?.TyreType;
            }

            return string.IsNullOrWhiteSpace(type) ? null : type.Trim();
        }

        private void TrackPits(DriverInfo driver, double now)
        {
            if (driver.InPits && !_wasInPits)
            {
                _pitEntryTime = now;
                _pitSeenThisLap = true;
            }
            else if (!driver.InPits && _wasInPits)
            {
                // Sortie de stand → nouvel arrêt + nouveau relais. Pas comptabilisé avant le départ (garage, grille).
                _pitExitThisLap = true;
                double? duration = _pitEntryTime.HasValue ? now - _pitEntryTime.Value : null;
                if (driver.CompletedLaps > 0 || Laps.Count > 0)
                {
                    PitStops.Add(new PitStopRecord
                    {
                        Lap = driver.CompletedLaps,
                        EntryTime = _pitEntryTime ?? now,
                        Duration = duration,
                        CompoundBefore = CurrentStint?.Compound,
                    });

                    if (CurrentStint != null)
                    {
                        CurrentStint.EndLap = driver.CompletedLaps;
                    }

                    Stints.Add(new StintRecord { Index = Stints.Count + 1, StartLap = driver.CompletedLaps, Compound = CurrentCompound });
                }
                else if (CurrentStint != null)
                {
                    CurrentStint.StartLap = driver.CompletedLaps;
                    CurrentStint.Compound = CurrentCompound;
                }

                _pitEntryTime = null;
            }

            _wasInPits = driver.InPits;
        }

        private void TrackLaps(DriverInfo driver, double now)
        {
            int completed = driver.CompletedLaps;
            TimeSpan simLastLap = driver.Timing?.LastLapTime ?? TimeSpan.Zero;

            if (_lastCompletedLaps < 0)
            {
                _lastCompletedLaps = completed;
                _lapStartTime = null;
                _lastSeenLastLapTime = simLastLap;
                return;
            }

            if (completed > _lastCompletedLaps)
            {
                // Certains jeux ne renseignent pas la validité des tours (toujours « invalide ») : on ne s'y fie qu'après un tour valide.
                bool valid = _lapValidSoFar || !_anyValidLapSeen;
                _anyValidLapSeen |= _lapValidSoFar;
                bool countable = completed == _lastCompletedLaps + 1;
                double? measured = _lapStartTime.HasValue && countable ? now - _lapStartTime.Value : null;
                var record = new LapRecord
                {
                    Lap = completed,
                    Time = measured,
                    EndTime = now,
                    IsPitLap = _pitSeenThisLap || driver.InPits,
                    // Tour de sortie : celui pendant lequel la voiture quitte les stands. Pas forcément celui qui suit un tour « stand » :
                    // au Mans (sortie après la ligne), le tour commencé dans la voie des stands est déjà le tour de sortie.
                    IsOutLap = Laps.Count == 0 || _pitExitThisLap,
                    Valid = valid,
                    Compound = CurrentCompound,
                    Stint = CurrentStint?.Index,
                    TyreLaps = CurrentStint != null ? Math.Max(1, completed - CurrentStint.StartLap) : null,
                };

                Laps.Add(record);
                _pendingLap = record;
                _pendingSince = now;
                _lapStartTime = now;
                _pitSeenThisLap = driver.InPits;
                _pitExitThisLap = false;
                _lastCompletedLaps = completed;
                _lapValidSoFar = driver.CurrentLapValid;
            }
            else if (completed < _lastCompletedLaps)
            {
                // Retour en arrière (nouvelle session non détectée, rewind) : on recale.
                _lastCompletedLaps = completed;
                _lapStartTime = null;
            }
            else if (!driver.CurrentLapValid)
            {
                _lapValidSoFar = false;
            }

            // Le temps « officiel » du jeu arrive souvent quelques paquets après le passage de la ligne : on le préfère s'il est cohérent.
            if (_pendingLap != null)
            {
                if (simLastLap > TimeSpan.Zero && simLastLap != _lastSeenLastLapTime)
                {
                    double official = simLastLap.TotalSeconds;
                    if (!_pendingLap.Time.HasValue || Math.Abs(official - _pendingLap.Time.Value) < 3)
                    {
                        _pendingLap.Time = official;
                    }

                    _pendingLap = null;
                }
                else if (now - _pendingSince > LapTimeConfirmWindow)
                {
                    _pendingLap = null;
                }
            }

            _lastSeenLastLapTime = simLastLap;

            if (CurrentStint != null)
            {
                StintRecord stint = CurrentStint;
                stint.Laps = Math.Max(0, completed - stint.StartLap);
                var stintLaps = Laps.Where(l => l.Lap > stint.StartLap && l.IsClean && l.Time.HasValue).Select(l => l.Time.Value).ToList();
                stint.AverageLap = stintLaps.Count == 0 ? null : stintLaps.Average();
                stint.BestLap = stintLaps.Count == 0 ? null : stintLaps.Min();
            }
        }
    }

    public sealed class LapRecord
    {
        public int Lap { get; set; }

        public double? Time { get; set; }

        public double EndTime { get; set; }

        public bool IsPitLap { get; set; }

        public bool IsOutLap { get; set; }

        public bool Valid { get; set; }

        public string Compound { get; set; }

        /// <summary>Relais (1 = premier) dans lequel le tour a été fait.</summary>
        public int? Stint { get; set; }

        /// <summary>Âge des pneus en tours à la fin de ce tour (1 = premier tour du relais, tour de sortie compris).</summary>
        public int? TyreLaps { get; set; }

        public double? FuelUsed { get; set; }

        public double? EnergyUsedPct { get; set; }

        public double[] WearDeltaPct { get; set; }

        public bool IsClean => !IsPitLap && !IsOutLap && Valid;
    }

    public sealed class PitStopRecord
    {
        public int Lap { get; set; }

        public double EntryTime { get; set; }

        public double? Duration { get; set; }

        public string CompoundBefore { get; set; }
    }

    public sealed class StintRecord
    {
        public int Index { get; set; }

        public int StartLap { get; set; }

        public int? EndLap { get; set; }

        public int Laps { get; set; }

        public string Compound { get; set; }

        public double? AverageLap { get; set; }

        public double? BestLap { get; set; }
    }
}
