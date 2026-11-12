namespace SecondMonitor.Hub.Core.Race
{
    using System;

    /// <summary>
    /// Distance « déroulée » (tours × longueur + distance dans le tour) filtrée des incohérences ponctuelles des jeux
    /// (distance remise à zéro un paquet avant le compteur de tours, téléportation au stand…).
    /// Un saut isolé est ignoré ; trois sauts consécutifs = nouvelle référence (rewind, retour au garage).
    /// </summary>
    public sealed class LapProgress
    {
        private const double MaxSpeedMs = 140;
        private int _rejects;

        public bool HasPrevious { get; private set; }

        public double PreviousTime { get; private set; }

        public double PreviousUnwrapped { get; private set; }

        public double LapLength { get; private set; }

        /// <summary>Retourne vrai si l'échantillon est continu avec le précédent (on peut interpoler entre les deux).</summary>
        public bool Push(double time, int completedLaps, double lapDistance, double lapLength, out double unwrapped, out bool rebased)
        {
            rebased = false;
            unwrapped = (Math.Max(0, completedLaps) * lapLength) + Math.Clamp(lapDistance, 0, lapLength);
            if (lapLength <= 0)
            {
                return false;
            }

            if (!HasPrevious || Math.Abs(lapLength - LapLength) > 1)
            {
                Rebase(time, unwrapped, lapLength);
                rebased = true;
                return false;
            }

            double dt = time - PreviousTime;
            if (dt <= 0)
            {
                return false;
            }

            double du = unwrapped - PreviousUnwrapped;
            double limit = Math.Max(150, MaxSpeedMs * dt * 1.5);
            if (du < -5 || du > limit)
            {
                if (++_rejects >= 3)
                {
                    Rebase(time, unwrapped, lapLength);
                    rebased = true;
                }

                return false;
            }

            _rejects = 0;
            return true;
        }

        public void Commit(double time, double unwrapped)
        {
            PreviousTime = time;
            PreviousUnwrapped = Math.Max(PreviousUnwrapped, unwrapped);
        }

        private void Rebase(double time, double unwrapped, double lapLength)
        {
            HasPrevious = true;
            PreviousTime = time;
            PreviousUnwrapped = unwrapped;
            LapLength = lapLength;
            _rejects = 0;
        }
    }
}
