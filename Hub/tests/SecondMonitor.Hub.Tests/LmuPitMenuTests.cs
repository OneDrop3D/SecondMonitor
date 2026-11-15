namespace SecondMonitor.Hub.Tests;

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using NUnit.Framework;
using SecondMonitor.Collector;
using SecondMonitor.Hub.Core.Ai;

/// <summary>
/// Menu des stands de Le Mans Ultimate : choix des lignes (pneus, réparation, énergie, carburant) et échange complet
/// lecture → envoi → relecture avec un faux LMU local qui répond comme le vrai jeu.
/// Les menus imitent de vraies réponses de /rest/garage/PitMenu/receivePitMenu :
/// - CrewChief, CrewChiefV4/LMU/LMU_receivePitMenu_REST_API_contents(_VE).json : clés « PMC Value », currentSetting, default, name,
///   settings [{ text, type, isUsed }], textes à espace final (« New Medium  »), « Used Medium  -1% » sur les roues, « N/A » sans dégâts,
///   « 0% 0 laps » … « 100% 20 laps », ratio « 0.01 » … « 1.20 », carburant « 5L/0 laps », pressions « 141 (+1) ».
/// - LMULapTime, docs/LMU_REST_API.md : DAMAGE « Do Not Repair / Repair Body / Repair All », TIRES avec « Mixed Tyres ».
/// - Apexandchilloverlaysystem, src/telemetry/mfdControl.ts : emplacements « INVALID », « TIRES: » seul ignoré par le jeu.
/// - KoDriver, docs/lmu-windows-rest-api.md : réponses en « text/plain » sans charset.
/// </summary>
[TestFixture]
[NonParallelizable]
public class LmuPitMenuTests
{
    private const int Hyper = 0;
    private const int Lmp2 = 1;

    /// <summary>Menu d'une hypercar (énergie virtuelle) ou d'une LMP2 (carburant), au format du jeu.</summary>
    internal static JsonArray Menu(int car = Hyper, bool damage = true, bool relativeFuel = false)
    {
        var rows = new JsonArray();
        int pmc = 1;
        JsonObject Row(string name, int current, IEnumerable<JsonObject> settings) =>
            new() { ["PMC Value"] = pmc++, ["currentSetting"] = current, ["default"] = current, ["name"] = name, ["settings"] = new JsonArray(settings.ToArray<JsonNode>()) };
        JsonObject T(string text) => new() { ["text"] = text };
        JsonObject Tyre(string text, string type, bool used = false) => new() { ["isUsed"] = used, ["text"] = text, ["type"] = type };

        rows.Add(damage
            ? Row("DAMAGE:", 0, new[] { T("Do Not Repair"), T("Repair Body"), T("Repair All") })
            : Row("DAMAGE:", 0, new[] { T("N/A") }));
        rows.Add(Row("DRIVER:", 0, new[] { T("Sébastien Loeb") }));
        if (car == Hyper)
        {
            rows.Add(Row("VIRTUAL ENERGY:", 100, Enumerable.Range(0, 101).Select(i => T($"{i}% {i / 5} laps"))));
            rows.Add(Row("FUEL RATIO:", 78, Enumerable.Range(1, 120).Select(i => T((i / 100.0).ToString("0.00", CultureInfo.InvariantCulture)))));
        }
        else
        {
            rows.Add(Row("FUEL:", 45, Enumerable.Range(relativeFuel ? 0 : 5, 59).Select(i => T($"{(relativeFuel ? "+" : string.Empty)}{i}L/{i / 10} laps"))));
        }

        rows.Add(Row("TIRES:", 0, new[]
        {
            T("No Change"), Tyre("New Soft ", "Soft"), Tyre("New Medium ", "Medium"), T("INVALID"), Tyre("New Wet ", "Wet"), Tyre("Used Soft ", "Soft", true), T("Mixed Tyres"),
        }));
        foreach (string c in new[] { "FL", "FR", "RL", "RR" })
        {
            // Chaque roue a sa propre liste : à l'arrière droit, le train usé vient avant le medium neuf.
            rows.Add(Row($"{c} TIRE:", 0, c == "RR"
                ? new[] { T("No Change"), Tyre("New Soft ", "Soft"), Tyre("Used Soft  -1%", "Soft", true), Tyre("New Medium ", "Medium"), T("INVALID"), Tyre("New Wet ", "Wet") }
                : new[] { T("No Change"), Tyre("New Soft ", "Soft"), Tyre("New Medium ", "Medium"), T("INVALID"), Tyre("New Wet ", "Wet"), Tyre("Used Soft  -1%", "Soft", true) }));
        }

        rows.Add(Row("R WING:", 5, new[] { "-1.6 deg (-5)", "0.4 deg (-4)", "2.4 deg (-3)", "4.4 deg (-2)", "6.4 deg (-1)", "8.4 deg", "10.4 deg (+1)" }.Select(T)));
        foreach (string c in new[] { "FL", "FR", "RL", "RR" })
        {
            rows.Add(Row($"{c} PRESS:", 0, Enumerable.Range(140, 71).Select(p => T(p == 140 ? "140" : $"{p} (+{p - 140})"))));
        }

        rows.Add(Row("REPLACE BRAKES:", 0, new[] { T("No"), T("Yes") }));
        return rows;
    }

