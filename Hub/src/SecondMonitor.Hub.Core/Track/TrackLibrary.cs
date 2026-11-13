namespace SecondMonitor.Hub.Core.Track
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;

    /// <summary>
    /// Tracés de circuits réels (bibliothèque libre, voir Track/Library/circuits.json et tools/circuits.py).
    /// Ils servent tant que le tracé n'a pas été appris dans le jeu (premier tour, ou jeu sans positions).
    /// Un tracé n'est retenu que si son nom ET sa longueur correspondent (± 8 %) : jamais le tracé complet pour une version courte.
    /// Départ et sens sont recalés en roulant par TrackCalibration ; un tracé de faible confiance n'est montré qu'une fois calé.
    /// </summary>
    public static class TrackLibrary
    {
        private static readonly Lazy<List<LibraryTrack>> Tracks = new(Load);
        private static readonly string[] VariantWords =
        {
            "short", "court", "national", "club", "indy", "west", "east", "oval", "nordschleife", "endurance", "historic", "sprint", "outer",
            "inner", "school", "paddock", "junior", "fosters", "island", "woodland", "bugatti", "classic", "200", "100", "kart", "karting", "drift",
            "rallycross", "legends", "north", "south", "moto", "chicane", "1966", "1967", "1971", "1975", "1976", "1977", "1988", "1991", "1993",
        };

        private static readonly string[] ReverseWords = { "reverse", "reversed", "inverse", "inverted", "rev" };

        public static int Count => Tracks.Value.Count;

        /// <summary>Le tracé correspondant au circuit annoncé par le jeu, ou null.</summary>
        public static LibraryTrack Find(string trackName, string layoutName, double lapLength)
        {
            string game = Normalize($"{trackName} {layoutName}");
            if (game.Trim().Length < 3)
            {
                return null;
            }

            bool reverse = ReverseWords.Any(w => Contains(game, w));
            LibraryTrack best = null;
            double bestScore = double.MinValue;
            foreach (LibraryTrack t in Tracks.Value)
            {
                double error = lapLength > 500 ? Math.Abs(t.Length - lapLength) / lapLength : 0;
                if (error > 0.08)
                {
                    continue; // autre configuration du circuit
                }

                string lib = Normalize($"{t.Name} {t.Location} {string.Join(' ', t.Aliases)}");
                if (VariantWords.Any(v => Contains(game, v) && !Contains(lib, v)))
                {
                    continue;
                }

                int match = new[] { t.Name, t.Location }.Concat(t.Aliases).Select(Normalize).Where(n => n.Length >= 3 && Contains(game, n)).Select(n => n.Length).DefaultIfEmpty(0).Max();
                if (match == 0)
                {
                    continue;
                }

                // Nom le plus précis d'abord, puis la longueur la plus proche (variantes d'un même circuit).
                double score = match - (200 * error);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = t;
                }
            }

            return best != null && reverse ? best.Reversed() : best;
        }

        private static bool Contains(string text, string word) => $" {text} ".Contains($" {word} ", StringComparison.Ordinal) || (word.Contains(' ') && text.Contains(word, StringComparison.Ordinal));

        private static string Normalize(string s)
        {
            string d = (s ?? string.Empty).Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in d)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
            }

            return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static List<LibraryTrack> Load()
        {
            try
            {
                using Stream stream = typeof(TrackLibrary).Assembly.GetManifestResourceStream("SecondMonitor.Hub.Core.Track.circuits.json");
                if (stream == null)
                {
                    return new List<LibraryTrack>();
                }

                return JsonSerializer.Deserialize<List<LibraryTrack>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<LibraryTrack>();
            }
            catch (Exception)
            {
                return new List<LibraryTrack>();
            }
        }
    }

    public sealed class LibraryTrack
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Location { get; set; }

        public List<string> Aliases { get; set; } = new();

        public double Length { get; set; }

        public string Source { get; set; }

        /// <summary>400 points [x est, z sud] en mètres, départ sur la ligne, sens de la course.</summary>
        public List<double[]> Points { get; set; } = new();

        /// <summary>Confiance (0-1) dans le départ et le sens à l'import ; sous DisplayConfidence, le tracé attend d'être calé.</summary>
        public double Confidence { get; set; } = 0.7;

        /// <summary>Comment départ et sens ont été obtenus (rapport d'import).</summary>
        public string Notes { get; set; }

        public const double DisplayConfidence = 0.6;

        /// <summary>Le même circuit parcouru en sens inverse (F1 25…) : départ inconnu, à caler en roulant.</summary>
        public LibraryTrack Reversed() => new()
        {
            Id = Id + "-inverse",
            Name = Name + " (sens inverse)",
            Location = Location,
            Aliases = Aliases,
            Length = Length,
            Source = Source,
            Points = Enumerable.Range(0, Points.Count).Select(i => Points[(Points.Count - i) % Points.Count]).ToList(),
            Confidence = Math.Min(Confidence, 0.3),
            Notes = "sens inverse : départ à caler",
        };
    }
}
