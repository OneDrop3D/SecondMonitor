namespace SecondMonitor.Hub.Core.Display
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>Une fenêtre retenue : son numéro (1 = principale), sa place et l'écran où elle était.</summary>
    public sealed class SavedWindow
    {
        public int Number { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        /// <summary>Surface complète de l'écran où était la fenêtre : pour la retrouver si les écrans ont été réarrangés.</summary>
        public int ScreenX { get; set; }

        public int ScreenY { get; set; }

        public int ScreenWidth { get; set; }

        public int ScreenHeight { get; set; }

        [JsonIgnore]
        public ScreenRect Rect
        {
            get => new(X, Y, Width, Height);
            set => (X, Y, Width, Height) = (value.X, value.Y, value.Width, value.Height);
        }

        [JsonIgnore]
        public ScreenRect? Screen
        {
            get => ScreenWidth > 0 && ScreenHeight > 0 ? new ScreenRect(ScreenX, ScreenY, ScreenWidth, ScreenHeight) : null;
            set => (ScreenX, ScreenY, ScreenWidth, ScreenHeight) = value is { } s ? (s.X, s.Y, s.Width, s.Height) : (0, 0, 0, 0);
        }
    }

    /// <summary>Fenêtres de SecondMonitor à rouvrir au prochain lancement (la page de chacune est gardée par l'interface).</summary>
    public sealed class WindowLayout
    {
        public SavedWindow Main { get; set; }

        /// <summary>Fenêtres ajoutées (« Nouvelle fenêtre ») encore ouvertes quand l'application a été quittée.</summary>
        public List<SavedWindow> Windows { get; set; } = new();
    }

    /// <summary>
    /// fenetres.json dans le dossier de données : fenêtres ouvertes, position, taille et écran.
    /// Fichier illisible : on repart sans fenêtre ajoutée (jamais d'erreur au lancement).
    /// </summary>
    public sealed class WindowLayoutStore
    {
        public const string FileName = "fenetres.json";

        /// <summary>Au-delà, une fenêtre de plus n'a pas de sens (et protège d'un fichier modifié à la main).</summary>
        public const int MaxWindows = 8;

        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
        private readonly object _lock = new();
        private readonly string _path;

        public WindowLayoutStore(string dataDirectory)
        {
            _path = Path.Combine(dataDirectory, FileName);
        }

        public string FilePath => _path;

        public WindowLayout Load()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_path))
                    {
                        return Clean(JsonSerializer.Deserialize<WindowLayout>(File.ReadAllText(_path), Options));
                    }
                }
                catch (Exception)
                {
                    // Fichier corrompu : rien à rouvrir.
                }

                return new WindowLayout();
            }
        }

        public void Save(WindowLayout layout)
        {
            string json = JsonSerializer.Serialize(Clean(layout), Options);
            lock (_lock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                string temp = _path + ".tmp";
                File.WriteAllText(temp, json);
                File.Move(temp, _path, overwrite: true);
            }
        }

        /// <summary>Numéros valables et uniques, tailles positives, nombre limité.</summary>
        public static WindowLayout Clean(WindowLayout layout)
        {
            var clean = new WindowLayout();
            if (layout == null)
            {
                return clean;
            }

            if (layout.Main is { Width: > 0, Height: > 0 } main)
            {
                clean.Main = Copy(main, 1);
            }

            clean.Windows = (layout.Windows ?? new List<SavedWindow>())
                .Where(w => w != null && w.Number >= 2 && w.Number <= 99 && w.Width > 0 && w.Height > 0)
                .GroupBy(w => w.Number)
                .Select(g => Copy(g.First(), g.Key))
                .OrderBy(w => w.Number)
                .Take(MaxWindows)
                .ToList();
            return clean;
        }

        private static SavedWindow Copy(SavedWindow w, int number) => new()
        {
            Number = number,
            X = w.X,
            Y = w.Y,
            Width = w.Width,
            Height = w.Height,
            ScreenX = w.ScreenX,
            ScreenY = w.ScreenY,
            ScreenWidth = w.ScreenWidth,
            ScreenHeight = w.ScreenHeight,
        };
    }
}