    private static JsonElement Order(object o) => JsonSerializer.SerializeToElement(o);

    private static JsonNode R(JsonArray m, string name) => m.First(r => r!["name"]!.GetValue<string>() == name)!;

    private static int At(JsonArray m, string name) => R(m, name)["currentSetting"]!.GetValue<int>();

    private static string Choice(JsonArray m, string name) => R(m, name)["settings"]![At(m, name)]!["text"]!.GetValue<string>().Trim();

    [TearDown]
    public void Reset()
    {
        LmuPitMenu.UseAddress(6397, "127.0.0.1", "localhost");
        LmuPitMenu.Log = null;
        LmuPitMenu.DumpFile = null;
    }

    [Test]
    public void Plan_SetsSoftsRepairAndEnergy_OnEveryWheelByCompound()
    {
        JsonArray m = Menu();
        var (done, missing) = LmuPitMenu.Plan(m, Order(new { tyres = "Soft", repair = true, energyTargetPct = 78.0 }));
        Assert.That(Choice(m, "TIRES:"), Is.EqualTo("New Soft"));
        foreach (string c in new[] { "FL", "FR", "RL", "RR" })
        {
            Assert.That(Choice(m, $"{c} TIRE:"), Is.EqualTo("New Soft"), c);
        }

        Assert.That(Choice(m, "DAMAGE:"), Is.EqualTo("Repair All"));
        Assert.That(At(m, "VIRTUAL ENERGY:"), Is.EqualTo(78), "currentSetting = pourcentage (TinyPedal, CrewChief)");
        Assert.That(done, Is.EqualTo(new[] { "pneus soft neufs", "réparation", "énergie 78 %" }));
        Assert.That(missing, Is.Empty);
    }

    [Test]
    public void Plan_MediumIsPickedByTypeEvenWhenEachWheelListsItElsewhere()
    {
        JsonArray m = Menu();
        LmuPitMenu.Plan(m, Order(new { tyres = "Medium" }));
        Assert.That(At(m, "FL TIRE:"), Is.EqualTo(2));
        Assert.That(At(m, "RR TIRE:"), Is.EqualTo(3), "pas le train usé qui occupe l'index 2 à l'arrière droit");
        Assert.That(Choice(m, "RR TIRE:"), Is.EqualTo("New Medium"));
    }

