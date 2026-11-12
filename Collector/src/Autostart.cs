namespace SecondMonitor.Collector
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;

    /// <summary>Lancement automatique à l'ouverture de session (Windows : registre utilisateur ; Linux : ~/.config/autostart).</summary>
    internal static class Autostart
    {
        private const string Name = "SecondMonitor Collecteur";

        public static string Set(bool enabled)
        {
            string exe = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "SecondMonitor-Collecteur");
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                    if (enabled)
                    {
                        key.SetValue(Name, $"\"{exe}\" --reduit");
                    }
                    else
                    {
                        key.DeleteValue(Name, false);
                    }
                }
                else
                {
                    string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "autostart");
                    string file = Path.Combine(dir, "secondmonitor-collecteur.desktop");
                    if (enabled)
                    {
                        Directory.CreateDirectory(dir);
                        File.WriteAllText(file, $"[Desktop Entry]\nType=Application\nName={Name}\nExec=\"{exe}\" --reduit\nTerminal=false\nX-GNOME-Autostart-enabled=true\n");
                    }
                    else if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }

                return enabled ? "Le collecteur se lancera automatiquement à l'ouverture de session (fenêtre réduite)." : "Lancement automatique désactivé.";
            }
            catch (Exception ex)
            {
                return $"Impossible de modifier le lancement automatique : {ex.Message}";
            }
        }
    }

    /// <summary>Petites fonctions système (Windows uniquement, sans effet ailleurs).</summary>
    internal static class Native
    {
        public static void MinimizeConsole()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            try
            {
                IntPtr handle = GetConsoleWindow();
                if (handle != IntPtr.Zero)
                {
                    ShowWindow(handle, 6); // SW_MINIMIZE
                }
            }
            catch (Exception)
            {
                // Pas de fenêtre console (lancé en arrière-plan).
            }
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
