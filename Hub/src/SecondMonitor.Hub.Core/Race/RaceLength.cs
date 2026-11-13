namespace SecondMonitor.Hub.Core.Race
{
    using System;

    /// <summary>
    /// Temps restant et tours restants, pour le bloc « Fin de session » de la barre du haut.
    /// Course au temps : estimation à partir du rythme réel et de la règle de fin
    /// (quand le temps est écoulé, le leader termine son tour en cours, puis chacun reçoit le drapeau à son passage suivant).
    /// </summary>
    public sealed class RaceLengthDto
    {
        /// <summary>« laps » (course aux tours), « time » (au temps), « time+1 » (au temps + un tour), null si inconnu.</summary>
        public string Mode { get; set; }

        public double? TimeRemaining { get; set; }

        /// <summary>Durée totale de la session (temps écoulé + restant), si connue.</summary>
        public double? TimeTotal { get; set; }

        /// <summary>Tour en cours du leader (1 = premier tour) et nombre total de tours qu'il fera (exact ou estimé).</summary>
        public int? LeaderLap { get; set; }

        public int? LeaderTotalLaps { get; set; }

        public double? LeaderLapsRemaining { get; set; }

        /// <summary>Tour en cours du joueur et nombre total de tours qu'il fera (s'il est doublé, moins que le leader).</summary>
        public int? PlayerLap { get; set; }

        public int? PlayerTotalLaps { get; set; }

        public double? PlayerLapsRemaining { get; set; }

        /// <summary>Secondes avant que le joueur ne reçoive le drapeau à damier (estimation).</summary>
        public double? PlayerFinishIn { get; set; }

        /// <summary>Essais / qualification au temps : nombre de tours que le joueur peut encore boucler à son rythme.</summary>
        public double? PlayerLapsPossible { get; set; }

        /// <summary>Avancement 0…1 (tours du joueur en course, sinon temps écoulé).</summary>
        public double Progress { get; set; }

        /// <summary>Vrai si les tours sont une estimation (course au temps) plutôt qu'une donnée du jeu.</summary>
        public bool Estimated { get; set; }

        public double? LeaderPace { get; set; }

        public double? PlayerPace { get; set; }

        /// <summary>Vrai si le leader a déjà reçu le drapeau à damier : chacun termine son tour en cours.</summary>
        public bool LeaderFinished { get; set; }
    }

    /// <summary>Données d'entrée du calcul (distances en tours : 12,4 = 12 tours bouclés + 40 % du suivant).</summary>
    public sealed class RaceLengthInput
    {
        public bool IsRace { get; set; }

        /// <summary>Valeurs de SessionLengthType : Laps, Time, TimeWithExtraLap, Na.</summary>
        public string LengthType { get; set; }

        public int TotalLaps { get; set; }

        public double? TimeRemaining { get; set; }

        public double SessionTime { get; set; }

        public double LeaderDistance { get; set; }

        public double? LeaderPace { get; set; }

        public bool LeaderFinished { get; set; }

        public double? PlayerDistance { get; set; }

        public double? PlayerPace { get; set; }

        public bool PlayerIsLeader { get; set; }

        public bool PlayerFinished { get; set; }
    }

    public static class RaceLength
    {
        private const double Eps = 1e-6;

        public static RaceLengthDto Estimate(RaceLengthInput i)
        {
            var r = new RaceLengthDto
            {
                TimeRemaining = i.TimeRemaining is > 0 ? i.TimeRemaining : (i.LengthType is "Time" or "TimeWithExtraLap" ? 0 : null),
                LeaderPace = Valid(i.LeaderPace),
                PlayerPace = Valid(i.PlayerPace) ?? Valid(i.LeaderPace),
                LeaderFinished = i.LeaderFinished,
            };
            bool timed = i.LengthType is "Time" or "TimeWithExtraLap";
            bool laps = i.LengthType == "Laps" && i.TotalLaps > 0;
            r.Mode = laps ? "laps" : i.LengthType == "TimeWithExtraLap" ? "time+1" : timed ? "time" : null;
            if (timed && i.TimeRemaining.HasValue)
            {
                r.TimeTotal = i.SessionTime + Math.Max(0, i.TimeRemaining.Value);
            }

            double dl = Math.Max(0, i.LeaderDistance);
            double? dp = i.PlayerDistance is double d ? Math.Max(0, d) : null;
            r.LeaderLap = (int)Math.Floor(dl) + 1;
            r.PlayerLap = dp.HasValue ? (int)Math.Floor(dp.Value) + 1 : null;

            if (!i.IsRace)
            {
                // Essais, qualification : le temps compte, les tours sont ceux qu'on peut encore boucler.
                if (r.TimeRemaining is double left && r.PlayerPace is double pace)
                {
                    r.PlayerLapsPossible = Math.Round(left / pace, 1);
                }

                r.Progress = r.TimeTotal is > 0 ? Math.Clamp(i.SessionTime / r.TimeTotal.Value, 0, 1) : 0;
                return r;
            }

            // ── Nombre de tours du leader et instant de son arrivée ──
            int? leaderTotal = null;
            double? leaderFinishIn = null;
            if (i.LeaderFinished)
            {
                leaderTotal = (int)Math.Floor(dl + Eps);
                leaderFinishIn = 0;
            }
            else if (laps)
            {
                leaderTotal = i.TotalLaps;
                leaderFinishIn = r.LeaderPace is double lp ? Math.Max(0, i.TotalLaps - dl) * lp : null;
            }
            else if (timed && r.LeaderPace is double lp)
            {
                double atZero = dl + (Math.Max(0, i.TimeRemaining ?? 0) / lp);
                int total = CeilLap(atZero);
                if (i.LengthType == "TimeWithExtraLap")
                {
                    total += 1;
                }

                leaderTotal = Math.Max(total, (int)Math.Floor(dl) + 1);
                leaderFinishIn = (leaderTotal.Value - dl) * lp;
            }

            if (leaderTotal is int lt)
            {
                r.LeaderTotalLaps = lt;
                r.LeaderLapsRemaining = Math.Max(0, lt - dl);
                r.LeaderLap = Math.Min(r.LeaderLap.Value, lt);
                r.Estimated = !laps && !i.LeaderFinished;
            }

            // ── Tours du joueur : il reçoit le drapeau à son premier passage après l'arrivée du leader ──
            if (dp is double p)
            {
                int? playerTotal = null;
                if (i.PlayerFinished)
                {
                    playerTotal = (int)Math.Floor(p + Eps);
                    r.PlayerFinishIn = 0;
                }
                else if (i.LeaderFinished)
                {
                    // Le leader a franchi la ligne : on termine le tour en cours.
                    playerTotal = (int)Math.Floor(p) + 1;
                    r.PlayerFinishIn = r.PlayerPace is double pace ? (playerTotal.Value - p) * pace : null;
                }
                else if (i.PlayerIsLeader && leaderTotal.HasValue)
                {
                    playerTotal = leaderTotal;
                    r.PlayerFinishIn = leaderFinishIn;
                }
                else if (leaderFinishIn is double lf && r.PlayerPace is double pp)
                {
                    playerTotal = CeilLap(p + (lf / pp));
                    if (leaderTotal is int cap)
                    {
                        playerTotal = Math.Min(playerTotal.Value, cap);
                    }

                    playerTotal = Math.Max(playerTotal.Value, (int)Math.Floor(p) + 1);
                    r.PlayerFinishIn = (playerTotal.Value - p) * pp;
                }
                else if (laps && leaderTotal is int lt2)
                {
                    // Rythme inconnu : on retire les tours de retard sur le leader.
                    playerTotal = Math.Max((int)Math.Floor(p) + 1, lt2 - (int)Math.Floor(Math.Max(0, dl - p)));
                }

                if (playerTotal is int pt)
                {
                    r.PlayerTotalLaps = pt;
                    r.PlayerLapsRemaining = Math.Round(Math.Max(0, pt - p), 2);
                    r.PlayerLap = Math.Min(r.PlayerLap.Value, Math.Max(1, pt));
                    r.Progress = pt > 0 ? Math.Clamp(p / pt, 0, 1) : 0;
                }
            }

            if (r.Progress == 0 && r.LeaderTotalLaps is int lt3 && lt3 > 0)
            {
                r.Progress = Math.Clamp(dl / lt3, 0, 1);
            }
            else if (r.Progress == 0 && r.TimeTotal is > 0)
            {
                r.Progress = Math.Clamp(i.SessionTime / r.TimeTotal.Value, 0, 1);
            }

            return r;
        }

        /// <summary>Numéro du passage sur la ligne qui suit (ou coïncide avec) la distance donnée.</summary>
        private static int CeilLap(double distance) => (int)Math.Ceiling(distance - Eps);

        private static double? Valid(double? pace) => pace is > 5 and < 3600 ? pace : null;
    }
}