    [Test]
    public void Plan_NewTyres_TakeTheFittedCompoundFromTelemetry()
    {
        JsonArray m = Menu();
        var (done, _) = LmuPitMenu.Plan(m, Order(new { tyres = "neufs" }), new LmuPitMenu.CarNow("Medium", "Medium", 40));
        Assert.That(Choice(m, "TIRES:"), Is.EqualTo("New Medium"));
        Assert.That(Choice(m, "RR TIRE:"), Is.EqualTo("New Medium"));
        Assert.That(done.Single(), Is.EqualTo("pneus medium neufs"));
    }

    [Test]
    public void Plan_NewTyres_KeepEachAxleCompound()
    {
        JsonArray m = Menu();
        var (done, _) = LmuPitMenu.Plan(m, Order(new { tyres = "neufs" }), new LmuPitMenu.CarNow("Soft", "Medium", null));
        Assert.That(Choice(m, "FL TIRE:"), Is.EqualTo("New Soft"));
        Assert.That(Choice(m, "FR TIRE:"), Is.EqualTo("New Soft"));
        Assert.That(Choice(m, "RL TIRE:"), Is.EqualTo("New Medium"));
        Assert.That(Choice(m, "RR TIRE:"), Is.EqualTo("New Medium"));
        Assert.That(Choice(m, "TIRES:"), Is.EqualTo("Mixed Tyres"));
        Assert.That(done.Single(), Is.EqualTo("pneus neufs (soft avant, medium arrière)"));
    }

    [Test]
    public void Plan_NewTyres_WithoutTelemetry_KeepTheChosenCompoundAndSkipUsedSets()
    {
        JsonArray m = Menu();
        R(m, "TIRES:")["currentSetting"] = 5; // choisi dans le menu : soft usé
        LmuPitMenu.Plan(m, Order(new { tyres = "neufs" }));
        Assert.That(Choice(m, "TIRES:"), Is.EqualTo("New Soft"), "soft neuf, pas le train usé");
    }

    [Test]
    public void Plan_NewTyres_UnknownCompound_ChangesNothing()
    {
        JsonArray m = Menu();
        var (done, missing) = LmuPitMenu.Plan(m, Order(new { tyres = "neufs" }));
        Assert.That(done, Is.Empty);
        Assert.That(missing.Single(), Does.Contain("gomme montée inconnue"));
        Assert.That(At(m, "TIRES:"), Is.Zero);
        Assert.That(At(m, "FL TIRE:"), Is.Zero);
    }

    [Test]
    public void Plan_Rain_PicksTheWetSet_NeverInvalidOrMixed()
    {
        JsonArray m = Menu();
        LmuPitMenu.Plan(m, Order(new { tyres = "Pluie" }));
        Assert.That(Choice(m, "TIRES:"), Is.EqualTo("New Wet"));
        Assert.That(Choice(m, "RR TIRE:"), Is.EqualTo("New Wet"));
    }

    [Test]
    public void Plan_SaysWhatIsMissing_AndLeavesTheTyresAlone()
    {
        JsonArray m = Menu();
        var (done, missing) = LmuPitMenu.Plan(m, Order(new { tyres = "Hard" }));
        Assert.That(done, Is.Empty);
        Assert.That(missing.Single(), Does.Contain("hard"));
        Assert.That(At(m, "TIRES:") + At(m, "FL TIRE:") + At(m, "RR TIRE:"), Is.Zero);
    }

    [Test]
    public void Plan_NoTyres_PutsEveryTyreRowOnNoChange()
    {
        JsonArray m = Menu();
        foreach (string n in new[] { "TIRES:", "FL TIRE:", "FR TIRE:", "RL TIRE:", "RR TIRE:" })
        {
            R(m, n)["currentSetting"] = 1;
        }

        var (done, _) = LmuPitMenu.Plan(m, Order(new { noTyres = true }));
        Assert.That(done.Single(), Is.EqualTo("pas de pneus"));
        Assert.That(new[] { "TIRES:", "FL TIRE:", "FR TIRE:", "RL TIRE:", "RR TIRE:" }.Sum(n => At(m, n)), Is.Zero);
    }

