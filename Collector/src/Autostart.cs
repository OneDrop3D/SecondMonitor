namespace SecondMonitor.Collector
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Lancement automatique à l'ouverture de session (fenêtre réduite) :
    /// Windows : registre utilisateur ; Linux : ~/.config/autostart ; Mac : ~/Library/LaunchAgents.
    /// </summary>
    internal static class Autostart
    {
        private const string Name = "SecondMonitor Collecteur";

        private static string Exe => Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "SecondMonitor-Collecteur");

        private static string LinuxFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "autostart", "secondmonitor-collecteur.desktop");

        private static string MacFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents", "fr.onedrop3d.secondmonitor-collecteur.plist");

        public static bool IsEnabled()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                    return key?.GetValue(Name) != null;
                }

                return File.Exists(OperatingSystem.IsMacOS() ? MacFile : LinuxFile);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string Set(bool enabled)
        {
            string exe = Exe;
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
                else if (OperatingSystem.IsMacOS())
                {
                    if (enabled)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(MacFile)!);
                        File.WriteAllText(MacFile, $"""
                            <?xml version="1.0" encoding="UTF-8"?>
                            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
                            <plist version="1.0"><dict>
                              <key>Label</key><string>fr.onedrop3d.secondmonitor-collecteur</string>
                              <key>ProgramArguments</key><array><string>{System.Security.SecurityElement.Escape(exe)}</string><string>--reduit</string></array>
                              <key>RunAtLoad</key><true/>
                            </dict></plist>
                            """);
                    }
                    else if (File.Exists(MacFile))
                    {
                        File.Delete(MacFile);
                    }
                }
                else
                {
                    if (enabled)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(LinuxFile)!);
                        File.WriteAllText(LinuxFile, $"[Desktop Entry]\nType=Application\nName={Name}\nExec=\"{exe}\" --reduit\nTerminal=false\nX-GNOME-Autostart-enabled=true\n");
                    }
                    else if (File.Exists(LinuxFile))
                    {
                        File.Delete(LinuxFile);
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
        /// <summary>
        /// Le programme Windows n'a pas de console (application à fenêtre). En mode « --sans-fenetre », on se rattache
        /// à la console qui l'a lancé, ou on en ouvre une.
        /// </summary>
        public static void EnsureConsole()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            try
            {
                if (GetConsoleWindow() != IntPtr.Zero)
                {
                    return;
                }

                if (!AttachConsole(-1))
                {
                    AllocConsole();
                }

                var output = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
                Console.SetOut(output);
                Console.SetError(output);
            }
            catch (Exception)
            {
                // Sans console : le journal reste dans collecteur.log.
            }
        }

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

        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int processId);

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
