namespace SecondMonitor.Hub.Core.Track
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SecondMonitor.Hub.Core.Race;

    /// <summary>
    /// Noms des virages et lignes droites des circuits connus, d'après la distance depuis la ligne.
    /// Le Mans : repères relevés sur le tracé appris dans Le Mans Ultimate (13 624 m). Une zone hors repère garde son numéro.
    /// </summary>
    public static class TrackPlaces
    {
        private const double LeMansLength = 13624;

        // Un virage prend le repère qui contient sa corde, une ligne droite celui qui contient son milieu
        // (sa vitesse maximale est tout au bout, déjà au freinage du virage suivant).
        private static readonly (string Kind, double From, double To, string Name)[] LeMans =
        {
            ("corner", 150, 430, "Courbe Dunlop"),
            ("corner", 430, 800, "Chicane Dunlop"),
            ("corner", 800, 1420, "Esses de la Forêt"),
            ("corner", 1420, 1800, "Tertre Rouge"),
            ("corner", 3500, 4200, "1re chicane"),
            ("corner", 5500, 6200, "2e chicane"),
            ("corner", 7200, 7800, "Mulsanne"),
            ("corner", 9300, 9850, "Indianapolis"),
            ("corner", 9850, 10300, "Arnage"),
            ("corner", 11100, 12600, "Virages Porsche"),
            ("corner", 13150, 13600, "Chicane Ford"),
            ("straight", 13450, 250, "Ligne droite des stands"),
            ("straight", 250, 520, "Dunlop"),
            ("straight", 520, 1050, "Pont Dunlop"),
            ("straight", 1050, 1520, "Esses de la Forêt"),
            ("straight", 1700, 7350, "Hunaudières"),
            ("straight", 7700, 9450, "Mulsanne – Indianapolis"),
            ("straight", 9700, 9950, "Indianapolis – Arnage"),
            ("straight", 10150, 11300, "Arnage – Porsche"),
            ("straight", 11300, 12350, "Virages Porsche"),
            ("straight", 12350, 13250, "Porsche – Ford"),
        };

        public static void Apply(IEnumerable<TrackSegment> segments, string trackName, double lapLength)
        {
            // Même nom, autre longueur : autre tracé (circuit Bugatti…), les repères n'y valent rien.
            if (trackName == null || !trackName.Contains("Sarthe", StringComparison.OrdinalIgnoreCase) || Math.Abs(lapLength - LeMansLength) > LeMansLength * 0.02)
            {
                return;
            }

            double scale = LeMansLength / lapLength;
            foreach (TrackSegment s in segments)
            {
                double at = (s.Kind == "corner" ? s.Apex : s.Start + (Mod(s.End - s.Start, lapLength) / 2)) * scale;
                s.Place = LeMans.FirstOrDefault(p => p.Kind == s.Kind && Within(Mod(at, LeMansLength), p.From, p.To)).Name;
            }
        }

        private static double Mod(double v, double m) => ((v % m) + m) % m;

        // From > To : le repère chevauche la ligne.
        private static bool Within(double d, double from, double to) => from <= to ? d >= from && d < to : d >= from || d < to;
    }
}
