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
     *  Même système que Multibook et l'application ClickDeck (« Site web V2/docs/MISES-A-JOUR.md ») :
     *  un manifeste « schema 1 » lu sur deux sources, la version la plus haute l'emporte, une source injoignable est ignorée :
     *      le site OneDrop3D (si son adresse est connue) : https://<site>/api/mises-a-jour/secondmonitor
     *      GitHub : https://raw.githubusercontent.com/OneDrop3D/mises-a-jour/main/secondmonitor.json
     *  Les fichiers viennent des publications du dépôt commun, comme Multibook et ClickDeck :
     *      https://github.com/OneDrop3D/mises-a-jour/releases/download/secondmonitor-v<version>/<fichier>
     *  ou, pendant la transition (les versions jusqu'à 0.7.1 ne connaissent que lui), du dépôt de SecondMonitor :
     *      https://github.com/OneDrop3D/SecondMonitor/releases/download/v<version>/<fichier>
     *  ou du site lui-même (https://<site>/api/telechargements/<id>) quand c'est le site qui a donné le manifeste.
     *  Seules ces adresses https sont acceptées, et un fichier sans SHA-256 n'est jamais proposé.
     *  L'interface ne fournit jamais d'adresse : on ne télécharge que ce que le manifeste a donné.
     *  Un système dont le fichier est resté à une version précédente (« une plateforme absente de l'envoi garde son fichier »)
     *  se voit proposer la version de CE fichier, jamais une version qu'il ne pourrait pas installer.
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

        /// <summary>Publications du dépôt commun des produits OneDrop3D (Multibook, ClickDeck, ClickOS, SecondMonitor).</summary>
        public const string CommonReleasePrefix = "https://github.com/OneDrop3D/mises-a-jour/releases/download/";

        /// <summary>Publications du dépôt de SecondMonitor (versions publiées avant le passage au dépôt commun).</summary>
        public const string ReleasePrefix = "https://github.com/OneDrop3D/SecondMonitor/releases/download/";

        public static readonly string[] ReleasePrefixes = { CommonReleasePrefix, ReleasePrefix };

        /// <summary>
        /// Adresse du site OneDrop3D inscrite dans l'application (vide tant que le site n'est pas en ligne : seul GitHub est lu).
        /// Comme Multibook (MULTIBOOK_SITE_MAJ) et ClickDeck (CLICKDECK_UPDATE_SITE_URL), elle peut être donnée par l'environnement.
        /// </summary>
        public const string BuiltInSite = "";

        public const string SiteVariable = "SECONDMONITOR_SITE_MAJ";

        /// <summary>« https://site » (sans « / » final), ou null. Seulement en https, sans chemin ni identifiants.</summary>
        public static string Site => NormalizeSite(Environment.GetEnvironmentVariable(SiteVariable))
            ?? NormalizeSite(Environment.GetEnvironmentVariable("ONEDROP3D_SITE_URL"))
            ?? NormalizeSite(BuiltInSite);

        public static string NormalizeSite(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim().TrimEnd('/'), UriKind.Absolute, out Uri u)
                || u.Scheme != "https" || u.UserInfo.Length > 0 || (u.AbsolutePath != "/" && u.AbsolutePath.Length > 0) || u.Query.Length > 0)
            {
                return null;
            }

            return $"https://{u.Authority}";
        }

        /// <summary>Manifestes à lire, dans l'ordre (le site d'abord, puis GitHub) ; en mode test, le seul manifeste local.</summary>
        public static IReadOnlyList<string> Manifests
        {
            get
            {
                if (TestManifest is string test)
                {
                    return new[] { test };
                }

                return Site is string site ? new[] { $"{site}/api/mises-a-jour/secondmonitor", ManifestUrl } : new[] { ManifestUrl };
            }
        }

        /// <summary>Le site d'où vient ce manifeste (ses fichiers peuvent alors être servis par le site), sinon null.</summary>
        public static string SiteOf(string manifestUrl) =>
            Site is string site && manifestUrl != null && manifestUrl.StartsWith(site + "/", StringComparison.Ordinal) ? site : null;

        /// <summary>Version de l'étiquette d'une publication (« …/download/v0.6.0/… », « …/download/secondmonitor-v0.7.2/… »), sinon null.</summary>
        public static string TagVersion(string url)
        {
            Match m = Regex.Match(url ?? string.Empty, @"/releases/download/(?:[a-z0-9-]+-)?v(\d+(?:\.\d+){1,3}(?:-[0-9A-Za-z.]+)?)/", RegexOptions.CultureInvariant);
            return m.Success ? UpdateVersion.Normalize(m.Groups[1].Value) : null;
        }

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
        /// Le fichier à installer vient des publications du dépôt commun OneDrop3D/mises-a-jour ou de celui de SecondMonitor,
        /// ou du site qui a donné le manifeste (« /api/telechargements/&lt;id&gt; »), et de nulle part ailleurs :
        /// https, exactement « …/releases/download/&lt;étiquette&gt;/&lt;fichier&gt; », sans « .. », « % », « @ », « ? » ni « \ ».
        /// </summary>
        public static bool IsAllowedFileUrl(string url, string testOrigin = null, string siteOrigin = null)
        {
            if (string.IsNullOrWhiteSpace(url) || url.IndexOfAny(new[] { '\\', '%', '?', '#', '@', ' ' }) >= 0)
            {
                return false;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri u) || u.UserInfo.Length > 0)
            {
                return false;
            }

            if (testOrigin != null)
            {
                return url.StartsWith(testOrigin, StringComparison.Ordinal) && TwoSegments(url.Substring(testOrigin.Length));
            }

            // Fichier servi par le site OneDrop3D qui a donné le manifeste.
            if (siteOrigin != null && NormalizeSite(siteOrigin) is string site && url.StartsWith(site + "/api/telechargements/", StringComparison.Ordinal))
            {
                string id = url.Substring(site.Length + "/api/telechargements/".Length);
                return u.Scheme == "https" && Segment.IsMatch(id) && id != "." && id != "..";
            }

            string prefix = ReleasePrefixes.FirstOrDefault(p => url.StartsWith(p, StringComparison.Ordinal));
            return prefix != null && TwoSegments(url.Substring(prefix.Length)) && u.Scheme == "https" && u.Host == "github.com" && u.IsDefaultPort;
        }

        /// <summary>« étiquette/fichier » : deux segments simples, ni « . » ni « .. ».</summary>
        private static bool TwoSegments(string rest)
        {
            string[] parts = rest.Split('/');
            return parts.Length == 2 && parts.All(p => Segment.IsMatch(p) && p != "." && p != "..");
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

        /// <summary>Site OneDrop3D qui a donné ce fichier (ses adresses de téléchargement y sont permises), sinon null.</summary>
        public string Site { get; set; }
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
        public static UpdateOffer Parse(string json, string current, IReadOnlyList<string> keys, string testOrigin = null, string siteOrigin = null)
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

            string pageText = Text(m, "page");
            var offer = new UpdateOffer
            {
                Version = version,
                Current = UpdateVersion.Normalize(current),
                Date = Text(m, "date"),
                Notes = Text(m, "notes"),
                Page = pageText != null && (pageText.StartsWith("https://github.com/OneDrop3D/", StringComparison.Ordinal) || testOrigin != null
                    || (siteOrigin != null && pageText.StartsWith(UpdateSource.NormalizeSite(siteOrigin) + "/", StringComparison.Ordinal))) ? pageText : null,
                File = m.TryGetProperty("files", out JsonElement files) ? ChooseFile(files, keys, testOrigin, siteOrigin) : null,
            };

            // Le fichier de ce système est resté à une version précédente (pas encore reconstruit pour la dernière) :
            // on propose la version de CE fichier, ou rien si elle n'est pas plus récente (sinon : réinstaller la même, en boucle).
            if (offer.File != null && UpdateSource.TagVersion(offer.File.Url) is string fileVersion && UpdateVersion.Compare(fileVersion, version) < 0)
            {
                if (UpdateVersion.Compare(fileVersion, current) <= 0)
                {
                    return null;
                }

                offer.Notes = $"Version {fileVersion} pour ce système (la {version} n'y est pas encore disponible).";
                offer.Version = fileVersion;
            }

            return offer;
        }

        /// <summary>Le premier fichier valable parmi les clés de cette plateforme (adresse du dépôt + SHA-256 de 64 caractères).</summary>
        public static UpdateFile ChooseFile(JsonElement files, IReadOnlyList<string> keys, string testOrigin = null, string siteOrigin = null)
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
                // Le site donne parfois une adresse relative (« /api/telechargements/12 ») : elle se lit sur le site.
                if (siteOrigin != null && url != null && url.StartsWith("/", StringComparison.Ordinal) && !url.StartsWith("//", StringComparison.Ordinal))
                {
                    url = UpdateSource.NormalizeSite(siteOrigin) + url;
                }

                string sha = Text(f, "sha256")?.Trim().ToLowerInvariant();
                if (!UpdateSource.IsAllowedFileUrl(url, testOrigin, siteOrigin) || sha == null || !Regex.IsMatch(sha, "^[0-9a-f]{64}$"))
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
                    Site = siteOrigin == null ? null : UpdateSource.NormalizeSite(siteOrigin),
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

        /// <summary>
        /// Lit chaque source (site puis GitHub) et garde la version la plus haute. Une source injoignable ou illisible est
        /// ignorée ; si aucune ne répond, l'erreur de la dernière est rendue.
        /// </summary>
        public static Task<UpdateOffer> CheckAsync(HttpClient http, string current, IReadOnlyList<string> keys, CancellationToken ct) =>
            CheckAsync(http, current, keys, UpdateSource.Manifests, ct);

        public static async Task<UpdateOffer> CheckAsync(HttpClient http, string current, IReadOnlyList<string> keys, IReadOnlyList<string> manifests, CancellationToken ct)
        {
            UpdateOffer best = null;
            Exception lastError = null;
            bool anyRead = false;
            foreach (string url in manifests)
            {
                try
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeout.CancelAfter(TimeSpan.FromSeconds(15));
                    using HttpResponseMessage r = await http.GetAsync(url, timeout.Token).ConfigureAwait(false);
                    if (!r.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"Le serveur des mises à jour a répondu {(int)r.StatusCode}.");
                    }

                    string json = await r.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
                    UpdateOffer offer = Parse(json, current, keys, UpdateSource.TestOrigin, UpdateSource.SiteOf(url));
                    anyRead = true;
                    if (offer != null && (best == null || UpdateVersion.Compare(offer.Version, best.Version) > 0
                        || (UpdateVersion.Compare(offer.Version, best.Version) == 0 && best.File == null && offer.File != null)))
                    {
                        best = offer;
                    }
                }
                catch (Exception ex) when (!ct.IsCancellationRequested && ex is HttpRequestException or OperationCanceledException or FormatException or JsonException)
                {
                    lastError = ex is OperationCanceledException ? new HttpRequestException("Le serveur des mises à jour ne répond pas.") : ex;
                }
            }

            if (!anyRead && lastError != null)
            {
                throw lastError;
            }

            return best;
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
            if (file == null || !UpdateSource.IsAllowedFileUrl(file.Url, UpdateSource.TestOrigin, file.Site))
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

            // Gros fichier : quatre connexions en parallèle, chacune sur sa tranche (comme Multibook) ; sinon, ou si le serveur
            // refuse les plages, une seule connexion du début à la fin.
            if (!(file.Size >= ParallelMinBytes && await TryParallelAsync(http, file.Url, partial, file.Size, progress, ct).ConfigureAwait(false)))
            {
                await SingleStreamAsync(http, file, partial, name, progress, ct).ConfigureAwait(false);
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

        /// <summary>Une connexion, du début à la fin (60 s sans aucun octet : connexion morte).</summary>
        private static async Task SingleStreamAsync(HttpClient http, UpdateFile file, string partial, string name, Action<long, long> progress, CancellationToken ct)
        {
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
        }

        /// <summary>En dessous de cette taille, une seule connexion suffit.</summary>
        public const long ParallelMinBytes = 8L * 1024 * 1024;

        private const int Streams = 4;

        /// <summary>
        /// Quatre connexions, chacune sur sa tranche (en-tête Range). Faux (fichier partiel effacé) si le serveur ne répond pas
        /// 206 ou si une tranche échoue : le téléchargement repart alors sur une seule connexion.
        /// </summary>
        public static async Task<bool> TryParallelAsync(HttpClient http, string url, string partial, long size, Action<long, long> progress, CancellationToken ct)
        {
            long done = 0;
            using var all = CancellationTokenSource.CreateLinkedTokenSource(ct);
            try
            {
                using (var create = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    create.SetLength(size);
                }

                long part = (size + Streams - 1) / Streams;
                var tasks = new List<Task>();
                for (int i = 0; i < Streams; i++)
                {
                    long start = i * part;
                    long end = Math.Min(size, (i + 1) * part) - 1;
                    if (start > end)
                    {
                        break;
                    }

                    tasks.Add(Task.Run(async () =>
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Get, url);
                        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);
                        using HttpResponseMessage r = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, all.Token).ConfigureAwait(false);
                        if (r.StatusCode != System.Net.HttpStatusCode.PartialContent)
                        {
                            throw new NotSupportedException($"plages refusées ({(int)r.StatusCode})");
                        }

                        using Stream input = await r.Content.ReadAsStreamAsync(all.Token).ConfigureAwait(false);
                        using var output = new FileStream(partial, FileMode.Open, FileAccess.Write, FileShare.ReadWrite, 1 << 20);
                        output.Seek(start, SeekOrigin.Begin);
                        byte[] buffer = new byte[1 << 16];
                        long received = 0;
                        long expected = end - start + 1;
                        using var stall = CancellationTokenSource.CreateLinkedTokenSource(all.Token);
                        while (received < expected)
                        {
                            stall.CancelAfter(TimeSpan.FromSeconds(60));
                            int n = await input.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, expected - received)), stall.Token).ConfigureAwait(false);
                            if (n == 0)
                            {
                                break;
                            }

                            await output.WriteAsync(buffer.AsMemory(0, n), all.Token).ConfigureAwait(false);
                            received += n;
                            Interlocked.Add(ref done, n);
                        }

                        if (received != expected)
                        {
                            throw new IOException("tranche incomplète");
                        }
                    }, all.Token));
                }

                Task whole = Task.WhenAll(tasks);
                while (await Task.WhenAny(whole, Task.Delay(200, ct)).ConfigureAwait(false) != whole)
                {
                    progress?.Invoke(Interlocked.Read(ref done), size);
                }

                try
                {
                    await whole.ConfigureAwait(false);
                }
                catch (Exception)
                {
                    all.Cancel(); // une tranche a échoué : on arrête les autres
                    throw;
                }

                progress?.Invoke(size, size);
                return true;
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                all.Cancel();
                try
                {
                    File.Delete(partial);
                }
                catch (IOException)
                {
                    // encore ouvert par une tranche qui se termine : réécrit par la connexion unique
                }

                return false;
            }
        }

        private static string Text(JsonElement e, string name) =>
            e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    }
}
