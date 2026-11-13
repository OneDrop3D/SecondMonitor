namespace SecondMonitor.Hub.Core.Update
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    /*  Mises à jour de SecondMonitor (hub ET collecteur : ce fichier est lié dans les deux projets).
     *
     *  Même architecture que Multibook et l'application ClickDeck (« Site web V2/docs/MISES-A-JOUR.md ») :
     *  un manifeste « schema 1 » publié par OneDrop3D dans le dépôt public OneDrop3D/mises-a-jour,
     *      https://raw.githubusercontent.com/OneDrop3D/mises-a-jour/main/secondmonitor.json
     *  dont les fichiers sont dans les publications (préversions) du dépôt OneDrop3D/SecondMonitor :
     *      https://github.com/OneDrop3D/SecondMonitor/releases/download/v<version>/<fichier>
     *  Seules ces adresses https sont acceptées, et un fichier sans SHA-256 n'est jamais proposé.
     *  L'interface ne fournit jamais d'adresse : on ne télécharge que ce que le manifeste a donné.
     *
     *  Clés « files » propres à SecondMonitor (une archive contient toujours LES DEUX applications) :
     *      macos-arm64, macos-x64   image disque .dmg
     *      windows-x64              installateur Inno Setup (.exe)
     *      windows-x64-zip          dossier sans installation (.zip)
     *      linux-x64                archive .tar.gz avec installer.sh
     */

    /// <summary>D'où viennent les mises à jour, et le mode test (serveur local, variables d'environnement réservées aux essais).</summary>
    public static class UpdateSource
    {
        public const string ManifestUrl = "https://raw.githubusercontent.com/OneDrop3D/mises-a-jour/main/secondmonitor.json";

        public const string ReleasePrefix = "https://github.com/OneDrop3D/SecondMonitor/releases/download/";

        /// <summary>Essais seulement : adresse d'un manifeste servi PAR CET ORDINATEUR (http://127.0.0.1:port/…).</summary>
        public const string TestManifestVariable = "SM_MAJ_TEST_MANIFESTE";

        /// <summary>Essais seulement : dossier de travail (téléchargement, journal, script) au lieu du cache de l'utilisateur.</summary>
        public const string TestFolderVariable = "SM_MAJ_TEST_DOSSIER";

        /// <summary>Manifeste de test, s'il est défini ET local. Une adresse d'une autre machine est ignorée.</summary>
        public static string TestManifest
        {
            get
            {
                string v = Environment.GetEnvironmentVariable(TestManifestVariable);
                return LocalOrigin(v) != null ? v : null;
            }
        }

        public static bool TestMode => TestManifest != null;

        public static string Manifest => TestManifest ?? ManifestUrl;

        /// <summary>En mode test, les fichiers ne peuvent venir que du même serveur local que le manifeste.</summary>
        public static string TestOrigin => LocalOrigin(TestManifest);

        /// <summary>« http://127.0.0.1:1234/ » pour une adresse locale, sinon null.</summary>
        public static string LocalOrigin(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out Uri u))
            {
                return null;
            }

            bool local = u.Host is "127.0.0.1" or "localhost" or "[::1]" or "::1";
            if (!local || (u.Scheme != "http" && u.Scheme != "https") || u.UserInfo.Length > 0)
            {
                return null;
            }

            return $"{u.Scheme}://{u.Authority}/";
        }

        private static readonly Regex Segment = new("^[A-Za-z0-9][A-Za-z0-9._+-]*$", RegexOptions.CultureInvariant);

        /// <summary>
        /// Le fichier à installer vient des publications du dépôt OneDrop3D/SecondMonitor, et de nulle part ailleurs :
        /// https, exactement « …/releases/download/&lt;étiquette&gt;/&lt;fichier&gt; », sans « .. », « % », « @ », « ? » ni « \ ».
        /// </summary>
        public static bool IsAllowedFileUrl(string url, string testOrigin = null)
        {
            if (string.IsNullOrWhiteSpace(url) || url.IndexOfAny(new[] { '\\', '%', '?', '#', '@', ' ' }) >= 0)
            {
                return false;
            }

            string prefix = testOrigin ?? ReleasePrefix;
            if (!url.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            string[] parts = url.Substring(prefix.Length).Split('/');
            if (parts.Length != 2 || !parts.All(p => Segment.IsMatch(p) && p != "." && p != ".."))
            {
                return false;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri u) || u.UserInfo.Length > 0)
            {
                return false;
            }

            return testOrigin != null || (u.Scheme == "https" && u.Host == "github.com" && u.IsDefaultPort);
        }
    }

    /// <summary>Comparaison x.y.z, numéro par numéro ; une version « -beta » passe avant la version nue.</summary>
    public static class UpdateVersion
    {
        public static int Compare(string a, string b)
        {
            (List<long> na, string pa) = Split(a);
            (List<long> nb, string pb) = Split(b);
            for (int i = 0; i < Math.Max(na.Count, nb.Count); i++)
            {
                long x = i < na.Count ? na[i] : 0;
                long y = i < nb.Count ? nb[i] : 0;
                if (x != y)
                {
                    return x < y ? -1 : 1;
                }
            }

            if (pa.Length == 0 && pb.Length == 0)
            {
                return 0;
            }

            if (pa.Length == 0)
            {
                return 1;
            }

            if (pb.Length == 0)
            {
                return -1;
            }

            return Math.Sign(string.CompareOrdinal(pa, pb));
        }

        /// <summary>« v0.6.0 », « 0.6.0.0 » → « 0.6.0 » (trois numéros, comme les fichiers publiés).</summary>
        public static string Normalize(string v)
        {
            v = (v ?? string.Empty).Trim().TrimStart('v', 'V');
            int dash = v.IndexOf('-');
            string core = dash >= 0 ? v.Substring(0, dash) : v;
            string pre = dash >= 0 ? v.Substring(dash) : string.Empty;
            string[] n = core.Split('.');
            if (n.Length > 3 && n.Skip(3).All(x => x == "0"))
            {
                core = string.Join(".", n.Take(3));
            }

            return core + pre;
        }

        private static (List<long>, string) Split(string v)
        {
            v = (v ?? string.Empty).Trim().TrimStart('v', 'V');
            int dash = v.IndexOf('-');
            string core = dash >= 0 ? v.Substring(0, dash) : v;
            string pre = dash >= 0 ? v.Substring(dash + 1) : string.Empty;
            var numbers = core.Split('.').Select(x => long.TryParse(x, NumberStyles.None, CultureInfo.InvariantCulture, out long n) ? n : 0).ToList();
            return (numbers, pre);
        }
    }

    /// <summary>Le fichier de cette plateforme dans le manifeste.</summary>
    public sealed class UpdateFile
    {
        public string Key { get; set; }

        public string Url { get; set; }

        public string Name { get; set; }

        public long Size { get; set; }

        public string Sha256 { get; set; }
    }

    /// <summary>Une version plus récente que celle qui tourne.</summary>
    public sealed class UpdateOffer
    {
        public string Version { get; set; }

        public string Current { get; set; }

        public string Date { get; set; }

        public string Notes { get; set; }

        public string Page { get; set; }

        /// <summary>Null : pas de fichier pour ce système dans cette version (on renvoie vers la page).</summary>
        public UpdateFile File { get; set; }
    }

    public static class UpdatePlatform
    {
        /// <summary>
        /// Clés du manifeste à essayer, dans l'ordre, pour ce système. Windows installé par l'installateur (unins000.exe à côté
        /// du programme) : l'installateur, sinon le zip. Windows « portable » (zip décompressé) : le zip seulement, l'installateur
        /// irait s'installer ailleurs.
        /// </summary>
        public static IReadOnlyList<string> Keys(string os, string arch, bool installedWithSetup)
        {
            switch (os)
            {
                case "mac":
                    return new[] { arch == "x64" ? "macos-x64" : "macos-arm64" };
                case "windows":
                    return installedWithSetup ? new[] { "windows-x64", "windows-x64-zip" } : new[] { "windows-x64-zip" };
                case "linux":
                    return arch == "x64" ? new[] { "linux-x64" } : Array.Empty<string>();
                default:
                    return Array.Empty<string>();
            }
        }

        public static string CurrentOs => OperatingSystem.IsMacOS() ? "mac" : OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsLinux() ? "linux" : "autre";

        public static string CurrentArch => System.Runtime.InteropServices.RuntimeInformation.OSArchitecture switch
        {
            System.Runtime.InteropServices.Architecture.X64 => "x64",
            System.Runtime.InteropServices.Architecture.Arm64 => "arm64",
            _ => "autre",
        };

        /// <summary>Windows : installé par l'installateur Inno Setup (désinstalleur à côté du programme).</summary>
        public static bool InstalledWithSetup(string programDirectory) =>
            OperatingSystem.IsWindows() && File.Exists(Path.Combine(programDirectory, "unins000.exe"));

        public static IReadOnlyList<string> CurrentKeys(string programDirectory) =>
            Keys(CurrentOs, CurrentArch, InstalledWithSetup(programDirectory));
    }

    /// <summary>Lecture du manifeste, choix du fichier, téléchargement vérifié.</summary>
    public static class UpdateManifest
    {
        /// <summary>
        /// La version annoncée par le manifeste si elle est plus récente que <paramref name="current"/>, sinon null.
        /// Le fichier n'est retenu que si son adresse est celle du dépôt attendu et qu'il a un SHA-256.
        /// </summary>
        public static UpdateOffer Parse(string json, string current, IReadOnlyList<string> keys, string testOrigin = null)
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement m = doc.RootElement;
            if (m.ValueKind != JsonValueKind.Object || !m.TryGetProperty("schema", out JsonElement schema)
                || schema.ValueKind != JsonValueKind.Number || schema.GetInt32() != 1
                || Text(m, "version") is not string version || version.Length == 0)
            {
                throw new FormatException("Manifeste de mise à jour illisible (format inconnu).");
            }

            string product = Text(m, "product");
            if (product != null && product != "secondmonitor")
            {
                throw new FormatException($"Ce manifeste est celui de « {product} », pas de SecondMonitor.");
            }

            version = UpdateVersion.Normalize(version);
            if (UpdateVersion.Compare(version, current) <= 0)
            {
                return null;
            }

            return new UpdateOffer
            {
                Version = version,
                Current = UpdateVersion.Normalize(current),
                Date = Text(m, "date"),
                Notes = Text(m, "notes"),
                Page = Text(m, "page") is string page && (page.StartsWith("https://github.com/OneDrop3D/", StringComparison.Ordinal) || testOrigin != null) ? page : null,
                File = m.TryGetProperty("files", out JsonElement files) ? ChooseFile(files, keys, testOrigin) : null,
            };
        }

        /// <summary>Le premier fichier valable parmi les clés de cette plateforme (adresse du dépôt + SHA-256 de 64 caractères).</summary>
        public static UpdateFile ChooseFile(JsonElement files, IReadOnlyList<string> keys, string testOrigin = null)
        {
            if (files.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (string key in keys)
            {
                if (!files.TryGetProperty(key, out JsonElement f) || f.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                string url = Text(f, "url");
                string sha = Text(f, "sha256")?.Trim().ToLowerInvariant();
                if (!UpdateSource.IsAllowedFileUrl(url, testOrigin) || sha == null || !Regex.IsMatch(sha, "^[0-9a-f]{64}$"))
                {
                    continue;
                }

                string name = Text(f, "name");
                return new UpdateFile
                {
                    Key = key,
                    Url = url,
                    Name = string.IsNullOrWhiteSpace(name) ? url.Substring(url.LastIndexOf('/') + 1) : name,
                    Size = f.TryGetProperty("size", out JsonElement s) && s.ValueKind == JsonValueKind.Number && s.TryGetInt64(out long size) ? size : 0,
                    Sha256 = sha,
                };
            }

            return null;
        }

        public static HttpClient CreateClient(string userAgentVersion)
        {
            var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true, MaxAutomaticRedirections = 5 })
            {
                Timeout = Timeout.InfiniteTimeSpan,
            };
            http.DefaultRequestHeaders.UserAgent.ParseAdd($"SecondMonitor/{UpdateVersion.Normalize(userAgentVersion)}");
            return http;
        }

        public static async Task<UpdateOffer> CheckAsync(HttpClient http, string current, IReadOnlyList<string> keys, CancellationToken ct)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            using HttpResponseMessage r = await http.GetAsync(UpdateSource.Manifest, timeout.Token).ConfigureAwait(false);
            if (!r.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Le serveur des mises à jour a répondu {(int)r.StatusCode}.");
            }

            string json = await r.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
            return Parse(json, current, keys, UpdateSource.TestOrigin);
        }

        /// <summary>Un simple nom de fichier, jamais un chemin.</summary>
        public static string SafeName(string name)
        {
            string n = new string((name ?? "mise-a-jour").Select(c => char.IsLetterOrDigit(c) || ".-_+".IndexOf(c) >= 0 ? c : '_').ToArray()).TrimStart('.');
            return n.Length == 0 ? "mise-a-jour" : n;
        }

        public static string Sha256Of(string path)
        {
            using FileStream f = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(f)).ToLowerInvariant();
        }

        /// <summary>
        /// Télécharge le fichier dans <paramref name="folder"/> (vidé d'abord), puis vérifie sa taille et son SHA-256 :
        /// un fichier qui ne correspond pas est supprimé avant toute ouverture. Pas de quarantaine macOS : rien ne la pose ici.
        /// </summary>
        public static async Task<string> DownloadAsync(HttpClient http, UpdateFile file, string folder, Action<long, long> progress, CancellationToken ct)
        {
            if (file == null || !UpdateSource.IsAllowedFileUrl(file.Url, UpdateSource.TestOrigin))
            {
                throw new InvalidOperationException("Aucun fichier de mise à jour valable pour ce système.");
            }

            try
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
            catch (IOException)
            {
                // Un reste verrouillé : on écrit à côté.
            }

            Directory.CreateDirectory(folder);
            string name = SafeName(file.Name);
            string target = Path.Combine(folder, name);
            string partial = target + ".part";

            using (HttpResponseMessage r = await http.GetAsync(file.Url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
            {
                if (!r.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Le serveur a répondu {(int)r.StatusCode} pour {name}.");
                }

                long total = r.Content.Headers.ContentLength ?? file.Size;
                using Stream input = await r.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                using var output = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20);
                byte[] buffer = new byte[1 << 16];
                long done = 0;
                long reported = 0;
                using var stall = CancellationTokenSource.CreateLinkedTokenSource(ct);
                while (true)
                {
                    // 60 s sans aucun octet : connexion morte.
                    stall.CancelAfter(TimeSpan.FromSeconds(60));
                    int n = await input.ReadAsync(buffer, stall.Token).ConfigureAwait(false);
                    if (n == 0)
                    {
                        break;
                    }

                    await output.WriteAsync(buffer.AsMemory(0, n), ct).ConfigureAwait(false);
                    done += n;
                    if (done - reported > 256 * 1024)
                    {
                        reported = done;
                        progress?.Invoke(done, total);
                    }
                }

                progress?.Invoke(done, total);
            }

            long length = new FileInfo(partial).Length;
            string sha = Sha256Of(partial);
            if ((file.Size > 0 && length != file.Size) || sha != file.Sha256)
            {
                File.Delete(partial);
                throw new InvalidDataException("Le fichier reçu ne correspond pas à l'empreinte publiée : il a été supprimé.");
            }

            File.Move(partial, target, true);
            return target;
        }

        private static string Text(JsonElement e, string name) =>
            e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    }
}
