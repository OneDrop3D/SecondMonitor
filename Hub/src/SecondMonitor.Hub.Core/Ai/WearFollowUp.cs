namespace SecondMonitor.Hub.Core.Ai
{
    using System;
    using System.Linq;
    using SecondMonitor.Hub.Core.Race;
    using static SecondMonitor.Hub.Core.Ai.RadioFormat;

    /// <summary>
    /// L'ingénieur tient parole : après une usure anormale expliquée par le pilote, il refait le point deux tours plus tard
    /// (« Point usure arrière droit : 1,2 % par tour, c'est revenu à la normale. »). Une phrase par roue surveillée, une seule fois.
    /// </summary>
    public static class WearFollowUp
    {
        /// <summary>Tours à attendre avant de refaire le point.</summary>
        public const int Laps = 2;

        /// <summary>Phrase à dire maintenant (et la surveillance est close), ou null.</summary>
        public static string Check(RaceSnapshot snap, RaceMemory memory, DateTime nowUtc)
        {
            if (snap?.Player?.Wheels == null || memory == null)
            {
                return null;
            }

            int? laps = snap.Drivers.FirstOrDefault(d => d.IsPlayer)?.Laps;
            foreach (WearWatch watch in memory.WearWatches)
            {
                WheelStatus w = snap.Player.Wheels.FirstOrDefault(x => x.Position == watch.Wheel);
                if (w == null || laps == null || (nowUtc - watch.AtUtc).TotalMinutes > 30 || w.WearPct + 0.5 < watch.StartWearPct)
                {
                    memory.EndWearWatch(watch); // pneus changés, session finie ou trop vieux : plus rien à dire
                    continue;
                }

                int done = laps.Value - watch.Lap;
                if (done < Laps)
                {
                    continue;
                }

                memory.EndWearWatch(watch);
                double perLap = Math.Max(0, (w.WearPct - watch.StartWearPct) / done);
                string wheel = Wheel(watch.Wheel);
                if (watch.BaselinePerLap is double baseline && baseline > 0.05)
                {
                    if (perLap <= baseline * 1.15)
                    {
                        return $"Point usure {wheel} : {N(perLap, 1)} % par tour sur les {done} derniers tours, c'est revenu à la normale.";
                    }

                    string tip = watch.Proposal != null ? $" Essaie : {watch.Proposal}." : " Ménage-le en sortie de virage.";
                    return $"Point usure {wheel} : {N(perLap, 1)} % par tour, encore plus que ton rythme du relais ({N(baseline, 1)} %).{tip}";
                }

                return $"Point usure {wheel} : {N(perLap, 1)} % par tour sur les {done} derniers tours.";
            }

            return null;
        }
    }
}