    [Test]
    public void Plan_RepairWithoutDamage_SaysNothingToRepair()
    {
        JsonArray m = Menu(damage: false);
        var (done, missing) = LmuPitMenu.Plan(m, Order(new { repair = true }));
        Assert.That(done, Is.Empty);
        Assert.That(missing.Single(), Is.EqualTo("réparation (rien à réparer)"));
    }

    [Test]
    public void Plan_LitresOnAnEnergyCar_GoThroughTheFuelRatio()
    {
        JsonArray m = Menu(); // ratio 0.79
        var (done, _) = LmuPitMenu.Plan(m, Order(new { fuelTargetLitres = 47.4 }));
        Assert.That(At(m, "VIRTUAL ENERGY:"), Is.EqualTo(60), "47,4 L / 0,79 = 60 %");
        Assert.That(done.Single(), Is.EqualTo("énergie 60 %"));
    }

    [Test]
    public void Plan_FuelRatio_PicksTheClosestChoice_BeforeTheEnergy()
    {
        JsonArray m = Menu(); // ratio 0.79
        var (done, missing) = LmuPitMenu.Plan(m, Order(new { fuelRatio = 0.94, energyTargetPct = 100.0 }));
        Assert.That(Choice(m, "FUEL RATIO:"), Is.EqualTo("0.94"));
        Assert.That(done, Does.Contain("ratio 0,94"));
        Assert.That(missing, Is.Empty);

        // Des litres seuls passent par le NOUVEAU ratio : 47 L / 0,94 = 50 %.
        JsonArray m2 = Menu();
        LmuPitMenu.Plan(m2, Order(new { fuelRatio = 0.94, fuelTargetLitres = 47.0 }));
        Assert.That(At(m2, "VIRTUAL ENERGY:"), Is.EqualTo(50));

        JsonArray lmp2 = Menu(Lmp2);
        var (_, miss2) = LmuPitMenu.Plan(lmp2, Order(new { fuelRatio = 0.94 }));
        Assert.That(miss2.Single(), Does.StartWith("ratio (pas de ligne FUEL RATIO"));
    }

    [Test]
    public void Plan_FullTank_TakesTheTopOfTheList()
    {
        JsonArray hyper = Menu();
        R(hyper, "VIRTUAL ENERGY:")["currentSetting"] = 20;
        LmuPitMenu.Plan(hyper, Order(new { energyTargetPct = 100.0, fuelTargetLitres = 9999.0 }));
        Assert.That(At(hyper, "VIRTUAL ENERGY:"), Is.EqualTo(100));

        JsonArray lmp2 = Menu(Lmp2);
        LmuPitMenu.Plan(lmp2, Order(new { fuelTargetLitres = 9999.0 }));
        Assert.That(Choice(lmp2, "FUEL:"), Is.EqualTo("63L/6 laps"));
    }

    [Test]
    public void Plan_FuelRow_IsTheLevelAfterTheStop()
    {
        JsonArray m = Menu(Lmp2);
        var (done, _) = LmuPitMenu.Plan(m, Order(new { fuelTargetLitres = 39.6 }), new LmuPitMenu.CarNow("Medium", "Medium", 12));
        Assert.That(Choice(m, "FUEL:"), Is.EqualTo("40L/4 laps"));
        Assert.That(done.Single(), Is.EqualTo("carburant 40 L"));
    }

    [Test]
    public void Plan_RelativeFuelStrategy_AddsOnlyWhatIsMissing()
    {
        JsonArray m = Menu(Lmp2, relativeFuel: true);
        var (done, _) = LmuPitMenu.Plan(m, Order(new { fuelTargetLitres = 50.0 }), new LmuPitMenu.CarNow("Medium", "Medium", 30));
        Assert.That(Choice(m, "FUEL:"), Is.EqualTo("+20L/2 laps"));
        Assert.That(done.Single(), Is.EqualTo("carburant 20 L ajoutés"));

        JsonArray blind = Menu(Lmp2, relativeFuel: true);
        var (none, missing) = LmuPitMenu.Plan(blind, Order(new { fuelTargetLitres = 50.0 }));
        Assert.That(none, Is.Empty);
        Assert.That(missing.Single(), Does.Contain("litres à ajouter"));
    }

