namespace SecondMonitor.Hub.Core.Ai
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Prépare un texte pour la synthèse vocale : les petits modèles de voix lisent mal les chiffres et les symboles,
    /// on écrit donc tout en toutes lettres (« 2:01.803 » → « deux minutes, une seconde huit », « 0,57 s » → « zéro virgule
    /// cinquante-sept seconde », « #52 » → « numéro cinquante-deux », « T24 » → « tour vingt-quatre », « % » → « pour cent »).
    /// Le texte affiché à l'écran garde les chiffres.
    /// </summary>
    public static class FrenchSpeech
    {
        private static readonly string[] Units =
        {
            "zéro", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf", "dix",
            "onze", "douze", "treize", "quatorze", "quinze", "seize", "dix-sept", "dix-huit", "dix-neuf",
        };

        private static readonly string[] Tens = { string.Empty, "dix", "vingt", "trente", "quarante", "cinquante", "soixante", "soixante", "quatre-vingt", "quatre-vingt" };

        private static readonly Regex Feminine = new(@"^\s*(seconde|secondes|minute|minutes|heure|heures|place|places|fois)\b", RegexOptions.Compiled);

        public static string ToSpeech(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            string t = text.Replace('−', '-').Replace(' ', ' ').Replace(' ', ' ');

            // Temps au tour « 2:01.803 » / « 1:58,4 ».
            t = Regex.Replace(t, @"\b(\d{1,2}):(\d{2})(?:[.,](\d{1,3}))?\b", m =>
            {
                int min = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                int sec = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                string s = $"{Number(min, true)} minute{(min > 1 ? "s" : string.Empty)}, {Number(sec, true)} seconde{(sec > 1 ? "s" : string.Empty)}";
                if (m.Groups[3].Success)
                {
                    s += " " + Number(int.Parse(m.Groups[3].Value[..1], CultureInfo.InvariantCulture), false);
                }

                return s;
            });

            // Abréviations de course avant les nombres.
            t = Regex.Replace(t, @"#\s?(\d+)", m => "numéro " + m.Groups[1].Value);
            t = Regex.Replace(t, @"\bT(\d+)\b", m => "tour " + m.Groups[1].Value);
            t = Regex.Replace(t, @"\bP(\d+)\b", m => "P " + m.Groups[1].Value);
            t = Regex.Replace(t, @"\bV(\d+)\b", m => "virage " + m.Groups[1].Value);
            t = Regex.Replace(t, @"\bLD(\d+)\b", m => "ligne droite " + m.Groups[1].Value);

            // Unités.
            t = Regex.Replace(t, @"(\d)\s?km/h", "$1 kilomètres heure");
            t = Regex.Replace(t, @"(\d)\s?s/(?:tour|t)\b", "$1 seconde par tour");
            t = Regex.Replace(t, @"(\d)\s?%/tour\b", "$1 pour cent par tour");
            t = Regex.Replace(t, @"(\d)\s?L/tour\b", "$1 litres par tour");
            t = Regex.Replace(t, @"(\d)\s?°C?", "$1 degrés");
            t = Regex.Replace(t, @"(\d)\s?%", "$1 pour cent");
            t = Regex.Replace(t, @"(\d)\s?L\b", "$1 litres");
            t = Regex.Replace(t, @"(\d)\s?s\b", "$1 secondes");
            t = Regex.Replace(t, @"(\d)\s?min\b", "$1 minutes");
            t = Regex.Replace(t, @"(\d)\s?kPa\b", "$1 kilopascals");

            // Nombres signés et décimaux (virgule ou point), puis entiers.
            t = Regex.Replace(t, @"(?<![\w-])([+-])(?=\d)", m => m.Value == "-" ? "moins " : "plus ");
            t = Regex.Replace(t, @"\b(\d+)[.,](\d+)\b", m => Number(long.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), false) + " virgule " + Decimals(m.Groups[2].Value));
            t = Regex.Replace(t, @"\b\d+\b", m =>
            {
                long n = long.Parse(m.Value, CultureInfo.InvariantCulture);
                bool feminine = n % 10 == 1 && n % 100 != 11 && Feminine.IsMatch(t[(m.Index + m.Length)..]);
                return Number(n, feminine);
            });

            return Regex.Replace(t, @"\s{2,}", " ").Trim();
        }

        /// <summary>Nombre entier en toutes lettres (orthographe traditionnelle, avec traits d'union).</summary>
        public static string Number(long n, bool feminine = false)
        {
            if (n < 0)
            {
                return "moins " + Number(-n, feminine);
            }

            if (n >= 1_000_000_000)
            {
                return n.ToString(CultureInfo.InvariantCulture);
            }

            if (n >= 1_000_000)
            {
                long millions = n / 1_000_000;
                string head = Number(millions) + (millions > 1 ? " millions" : " million");
                return n % 1_000_000 == 0 ? head : head + " " + Number(n % 1_000_000, feminine);
            }

            if (n >= 1000)
            {
                long thousands = n / 1000;
                string head = thousands == 1 ? "mille" : Number(thousands).Replace("cents", "cent") + " mille";
                return n % 1000 == 0 ? head : head + " " + Number(n % 1000, feminine);
            }

            if (n >= 100)
            {
                long hundreds = n / 100;
                long rest = n % 100;
                string head = hundreds == 1 ? "cent" : Units[hundreds] + " cent" + (rest == 0 ? "s" : string.Empty);
                return rest == 0 ? head : head + " " + Number(rest, feminine);
            }

            if (n < 20)
            {
                return n == 1 && feminine ? "une" : Units[n];
            }

            long ten = n / 10;
            long unit = n % 10;
            if (ten is 7 or 9)
            {
                // 70–79 : soixante-dix… ; 90–99 : quatre-vingt-dix…
                string tail = Units[10 + unit];
                return ten == 7 && unit == 1 ? "soixante et onze" : $"{Tens[ten]}-{tail}";
            }

            if (unit == 0)
            {
                return ten == 8 ? "quatre-vingts" : Tens[ten];
            }

            if (unit == 1 && ten != 8)
            {
                return $"{Tens[ten]} et {(feminine ? "une" : "un")}";
            }

            return $"{Tens[ten]}-{(unit == 1 && feminine ? "une" : Units[unit])}";
        }

        /// <summary>Partie décimale : « 05 » → « zéro cinq », « 57 » → « cinquante-sept », « 803 » → « huit cent trois ».</summary>
        private static string Decimals(string digits)
        {
            int leadingZeros = 0;
            while (leadingZeros < digits.Length - 1 && digits[leadingZeros] == '0')
            {
                leadingZeros++;
            }

            string zeros = string.Join(" ", new string('0', leadingZeros).ToCharArray().Select(_ => "zéro"));
            string rest = Number(long.Parse(digits[leadingZeros..], CultureInfo.InvariantCulture));
            return string.IsNullOrEmpty(zeros) ? rest : zeros + " " + rest;
        }
    }
}
