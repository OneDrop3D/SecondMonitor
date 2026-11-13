namespace SecondMonitor.Hub.Core.Ai
{
    using System.Linq;
    using SecondMonitor.Hub.Core.Race;
    using static SecondMonitor.Hub.Core.Ai.RadioFormat;

    /// <summary>
    /// Radio d'arrivée : position finale dans la catégorie, places gagnées ou perdues, meilleur tour, écart au vainqueur,
    /// et un mot court selon le résultat. Une seule fois par course.
    /// </summary>
    public sealed class RaceFinishRadio
    {
        private bool _done;

        public void Reset() => _done = false;

        /// <summary>Message à dire maintenant (une fois, quand le joueur franchit l'arrivée), ou null.</summary>
        public string Check(RaceSnapshot snap)
        {
            DriverRow me = snap?.Drivers?.FirstOrDefault(d => d.IsPlayer);
            if (_done || snap?.HasData != true || snap.SessionType != "Race" || me == null || me.FinishStatus != "Finished")
            {
                return null;
            }

            _done = true;
            return Build(snap);
        }

        public static string Build(RaceSnapshot snap)
        {
            DriverRow me = snap.Drivers.FirstOrDefault(d => d.IsPlayer);
            if (me == null)
            {
                return null;
            }

            var cls = snap.Drivers.Where(d => d.ClassId == me.ClassId).OrderBy(d => d.ClassPosition).ToList();
            int pos = snap.IsMultiClass ? me.ClassPosition : me.Position;
            string where = snap.IsMultiClass ? $"P{pos} dans ta catégorie" : $"P{pos}";
            string moves = me.PositionsGained > 0 ? $", {me.PositionsGained} place{(me.PositionsGained > 1 ? "s" : string.Empty)} gagnée{(me.PositionsGained > 1 ? "s" : string.Empty)}"
                : me.PositionsGained < 0 ? $", {-me.PositionsGained} place{(me.PositionsGained < -1 ? "s" : string.Empty)} perdue{(me.PositionsGained < -1 ? "s" : string.Empty)}" : string.Empty;
            string text = $"Drapeau à damier ! {where}{moves}.";

            if (me.BestLap is > 0)
            {
                DriverRow fastest = cls.Where(d => d.BestLap > 0).OrderBy(d => d.BestLap).FirstOrDefault();
                string best = fastest?.Id == me.Id ? ", meilleur tour de la catégorie" : string.Empty;
                text += $" Meilleur tour {Lap(me.BestLap)}{(me.BestLapNumber.HasValue ? $" au tour {me.BestLapNumber}" : string.Empty)}{best}.";
            }

            if (pos == 1)
            {
                DriverRow second = cls.ElementAtOrDefault(1);
                if (second?.GapToPlayer is double g && g > 0 && second.GapToClassLeaderLaps == 0)
                {
                    text += $" {Sec(g)} d'avance sur le numéro {second.Number}.";
                }
            }
            else if (me.GapToClassLeaderLaps > 0)
            {
                text += $" À {me.GapToClassLeaderLaps} tour{(me.GapToClassLeaderLaps > 1 ? "s" : string.Empty)} du vainqueur.";
            }
            else if (me.GapToClassLeader is double gl && gl > 0)
            {
                text += $" À {Sec(gl)} du vainqueur.";
            }

            text += " " + (pos == 1 ? "Victoire, bravo !"
                : pos <= 3 ? "Beau podium, bravo !"
                : me.PositionsGained >= 2 ? "Belle remontée, bien joué."
                : me.PositionsGained <= -2 ? "Course difficile, on regardera ça ensemble."
                : "Bonne course, merci.");
            return text;
        }
    }
}