    [TestCase("New Soft ", "soft neufs")]
    [TestCase("Used Medium  -1%", "medium usés")]
    [TestCase("New Wet ", "pluie neufs")]
    [TestCase("Neuf Mouillé", "pluie neufs")]
    [TestCase("Neuf Medium", "medium neufs")]
    public void Pretty_SaysTheGameLabelsInFrench(string text, string expected)
    {
        Assert.That(LmuPitMenu.Pretty(text), Is.EqualTo(expected));
    }

    [Test]
    public void Describe_ListsEveryRowWithItsChoices()
    {
        List<string> lines = LmuPitMenu.Describe(Menu());
        Assert.That(lines, Has.Count.EqualTo(Menu().Count));
        string tyres = lines.Single(l => l.StartsWith("• TIRES:"));
        Assert.That(tyres, Does.Contain("1 New Soft (Soft)").And.Contain("5 Used Soft (Soft, usé)").And.Contain("6 Mixed Tyres"));
        Assert.That(lines.Single(l => l.StartsWith("• VIRTUAL ENERGY:")), Does.Contain("= 100 « 100% 20 laps »").And.Contain("101 choix").And.Contain("…"));
    }

    // ───── Échanges avec un faux Le Mans Ultimate ─────

    [Test]
    public async Task Api_TestDescribesTheMenu_AndApplyIsReadPostRead()
    {
        using var lmu = FakeLmu.Start(Menu());
        var log = new List<string>();
        LmuPitMenu.Log = log.Add;
        string dump = Path.Combine(Path.GetTempPath(), $"menu-stands-lmu-{Guid.NewGuid():N}.txt");
        LmuPitMenu.DumpFile = dump;
        try
        {
            LmuPitMenu.Inspection test = await LmuPitMenu.InspectAsync(lmu.Token, new LmuPitMenu.CarNow("Soft", "Soft", 61.2));
            Assert.That(test.Ok, Is.True, test.Detail);
            Assert.That(test.Text, Does.StartWith($"Menu des stands lu : {Menu().Count} lignes. Je peux régler : pneus (soft, medium, pluie), énergie (aujourd'hui 100 %), ratio (aujourd'hui 0.79), réparation."));
            Assert.That(test.Detail, Does.Contain("• FL TIRE:").And.Contain("Used Soft  -1% (Soft, usé)").And.Contain("navigationState=NAV_REALTIME").And.Contain("PitState=REQUEST"));
            Assert.That(test.Detail, Does.Contain("Reconnu : pneus : oui (TIRES: + 4 roues)").And.Contain("gomme Soft, 61,2 L à bord").And.Contain(dump));
            Assert.That(lmu.Requests.Count(r => r.Method == "POST"), Is.Zero, "le test ne change rien");

            lmu.Requests.Clear();
            var (ok, text) = await LmuPitMenu.ApplyAsync(Order(new { tyres = "Soft", repair = true, energyTargetPct = 54.0 }), lmu.Token);
            Assert.That(ok, Is.True, text);
            Assert.That(text, Is.EqualTo("Menu des stands réglé : pneus soft neufs, réparation, énergie 54 %."));
            Assert.That(lmu.Requests.Select(r => $"{r.Method} {r.Path}"), Is.EqualTo(new[]
            {
                "GET " + LmuPitMenu.MenuPath, "POST " + LmuPitMenu.LoadPath, "GET " + LmuPitMenu.MenuPath,
            }), "une lecture, un envoi, une relecture : jamais de boucle");

            (string Method, string Path, string ContentType, string Body) post = lmu.Requests.Single(r => r.Method == "POST");
            Assert.That(post.ContentType, Is.EqualTo("application/json"));
            Assert.That(post.Body, Does.StartWith("[{\"PMC Value\":1,"), "tableau nu, champs du jeu gardés");
            Assert.That(post.Body, Does.Contain("Sébastien Loeb").And.Contain("141 (+1)"), "octets tels quels, sans \\u00e9 ni \\u002B");
            Assert.That(Choice(lmu.Menu, "RR TIRE:"), Is.EqualTo("New Soft"));
            Assert.That(At(lmu.Menu, "VIRTUAL ENERGY:"), Is.EqualTo(54));

            Assert.That(log, Has.Some.StartsWith($"LMU GET http://127.0.0.1:{lmu.Port}{LmuPitMenu.MenuPath} → 200").And.Contains("[{\"PMC Value\":1,"));
            Assert.That(log, Has.Some.StartsWith("Menu des stands, changements : ").And.Contains("TIRES: 0 « No Change » → 1 « New Soft »").And.Contains("RR TIRE: 0 « No Change » → 1 « New Soft »"));
            Assert.That(log, Has.Some.Contains("demande : {\"tyres\":\"Soft\""));
            string saved = await File.ReadAllTextAsync(dump);
            Assert.That(saved, Does.Contain("ENVOYÉ :").And.Contain("\"name\":\"REPLACE BRAKES:\""), "réponses complètes dans le fichier");
        }
        finally
        {
            File.Delete(dump);
        }
    }

