namespace SecondMonitor.Hub.Core.Display
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Rectangle à l'écran, en pixels. Origine en haut à gauche de l'écran principal, y vers le bas
    /// (convention de Photino pour la position des fenêtres, sur Mac comme sous Windows et Linux).
    /// </summary>
    public readonly record struct ScreenRect(int X, int Y, int Width, int Height)
    {
        public int Right => X + Width;

        public int Bottom => Y + Height;

        /// <summary>Surface commune aux deux rectangles (vide si aucune).</summary>
        public ScreenRect Intersect(ScreenRect other)
        {
            int left = Math.Max(X, other.X);
            int top = Math.Max(Y, other.Y);
            int right = Math.Min(Right, other.Right);
            int bottom = Math.Min(Bottom, other.Bottom);
            return right > left && bottom > top ? new ScreenRect(left, top, right - left, bottom - top) : default;
        }

        public long Area => (long)Math.Max(0, Width) * Math.Max(0, Height);
    }

    /// <summary>Un écran : sa surface complète, et sa surface utile (sans la barre des menus, le Dock ni la barre des tâches).</summary>
    public sealed record ScreenArea(ScreenRect Area, ScreenRect Work);

    /// <summary>
    /// Où ouvrir les fenêtres de SecondMonitor quand plusieurs écrans sont branchés : une fenêtre ajoutée va sur un écran
    /// qui n'en a pas encore (sinon décalée sur le même écran), et une fenêtre rouverte au lancement reprend sa place
    /// si son écran est toujours là. Logique pure, sans fenêtre : testée dans Hub.Tests (FenetresTests).
    /// </summary>
    public static class WindowPlacement
    {
        public const int DefaultWidth = 1500;
        public const int DefaultHeight = 940;
        public const int MinWidth = 720;
        public const int MinHeight = 480;

        /// <summary>Hauteur de la barre de titre : elle doit rester atteignable pour pouvoir déplacer la fenêtre.</summary>
        private const int TitleBar = 32;

        /// <summary>Largeur minimale de barre de titre visible pour considérer une position comme encore valable.</summary>
        private const int MinVisibleTitle = 120;

        /// <summary>Décalage entre deux fenêtres sur un même écran.</summary>
        private const int Cascade = 40;

        private static readonly Regex TabName = new("^[a-z]{1,20}$", RegexOptions.CultureInvariant);

        /// <summary>
        /// Mac : Photino donne les écrans en coordonnées Cocoa (origine en bas à gauche de l'écran principal, y vers le haut),
        /// mais place les fenêtres depuis le haut de l'écran principal. Conversion vers la convention des fenêtres.
        /// </summary>
        public static ScreenRect FromCocoa(ScreenRect rect, int primaryHeight)
            => new(rect.X, primaryHeight - (rect.Y + rect.Height), rect.Width, rect.Height);

        /// <summary>Écran qui contient la plus grande partie de la fenêtre ; -1 si elle n'est sur aucun écran.</summary>
        public static int ScreenIndexOf(ScreenRect window, IReadOnlyList<ScreenArea> screens)
        {
            int best = -1;
            long bestArea = 0;
            for (int i = 0; i < screens.Count; i++)
            {
                long area = window.Intersect(screens[i].Area).Area;
                if (area > bestArea)
                {
                    best = i;
                    bestArea = area;
                }
            }

            return best;
        }

        /// <summary>Ramène la fenêtre dans la surface utile de l'écran (taille réduite si besoin, position décalée).</summary>
        public static ScreenRect FitInto(ScreenRect window, ScreenRect work)
        {
            int width = Math.Min(window.Width, work.Width);
            int height = Math.Min(window.Height, work.Height);
            int x = Math.Clamp(window.X, work.X, work.Right - width);
            int y = Math.Clamp(window.Y, work.Y, work.Bottom - height);
            return new ScreenRect(x, y, width, height);
        }

        /// <summary>
        /// Position mémorisée encore utilisable ? Oui si sa barre de titre est sur un écran branché : on la garde (ramenée dans l'écran).
        /// Sinon, si un écran de même taille que celui d'origine est branché ailleurs (écrans réarrangés), la fenêtre y est replacée
        /// au même endroit. Null si son écran a disparu : la fenêtre sera placée comme une nouvelle.
        /// </summary>
        public static ScreenRect? Restore(ScreenRect saved, ScreenRect? savedScreen, IReadOnlyList<ScreenArea> screens)
        {
            if (saved.Width <= 0 || saved.Height <= 0)
            {
                return null;
            }

            if (screens.Count == 0)
            {
                return saved; // écrans inconnus : on fait confiance à la position mémorisée
            }

            int index = ScreenIndexOf(saved, screens);
            if (index >= 0 && new ScreenRect(saved.X, saved.Y, saved.Width, TitleBar).Intersect(screens[index].Area).Width >= MinVisibleTitle)
            {
                return FitInto(saved, screens[index].Work);
            }

            if (savedScreen is { } origin && origin.Width > 0 && origin.Height > 0)
            {
                ScreenArea twin = screens.FirstOrDefault(s => s.Area.Width == origin.Width && s.Area.Height == origin.Height);
                if (twin != null)
                {
                    var moved = new ScreenRect(saved.X - origin.X + twin.Area.X, saved.Y - origin.Y + twin.Area.Y, saved.Width, saved.Height);
                    return FitInto(moved, twin.Work);
                }
            }

            return null;
        }

        /// <summary>
        /// Place une fenêtre ajoutée : elle remplit la surface utile du premier écran où aucune fenêtre de SecondMonitor n'est encore
        /// (<paramref name="otherScreen"/> vrai). Sans écran libre (un seul écran branché), elle s'ouvre décalée par rapport
        /// à la fenêtre principale, sur l'écran de celle-ci.
        /// </summary>
        public static ScreenRect PlaceNew(ScreenRect main, IReadOnlyList<ScreenRect> others, IReadOnlyList<ScreenArea> screens, out bool otherScreen)
        {
            others ??= Array.Empty<ScreenRect>();
            int step = Cascade * (others.Count + 1);
            otherScreen = false;
            if (screens.Count == 0)
            {
                return new ScreenRect(main.X + step, main.Y + step, Math.Max(MinWidth, main.Width), Math.Max(MinHeight, main.Height));
            }

            int mainIndex = Math.Max(0, ScreenIndexOf(main, screens));
            var occupied = new HashSet<int> { mainIndex };
            foreach (ScreenRect other in others)
            {
                int i = ScreenIndexOf(other, screens);
                if (i >= 0)
                {
                    occupied.Add(i);
                }
            }

            for (int i = 0; i < screens.Count; i++)
            {
                if (!occupied.Contains(i))
                {
                    otherScreen = true;
                    return screens[i].Work;
                }
            }

            ScreenRect work = screens[mainIndex].Work;
            int width = Math.Max(Math.Min(MinWidth, work.Width), Math.Min(main.Width, work.Width - step));
            int height = Math.Max(Math.Min(MinHeight, work.Height), Math.Min(main.Height, work.Height - step));
            return FitInto(new ScreenRect(main.X + step, main.Y + step, width, height), work);
        }

        /// <summary>Plus petit numéro libre pour une fenêtre ajoutée (la principale est la 1).</summary>
        public static int NextNumber(IEnumerable<int> open)
        {
            var used = new HashSet<int>(open ?? Enumerable.Empty<int>());
            int n = 2;
            while (used.Contains(n))
            {
                n++;
            }

            return n;
        }

        /// <summary>Nom d'onglet accepté dans l'adresse d'une fenêtre (« race », « strategy »…) ; null sinon.</summary>
        public static string SafeTab(string tab)
            => tab != null && TabName.IsMatch(tab) ? tab : null;

        /// <summary>Adresse d'une fenêtre : « http://localhost:8686/?fenetre=2#race ». Le numéro sépare ses préférences d'affichage.</summary>
        public static string WindowUrl(string baseUrl, int number, string tab = null)
        {
            string root = (baseUrl ?? string.Empty).TrimEnd('/');
            string safe = SafeTab(tab);
            return $"{root}/?fenetre={number}{(safe != null ? "#" + safe : string.Empty)}";
        }
    }
}
