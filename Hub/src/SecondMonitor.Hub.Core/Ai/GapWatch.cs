namespace SecondMonitor.Hub.Core.Ai
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using SecondMonitor.Hub.Core.Race;
    using static SecondMonitor.Hub.Core.Ai.RadioFormat;

    /// <summary>Annonce de l'écart demandée par le pilote (« annonce-moi l'écart à chaque fois qu'il baisse d'une seconde »).</summary>
    public sealed class GapWatch
    {
        /// <summary>« ahead » (voiture devant, par défaut) ou « behind ».</summary>
        public string Side { get; set; } = "ahead";

        /// <summary>Variation d'écart qui déclenche une annonce (s).</summary>
        public double Step { get; set; } = 1.0;

        public string TargetId { get; set; }

        public double? LastAnnounced { get; set; }

        /// <summary>« À chaque passage de la ligne » : une annonce par tour, avec ce qui a bougé sur le tour.</summary>
        public bool PerLap { get; set; }

        public int LastLap { get; set; } = -1;

        public DateTime LastAtUtc { get; set; }
    }

    /// <summary>Commande vocale et annonces de l'écart, voiture de la même catégorie devant ou derrière.</summary>
    public static class GapWatcher
    {
        /// <summary>Vrai : démarrer (avec le côté et le pas) ; faux : arrêter ; null : ce n'est pas cette commande.</summary>
        public static (bool Start, GapWatch Watch)? Parse(string text)
        {
            string t = RadioCommands.Normalize(text);
            if (!Regex.IsMatch(t, @"\becarts?\b"))
            {
                return null;
            }

            if (Regex.IsMatch(t, @"\b(arrete|arreter|stop|stoppe|coupe|plus besoin|ne m annonce plus|n annonce plus)\b"))
            {
                return (false, null);
            }

            if (!Regex.IsMatch(t, @"\b(annonce|annonces|annoncer|dis|donne|previens|tiens moi au courant)\b") || !Regex.IsMatch(t, @"\b(chaque|toutes les|tous les|des que|quand il|a chaque)\b"))
            {
                return null;
            }

            var watch = new GapWatch
            {
                Side = Regex.IsMatch(t, @"\b(derriere|qui suit|poursuivant)\b") ? "behind" : "ahead",
                PerLap = Regex.IsMatch(t, @"\b(passages?|chaque tour|tous les tours|a chaque tour|ligne|par tour|fin de (chaque )?tour)\b"),
            };
            Match n = Regex.Match(t, @"\b(\d+(?:[.,]\d+)?)\s*(seconde|secondes|s)\b");
            if (n.Success && double.TryParse(n.Groups[1].Value.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double step))
            {
                watch.Step = Math.Clamp(step, 0.3, 10);
            }
            else if (Regex.IsMatch(t, @"\bdemi seconde\b"))
            {
                watch.Step = 0.5;
            }
            else if (Regex.IsMatch(t, @"\b(deux) secondes\b"))
            {
                watch.Step = 2;
            }

            return (true, watch);
        }

        public static string Confirm(GapWatch w) => w.PerLap
            ? $"Copy, l'écart avec la voiture {(w.Side == "behind" ? "derrière" : "devant")} à chaque passage sur la ligne. Dis « arrête les écarts » pour couper."
            : $"Copy, je t'annonce l'écart avec la voiture {(w.Side == "behind" ? "derrière" : "devant")} à chaque {(Math.Abs(w.Step - 1) < 0.01 ? "seconde" : N(w.Step, 1) + " seconde")} gagnée ou perdue. Dis « arrête les écarts » pour couper.";

        /// <summary>Phrase à dire maintenant, ou null. Nouvelle voiture : on repart de son écart sans rien dire.</summary>
        public static string Check(GapWatch w, RaceSnapshot snap, DateTime nowUtc)
        {
            BattleDto b = snap?.Player?.Battles?.FirstOrDefault(x => x.Side == w?.Side);
            if (w == null || b?.Gap is not double gap)
            {
                return null;
            }

            if (w.PerLap)
            {
                return PerLap(w, b, gap, snap);
            }

            if (b.Id != w.TargetId || w.LastAnnounced == null)
            {
                w.TargetId = b.Id;
                w.LastAnnounced = gap;
                return null;
            }

            double change = gap - w.LastAnnounced.Value;
            if (Math.Abs(change) < w.Step || (nowUtc - w.LastAtUtc).TotalSeconds < 8)
            {
                return null;
            }

            w.LastAnnounced = gap;
            w.LastAtUtc = nowUtc;
            string trend = w.Side == "ahead"
                ? change < 0 ? "tu reprends" : "il s'éloigne"
                : change < 0 ? "il revient" : "tu creuses";
            return $"Numéro {b.Number} à {Sec(gap)}, {trend}.";
        }

        /// <summary>Une annonce à chaque tour bouclé : « Numéro 50 à 27,2 s, plus 1,1 sur le tour. »</summary>
        private static string PerLap(GapWatch w, BattleDto b, double gap, RaceSnapshot snap)
        {
            DriverRow me = snap.Drivers.FirstOrDefault(d => d.IsPlayer);
            if (me == null || me.InPits)
            {
                return null;
            }

            // Juste après la ligne (premiers 15 % du tour) et une seule fois par tour.
            if (me.Laps == w.LastLap || me.LapFraction > 0.15)
            {
                return null;
            }

            bool sameCar = b.Id == w.TargetId && w.LastAnnounced.HasValue;
            double? change = sameCar ? gap - w.LastAnnounced.Value : null;
            w.LastLap = me.Laps;
            w.TargetId = b.Id;
            w.LastAnnounced = gap;
            if (change is not double c)
            {
                return $"Numéro {b.Number} à {Sec(gap)}.";
            }

            string trend = Math.Abs(c) < 0.1 ? "stable"
                : w.Side == "ahead" ? (c < 0 ? $"tu reprends {Sec(-c)}" : $"il prend {Sec(c)}")
                : (c < 0 ? $"il reprend {Sec(-c)}" : $"tu prends {Sec(c)}");
            return $"Numéro {b.Number} à {Sec(gap)}, {trend}.";
        }
    }
}