    [Test]
    public async Task Api_AlreadySet_SendsNothing()
    {
        JsonArray menu = Menu(damage: false);
        R(menu, "VIRTUAL ENERGY:")["currentSetting"] = 70;
        using var lmu = FakeLmu.Start(menu);
        var (ok, text) = await LmuPitMenu.ApplyAsync(Order(new { energyTargetPct = 70.0 }), lmu.Token);
        Assert.That(ok, Is.True, text);
        Assert.That(text, Is.EqualTo("Menu des stands déjà réglé : énergie 70 %."));
        Assert.That(lmu.Requests.Count(r => r.Method == "POST"), Is.Zero);
    }

    [Test]
    public async Task Api_GameRefusesASet_SaysWhichOne()
    {
        using var lmu = FakeLmu.Start(Menu());
        lmu.Refuse.Add("RR TIRE:");
        var (ok, text) = await LmuPitMenu.ApplyAsync(Order(new { tyres = "Medium" }), lmu.Token);
        Assert.That(ok, Is.False);
        Assert.That(text, Does.Contain("Le jeu a gardé sa valeur pour : rr tire (No Change au lieu de New Medium)"));
    }

    [Test]
    public async Task Api_AllTyresRowIsRecomputedByTheGame_WheelsAreWhatCount()
    {
        using var lmu = FakeLmu.Start(Menu());
        lmu.RecomputeTires = true;
        var (ok, text) = await LmuPitMenu.ApplyAsync(Order(new { tyres = "Medium" }), lmu.Token);
        Assert.That(ok, Is.True, text);
        Assert.That(Choice(lmu.Menu, "FL TIRE:"), Is.EqualTo("New Medium"));
    }

    [Test]
    public async Task Api_FallsBackToTheOtherAddress()
    {
        using var lmu = FakeLmu.Start(Menu());
        LmuPitMenu.UseAddress(lmu.Port, "[::1]", "127.0.0.1"); // le faux LMU n'écoute qu'en IPv4, comme LMU 1.3.3
        var log = new List<string>();
        LmuPitMenu.Log = log.Add;
        var (ok, text) = await LmuPitMenu.TestAsync(lmu.Token);
        Assert.That(ok, Is.True, text);
        Assert.That(log, Has.Some.StartsWith($"LMU GET http://[::1]:{lmu.Port}{LmuPitMenu.MenuPath} : échec"));
        Assert.That(log, Has.Some.StartsWith($"LMU GET http://127.0.0.1:{lmu.Port}{LmuPitMenu.MenuPath} → 200"));
    }

