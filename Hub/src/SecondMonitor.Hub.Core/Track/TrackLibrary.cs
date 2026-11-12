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
    /// </summary>
    public static class TrackLibrary
    {
        private static readonly Lazy<List<LibraryTrack>> Tracks = new(Load);
        private static readonly string[] VariantWords = { "short", "court", "national", "club", "indy", "west", "east", "oval", "nordschleife", "endurance", "historic" };

        public static int Count => Tracks.Value.Count;

        /// <summary>Le tracé correspondant au circuit annoncé par le jeu, ou null.</summary>
        public static LibraryTrack Find(string trackName, string layoutName, double lapLength)
        {
            string game = Normalize($"{trackName} {layoutName}");
            if (game.Trim().Length < 3)
            {
                return null;
            }

            LibraryTrack best = null;
            int bestScore = 0;
            foreach (LibraryTrack t in Tracks.Value)
            {
                if (lapLength > 500 && Math.Abs(t.Length - lapLength) / lapLength > 0.08)
                {
                    continue; // autre configuration du circuit
                }

                string lib = Normalize($"{t.Name} {t.Location} {string.Join(' ', t.Aliases)}");
                if (VariantWords.Any(v => Contains(game, v) && !Contains(lib, v)))
                {
                    continue;
                }

                int score = new[] { t.Name, t.Location }.Concat(t.Aliases).Select(Normalize).Where(n => n.Length >= 3 && Contains(game, n)).Select(n => n.Length).DefaultIfEmpty(0).Max();
                if (score > bestScore)
                {
                    bestScore = score;
                    best = t;
                }
            }

            return best;
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
    }
}
