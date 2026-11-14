namespace SecondMonitor.Hub.Tests;

using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using SecondMonitor.Hub.Core.Update;

/// <summary>Mises à jour : versions, choix du fichier par système, adresses acceptées, SHA-256, scripts de remplacement.</summary>
[TestFixture]
[NonParallelizable]
public class UpdateTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    private const string Base = "https://github.com/OneDrop3D/SecondMonitor/releases/download/v0.6.0/";

    [TestCase("0.10.0", "0.9.3", 1)]
    [TestCase("v1.2.0", "1.2", 0)]
    [TestCase("0.6.0.0", "0.6.0", 0)]
    [TestCase("2.0.0-beta.1", "2.0.0", -1)]
    [TestCase("2.0.0-beta.2", "2.0.0-beta.1", 1)]
    [TestCase("0.5.0", "0.5.1", -1)]
    public void Versions(string a, string b, int expected) => Assert.That(UpdateVersion.Compare(a, b), Is.EqualTo(expected));

    [Test]
    public void VersionNormalisee()
    {
        Assert.That(UpdateVersion.Normalize("0.5.0.0"), Is.EqualTo("0.5.0"));
        Assert.That(UpdateVersion.Normalize("v0.6.1"), Is.EqualTo("0.6.1"));
        Assert.That(UpdateVersion.Normalize("1.0.0-beta.1"), Is.EqualTo("1.0.0-beta.1"));
    }

    [Test]
    public void ClesParSysteme()
    {
        Assert.That(UpdatePlatform.Keys("mac", "arm64", false), Is.EqualTo(new[] { "macos-arm64" }));
        Assert.That(UpdatePlatform.Keys("mac", "x64", false), Is.EqualTo(new[] { "macos-x64" }));
        Assert.That(UpdatePlatform.Keys("windows", "x64", true), Is.EqualTo(new[] { "windows-x64", "windows-x64-zip" }));
        Assert.That(UpdatePlatform.Keys("windows", "x64", false), Is.EqualTo(new[] { "windows-x64-zip" }));
        Assert.That(UpdatePlatform.Keys("linux", "x64", false), Is.EqualTo(new[] { "linux-x64" }));
        Assert.That(UpdatePlatform.Keys("linux", "arm64", false), Is.Empty);
    }

    [Test]
    public void SeulLeDepotAttenduEstAccepte()
    {
        Assert.That(UpdateSource.IsAllowedFileUrl(Base + "SecondMonitor-0.6.0-mac-arm64.dmg"), Is.True);
        Assert.That(UpdateSource.IsAllowedFileUrl(Base + "SecondMonitor-0.6.0-windows-x64-installation.exe"), Is.True);
        foreach (string bad in new[]
        {
            null,
            "",
            "http://github.com/OneDrop3D/SecondMonitor/releases/download/v0.6.0/a.dmg",
            "https://github.com/Autre/SecondMonitor/releases/download/v0.6.0/a.dmg",
            "https://github.com/OneDrop3D/multibook/releases/download/v0.6.0/a.dmg",
            "https://github.com/OneDrop3D/mises-a-jour/a.dmg",
            "https://raw.githubusercontent.com/OneDrop3D/mises-a-jour/main/a.dmg",
            "https://github.com.evil.example/OneDrop3D/SecondMonitor/releases/download/v0.6.0/a.dmg",
            "https://github.com@evil.example/OneDrop3D/SecondMonitor/releases/download/v0.6.0/a.dmg",
            "https://x@github.com/OneDrop3D/SecondMonitor/releases/download/v0.6.0/a.dmg",
            "https://github.com:8443/OneDrop3D/SecondMonitor/releases/download/v0.6.0/a.dmg",
            Base + "../../../../Autre/depot/releases/download/v1/a.dmg",
            Base + "%2e%2e/%2e%2e/a.dmg",
            Base + "a.dmg?x=1",
            Base + "sous/dossier/a.dmg",
            Base + "..",
            Base,
            "https://onedrop3d.example/SecondMonitor.dmg",
        })
        {
            Assert.That(UpdateSource.IsAllowedFileUrl(bad), Is.False, bad ?? "(null)");
        }
    }

    [Test]
    public void ModeTestSeulementEnLocal()
    {
        Assert.That(UpdateSource.LocalOrigin("http://127.0.0.1:8123/secondmonitor.json"), Is.EqualTo("http://127.0.0.1:8123/"));
        Assert.That(UpdateSource.LocalOrigin("http://localhost:9/x.json"), Is.EqualTo("http://localhost:9/"));
        Assert.That(UpdateSource.LocalOrigin("http://192.168.1.20:8123/x.json"), Is.Null);
        Assert.That(UpdateSource.LocalOrigin("https://evil.example/x.json"), Is.Null);
        Assert.That(UpdateSource.IsAllowedFileUrl("http://127.0.0.1:8123/v0.6.0/a.dmg", "http://127.0.0.1:8123/"), Is.True);
        Assert.That(UpdateSource.IsAllowedFileUrl("http://127.0.0.1:9999/v0.6.0/a.dmg", "http://127.0.0.1:8123/"), Is.False);
    }

    private static string Manifest(string version, string files, string product = "secondmonitor", int schema = 1) =>
        $$"""{ "schema": {{schema}}, "product": "{{product}}", "version": "{{version}}", "date": "2026-11-20", "notes": "- nouveau", "page": "https://github.com/OneDrop3D/SecondMonitor/releases/tag/v{{version}}", "files": { {{files}} } }""";

    private static string File(string key, string name, string sha = Sha, long size = 1234) =>
        $$""" "{{key}}": { "url": "{{Base}}{{name}}", "name": "{{name}}", "size": {{size}}, "sha256": "{{sha}}" } """;

    [Test]
    public void ManifesteChoisitLeBonFichier()
    {
        string json = Manifest("0.6.0", string.Join(",",
            File("macos-arm64", "SecondMonitor-0.6.0-mac-arm64.dmg"),
            File("windows-x64", "SecondMonitor-0.6.0-windows-x64-installation.exe"),
            File("windows-x64-zip", "SecondMonitor-0.6.0-windows-x64.zip"),
            File("linux-x64", "SecondMonitor-0.6.0-linux-x64.tar.gz")));
        UpdateOffer mac = UpdateManifest.Parse(json, "0.5.0.0", UpdatePlatform.Keys("mac", "arm64", false));
        Assert.That(mac.Version, Is.EqualTo("0.6.0"));
        Assert.That(mac.Current, Is.EqualTo("0.5.0"));
        Assert.That(mac.File.Name, Is.EqualTo("SecondMonitor-0.6.0-mac-arm64.dmg"));
        Assert.That(mac.File.Size, Is.EqualTo(1234));
        Assert.That(mac.Page, Does.StartWith("https://github.com/OneDrop3D/"));
        Assert.That(UpdateManifest.Parse(json, "0.5.0", UpdatePlatform.Keys("windows", "x64", true)).File.Key, Is.EqualTo("windows-x64"));
        Assert.That(UpdateManifest.Parse(json, "0.5.0", UpdatePlatform.Keys("windows", "x64", false)).File.Key, Is.EqualTo("windows-x64-zip"));
        Assert.That(UpdateManifest.Parse(json, "0.5.0", UpdatePlatform.Keys("linux", "x64", false)).File.Name, Does.EndWith(".tar.gz"));
        // Pas de fichier Mac Intel : la version est annoncée, sans fichier (l'interface renvoie vers la page).
        UpdateOffer intel = UpdateManifest.Parse(json, "0.5.0", UpdatePlatform.Keys("mac", "x64", false));
        Assert.That(intel.Version, Is.EqualTo("0.6.0"));
        Assert.That(intel.File, Is.Null);
    }

    [Test]
    public void ManifesteRienDePlusRecent()
    {
        string json = Manifest("0.5.0", File("macos-arm64", "a.dmg"));
        Assert.That(UpdateManifest.Parse(json, "0.5.0", new[] { "macos-arm64" }), Is.Null);
        Assert.That(UpdateManifest.Parse(json, "0.6.0", new[] { "macos-arm64" }), Is.Null);
    }

    [Test]
    public void ManifesteFichierSansEmpreinteOuAilleursRefuse()
    {
        string sansSha = Manifest("0.6.0", File("macos-arm64", "a.dmg", sha: ""));
        Assert.That(UpdateManifest.Parse(sansSha, "0.5.0", new[] { "macos-arm64" }).File, Is.Null);
        string courte = Manifest("0.6.0", File("macos-arm64", "a.dmg", sha: "abcd"));
        Assert.That(UpdateManifest.Parse(courte, "0.5.0", new[] { "macos-arm64" }).File, Is.Null);
        string ailleurs = Manifest("0.6.0", """ "macos-arm64": { "url": "https://evil.example/a.dmg", "name": "a.dmg", "size": 1, "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef" } """);
        Assert.That(UpdateManifest.Parse(ailleurs, "0.5.0", new[] { "macos-arm64" }).File, Is.Null);
        Assert.Throws<FormatException>(() => UpdateManifest.Parse(Manifest("0.6.0", "", schema: 2), "0.5.0", new[] { "macos-arm64" }));
        Assert.Throws<FormatException>(() => UpdateManifest.Parse(Manifest("9.0.0", "", product: "multibook"), "0.5.0", new[] { "macos-arm64" }));
    }

    [Test]
    public void NomDeFichierSansChemin()
    {
        Assert.That(UpdateManifest.SafeName("../../etc/passwd"), Is.EqualTo("_.._etc_passwd"));
        Assert.That(UpdateManifest.SafeName("SecondMonitor-0.6.0-mac-arm64.dmg"), Is.EqualTo("SecondMonitor-0.6.0-mac-arm64.dmg"));
        Assert.That(UpdateManifest.SafeName(""), Is.EqualTo("mise-a-jour"));
    }

    [Test]
    public void Empreinte()
    {
        string f = Path.GetTempFileName();
        System.IO.File.WriteAllText(f, "SecondMonitor");
        Assert.That(UpdateManifest.Sha256Of(f), Is.EqualTo(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("SecondMonitor"))).ToLowerInvariant()));
        System.IO.File.Delete(f);
    }

    /// <summary>Téléchargement réel depuis un petit serveur local (mode test) : empreinte juste, puis empreinte fausse.</summary>
    [Test]
    public async Task TelechargementVerifie()
    {
        byte[] payload = new byte[300_000];
        new Random(4).NextBytes(payload);
        string sha = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
        int port = FreePort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();
        _ = Task.Run(async () =>
        {
            while (listener.IsListening)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await listener.GetContextAsync();
                }
                catch (Exception)
                {
                    return;
                }

                ctx.Response.ContentLength64 = payload.Length;
                await ctx.Response.OutputStream.WriteAsync(payload);
                ctx.Response.Close();
            }
        });

        string dir = Path.Combine(Path.GetTempPath(), "sm-maj-test-" + Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable(UpdateSource.TestManifestVariable, $"http://127.0.0.1:{port}/secondmonitor.json");
        try
        {
            using HttpClient http = UpdateManifest.CreateClient("0.5.0");
            var file = new UpdateFile { Url = $"http://127.0.0.1:{port}/v0.6.0/SecondMonitor-0.6.0-mac-arm64.dmg", Name = "SecondMonitor-0.6.0-mac-arm64.dmg", Size = payload.Length, Sha256 = sha };
            long seen = 0;
            string path = await UpdateManifest.DownloadAsync(http, file, dir, (done, _) => seen = done, CancellationToken.None);
            Assert.That(System.IO.File.ReadAllBytes(path), Is.EqualTo(payload));
            Assert.That(seen, Is.EqualTo(payload.Length));

            file.Sha256 = Sha; // empreinte publiée différente : le fichier est supprimé
            Assert.ThrowsAsync<InvalidDataException>(() => UpdateManifest.DownloadAsync(http, file, dir, null, CancellationToken.None));
            Assert.That(Directory.GetFiles(dir), Is.Empty);

            file.Url = "https://evil.example/v0.6.0/a.dmg"; // jamais une adresse hors du dépôt attendu
            Assert.ThrowsAsync<InvalidOperationException>(() => UpdateManifest.DownloadAsync(http, file, dir, null, CancellationToken.None));
        }
        finally
        {
            Environment.SetEnvironmentVariable(UpdateSource.TestManifestVariable, null);
            listener.Stop();
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void ScriptWindows()
    {
        var c = new InstallContext
        {
            File = @"C:\Users\Jean & Co\AppData\Local\SecondMonitor\mise-a-jour\telechargement\SecondMonitor-0.6.0-windows-x64-installation.exe",
            Version = "0.6.0",
            Current = "0.5.0",
            App = "collecteur",
            Exe = @"C:\Users\Jean & Co\AppData\Local\Programs\SecondMonitor\SecondMonitor-Collecteur.exe",
            Args = new[] { "--reduit" },
            Work = @"C:\Users\Jean & Co\AppData\Local\SecondMonitor\mise-a-jour",
            Pid = 4242,
        };
        string s = UpdateInstaller.WindowsScript(c, @"C:\Users\Jean & Co\AppData\Local\Programs\SecondMonitor", setup: true, fresh: null);
        Assert.That(s, Does.Contain("/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS"));
        Assert.That(s, Does.Contain("chcp 65001"));
        Assert.That(s, Does.Contain("set \"PID=4242\""));
        Assert.That(s, Does.Contain("set \"AUTRE=SecondMonitorHub.exe\""));
        Assert.That(s, Does.Contain("robocopy \"%APPDIR%\" \"%SAUVEGARDE%\" /MIR"));
        Assert.That(s, Does.Contain("\"%APPDIR%\\%MOI%\" \"--reduit\""));
        Assert.That(s, Does.Contain("\r\n"));
        Assert.That(s.Replace("\r\n", string.Empty), Does.Not.Contain("\n"));

        string zip = UpdateInstaller.WindowsScript(c, @"D:\Jeux\SecondMonitor", setup: false, fresh: @"D:\Jeux\SecondMonitor.nouvelle");
        Assert.That(zip, Does.Contain("move \"%NOUVELLE%\" \"%APPDIR%\""));
        Assert.That(zip, Does.Not.Contain("VERYSILENT"));
    }

    // ───── Le script macOS/Linux, exécuté pour de vrai sur de FAUX dossiers d'application ─────

    private sealed class Bench : IDisposable
    {
        public Bench(bool newStarts)
        {
            Root = Path.Combine(Path.GetTempPath(), "sm-maj-banc-" + Guid.NewGuid().ToString("N"));
            Target = Path.Combine(Root, "Applications", "secondmonitor");
            Fresh = Target + ".nouvelle";
            Work = Path.Combine(Root, "travail");
            Directory.CreateDirectory(Path.Combine(Work, "telechargement"));
            System.IO.File.WriteAllText(Path.Combine(Work, "telechargement", "SecondMonitor-0.6.0-linux-x64.tar.gz"), "archive");
            FakeApp(Target, "0.5.0", true);
            FakeApp(Fresh, "0.6.0", newStarts);
            Context = new InstallContext
            {
                File = Path.Combine(Work, "telechargement", "SecondMonitor-0.6.0-linux-x64.tar.gz"),
                Version = "0.6.0",
                Current = "0.5.0",
                App = "hub",
                Exe = Path.Combine(Target, "app.sh"),
                Work = Work,
                Pid = DeadPid(),
                ExitTimeout = 2,
                StartTimeout = 3,
                Environment = new Dictionary<string, string>(),
            };
        }

        public string Root { get; }

        public string Target { get; }

        public string Fresh { get; }

        public string Work { get; }

        public InstallContext Context { get; }

        /// <summary>Une fausse application : écrit sa version dans la marque de démarrage (ou pas), puis dort.</summary>
        private void FakeApp(string dir, string version, bool starts)
        {
            Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(Path.Combine(dir, "version.txt"), version);
            string mark = UpdatePaths.StartMark(Work, "hub");
            System.IO.File.WriteAllText(Path.Combine(dir, "app.sh"), starts ? $"#!/bin/sh\nprintf '{version}' > '{mark}'\nsleep 8\n" : "#!/bin/sh\nexit 1\n");
        }

        public string Run()
        {
            string launch = "(nohup /bin/sh \"$CIBLE_1/app.sh\" >/dev/null 2>&1 &)";
            string script = Path.Combine(Work, "maj-installer.sh");
            System.IO.File.WriteAllText(script, UpdateInstaller.UnixScriptForTest(Context, Target, Fresh, launch));
            using Process p = Process.Start("/bin/sh", script)!;
            p.WaitForExit(30000);
            return System.IO.File.ReadAllText(UpdatePaths.Journal(Work));
        }

        public string VersionIn(string dir) => System.IO.File.Exists(Path.Combine(dir, "version.txt")) ? System.IO.File.ReadAllText(Path.Combine(dir, "version.txt")) : "ABSENTE";

        public void Dispose()
        {
            // Le script de remplacement (ou l'application qu'il relance) peut encore écrire un instant dans le banc
            // (vu sous Linux, GitHub Actions) : on l'arrête de nouveau et on réessaie, jusqu'à 10 s.
            for (int i = 0; ; i++)
            {
                using (Process p = Process.Start("pkill", new[] { "-f", Root })!)
                {
                    p.WaitForExit(2000);
                }

                try
                {
                    Directory.Delete(Root, true);
                    return;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException && i < 40)
                {
                    Thread.Sleep(250);
                }
            }
        }

        private static int DeadPid()
        {
            using Process p = Process.Start("/usr/bin/true")!;
            p.WaitForExit();
            return p.Id;
        }
    }

    [Test]
    public void ScriptEchangeEtRelance()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("script macOS/Linux");
        }

        using var b = new Bench(newStarts: true);
        string journal = b.Run();
        Assert.That(b.VersionIn(b.Target), Is.EqualTo("0.6.0"), journal);
        Assert.That(Directory.Exists(b.Target + ".ancienne"), Is.False, "l'ancienne n'a pas été effacée");
        Assert.That(Directory.Exists(b.Fresh), Is.False);
        Assert.That(Directory.Exists(Path.GetDirectoryName(b.Context.File)), Is.False, "le téléchargement n'a pas été nettoyé");
        Assert.That(System.IO.File.Exists(UpdatePaths.Failure(b.Work)), Is.False);
        Assert.That(journal, Does.Contain("mise à jour terminée"));
    }

    [Test]
    public void ScriptRetourArriereSiLaNouvelleNeDemarrePas()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("script macOS/Linux");
        }

        using var b = new Bench(newStarts: false);
        string journal = b.Run();
        Assert.That(b.VersionIn(b.Target), Is.EqualTo("0.5.0"), journal);
        Assert.That(Directory.Exists(b.Target + ".ancienne"), Is.False);
        Assert.That(System.IO.File.ReadAllText(UpdatePaths.Failure(b.Work)), Does.Contain("ne démarre pas"));
        Assert.That(System.IO.File.Exists(b.Context.File), Is.True, "le fichier doit rester pour une installation à la main");
    }

    [Test]
    public void ScriptAbandonneSiLApplicationNeSeFermePas()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("script macOS/Linux");
        }

        using var b = new Bench(newStarts: true);
        using Process alive = Process.Start("sleep", "30")!;
        b.Context.Pid = alive.Id;
        b.Context.ExitTimeout = 1;
        b.Run();
        alive.Kill();
        Assert.That(b.VersionIn(b.Target), Is.EqualTo("0.5.0"));
        Assert.That(Directory.Exists(b.Fresh), Is.False);
    }

    // ───── Même système que Multibook et ClickDeck : dépôt commun, site, plusieurs sources, fichier resté en arrière ─────

    private const string Common = "https://github.com/OneDrop3D/mises-a-jour/releases/download/secondmonitor-v0.7.2/";

    [Test]
    public void DepotCommunEtSiteAcceptes()
    {
        Assert.That(UpdateSource.IsAllowedFileUrl(Common + "SecondMonitor-0.7.2-mac-arm64.dmg"), Is.True, "dépôt commun des produits OneDrop3D");
        Assert.That(UpdateSource.IsAllowedFileUrl(Base + "SecondMonitor-0.6.0-mac-arm64.dmg"), Is.True, "ancien dépôt, pour la transition");
        // Fichier servi par le site : seulement si c'est le site qui a donné le manifeste, en https, sur /api/telechargements/<id>.
        Assert.That(UpdateSource.IsAllowedFileUrl("https://onedrop3d.example/api/telechargements/42", siteOrigin: "https://onedrop3d.example"), Is.True);
        Assert.That(UpdateSource.IsAllowedFileUrl("https://onedrop3d.example/api/telechargements/42"), Is.False);
        Assert.That(UpdateSource.IsAllowedFileUrl("https://autre.example/api/telechargements/42", siteOrigin: "https://onedrop3d.example"), Is.False);
        Assert.That(UpdateSource.IsAllowedFileUrl("https://onedrop3d.example/api/telechargements/../x", siteOrigin: "https://onedrop3d.example"), Is.False);
        Assert.That(UpdateSource.IsAllowedFileUrl("https://onedrop3d.example/admin/42", siteOrigin: "https://onedrop3d.example"), Is.False);
        Assert.That(UpdateSource.NormalizeSite("http://onedrop3d.example"), Is.Null, "le site seulement en https");
        Assert.That(UpdateSource.NormalizeSite("https://onedrop3d.example/"), Is.EqualTo("https://onedrop3d.example"));
        Assert.That(UpdateSource.NormalizeSite("https://onedrop3d.example/chemin"), Is.Null);
    }

    [TestCase("https://github.com/OneDrop3D/SecondMonitor/releases/download/v0.6.0/a.exe", "0.6.0")]
    [TestCase("https://github.com/OneDrop3D/mises-a-jour/releases/download/secondmonitor-v0.7.2/a.dmg", "0.7.2")]
    [TestCase("https://github.com/OneDrop3D/mises-a-jour/releases/download/secondmonitor-v0.8.0-beta.1/a.dmg", "0.8.0-beta.1")]
    [TestCase("https://onedrop3d.example/api/telechargements/42", null)]
    public void VersionDeLEtiquette(string url, string version)
    {
        Assert.That(UpdateSource.TagVersion(url), Is.EqualTo(version));
    }

    [Test]
    public void FichierResteEnArrierePasDeBoucle()
    {
        // Manifeste 0.7.1, mais le fichier Windows est resté en 0.6.0 (pas reconstruit) : un PC en 0.6.0 n'a rien à installer.
        string json = Manifest("0.7.1", string.Join(",",
            File("macos-arm64", "SecondMonitor-0.7.1-mac-arm64.dmg").Replace("/v0.6.0/", "/v0.7.1/"),
            File("windows-x64", "SecondMonitor-0.6.0-windows-x64-installation.exe")));
        Assert.That(UpdateManifest.Parse(json, "0.6.0", UpdatePlatform.Keys("windows", "x64", true)), Is.Null);
        // Un PC plus ancien reçoit la 0.6.0, annoncée comme telle.
        UpdateOffer old = UpdateManifest.Parse(json, "0.5.0", UpdatePlatform.Keys("windows", "x64", true));
        Assert.That(old.Version, Is.EqualTo("0.6.0"));
        Assert.That(old.Notes, Does.Contain("0.7.1"));
        // Le Mac, lui, a bien la 0.7.1.
        Assert.That(UpdateManifest.Parse(json, "0.6.0", UpdatePlatform.Keys("mac", "arm64", false)).Version, Is.EqualTo("0.7.1"));
    }

    [Test]
    public void AdresseRelativeDuSite()
    {
        string json = Manifest("0.7.2", """ "macos-arm64": { "url": "/api/telechargements/42", "name": "SecondMonitor-0.7.2-mac-arm64.dmg", "size": 10, "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef" } """);
        UpdateOffer offer = UpdateManifest.Parse(json, "0.7.1", new[] { "macos-arm64" }, siteOrigin: "https://onedrop3d.example");
        Assert.That(offer.File.Url, Is.EqualTo("https://onedrop3d.example/api/telechargements/42"));
        Assert.That(offer.File.Site, Is.EqualTo("https://onedrop3d.example"));
        Assert.That(UpdateManifest.Parse(json, "0.7.1", new[] { "macos-arm64" }).File, Is.Null, "sans le site, l'adresse relative ne vaut rien");
    }

    /// <summary>Petit serveur local : chemin → (statut, corps) ; « plages » : répond 206 aux en-têtes Range.</summary>
    private static (HttpListener Listener, string Base) Serve(Func<string, (int Status, byte[] Body)> route, bool ranges = false)
    {
        int port = FreePort();
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();
        _ = Task.Run(async () =>
        {
            while (listener.IsListening)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await listener.GetContextAsync();
                }
                catch (Exception)
                {
                    return;
                }

                _ = Task.Run(() =>
                {
                    try
                    {
                        var (status, body) = route(ctx.Request.Url!.AbsolutePath);
                        string? range = ctx.Request.Headers["Range"];
                        if (ranges && status == 200 && range != null && range.StartsWith("bytes="))
                        {
                            string[] se = range[6..].Split('-');
                            long a = long.Parse(se[0]), b = long.Parse(se[1]);
                            ctx.Response.StatusCode = 206;
                            ctx.Response.ContentLength64 = b - a + 1;
                            ctx.Response.OutputStream.Write(body, (int)a, (int)(b - a + 1));
                        }
                        else
                        {
                            ctx.Response.StatusCode = status;
                            ctx.Response.ContentLength64 = body.Length;
                            ctx.Response.OutputStream.Write(body);
                        }

                        ctx.Response.Close();
                    }
                    catch (Exception)
                    {
                        // client parti
                    }
                });
            }
        });
        return (listener, $"http://127.0.0.1:{port}");
    }

    [Test]
    public async Task PlusieursSourcesLaPlusHauteLEmporte()
    {
        string site = Manifest("0.7.3", File("macos-arm64", "SecondMonitor-0.7.3-mac-arm64.dmg").Replace(Base, Common.Replace("0.7.2", "0.7.3")));
        string github = Manifest("0.7.2", File("macos-arm64", "SecondMonitor-0.7.2-mac-arm64.dmg").Replace(Base, Common));
        var (listener, root) = Serve(path => path switch
        {
            "/site.json" => (200, Encoding.UTF8.GetBytes(site)),
            "/github.json" => (200, Encoding.UTF8.GetBytes(github)),
            _ => (404, Array.Empty<byte>()),
        });
        using (listener)
        {
            using var http = UpdateManifest.CreateClient("0.7.1");
            var keys = new[] { "macos-arm64" };
            UpdateOffer best = await UpdateManifest.CheckAsync(http, "0.7.1", keys, new[] { root + "/site.json", root + "/github.json" }, CancellationToken.None);
            Assert.That(best.Version, Is.EqualTo("0.7.3"));
            // Une source injoignable est ignorée sans message.
            UpdateOffer one = await UpdateManifest.CheckAsync(http, "0.7.1", keys, new[] { root + "/absent.json", root + "/github.json" }, CancellationToken.None);
            Assert.That(one.Version, Is.EqualTo("0.7.2"));
            // À jour partout : rien.
            Assert.That(await UpdateManifest.CheckAsync(http, "0.7.3", keys, new[] { root + "/site.json", root + "/github.json" }, CancellationToken.None), Is.Null);
            // Aucune source : l'erreur remonte.
            Assert.ThrowsAsync<HttpRequestException>(() => UpdateManifest.CheckAsync(http, "0.7.1", keys, new[] { root + "/absent.json" }, CancellationToken.None));
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task TelechargementEnQuatreFlux(bool serverRanges)
    {
        byte[] payload = new byte[9 * 1024 * 1024 + 123];
        new Random(5).NextBytes(payload);
        var (listener, root) = Serve(path => path == "/gros.dmg" ? (200, payload) : (404, Array.Empty<byte>()), ranges: serverRanges);
        string dir = Path.Combine(Path.GetTempPath(), "sm-maj-flux-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            using (listener)
            {
                using var http = UpdateManifest.CreateClient("0.7.1");
                string partial = Path.Combine(dir, "gros.dmg.part");
                long last = 0;
                bool ok = await UpdateManifest.TryParallelAsync(http, root + "/gros.dmg", partial, payload.Length, (d, _) => last = d, CancellationToken.None);
                Assert.That(ok, Is.EqualTo(serverRanges), "sans plages, repli sur une seule connexion");
                if (serverRanges)
                {
                    Assert.That(System.IO.File.ReadAllBytes(partial), Is.EqualTo(payload));
                    Assert.That(last, Is.EqualTo(payload.Length));
                }
                else
                {
                    Assert.That(System.IO.File.Exists(partial), Is.False, "fichier partiel effacé");
                }
            }
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    private static int FreePort()
    {
        var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }
}