    [Test]
    public async Task Api_EmptyMenu_SaysTheCarIsNotInSession()
    {
        using var lmu = FakeLmu.Start(new JsonArray());
        LmuPitMenu.Inspection r = await LmuPitMenu.InspectAsync(lmu.Token);
        Assert.That(r.Ok, Is.False);
        Assert.That(r.Text, Does.Contain("vide"));
        Assert.That(r.Detail, Does.Contain("État du jeu"));
    }

    [Test]
    public async Task Api_ErrorStatus_IsReported()
    {
        using var lmu = FakeLmu.Start(Menu());
        lmu.MenuStatus = 404;
        var (ok, text) = await LmuPitMenu.ApplyAsync(Order(new { repair = true }), lmu.Token);
        Assert.That(ok, Is.False);
        Assert.That(text, Does.Contain("404"));
        Assert.That(lmu.Requests.Count(r => r.Method == "POST"), Is.Zero);
    }

    [Test]
    public async Task Api_GameNotRunning_SaysSo()
    {
        LmuPitMenu.UseAddress(FakeLmu.FreePort(), "127.0.0.1");
        var (ok, text) = await LmuPitMenu.TestAsync(CancellationToken.None);
        Assert.That(ok, Is.False);
        Assert.That(text, Does.Contain("connexion refusée").And.Contain("jeu est-il lancé"));
    }

    /// <summary>
    /// Faux Le Mans Ultimate : menu en « text/plain » sans charset, envoi d'un tableau seulement, lignes refusées au choix,
    /// « TIRES: » recalculé d'après les roues comme le jeu. Chaque requête est notée.
    /// </summary>
    private sealed class FakeLmu : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly CancellationTokenSource _cts = new(TimeSpan.FromSeconds(30));
        private Task _loop = Task.CompletedTask;

        private FakeLmu(JsonArray menu)
        {
            Menu = menu;
        }

        public JsonArray Menu { get; private set; }

        public int Port { get; private set; }

        public CancellationToken Token => _cts.Token;

        public HashSet<string> Refuse { get; } = new();

        public bool RecomputeTires { get; set; }

        public int MenuStatus { get; set; } = 200;

        public List<(string Method, string Path, string ContentType, string Body)> Requests { get; } = new();

        public static int FreePort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        public static FakeLmu Start(JsonArray menu)
        {
            var fake = new FakeLmu(menu);
            fake.Port = FreePort();
            fake._listener.Prefixes.Add($"http://127.0.0.1:{fake.Port}/");
            try
            {
                fake._listener.Start();
            }
            catch (HttpListenerException)
            {
                Assert.Ignore("Port local indisponible pour le faux LMU.");
            }

            LmuPitMenu.UseAddress(fake.Port, "127.0.0.1", "localhost");
            fake._loop = Task.Run(fake.ServeAsync);
            return fake;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
            try
            {
                _loop.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception)
            {
                // Arrêt du faux jeu.
            }

            _listener.Close();
        }

