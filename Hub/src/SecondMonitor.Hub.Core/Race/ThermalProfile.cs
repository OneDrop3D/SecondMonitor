namespace SecondMonitor.Hub.Core.Race
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Plages de température personnalisées pour un jeu (et éventuellement une voiture précise) :
    /// en dessous du minimum = froid (bleu), dans la plage = bon (vert), au-dessus du maximum = trop chaud (rouge).
    /// Une valeur nulle = on garde celle fournie par le jeu.
    /// </summary>
    public sealed class ThermalProfile
    {
        /// <summary>Nom du jeu tel que transmis par SecondMonitor (ex. « LMU », « ACC ») ; vide = tous les jeux.</summary>
        public string Game { get; set; } = string.Empty;

        /// <summary>Texte contenu dans le nom de la voiture (ex. « 963 », « GT3 ») ; vide = toutes les voitures du jeu.</summary>
        public string Car { get; set; } = string.Empty;

        public double? TyreMin { get; set; }

        public double? TyreMax { get; set; }

        public double? CoreMin { get; set; }

        public double? CoreMax { get; set; }

        public double? BrakeMin { get; set; }

        public double? BrakeMax { get; set; }

        /// <summary>Pression idéale (kPa) ; affichée en bar.</summary>
        public double? PressureIdeal { get; set; }

        /// <summary>Profil le plus précis pour ce jeu et cette voiture (voiture précise &gt; jeu entier &gt; tous les jeux).</summary>
        public static ThermalProfile Find(IEnumerable<ThermalProfile> profiles, string game, string car)
        {
            if (profiles == null)
            {
                return null;
            }

            bool Matches(string pattern, string value) => string.IsNullOrWhiteSpace(pattern) || (value ?? string.Empty).Contains(pattern.Trim(), StringComparison.OrdinalIgnoreCase);
            return profiles
                .Where(p => Matches(p.Game, game) && Matches(p.Car, car))
                .OrderByDescending(p => (string.IsNullOrWhiteSpace(p.Car) ? 0 : 2) + (string.IsNullOrWhiteSpace(p.Game) ? 0 : 1))
                .FirstOrDefault();
        }
    }
}
