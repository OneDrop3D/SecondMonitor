namespace SecondMonitor.Hub.Core.Ai
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Mise en forme des chiffres pour la radio : courte à l'écran (« 2:01.803 », « 0,35 s », « 204 km/h »),
    /// et lue en toutes lettres par <see cref="FrenchSpeech"/> pour la voix.
    /// </summary>
    public static class RadioFormat
    {
        public static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

        public static string N(double v, int digits) => v.ToString("F" + digits, Fr);

        /// <summary>Temps au tour « 2:01.803 » (ou « 58.412 » sous la minute).</summary>
        public static string Lap(double? seconds, int decimals = 3)
        {
            if (seconds is not double s || s <= 0 || double.IsNaN(s))
            {
                return "inconnu";
            }

            int m = (int)(s / 60);
            double rest = Math.Round(s - (m * 60), decimals);
            if (rest >= 60)
            {
                m++;
                rest -= 60;
            }

            string sec = rest.ToString((m > 0 ? "00." : "0.") + new string('0', decimals), CultureInfo.InvariantCulture);
            return m > 0 ? $"{m}:{sec}" : sec;
        }

        /// <summary>Écart en secondes : « 0,35 s » sous la seconde, « 1,8 s » au-delà.</summary>
        public static string Sec(double seconds)
        {
            double a = Math.Abs(seconds);
            return a < 1 ? $"{N(a, 2)} s" : $"{N(a, 1)} s";
        }

        public static string Kph(double v) => $"{N(v, 0)} km/h";

        public static string Wheel(string position) => position switch
        {
            "FL" => "avant gauche",
            "FR" => "avant droit",
            "RL" => "arrière gauche",
            _ => "arrière droit",
        };

        public static string Duration(double seconds)
        {
            var t = TimeSpan.FromSeconds(Math.Max(0, seconds));
            if (t.TotalHours >= 1)
            {
                return $"{(int)t.TotalHours} h {t.Minutes:00}";
            }

            if (t.TotalMinutes >= 2)
            {
                return $"{(int)Math.Round(t.TotalMinutes)} min";
            }

            return t.TotalSeconds >= 60 ? $"1 min {t.Seconds:00}" : $"{(int)t.TotalSeconds} s";
        }

        /// <summary>Met une majuscule à la première lettre.</summary>
        public static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0], Fr) + s[1..];
    }
}
