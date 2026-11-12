namespace SecondMonitor.Hub.Core.Track
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;

    /// <summary>
    /// Apprend le tracé du circuit à partir des positions (x, z) d'une voiture sur un tour complet,
    /// puis le mémorise sur disque (un fichier par jeu + circuit) pour l'avoir immédiatement la fois suivante.
    /// </summary>
    public sealed class TrackMapBuilder
    {
        public const int OutlinePoints = 400;

        private readonly string _storageDirectory;
        private readonly List<(double LapDistance, double X, double Z)> _samples = new();
        private string _key;
        private int _recordingLap = -1;

        public TrackMapBuilder(string storageDirectory)
        {
            _storageDirectory = storageDirectory;
        }

        public List<double[]> Outline { get; private set; }

        public void SetTrack(string simulator, string trackFullName)
        {
            string key = Sanitize($"{simulator}__{trackFullName}");
            if (key == _key)
            {
                return;
            }

            _key = key;
            _samples.Clear();
            _recordingLap = -1;
            Outline = Load();
        }

        /// <summary>À appeler à chaque mise à jour avec une voiture « de référence » (le joueur en priorité).</summary>
        public void AddSample(int completedLaps, double lapDistance, double lapLength, double x, double z, bool inPits)
        {
            if (Outline != null || _key == null || lapLength <= 0 || (x == 0 && z == 0))
            {
                return;
            }

            if (inPits)
            {
                _samples.Clear();
                _recordingLap = -1;
                return;
            }

            if (completedLaps != _recordingLap)
            {
                TryFinalize(lapLength);
                _samples.Clear();
                _recordingLap = completedLaps;
            }

            if (lapDistance >= 0 && lapDistance <= lapLength * 1.02)
            {
                _samples.Add((lapDistance, x, z));
            }
        }

        private void TryFinalize(double lapLength)
        {
            if (_samples.Count < 60)
            {
                return;
            }

            var ordered = _samples.OrderBy(s => s.LapDistance).ToList();
            if (ordered[0].LapDistance > lapLength * 0.04 || ordered[^1].LapDistance < lapLength * 0.96)
            {
                return; // tour incomplet
            }

            // Pas de « trou » de plus de 4 % du tour.
            for (int i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].LapDistance - ordered[i - 1].LapDistance > lapLength * 0.04)
                {
                    return;
                }
            }

            var outline = new List<double[]>(OutlinePoints);
            int j = 0;
            for (int k = 0; k < OutlinePoints; k++)
            {
                double d = ordered[0].LapDistance + ((ordered[^1].LapDistance - ordered[0].LapDistance) * k / (OutlinePoints - 1));
                while (j < ordered.Count - 2 && ordered[j + 1].LapDistance < d)
                {
                    j++;
                }

                var a = ordered[j];
                var b = ordered[j + 1];
                double span = b.LapDistance - a.LapDistance;
                double t = span <= 1e-6 ? 0 : Math.Clamp((d - a.LapDistance) / span, 0, 1);
                outline.Add(new[] { Math.Round(a.X + ((b.X - a.X) * t), 2), Math.Round(a.Z + ((b.Z - a.Z) * t), 2) });
            }

            Outline = outline;
            Save();
        }

        private List<double[]> Load()
        {
            try
            {
                string path = PathFor(_key);
                return File.Exists(path) ? JsonSerializer.Deserialize<List<double[]>>(File.ReadAllText(path)) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void Save()
        {
            try
            {
                Directory.CreateDirectory(_storageDirectory);
                File.WriteAllText(PathFor(_key), JsonSerializer.Serialize(Outline));
            }
            catch (Exception)
            {
                // Non bloquant : la carte sera réapprise.
            }
        }

        private string PathFor(string key) => Path.Combine(_storageDirectory, key + ".json");

        private static string Sanitize(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(value.Select(c => invalid.Contains(c) || c == ' ' ? '_' : c).ToArray());
        }
    }
}
