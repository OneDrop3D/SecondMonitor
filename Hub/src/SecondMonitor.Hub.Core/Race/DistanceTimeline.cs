namespace SecondMonitor.Hub.Core.Race
{
    using System.Collections.Generic;

    /// <summary>
    /// Historique (temps de session, distance totale parcourue) d'une voiture.
    /// Permet un calcul d'écart universel : « il y a combien de secondes la voiture A était-elle là où B est maintenant ? »,
    /// indépendant de ce que fournit (ou non) chaque jeu.
    /// </summary>
    public sealed class DistanceTimeline
    {
        private readonly List<(double Time, double Distance)> _samples = new();
        private readonly double _retentionSeconds;

        public DistanceTimeline(double retentionSeconds = 420)
        {
            _retentionSeconds = retentionSeconds;
        }

        public int Count => _samples.Count;

        public void Add(double time, double distance)
        {
            if (_samples.Count > 0)
            {
                var last = _samples[^1];
                if (time <= last.Time)
                {
                    return;
                }

                // Retour en arrière (rewind, téléportation au stand…) : on repart de zéro pour éviter des écarts absurdes.
                if (distance < last.Distance - 50)
                {
                    _samples.Clear();
                }
                else if (distance < last.Distance)
                {
                    distance = last.Distance;
                }
            }

            _samples.Add((time, distance));

            int removeCount = 0;
            while (removeCount < _samples.Count - 2 && _samples[removeCount].Time < time - _retentionSeconds)
            {
                removeCount++;
            }

            if (removeCount > 0)
            {
                _samples.RemoveRange(0, removeCount);
            }
        }

        public void Clear() => _samples.Clear();

        /// <summary>Instant (interpolé) où cette voiture a atteint la distance donnée ; null si hors de l'historique.</summary>
        public double? TimeAtDistance(double distance)
        {
            if (_samples.Count < 2 || distance < _samples[0].Distance || distance > _samples[^1].Distance)
            {
                return null;
            }

            int lo = 0;
            int hi = _samples.Count - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (_samples[mid].Distance < distance)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }

            var a = _samples[lo];
            var b = _samples[hi];
            double span = b.Distance - a.Distance;
            if (span <= 1e-6)
            {
                return a.Time;
            }

            return a.Time + ((distance - a.Distance) / span * (b.Time - a.Time));
        }
    }
}
