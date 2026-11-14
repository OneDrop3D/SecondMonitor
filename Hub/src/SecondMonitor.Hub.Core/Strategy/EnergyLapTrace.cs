namespace SecondMonitor.Hub.Core.Strategy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SecondMonitor.Hub.Core.Race;

    /// <summary>
    /// Batterie, énergie virtuelle et essence relevées le long du tour, pour répondre PENDANT le tour au lieu d'attendre la ligne :
    /// où la batterie se vide et se recharge le plus, ce que le tour en cours consomme par rapport au précédent au même endroit,
    /// et une estimation de la fin du tour (ou de la consommation au tour dès le premier tour, par extrapolation).
    /// </summary>
    public sealed class EnergyLapTrace
    {
        /// <summary>Un relevé au plus tous les 20 m.</summary>
        private const double Step = 20;

        private Lap _current;
        private Lap _last;

        public void Reset()
        {
            _current = null;
            _last = null;
        }

        /// <summary>À chaque paquet du joueur. Valeurs absentes = null (voiture sans batterie, sans énergie virtuelle…).</summary>
        public void Update(int completedLaps, double lapDistance, double? battery, double? energy, double? fuel, bool inPits)
        {
            if (_current == null || completedLaps != _current.Number)
            {
                if (_current != null && completedLaps == _current.Number + 1 && _current.Complete)
                {
                    _last = _current;
                }

                _current = new Lap(completedLaps);
            }

            _current.Pit |= inPits;
            var s = new Sample(lapDistance, battery, energy, fuel);
            Sample prev = _current.Samples.Count > 0 ? _current.Samples[^1] : null;
            if (prev != null)
            {
                // Ravitaillement (l'essence ou l'énergie remonte) : ce tour ne sert pas de référence pour la consommation.
                _current.Refuelled |= (energy - prev.Energy > 1) == true || (fuel - prev.Fuel > 0.5) == true;
                if (lapDistance < prev.Distance + Step)
                {
                    if (lapDistance >= prev.Distance)
                    {
                        _current.Samples[^1] = s with { Distance = prev.Distance }; // même case : la dernière valeur, à la même distance
                    }

                    return; // recul (tête-à-queue) ou même case
                }
            }

            _current.Samples.Add(s);
        }

        /// <summary>État en direct, ou null avant le premier relevé. <paramref name="segments"/> : virages et lignes droites nommés.</summary>
        public LiveEnergyDto Build(double lapLength, IReadOnlyList<TrackSegment> segments)
        {
            if (_current == null || _current.Samples.Count == 0 || lapLength <= 0)
            {
                return null;
            }

            Sample now = _current.Samples[^1];
            double fraction = Math.Clamp(now.Distance / lapLength, 0, 1);
            bool startSeen = _current.Samples[0].Distance <= lapLength * 0.05;
            Lap reference = _last != null && !_last.Pit ? _last : null;
            var dto = new LiveEnergyDto { LapDistance = Math.Round(now.Distance), LapFraction = Math.Round(fraction, 3), Lap = _current.Number + 1 };
            dto.Battery = Quantity(s => s.Battery, now, startSeen, fraction, reference, extrapolate: false);
            dto.VirtualEnergy = Quantity(s => s.Energy, now, startSeen, fraction, reference?.Refuelled == true ? null : reference, extrapolate: true);
            dto.Fuel = Quantity(s => s.Fuel, now, startSeen, fraction, reference?.Refuelled == true ? null : reference, extrapolate: true);
            if (dto.Battery != null && reference != null && segments != null)
            {
                dto.Battery.Zones = Zones(reference, s => s.Battery, segments);
            }

            return dto;
        }

        private LiveQuantity Quantity(Func<Sample, double?> value, Sample now, bool startSeen, double fraction, Lap reference, bool extrapolate)
        {
            if (value(now) is not double v)
            {
                return null;
            }

            var q = new LiveQuantity { Now = Math.Round(v, 2) };
            if (startSeen && value(_current.Samples[0]) is double start)
            {
                q.LapStart = Math.Round(start, 2);
                q.ChangeSoFar = Math.Round(v - start, 2);
            }

            if (reference != null && value(reference.Samples[0]) is double refStart && value(reference.Samples[^1]) is double refEnd
                && At(reference, now.Distance, value) is double refHere)
            {
                q.LastLapChange = Math.Round(refEnd - refStart, 2);
                q.LastLapChangeAtSameDistance = Math.Round(refHere - refStart, 2);
                if (q.ChangeSoFar is double so)
                {
                    q.ProjectedLapChange = Math.Round(q.LastLapChange.Value + (so - q.LastLapChangeAtSameDistance.Value), 2);
                }
            }
            else if (extrapolate && q.ChangeSoFar is double so && fraction >= 0.3 && so < 0)
            {
                // Premier tour (ou dernier tour inutilisable) : ce qui est consommé depuis la ligne, ramené au tour entier.
                q.ProjectedLapChange = Math.Round(so / fraction, 2);
                q.Extrapolated = true;
            }

            return q;
        }

        /// <summary>Variation de la valeur sur chaque virage et ligne droite du tour de référence : la plus forte baisse et la plus forte hausse.</summary>
        private static List<ZoneChange> Zones(Lap lap, Func<Sample, double?> value, IReadOnlyList<TrackSegment> segments)
        {
            var changes = new List<ZoneChange>();
            foreach (TrackSegment seg in segments.Where(s => !string.IsNullOrWhiteSpace(s.Name) && s.End > s.Start))
            {
                if (At(lap, seg.Start, value) is double a && At(lap, seg.End, value) is double b)
                {
                    changes.Add(new ZoneChange { Name = seg.Name, Kind = seg.Kind, Change = Math.Round(b - a, 1) });
                }
            }

            var result = new List<ZoneChange>();
            if (changes.Where(c => c.Change <= -1).OrderBy(c => c.Change).FirstOrDefault() is ZoneChange spend)
            {
                result.Add(spend);
            }

            if (changes.Where(c => c.Change >= 1).OrderByDescending(c => c.Change).FirstOrDefault() is ZoneChange gain)
            {
                result.Add(gain);
            }

            return result;
        }

        /// <summary>Valeur interpolée à une distance du tour (null hors du relevé).</summary>
        private static double? At(Lap lap, double distance, Func<Sample, double?> value)
        {
            List<Sample> s = lap.Samples;
            if (s.Count == 0 || distance < s[0].Distance - Step || distance > s[^1].Distance + Step)
            {
                return null;
            }

            int i = s.FindLastIndex(x => x.Distance <= distance);
            if (i < 0)
            {
                return value(s[0]);
            }

            if (i == s.Count - 1)
            {
                return value(s[i]);
            }

            if (value(s[i]) is not double a || value(s[i + 1]) is not double b)
            {
                return value(s[i]);
            }

            double span = s[i + 1].Distance - s[i].Distance;
            return span <= 0 ? a : a + ((b - a) * (distance - s[i].Distance) / span);
        }

        private sealed record Sample(double Distance, double? Battery, double? Energy, double? Fuel);

        private sealed class Lap
        {
            public Lap(int number) => Number = number;

            public int Number { get; }

            public List<Sample> Samples { get; } = new();

            public bool Pit { get; set; }

            public bool Refuelled { get; set; }

            /// <summary>Tour relevé de la ligne à la ligne (pas pris en route).</summary>
            public bool Complete => Samples.Count > 10 && Samples[0].Distance < 300;
        }
    }

    /// <summary>Batterie, énergie virtuelle et essence en direct, comparées au tour précédent au même endroit.</summary>
    public sealed class LiveEnergyDto
    {
        /// <summary>Tour en cours (numéro affiché).</summary>
        public int Lap { get; set; }

        public double LapDistance { get; set; }

        public double LapFraction { get; set; }

        public LiveQuantity Battery { get; set; }

        public LiveQuantity VirtualEnergy { get; set; }

        public LiveQuantity Fuel { get; set; }
    }

    /// <summary>
    /// Une grandeur (%, ou litres pour l'essence). Les variations sont signées : négatif = dépensé, positif = récupéré ou ajouté.
    /// </summary>
    public sealed class LiveQuantity
    {
        public double Now { get; set; }

        public double? LapStart { get; set; }

        /// <summary>Depuis la ligne, sur ce tour.</summary>
        public double? ChangeSoFar { get; set; }

        /// <summary>Au même endroit du tour précédent.</summary>
        public double? LastLapChangeAtSameDistance { get; set; }

        /// <summary>Tour précédent entier.</summary>
        public double? LastLapChange { get; set; }

        /// <summary>Estimation pour ce tour entier : le tour précédent corrigé de l'écart actuel, ou une extrapolation.</summary>
        public double? ProjectedLapChange { get; set; }

        /// <summary>L'estimation vient d'une extrapolation (pas de tour précédent comparable).</summary>
        public bool Extrapolated { get; set; }

        /// <summary>Batterie : où elle a le plus baissé et le plus remonté au tour précédent.</summary>
        public List<ZoneChange> Zones { get; set; }
    }

    public sealed class ZoneChange
    {
        public string Name { get; set; }

        public string Kind { get; set; }

        public double Change { get; set; }
    }
}
