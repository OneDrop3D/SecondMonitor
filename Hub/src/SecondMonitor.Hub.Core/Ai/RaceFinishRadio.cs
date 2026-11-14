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

        /// <summary>
        /// Message à dire maintenant, une fois par session, ou null : en course quand le joueur franchit l'arrivée ; en qualification
        /// et en essais quand la session se termine (drapeau à damier), s'il a fait au moins un tour chronométré.
        /// </summary>
        public string Check(RaceSnapshot snap)
        {
            DriverRow me = snap?.Drivers?.FirstOrDefault(d => d.IsPlayer);
            if (_done || snap?.HasData != true || me == null)
            {
                return null;
            }

            if (snap.SessionType == "Race")
            {
                if (me.FinishStatus != "Finished")
                {
                    return null;
                }

                _done = true;
                return Build(snap);
            }

            if (snap.SessionType is "Qualification" or "Practice" or "WarmUp" && (snap.SessionPhase == "Checkered" || me.FinishStatus == "Finished") && me.BestLap is > 0)
            {
                _done = true;
                return BuildSession(snap);
            }

            return null;
        }

        /// <summary>
        /// Fin de qualification ou d'essais : « Fin de la qualification : P3 dans ta catégorie, meilleur tour 3:38.400 au tour 5,
        /// à 0,42 s de la pole. Beau tour, bravo ! »
        /// </summary>
        public static string BuildSession(RaceSnapshot snap)
        {
            DriverRow me = snap.Drivers.FirstOrDefault(d => d.IsPlayer);
            if (me?.BestLap is not > 0)
            {
                return null;
            }

            bool quali = snap.SessionType == "Qualification";
            var cls = snap.Drivers.Where(d => d.ClassId == me.ClassId && d.BestLap > 0).OrderBy(d => d.BestLap).ToList();
            int pos = cls.FindIndex(d => d.Id == me.Id) + 1;
            bool alone = cls.Count <= 1;
            string where = alone ? string.Empty : snap.IsMultiClass ? $"P{pos} dans ta catégorie, " : $"P{pos}, ";
            string text = $"Fin {(quali ? "de la qualification" : "des essais")} : {where}meilleur tour {Lap(me.BestLap)}{(me.BestLapNumber.HasValue ? $" au tour {me.BestLapNumber}" : string.Empty)}";
            if (!alone && pos > 1 && cls[0].BestLap is double pole && me.BestLap is double mine)
            {
                text += $", à {Sec(mine - pole)} {(quali ? "de la pole" : "du meilleur temps")}";
            }

            if (!quali && me.Laps > 0)
            {
                text += $", {me.Laps} tour{(me.Laps > 1 ? "s" : string.Empty)} bouclé{(me.Laps > 1 ? "s" : string.Empty)}";
            }

            text += ".";
            text += " " + (alone ? "Bon travail, merci."
                : pos == 1 ? (quali ? "Pole position, bravo !" : "Meilleur temps, bravo !")
                : pos <= 3 ? (quali ? "Belle qualification, bravo !" : "Bon rythme, bravo.")
                : quali ? "On partira de là, on va remonter." : "Bon travail, merci.");
            return text;
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