        private async Task ServeAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await _listener.GetContextAsync().WaitAsync(_cts.Token);
                }
                catch (Exception)
                {
                    return;
                }

                string path = ctx.Request.Url!.AbsolutePath;
                using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
                string body = await reader.ReadToEndAsync();
                lock (Requests)
                {
                    Requests.Add((ctx.Request.HttpMethod, path, ctx.Request.ContentType, body));
                }

                (int status, string answer) = (ctx.Request.HttpMethod, path) switch
                {
                    ("GET", LmuPitMenu.MenuPath) => (MenuStatus, MenuStatus == 200 ? Menu.ToJsonString() : "Not Found"),
                    ("POST", LmuPitMenu.LoadPath) => Load(body),
                    ("GET", LmuPitMenu.NavigationPath) => (200, "{\"loadingStatus\":{\"loading\":false},\"state\":{\"appBuild\":14200,\"gamePhase\":\"GREEN\",\"gameSession\":\"RACE1\",\"gameState\":\"GSTATE_DYN\",\"navigationState\":\"NAV_REALTIME\",\"settingMode\":\"SETTING_GRANDPRIX\"}}"),
                    ("GET", LmuPitMenu.GameStatePath) => (200, "{\"PitEntryDist\":812.5,\"PitState\":\"REQUEST\",\"gamePhase\":\"GPHASE_GREEN\",\"inControlOfVehicle\":true,\"isReplayActive\":false}"),
                    _ => (404, "Not Found"),
                };
                byte[] bytes = Encoding.UTF8.GetBytes(answer);
                ctx.Response.StatusCode = status;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.OutputStream.WriteAsync(bytes);
                ctx.Response.Close();
            }
        }

        private (int, string) Load(string body)
        {
            if (JsonNode.Parse(body) is not JsonArray posted)
            {
                return (400, "array expected"); // le vrai jeu plante sur un objet nu
            }

            var next = (JsonArray)Menu.DeepClone();
            for (int i = 0; i < next.Count && i < posted.Count; i++)
            {
                if (!Refuse.Contains(next[i]!["name"]!.GetValue<string>()))
                {
                    next[i]!["currentSetting"] = posted[i]!["currentSetting"]!.GetValue<int>();
                }
            }

            if (RecomputeTires)
            {
                // Le jeu montre « Mixed Tyres » à l'index qu'il veut : seules les roues font foi.
                R(next, "TIRES:")["currentSetting"] = 6;
            }

            Menu = next;
            return (200, string.Empty);
        }
    }

    // ───── Commandes à la voix ─────

    [TestCase("Coupe les alertes de limites de piste", "limites", false)]
    [TestCase("plus de track limits", "limites", false)]
    [TestCase("remets les limites de piste", "limites", true)]
    [TestCase("désactive les alertes", "alertes", false)]
    [TestCase("active le menu des stands automatique", "stand", true)]
    [TestCase("désactive le menu des stands", "stand", false)]
    public void Voice_Toggles(string text, string key, bool on)
    {
        RadioCommands.Toggle t = RadioCommands.ParseToggle(text);
        Assert.That(t, Is.Not.Null);
        Assert.That(t.Key, Is.EqualTo(key));
        Assert.That(t.On, Is.EqualTo(on));
    }

    [TestCase("Je veux plus de grip à l'arrière")]
    [TestCase("Quelle est la température des pneus ?")]
    [TestCase("Active le DRS")]
    public void Voice_Toggles_IgnoreOtherPhrases(string text)
    {
        Assert.That(RadioCommands.ParseToggle(text), Is.Null);
    }

    [TestCase("Essaie l'API", true)]
    [TestCase("teste le menu des stands", true)]
    [TestCase("Je veux des softs", false)]
    public void Voice_PitMenuTest(string text, bool expected)
    {
        Assert.That(RadioCommands.IsPitMenuTest(text), Is.EqualTo(expected));
    }
}

/// <summary>Mode « thinking » à la voix.</summary>
[TestFixture]
public class ThinkingVoiceTests
{
    [TestCase("Active le mode thinking", true)]
    [TestCase("active le thinkin", true)]
    [TestCase("réfléchis plus", true)]
    [TestCase("Coupe le thinking", false)]
    [TestCase("désactive le mode réflexion", false)]
    [TestCase("réponds plus vite", false)]
    public void Toggles(string text, bool on)
    {
        RadioCommands.Toggle t = RadioCommands.ParseToggle(text);
        Assert.That(t, Is.Not.Null);
        Assert.That(t.Key, Is.EqualTo("thinking"));
        Assert.That(t.On, Is.EqualTo(on));
    }

    [TestCase("Passe sur omni pour la réflexion")]
    [TestCase("utilise qwen pour la réflexion")]
    public void ModelSwitchIsNotAToggle(string text)
    {
        Assert.That(RadioCommands.ParseToggle(text), Is.Null);
    }
}
