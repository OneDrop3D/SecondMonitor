namespace SecondMonitor.Hub.Core.Update
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Formats.Tar;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;

    /*  « Redémarrer et installer » — même principe que Multibook (src-tauri/src/maj.rs) et ClickDeck.
     *
     *  L'application prépare tout pendant qu'elle tourne encore (c'est là que les erreurs sont faciles à montrer),
     *  se ferme, puis un petit script DÉTACHÉ remplace LES DEUX applications (SecondMonitor + Collecteur) et relance.
     *  Règle d'or : l'utilisateur ne reste jamais sans application.
     *
     *  - macOS : l'image disque (téléchargée par HttpClient : AUCUNE quarantaine, donc aucun passage par
     *    Réglages › Confidentialité et sécurité) est montée, chaque .app copiée à côté de l'ancienne (« …app.nouvelle »),
     *    vérifiée (identifiant, version, exécutable, signature), quarantaine retirée, image démontée AVANT de quitter.
     *    Le script échange les noms (mv, atomique sur un même volume), relance, et remet l'ancienne si la nouvelle
     *    ne démarre pas. Dossier non modifiable : on ouvre l'image, installation à la main.
     *  - Windows installé : l'installateur Inno Setup part en silencieux après la fermeture
     *    (/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS), après une copie de sauvegarde du dossier,
     *    remise en place si l'installation ou le démarrage échoue. Windows sans installation : le zip remplace le dossier.
     *  - Linux : l'archive .tar.gz remplace le dossier (même échange que macOS), puis installer.sh --silencieux
     *    rafraîchit les raccourcis.
     *
     *  La nouvelle version prouve qu'elle a démarré en écrivant « demarre-<app>.txt » (sa version) dans le dossier
     *  de travail ; sans cette preuve, retour arrière, et la raison est lue au démarrage suivant (maj-echec.txt).
     */

    /// <summary>Dossier de travail commun au hub et au collecteur (cache de l'utilisateur).</summary>
    public static class UpdatePaths
    {
        public static string WorkDirectory
        {
            get
            {
                string test = Environment.GetEnvironmentVariable(UpdateSource.TestFolderVariable);
                if (!string.IsNullOrWhiteSpace(test))
                {
                    return Path.GetFullPath(test);
                }

                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (OperatingSystem.IsMacOS())
                {
                    return Path.Combine(home, "Library", "Caches", "SecondMonitor", "mise-a-jour");
                }

                if (OperatingSystem.IsWindows())
                {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SecondMonitor", "mise-a-jour");
                }

                string cache = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
                return Path.Combine(string.IsNullOrWhiteSpace(cache) ? Path.Combine(home, ".cache") : cache, "secondmonitor", "mise-a-jour");
            }
        }

        public static string Downloads(string work) => Path.Combine(work, "telechargement");

        public static string Journal(string work) => Path.Combine(work, "maj.log");

        public static string Failure(string work) => Path.Combine(work, "maj-echec.txt");

        public static string StartMark(string work, string app) => Path.Combine(work, $"demarre-{app}.txt");

        /// <summary>Au démarrage : « cette version a bien démarré » (preuve attendue par le script de mise à jour).</summary>
        public static void MarkStarted(string app, string version)
        {
            try
            {
                string work = WorkDirectory;
                if (Directory.Exists(work))
                {
                    File.WriteAllText(StartMark(work, app), UpdateVersion.Normalize(version));
                }
            }
            catch (Exception)
            {
                // Sans importance hors mise à jour.
            }
        }

        /// <summary>Si la dernière mise à jour automatique a dû revenir en arrière, la raison — une seule fois.</summary>
        public static string TakePreviousFailure()
        {
            try
            {
                string f = Failure(WorkDirectory);
                if (!File.Exists(f))
                {
                    return null;
                }

                string t = File.ReadAllText(f).Trim();
                File.Delete(f);
                return t.Length == 0 ? null : t;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    /// <summary>Ce que l'installation a décidé : un script détaché (l'application doit se fermer) ou une installation à la main.</summary>
    public sealed class InstallPlan
    {
        /// <summary>Script de remplacement prêt : le lancer détaché puis quitter.</summary>
        public string Script { get; set; }

        /// <summary>Rien n'est remplacé automatiquement : ce message, et <see cref="OpenPath"/> est ouvert.</summary>
        public string Manual { get; set; }

        public string OpenPath { get; set; }
    }

    /// <summary>Tout ce dont la préparation a besoin (réglable pour les essais).</summary>
    public sealed class InstallContext
    {
        /// <summary>Fichier téléchargé et vérifié.</summary>
        public string File { get; set; }

        public string Version { get; set; }

        public string Current { get; set; }

        /// <summary>« hub » ou « collecteur » : l'application qui lance la mise à jour (et sera relancée).</summary>
        public string App { get; set; }

        /// <summary>Exécutable en cours (Environment.ProcessPath).</summary>
        public string Exe { get; set; }

        /// <summary>Arguments de lancement à redonner à la nouvelle version (--sans-fenetre, --navigateur, --reduit…).</summary>
        public IReadOnlyList<string> Args { get; set; } = Array.Empty<string>();

        public string Work { get; set; }

        public int Pid { get; set; }

        /// <summary>Secondes d'attente de la fermeture de l'application, puis abandon (rien n'est modifié).</summary>
        public int ExitTimeout { get; set; } = 60;

        /// <summary>Secondes pour voir la nouvelle version démarrer, sinon retour arrière.</summary>
        public int StartTimeout { get; set; } = 60;

        /// <summary>macOS : chercher l'autre application aussi dans /Applications et ~/Applications (jamais en mode test).</summary>
        public bool SearchSystemFolders { get; set; } = !UpdateSource.TestMode;

        /// <summary>Variables d'environnement à transmettre à la nouvelle version (essais : SMHUB_DATA, SM_MAJ_*).</summary>
        public IReadOnlyDictionary<string, string> Environment { get; set; } = TestEnvironment();

        public static IReadOnlyDictionary<string, string> TestEnvironment()
        {
            var env = new Dictionary<string, string>();
            foreach (string name in new[] { "SMHUB_DATA", UpdateSource.TestManifestVariable, UpdateSource.TestFolderVariable })
            {
                string v = System.Environment.GetEnvironmentVariable(name);
                if (!string.IsNullOrEmpty(v))
                {
                    env[name] = v;
                }
            }

            return env;
        }
    }

    public static class UpdateInstaller
    {
        public const string HubBundleId = "fr.onedrop3d.secondmonitor";
        public const string CollectorBundleId = "fr.onedrop3d.secondmonitor-collecteur";

        /// <summary>Prépare la mise à jour pour ce système (sans rien remplacer).</summary>
        public static InstallPlan Prepare(InstallContext c)
        {
            Directory.CreateDirectory(c.Work);
            TryDelete(UpdatePaths.Failure(c.Work));
            File.WriteAllText(UpdatePaths.Journal(c.Work), string.Empty);
            string ext = Path.GetExtension(c.File).ToLowerInvariant();
            if (OperatingSystem.IsMacOS())
            {
                return ext == ".dmg" ? PrepareMac(c) : Manual("Le fichier est ouvert : suivez les étapes du système.", c.File);
            }

            if (OperatingSystem.IsWindows())
            {
                return PrepareWindows(c);
            }

            return c.File.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)
                ? PrepareLinux(c)
                : Manual("Le fichier est ouvert : suivez les étapes du système.", c.File);
        }

        /// <summary>Lance le script de remplacement, détaché : il survit à l'application.</summary>
        public static void LaunchDetached(string script, string work)
        {
            ProcessStartInfo start;
            if (OperatingSystem.IsWindows())
            {
                // Doubles guillemets : sans eux, cmd retire ceux du chemin dès qu'il contient & ou des parenthèses.
                start = new ProcessStartInfo("cmd.exe", $"/D /C \"\"{script}\"\"") { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
            }
            else
            {
                // Double détachement : le script est rattaché à launchd / init dès que ce « sh -c » se termine.
                string detach = OperatingSystem.IsLinux()
                    ? "if command -v setsid >/dev/null 2>&1; then setsid /bin/sh \"$0\" </dev/null >/dev/null 2>&1 & else nohup /bin/sh \"$0\" </dev/null >/dev/null 2>&1 & fi"
                    : "nohup /bin/sh \"$0\" </dev/null >/dev/null 2>&1 &";
                start = new ProcessStartInfo("/bin/sh");
                start.ArgumentList.Add("-c");
                start.ArgumentList.Add(detach);
                start.ArgumentList.Add(script);
            }

            start.UseShellExecute = false;
            start.WorkingDirectory = work;
            using Process p = Process.Start(start) ?? throw new InvalidOperationException("Le script de mise à jour n'a pas pu démarrer.");
            if (!OperatingSystem.IsWindows())
            {
                p.WaitForExit(5000);
            }
        }

        /// <summary>Ouvre un fichier ou un dossier avec le système (installation à la main).</summary>
        public static void Open(string path)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })?.Dispose();
                }
                else
                {
                    Process.Start(OperatingSystem.IsMacOS() ? "open" : "xdg-open", new[] { path })?.Dispose();
                }
            }
            catch (Exception)
            {
                // Pas d'ouvreur : le message indique le chemin.
            }
        }

        // ───────────────────────────── macOS ─────────────────────────────

        private static InstallPlan PrepareMac(InstallContext c)
        {
            // …/SecondMonitor.app/Contents/MacOS/SecondMonitorHub → …/SecondMonitor.app
            string bundle = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(c.Exe)));
            if (bundle == null || !bundle.EndsWith(".app", StringComparison.OrdinalIgnoreCase) || !Directory.Exists(bundle))
            {
                return Manual("SecondMonitor n'est pas lancé depuis une application installée : l'image disque est ouverte, glissez les deux applications dans Applications.", c.File);
            }

            string parent = Path.GetDirectoryName(bundle);
            if (!Writable(parent))
            {
                return Manual($"SecondMonitor ne peut pas se remplacer lui-même (le dossier {parent} n'est pas modifiable par votre compte) : l'image disque est ouverte, glissez les deux applications dans Applications.", c.File);
            }

            string myId = c.App == "hub" ? HubBundleId : CollectorBundleId;
            string otherId = c.App == "hub" ? CollectorBundleId : HubBundleId;
            string otherName = c.App == "hub" ? "SecondMonitor Collecteur.app" : "SecondMonitor.app";
            var targets = new List<(string Target, string Id, bool Initiator)> { (bundle, myId, true) };
            string other = FindOtherBundle(parent, otherName, otherId, c.SearchSystemFolders);
            if (other != null && Writable(Path.GetDirectoryName(other)))
            {
                targets.Add((other, otherId, false));
            }

            var pairs = new List<Pair>();
            void Cleanup()
            {
                foreach (Pair p in pairs)
                {
                    TryDeleteDirectory(p.New);
                }
            }

            string mount = Path.Combine(c.Work, "montage");
            try
            {
                Detach(mount);
                TryDeleteDirectory(mount);
                Directory.CreateDirectory(mount);
                try
                {
                    Run("hdiutil", "attach", "-nobrowse", "-readonly", "-noautoopen", "-noverify", "-mountpoint", mount, c.File);
                }
                catch (InvalidOperationException ex)
                {
                    throw new InvalidOperationException($"L'image disque n'a pas pu être montée ({ex.Message}).");
                }

                try
                {
                    var inImage = Directory.GetDirectories(mount, "*.app")
                        .GroupBy(a => PlistValue(Path.Combine(a, "Contents", "Info.plist"), "CFBundleIdentifier") ?? string.Empty)
                        .ToDictionary(g => g.Key, g => g.First());
                    foreach (var (target, id, initiator) in targets)
                    {
                        if (!inImage.TryGetValue(id, out string source))
                        {
                            if (initiator)
                            {
                                throw new InvalidOperationException(inImage.Count == 0
                                    ? "L'image disque ne contient aucune application."
                                    : "L'image disque ne contient pas SecondMonitor (identifiant inattendu).");
                            }

                            continue; // l'autre application n'est pas dans l'image : elle reste telle quelle
                        }

                        string fresh = target + ".nouvelle";
                        TryDeleteDirectory(fresh);
                        TryDeleteDirectory(target + ".ancienne");
                        pairs.Add(new Pair(target, fresh, initiator, $"{target}/Contents/MacOS/"));
                        try
                        {
                            Run("ditto", source, fresh);
                        }
                        catch (InvalidOperationException ex)
                        {
                            throw new InvalidOperationException($"La copie de la nouvelle version a échoué ({ex.Message}).");
                        }
                    }
                }
                finally
                {
                    Detach(mount); // démontée tout de suite : plus rien ne dépend de l'image
                    TryDeleteDirectory(mount);
                }

                foreach (Pair p in pairs)
                {
                    string info = Path.Combine(p.New, "Contents", "Info.plist");
                    string id = PlistValue(info, "CFBundleIdentifier") ?? throw new InvalidOperationException("La nouvelle version est illisible (Info.plist).");
                    string expected = p.Initiator ? myId : otherId;
                    if (id != expected)
                    {
                        throw new InvalidOperationException($"L'application de l'image n'est pas celle attendue ({id}).");
                    }

                    string v = PlistValue(info, "CFBundleShortVersionString") ?? throw new InvalidOperationException("Version illisible dans la nouvelle application.");
                    if (UpdateVersion.Compare(v, c.Current) <= 0)
                    {
                        throw new InvalidOperationException($"L'image contient la version {v}, qui n'est pas plus récente que la vôtre ({c.Current}).");
                    }

                    string exe = PlistValue(info, "CFBundleExecutable") ?? throw new InvalidOperationException("Exécutable manquant dans la nouvelle application.");
                    if (!IsExecutable(Path.Combine(p.New, "Contents", "MacOS", exe)))
                    {
                        throw new InvalidOperationException("La nouvelle application est incomplète (exécutable absent).");
                    }

                    // Une signature cassée est refusée, sauf si l'application actuelle ne l'est déjà plus.
                    if (!SignatureValid(p.New) && SignatureValid(p.Target))
                    {
                        throw new InvalidOperationException("La signature de la nouvelle version est invalide : elle n'est pas installée.");
                    }

                    // Rien ne l'a posée (téléchargement par HttpClient), mais une image passée par un navigateur en aurait une.
                    TryRun("xattr", "-dr", "com.apple.quarantine", p.New);
                }
            }
            catch (Exception)
            {
                Cleanup();
                throw;
            }

            string args = Quote(c.Args.Where(a => !a.StartsWith("-psn", StringComparison.Ordinal)));
            string env = string.Join(string.Empty, c.Environment.Select(kv => $" --env {ShQ(kv.Key + "=" + kv.Value)}"));
            foreach (Pair p in pairs)
            {
                string launchArgs = p.Initiator ? args : (otherId == CollectorBundleId ? " --reduit" : string.Empty);
                p.Launch = $"open -n{env} {ShQ(p.Target)}" + (launchArgs.Length > 0 ? " --args" + launchArgs : string.Empty);
            }

            string script = UnixScript(c, pairs, quarantine: true, afterFailure: $"open {ShQ(c.File)}", afterSwap: null);
            return WriteUnixScript(c, script, pairs);
        }

        private static string FindOtherBundle(string parent, string name, string id, bool searchSystem)
        {
            var folders = new List<string> { parent };
            if (searchSystem)
            {
                folders.Add("/Applications");
                folders.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Applications"));
            }

            foreach (string folder in folders.Distinct())
            {
                string candidate = Path.Combine(folder, name);
                if (Directory.Exists(candidate) && PlistValue(Path.Combine(candidate, "Contents", "Info.plist"), "CFBundleIdentifier") == id)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void Detach(string mount)
        {
            if (!Directory.Exists(mount) || !Directory.EnumerateFileSystemEntries(mount).Any())
            {
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                if (TryRun("hdiutil", i >= 2 ? new[] { "detach", mount, "-quiet", "-force" } : new[] { "detach", mount, "-quiet" }))
                {
                    return;
                }

                if (!Directory.Exists(mount) || !Directory.EnumerateFileSystemEntries(mount).Any())
                {
                    return;
                }

                System.Threading.Thread.Sleep(500);
            }
        }

        private static string PlistValue(string plist, string key)
        {
            if (!File.Exists(plist))
            {
                return null;
            }

            try
            {
                string v = Output("plutil", "-extract", key, "raw", "-o", "-", plist).Trim();
                return v.Length == 0 ? null : v;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static bool SignatureValid(string app) => TryRun("codesign", "--verify", "--deep", "--strict", app);

        // ───────────────────────────── Linux ─────────────────────────────

        private static InstallPlan PrepareLinux(InstallContext c)
        {
            string dir = Path.GetDirectoryName(c.Exe);
            string parent = dir == null ? null : Path.GetDirectoryName(dir);
            if (parent == null || !File.Exists(Path.Combine(dir, "SecondMonitorHub")) && !File.Exists(Path.Combine(dir, "SecondMonitor-Collecteur")))
            {
                return Manual("SecondMonitor n'est pas lancé depuis son dossier d'installation : l'archive est affichée, décompressez-la puis lancez ./installer.sh.", Path.GetDirectoryName(c.File));
            }

            if (!Writable(parent) || !Writable(dir))
            {
                return Manual($"SecondMonitor ne peut pas remplacer son dossier ({dir}) : l'archive est affichée, décompressez-la puis lancez ./installer.sh.", Path.GetDirectoryName(c.File));
            }

            string fresh = dir + ".nouvelle";
            string extract = Path.Combine(parent, ".secondmonitor-maj-extrait");
            TryDeleteDirectory(fresh);
            TryDeleteDirectory(dir + ".ancienne");
            TryDeleteDirectory(extract);
            try
            {
                Directory.CreateDirectory(extract);
                using (FileStream f = File.OpenRead(c.File))
                using (var gz = new GZipStream(f, CompressionMode.Decompress))
                {
                    TarFile.ExtractToDirectory(gz, extract, overwriteFiles: true);
                }

                string root = Path.Combine(extract, "secondmonitor");
                if (!Directory.Exists(root))
                {
                    root = Directory.GetDirectories(extract).FirstOrDefault() ?? throw new InvalidOperationException("L'archive est vide.");
                }

                CheckPackage(root, c, "SecondMonitorHub", "SecondMonitor-Collecteur", elf: true);
                Directory.Move(root, fresh);
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                TryDeleteDirectory(fresh);
                throw new InvalidOperationException($"L'archive n'a pas pu être décompressée ({ex.Message}).");
            }
            catch (Exception)
            {
                TryDeleteDirectory(fresh);
                throw;
            }
            finally
            {
                TryDeleteDirectory(extract);
            }

            string exeName = c.App == "hub" ? "SecondMonitorHub" : "SecondMonitor-Collecteur";
            string otherExe = c.App == "hub" ? "SecondMonitor-Collecteur" : "SecondMonitorHub";
            string env = string.Join(string.Empty, c.Environment.Select(kv => $"{kv.Key}={ShQ(kv.Value)} "));
            var pair = new Pair(dir, fresh, true, $"{dir}/{exeName}")
            {
                Launch = $"(cd \"$CIBLE_1\" && {env}nohup \"$CIBLE_1/{exeName}\"{Quote(c.Args)} >/dev/null 2>&1 &)",
                OtherMotif = $"{dir}/{otherExe}",
                OtherLaunch = $"(cd \"$CIBLE_1\" && nohup \"$CIBLE_1/{otherExe}\"{(otherExe == "SecondMonitor-Collecteur" ? " --reduit" : string.Empty)} >/dev/null 2>&1 &)",
            };

            // Installation standard (~/.local/share/secondmonitor) : raccourcis et icônes rafraîchis, sans question.
            string afterSwap = $"if [ -x \"$CIBLE_1/installer.sh\" ] && [ \"$CIBLE_1\" = {ShQ(LinuxInstallDir())} ]; then \"$CIBLE_1/installer.sh\" --silencieux >>\"$JOURNAL\" 2>&1 || journal \"installer.sh a échoué (raccourcis non rafraîchis)\"; fi";
            string script = UnixScript(c, new List<Pair> { pair }, quarantine: false, afterFailure: $"xdg-open {ShQ(Path.GetDirectoryName(c.File))} >/dev/null 2>&1 &", afterSwap: afterSwap);
            return WriteUnixScript(c, script, new List<Pair> { pair });
        }

        public static string LinuxInstallDir()
        {
            string data = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            string root = string.IsNullOrWhiteSpace(data) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share") : data;
            return Path.Combine(root, "secondmonitor");
        }

        /// <summary>Le dossier décompressé doit contenir les deux programmes, d'une version plus récente.</summary>
        private static void CheckPackage(string root, InstallContext c, string hub, string collector, bool elf)
        {
            foreach (string name in new[] { hub, collector })
            {
                string path = Path.Combine(root, name);
                if (!File.Exists(path))
                {
                    throw new InvalidOperationException($"La nouvelle version est incomplète ({name} absent).");
                }

                if (elf)
                {
                    byte[] head = new byte[4];
                    using (FileStream f = File.OpenRead(path))
                    {
                        if (f.Read(head, 0, 4) != 4 || head[0] != 0x7f || head[1] != (byte)'E' || head[2] != (byte)'L' || head[3] != (byte)'F')
                        {
                            throw new InvalidOperationException($"{name} n'est pas un programme Linux.");
                        }
                    }

                    if (!OperatingSystem.IsWindows())
                    {
                        File.SetUnixFileMode(path, File.GetUnixFileMode(path) | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
                    }
                }
            }

            string versionFile = Path.Combine(root, "version.txt");
            if (File.Exists(versionFile))
            {
                string v = File.ReadAllText(versionFile).Trim();
                if (UpdateVersion.Compare(v, c.Current) <= 0)
                {
                    throw new InvalidOperationException($"L'archive contient la version {v}, qui n'est pas plus récente que la vôtre ({c.Current}).");
                }
            }
        }

        // ───────────────────────────── macOS et Linux : le script ─────────────────────────────

        private sealed class Pair
        {
            public Pair(string target, string fresh, bool initiator, string motif)
            {
                Target = target;
                New = fresh;
                Initiator = initiator;
                Motif = motif;
            }

            public string Target { get; }

            public string New { get; }

            public string Old => Target + ".ancienne";

            public bool Initiator { get; }

            /// <summary>Reconnaît les processus de cette application (pgrep -f).</summary>
            public string Motif { get; }

            public string Launch { get; set; }

            /// <summary>Linux : l'autre programme vit dans le même dossier.</summary>
            public string OtherMotif { get; set; }

            public string OtherLaunch { get; set; }
        }

        private static InstallPlan WriteUnixScript(InstallContext c, string text, List<Pair> pairs)
        {
            string path = Path.Combine(c.Work, "maj-installer.sh");
            try
            {
                File.WriteAllText(path, text, new UTF8Encoding(false));
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            catch (Exception ex)
            {
                foreach (Pair p in pairs)
                {
                    TryDeleteDirectory(p.New);
                }

                throw new InvalidOperationException($"Script de mise à jour : {ex.Message}");
            }

            return new InstallPlan { Script = path };
        }

        /// <summary>Le script de remplacement (macOS et Linux), visible pour les tests.</summary>
        private static string UnixScript(InstallContext c, List<Pair> pairs, bool quarantine, string afterFailure, string afterSwap)
        {
            var s = new StringBuilder();
            void L(string line) => s.Append(line).Append('\n');
            L("#!/bin/sh");
            L("# Mise à jour de SecondMonitor : remplacement des applications (voir UpdateInstaller.cs).");
            L($"PID={c.Pid}");
            L($"VERSION={ShQ(UpdateVersion.Normalize(c.Version))}");
            L($"JOURNAL={ShQ(UpdatePaths.Journal(c.Work))}");
            L($"ECHEC={ShQ(UpdatePaths.Failure(c.Work))}");
            L($"MARQUE={ShQ(UpdatePaths.StartMark(c.Work, c.App))}");
            L($"DOSSIER={ShQ(Path.GetDirectoryName(c.File))}");
            for (int i = 0; i < pairs.Count; i++)
            {
                Pair p = pairs[i];
                int n = i + 1;
                L($"CIBLE_{n}={ShQ(p.Target)}");
                L($"NOUVELLE_{n}={ShQ(p.New)}");
                L($"ANCIENNE_{n}={ShQ(p.Old)}");
                L($"MOTIF_{n}={ShQ(p.Motif)}");
                L($"AUTRE_{n}=0");
            }

            L("AUTRE_DOSSIER=0");
            L("journal() { printf '%s %s\\n' \"$(date '+%Y-%m-%d %H:%M:%S')\" \"$*\" >> \"$JOURNAL\"; }");
            L("echec() { journal \"ÉCHEC : $*\"; printf '%s\\n' \"$*\" > \"$ECHEC\"; }");

            // Relance : l'application qui a lancé la mise à jour, et l'autre si elle tournait.
            L("lancer() {");
            for (int i = 0; i < pairs.Count; i++)
            {
                Pair p = pairs[i];
                int n = i + 1;
                L(p.Initiator ? $"  {p.Launch}" : $"  if [ \"$AUTRE_{n}\" = 1 ]; then {p.Launch}; fi");
                if (p.OtherLaunch != null)
                {
                    L($"  if [ \"$AUTRE_DOSSIER\" = 1 ]; then {p.OtherLaunch}; fi");
                }
            }

            L("}");
            L("arreter_nouvelles() {");
            for (int i = 0; i < pairs.Count; i++)
            {
                L($"  pkill -KILL -f \"$MOTIF_{i + 1}\" 2>/dev/null");
                if (pairs[i].OtherMotif != null)
                {
                    L("  pkill -KILL -f \"$MOTIF_AUTRE\" 2>/dev/null");
                }
            }

            L("}");

            // Retour arrière : chaque ancienne reprend sa place.
            L("retour() {");
            for (int i = 0; i < pairs.Count; i++)
            {
                int n = i + 1;
                L($"  if [ -e \"$ANCIENNE_{n}\" ]; then rm -rf \"$CIBLE_{n}\"; mv \"$ANCIENNE_{n}\" \"$CIBLE_{n}\" 2>>\"$JOURNAL\"; fi");
                L($"  rm -rf \"$NOUVELLE_{n}\"");
            }

            L("  lancer");
            L($"  {afterFailure}");
            L("}");
            L(string.Empty);

            L("journal \"mise à jour vers $VERSION : attente de la fermeture ($PID)\"");
            L("n=0");
            L("while kill -0 \"$PID\" 2>/dev/null; do");
            L("  n=$((n+1))");
            L($"  if [ \"$n\" -gt {c.ExitTimeout * 5} ]; then");
            L("    journal \"l'application ne s'est pas fermée : mise à jour abandonnée, rien n'a été modifié\"");
            for (int i = 0; i < pairs.Count; i++)
            {
                L($"    rm -rf \"$NOUVELLE_{i + 1}\"");
            }

            L("    exit 0");
            L("  fi");
            L("  sleep 0.2");
            L("done");

            // L'autre application (collecteur ou SecondMonitor) est fermée proprement si elle tourne, puis relancée.
            for (int i = 0; i < pairs.Count; i++)
            {
                Pair p = pairs[i];
                int n = i + 1;
                if (!p.Initiator)
                {
                    L($"if pgrep -f \"$MOTIF_{n}\" >/dev/null 2>&1; then AUTRE_{n}=1; journal \"fermeture de $CIBLE_{n}\"; pkill -TERM -f \"$MOTIF_{n}\"; fi");
                    L($"k=0; while pgrep -f \"$MOTIF_{n}\" >/dev/null 2>&1 && [ \"$k\" -lt 50 ]; do sleep 0.2; k=$((k+1)); done");
                    L($"pkill -KILL -f \"$MOTIF_{n}\" 2>/dev/null");
                }

                if (p.OtherMotif != null)
                {
                    L($"MOTIF_AUTRE={ShQ(p.OtherMotif)}");
                    L("if pgrep -f \"$MOTIF_AUTRE\" >/dev/null 2>&1; then AUTRE_DOSSIER=1; journal \"fermeture de l'autre programme\"; pkill -TERM -f \"$MOTIF_AUTRE\"; fi");
                    L("k=0; while pgrep -f \"$MOTIF_AUTRE\" >/dev/null 2>&1 && [ \"$k\" -lt 50 ]; do sleep 0.2; k=$((k+1)); done");
                    L("pkill -KILL -f \"$MOTIF_AUTRE\" 2>/dev/null");
                }
            }

            for (int i = 0; i < pairs.Count; i++)
            {
                int n = i + 1;
                L($"rm -rf \"$ANCIENNE_{n}\"");
                L($"if ! mv \"$CIBLE_{n}\" \"$ANCIENNE_{n}\" 2>>\"$JOURNAL\"; then");
                L("  echec \"L'ancienne version n'a pas pu être mise de côté : elle est restée en place.\"");
                L("  retour; exit 1");
                L("fi");
                L($"if ! mv \"$NOUVELLE_{n}\" \"$CIBLE_{n}\" 2>>\"$JOURNAL\"; then");
                L("  echec \"La nouvelle version n'a pas pu être mise en place : l'ancienne a été remise.\"");
                L("  retour; exit 1");
                L("fi");
                if (quarantine)
                {
                    L($"xattr -dr com.apple.quarantine \"$CIBLE_{n}\" 2>/dev/null");
                }
            }

            if (afterSwap != null)
            {
                L(afterSwap);
            }

            L("rm -f \"$MARQUE\"");
            L("journal \"nouvelle version en place, lancement\"");
            L("lancer");

            // La nouvelle version doit PROUVER qu'elle a démarré (elle écrit sa version dans $MARQUE)…
            L("n=0; vue=0");
            L($"while [ \"$n\" -lt {c.StartTimeout * 5} ]; do");
            L("  if [ -f \"$MARQUE\" ] && grep -qF \"$VERSION\" \"$MARQUE\" 2>/dev/null; then vue=1; break; fi");
            L("  sleep 0.2; n=$((n+1))");
            L("done");

            // … et tourner encore trois secondes après : une version qui plante au démarrage ne passe pas.
            Pair me = pairs.First(p => p.Initiator);
            int mine = pairs.IndexOf(me) + 1;
            L("if [ \"$vue\" = 1 ]; then");
            L("  sleep 3");
            L($"  pgrep -f \"$MOTIF_{mine}\" >/dev/null 2>&1 || vue=0");
            L("fi");
            L("if [ \"$vue\" = 1 ]; then");
            for (int i = 0; i < pairs.Count; i++)
            {
                L($"  rm -rf \"$ANCIENNE_{i + 1}\"");
            }

            L("  rm -rf \"$DOSSIER\"");
            L("  rm -f \"$ECHEC\"");
            L("  journal \"mise à jour terminée\"");
            L("  exit 0");
            L("fi");
            L("echec \"La nouvelle version ne démarre pas : l'ancienne a été remise.\"");
            L("arreter_nouvelles");
            L("retour");
            L("exit 1");
            return s.ToString();
        }

        /// <summary>Visible pour les tests : le script macOS/Linux d'un échange de dossiers.</summary>
        internal static string UnixScriptForTest(InstallContext c, string target, string fresh, string launch)
        {
            var p = new Pair(target, fresh, true, target) { Launch = launch };
            return UnixScript(c, new List<Pair> { p }, quarantine: false, afterFailure: ":", afterSwap: null);
        }

        // ───────────────────────────── Windows ─────────────────────────────

        private static InstallPlan PrepareWindows(InstallContext c)
        {
            string dir = Path.GetDirectoryName(c.Exe);
            bool setup = c.File.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
            if (dir == null)
            {
                return Manual("Dossier de l'application introuvable : l'installateur est ouvert, suivez ses étapes.", c.File);
            }

            if (setup)
            {
                if (!File.Exists(Path.Combine(dir, "unins000.exe")))
                {
                    return Manual("SecondMonitor n'a pas été installé par son installateur : il s'ouvre, suivez ses étapes.", c.File);
                }

                return WriteCmd(c, WindowsScript(c, dir, setup: true, fresh: null));
            }

            // Zip : le dossier est remplacé (échange de noms, comme sur Mac).
            string parent = Path.GetDirectoryName(dir);
            string fresh = dir + ".nouvelle";
            string extract = Path.Combine(parent ?? c.Work, ".secondmonitor-maj-extrait");
            TryDeleteDirectory(fresh);
            TryDeleteDirectory(extract);
            try
            {
                ZipFile.ExtractToDirectory(c.File, extract, overwriteFiles: true);
                string root = Path.Combine(extract, "SecondMonitor");
                if (!Directory.Exists(root))
                {
                    root = Directory.GetDirectories(extract).FirstOrDefault() ?? throw new InvalidOperationException("Le zip est vide.");
                }

                CheckPackage(root, c, "SecondMonitorHub.exe", "SecondMonitor-Collecteur.exe", elf: false);
                Directory.Move(root, fresh);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                TryDeleteDirectory(fresh);
                return Manual($"SecondMonitor ne peut pas remplacer son dossier ({dir}) : le zip est ouvert, remplacez le dossier à la main.", c.File);
            }
            finally
            {
                TryDeleteDirectory(extract);
            }

            return WriteCmd(c, WindowsScript(c, dir, setup: false, fresh: fresh));
        }

        private static InstallPlan WriteCmd(InstallContext c, string text)
        {
            string path = Path.Combine(c.Work, "maj-installer.cmd");
            File.WriteAllText(path, text, new UTF8Encoding(false));
            return new InstallPlan { Script = path };
        }

        /// <summary>Le script .cmd de Windows (testable sur n'importe quel système).</summary>
        public static string WindowsScript(InstallContext c, string appDir, bool setup, string fresh)
        {
            // Dans un .cmd, % se double ; les valeurs sont entre guillemets : & ^ ( ) sont sans danger.
            static string V(string p) => (p ?? string.Empty).Replace("%", "%%");
            string me = c.App == "hub" ? "SecondMonitorHub.exe" : "SecondMonitor-Collecteur.exe";
            string other = c.App == "hub" ? "SecondMonitor-Collecteur.exe" : "SecondMonitorHub.exe";
            string otherArgs = other == "SecondMonitor-Collecteur.exe" ? " --reduit" : string.Empty;
            string args = string.Concat(c.Args.Select(a => " \"" + V(a.Replace("\"", string.Empty)) + "\""));
            var s = new StringBuilder();
            void L(string line) => s.Append(line).Append("\r\n");
            L("@echo off");
            L("rem Mise a jour de SecondMonitor (voir UpdateInstaller.cs). Fichier en UTF-8 : sans chcp, un accent dans le");
            L("rem nom d'utilisateur rend tous les chemins faux.");
            L("chcp 65001 >nul");
            L("setlocal");
            L($"set \"PID={c.Pid}\"");
            L($"set \"VERSION={V(UpdateVersion.Normalize(c.Version))}\"");
            L($"set \"APPDIR={V(appDir)}\"");
            L($"set \"MOI={me}\"");
            L($"set \"AUTRE={other}\"");
            L($"set \"INSTALLEUR={V(c.File)}\"");
            L($"set \"NOUVELLE={V(fresh)}\"");
            L($"set \"ANCIENNE={V(appDir)}.ancienne\"");
            L($"set \"SAUVEGARDE={V(Path.Combine(c.Work, "sauvegarde"))}\"");
            L($"set \"JOURNAL={V(UpdatePaths.Journal(c.Work))}\"");
            L($"set \"ECHEC={V(UpdatePaths.Failure(c.Work))}\"");
            L($"set \"MARQUE={V(UpdatePaths.StartMark(c.Work, c.App))}\"");
            L($"set \"DOSSIER={V(Path.GetDirectoryName(c.File))}\"");
            L("set AUTRE_OUVERT=0");
            L("echo %date% %time% mise a jour vers %VERSION% : attente de la fermeture (%PID%) >> \"%JOURNAL%\"");
            L("set N=0");
            L(":attendre");
            L("tasklist /FI \"PID eq %PID%\" /NH 2>nul | find \"%PID%\" >nul");
            L("if errorlevel 1 goto ferme");
            L("set /a N+=1");
            L($"if %N% GTR {c.ExitTimeout} goto abandon");
            L("ping -n 2 127.0.0.1 >nul");
            L("goto attendre");
            L(":abandon");
            L("echo %date% %time% l'application ne s'est pas fermee : mise a jour abandonnee >> \"%JOURNAL%\"");
            if (!setup)
            {
                L("rmdir /s /q \"%NOUVELLE%\" 2>nul");
            }

            L("exit /b 0");
            L(":ferme");
            L("rem L'autre application (collecteur ou SecondMonitor) : fermee proprement, puis de force, et relancee a la fin.");
            L("tasklist /FI \"IMAGENAME eq %AUTRE%\" /NH 2>nul | find /I \"%AUTRE%\" >nul");
            L("if errorlevel 1 goto autre_ferme");
            L("set AUTRE_OUVERT=1");
            L("taskkill /IM \"%AUTRE%\" >nul 2>&1");
            L("set N=0");
            L(":attendre_autre");
            L("tasklist /FI \"IMAGENAME eq %AUTRE%\" /NH 2>nul | find /I \"%AUTRE%\" >nul");
            L("if errorlevel 1 goto autre_ferme");
            L("set /a N+=1");
            L("if %N% GTR 10 taskkill /F /IM \"%AUTRE%\" >nul 2>&1");
            L("if %N% GTR 15 goto autre_ferme");
            L("ping -n 2 127.0.0.1 >nul");
            L("goto attendre_autre");
            L(":autre_ferme");
            L("del /q \"%MARQUE%\" 2>nul");
            if (setup)
            {
                L("rem Copie de l'installation actuelle : remise en place si l'installation ou le demarrage echoue.");
                L("robocopy \"%APPDIR%\" \"%SAUVEGARDE%\" /MIR /R:1 /W:1 /NFL /NDL /NJH /NJS /NP >nul");
                L("if errorlevel 8 (");
                L("  echo La copie de securite de l'installation actuelle a echoue : rien n'a ete modifie.> \"%ECHEC%\"");
                L("  goto relancer");
                L(")");
                L("start \"\" /wait \"%INSTALLEUR%\" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /NORESTARTAPPLICATIONS /LOG=\"%DOSSIER%\\installation.log\"");
                L("set CODE=%errorlevel%");
                L("echo %date% %time% installateur termine, code %CODE% >> \"%JOURNAL%\"");
                L("if not \"%CODE%\"==\"0\" (");
                L("  echo L'installateur a echoue ^(code %CODE%^) : l'ancienne version a ete remise.> \"%ECHEC%\"");
                L("  goto restaurer");
                L(")");
            }
            else
            {
                L("rmdir /s /q \"%ANCIENNE%\" 2>nul");
                L("move \"%APPDIR%\" \"%ANCIENNE%\" >nul 2>>\"%JOURNAL%\"");
                L("if errorlevel 1 (");
                L("  echo L'ancienne version n'a pas pu etre mise de cote ^(un fichier est ouvert^) : elle est restee en place.> \"%ECHEC%\"");
                L("  rmdir /s /q \"%NOUVELLE%\" 2>nul");
                L("  goto relancer");
                L(")");
                L("move \"%NOUVELLE%\" \"%APPDIR%\" >nul 2>>\"%JOURNAL%\"");
                L("if errorlevel 1 (");
                L("  echo La nouvelle version n'a pas pu etre mise en place : l'ancienne a ete remise.> \"%ECHEC%\"");
                L("  goto restaurer");
                L(")");
                L("rem Version « portable » : les reglages gardes a cote du programme suivent.");
                L("if exist \"%ANCIENNE%\\collecteur.json\" if not exist \"%APPDIR%\\collecteur.json\" copy /y \"%ANCIENNE%\\collecteur.json\" \"%APPDIR%\\\" >nul");
            }

            L("echo %date% %time% nouvelle version en place, lancement >> \"%JOURNAL%\"");
            L($"start \"\" /D \"%APPDIR%\" \"%APPDIR%\\%MOI%\"{args}");
            L($"if \"%AUTRE_OUVERT%\"==\"1\" if exist \"%APPDIR%\\%AUTRE%\" start \"\" /D \"%APPDIR%\" \"%APPDIR%\\%AUTRE%\"{otherArgs}");
            L("set N=0");
            L(":verifier");
            L("findstr /C:\"%VERSION%\" \"%MARQUE%\" >nul 2>&1");
            L("if not errorlevel 1 goto demarre");
            L("set /a N+=1");
            L($"if %N% GTR {c.StartTimeout} goto pas_demarre");
            L("ping -n 2 127.0.0.1 >nul");
            L("goto verifier");
            L(":demarre");
            L("ping -n 4 127.0.0.1 >nul");
            L("tasklist /FI \"IMAGENAME eq %MOI%\" /NH 2>nul | find /I \"%MOI%\" >nul");
            L("if errorlevel 1 goto pas_demarre");
            L(setup ? "rmdir /s /q \"%SAUVEGARDE%\" 2>nul" : "rmdir /s /q \"%ANCIENNE%\" 2>nul");
            L("rmdir /s /q \"%DOSSIER%\" 2>nul");
            L("del /q \"%ECHEC%\" 2>nul");
            L("echo %date% %time% mise a jour terminee >> \"%JOURNAL%\"");
            L("exit /b 0");
            L(":pas_demarre");
            L("echo La nouvelle version ne demarre pas : l'ancienne a ete remise.> \"%ECHEC%\"");
            L("taskkill /F /IM \"%MOI%\" >nul 2>&1");
            L("taskkill /F /IM \"%AUTRE%\" >nul 2>&1");
            L("ping -n 2 127.0.0.1 >nul");
            L(":restaurer");
            L("echo %date% %time% retour a l'ancienne version >> \"%JOURNAL%\"");
            if (setup)
            {
                L("robocopy \"%SAUVEGARDE%\" \"%APPDIR%\" /MIR /R:2 /W:1 /NFL /NDL /NJH /NJS /NP >nul");
            }
            else
            {
                L("if exist \"%ANCIENNE%\" (");
                L("  rmdir /s /q \"%APPDIR%\" 2>nul");
                L("  move \"%ANCIENNE%\" \"%APPDIR%\" >nul 2>>\"%JOURNAL%\"");
                L(")");
                L("rmdir /s /q \"%NOUVELLE%\" 2>nul");
            }

            L(":relancer");
            L($"start \"\" /D \"%APPDIR%\" \"%APPDIR%\\%MOI%\"{args}");
            L($"if \"%AUTRE_OUVERT%\"==\"1\" if exist \"%APPDIR%\\%AUTRE%\" start \"\" /D \"%APPDIR%\" \"%APPDIR%\\%AUTRE%\"{otherArgs}");
            L("exit /b 1");
            return s.ToString();
        }

        // ───────────────────────────── outils ─────────────────────────────

        private static InstallPlan Manual(string message, string open) => new() { Manual = message, OpenPath = open };

        /// <summary>'texte' pour le shell.</summary>
        public static string ShQ(string s) => "'" + (s ?? string.Empty).Replace("'", "'\\''") + "'";

        private static string Quote(IEnumerable<string> args) => string.Concat(args.Select(a => " " + ShQ(a)));

        [DllImport("libc", EntryPoint = "access", SetLastError = true)]
        private static extern int Access(string path, int mode);

        /// <summary>Le dossier peut-il recevoir et renommer des éléments (W_OK | X_OK) ?</summary>
        private static bool Writable(string dir)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                return false;
            }

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    string probe = Path.Combine(dir, $".sm-essai-{Guid.NewGuid():N}");
                    File.WriteAllText(probe, string.Empty);
                    File.Delete(probe);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            try
            {
                return Access(dir, 2 | 1) == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsExecutable(string path) =>
            File.Exists(path) && (OperatingSystem.IsWindows() || (File.GetUnixFileMode(path) & UnixFileMode.UserExecute) != 0);

        private static string Output(string program, params string[] args)
        {
            var start = new ProcessStartInfo(program) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            foreach (string a in args)
            {
                start.ArgumentList.Add(a);
            }

            using Process p = Process.Start(start) ?? throw new InvalidOperationException($"{program} introuvable");
            System.Threading.Tasks.Task<string> err = p.StandardError.ReadToEndAsync();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0)
            {
                // hdiutil ajoute un avertissement d'obsolescence : il n'apprend rien à l'utilisateur.
                string e = string.Join(" ", err.Result.Split('\n').Where(l => l.Trim().Length > 0 && !l.Contains("WARNING", StringComparison.Ordinal)));
                throw new InvalidOperationException($"{program} : {e}".Trim());
            }

            return output;
        }

        private static void Run(string program, params string[] args) => Output(program, args);

        private static bool TryRun(string program, params string[] args)
        {
            try
            {
                Output(program, args);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void TryDelete(string file)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception)
            {
                // Absent ou verrouillé.
            }
        }

        private static void TryDeleteDirectory(string dir)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
            catch (Exception)
            {
                // Verrouillé : il sera retiré à la prochaine tentative.
            }
        }
    }
}
