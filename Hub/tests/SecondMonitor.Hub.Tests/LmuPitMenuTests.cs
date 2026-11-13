namespace SecondMonitor.Hub.Tests;

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using NUnit.Framework;
using SecondMonitor.Collector;
using SecondMonitor.Hub.Core.Ai;

/// <summary>
/// Menu des stands de Le Mans Ultimate : choix des lignes (pneus, réparation, énergie, carburant) et échange complet
/// lecture → envoi → relecture avec un faux LMU sur le port 6397 (menu synthétique au format de l'API du jeu).
/// </summary>
[TestFixture]
[NonParallelizable]
public class LmuPitMenuTests
{
    private static JsonArray Menu() => JsonNode.Parse("""
        [
          { "name": "VIRTUAL ENERGY:", "currentSetting": 40, "settings": [ { "text": "0%" }, { "text": "10%" }, { "text": "20%" }, { "text": "30%" }, { "text": "40%" }, { "text": "50%" }, { "text": "60%" }, { "text": "70%" }, { "text": "80%" }, { "text": "90%" }, { "text": "100%" } ] },
          { "name": "FUEL RATIO:", "currentSetting": 1, "settings": [ { "text": "0.90" }, { "text": "1.00" } ] },
          { "name": "TIRES:", "currentSetting": 0, "settings": [ { "text": "No Change" }, { "text": "New Medium", "type": "Medium" }, { "text": "Used Medium", "type": "Medium", "isUsed": true }, { "text": "New Soft", "type": "Soft" }, { "text": "New Wet", "type": "Wet" } ] },
          { "name": "FL TIRE:", "currentSetting": 0, "settings": [ { "text": "No Change" }, { "text": "New Medium", "type": "Medium" }, { "text": "New Soft", "type": "Soft" }, { "text": "New Wet", "type": "Wet" } ] },
          { "name": "RR TIRE:", "currentSetting": 0, "settings": [ { "text": "No Change" }, { "text": "New Medium", "type": "Medium" }, { "text": "New Soft", "type": "Soft" }, { "text": "New Wet", "type": "Wet" } ] },
          { "name": "DAMAGE:", "currentSetting": 0, "settings": [ { "text": "Do Not Repair" }, { "text": "Repair Body" }, { "text": "Repair All" } ], "extra": { "keep": true } }
        ]
        """)!.AsArray();

    private static JsonElement Order(object o) => JsonSerializer.SerializeToElement(o);

    private static int At(JsonArray m, string name) => m.First(r => r!["name"]!.GetValue<string>() == name)!["currentSetting"]!.GetValue<int>();

    [Test]
    public void Plan_SetsSoftsRepairAndEnergy()
    {
        JsonArray m = Menu();
        var (done, missing) = LmuPitMenu.Plan(m, Order(new { tyres = "Soft", repair = true, energyTargetPct = 78.0 }));
        Assert.That(At(m, "TIRES:"), Is.EqualTo(3));
        Assert.That(At(m, "FL TIRE:"), Is.EqualTo(2));
        Assert.That(At(m, "RR TIRE:"), Is.EqualTo(2));
        Assert.That(At(m, "DAMAGE:"), Is.EqualTo(2));
        Assert.That(At(m, "VIRTUAL ENERGY:"), Is.EqualTo(8));
        Assert.That(done, Has.Count.EqualTo(3));
        Assert.That(missing, Is.Empty);
    }

    [Test]
    public void Plan_NewTyresKeepTheCompoundAndSkipUsedSets()
    {
        JsonArray m = Menu();
        m.First(r => r!["name"]!.GetValue<string>() == "TIRES:")!["currentSetting"] = 2; // monté : medium usé
        LmuPitMenu.Plan(m, Order(new { tyres = "neufs" }));
        Assert.That(At(m, "TIRES:"), Is.EqualTo(1), "medium neuf, pas le train usé");
    }

    [Test]
    public void Plan_SaysWhatIsMissing()
    {
        JsonArray m = Menu();
        var (done, missing) = LmuPitMenu.Plan(m, Order(new { tyres = "Hard" }));
        Assert.That(done, Is.Empty);
        Assert.That(missing.Single(), Does.Contain("hard"));
    }

    [Test]
    public async Task Api_ReadApplyAndVerifyAgainstAFakeGame()
    {
        JsonArray state = Menu();
        string? posted = null;
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:6397/rest/garage/PitMenu/");
        try
        {
            listener.Start();
        }
        catch (HttpListenerException)
        {
            Assert.Ignore("Port 6397 occupé sur cette machine.");
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        Task server = Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await listener.GetContextAsync().WaitAsync(cts.Token);
                }
                catch (Exception)
                {
                    return;
                }

                if (ctx.Request.HttpMethod == "POST" && ctx.Request.Url!.AbsolutePath.EndsWith("loadPitMenu"))
                {
                    using var reader = new StreamReader(ctx.Request.InputStream);
                    posted = await reader.ReadToEndAsync();
                    state = JsonNode.Parse(posted)!.AsArray();
                    ctx.Response.StatusCode = 200;
                }
                else
                {
                    byte[] body = Encoding.UTF8.GetBytes(state.ToJsonString());
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.OutputStream.WriteAsync(body);
                }

                ctx.Response.Close();
            }
        });

        var (okTest, testText) = await LmuPitMenu.TestAsync(cts.Token);
        Assert.That(okTest, Is.True, testText);
        Assert.That(testText, Does.StartWith("Menu des stands lu : 6 lignes"));

        var (ok, text) = await LmuPitMenu.ApplyAsync(Order(new { tyres = "Soft", repair = true }), cts.Token);
        Assert.That(ok, Is.True, text);
        Assert.That(text, Does.StartWith("Menu des stands réglé : pneus soft neufs, réparation."));
        Assert.That(posted, Does.Contain("\"extra\":{\"keep\":true}"), "le menu est renvoyé tel quel, champs inconnus compris");
        Assert.That(At(state, "TIRES:"), Is.EqualTo(3));

        cts.Cancel();
        listener.Stop();
        await server;
    }

    [Test]
    public async Task Api_GameNotRunning_SaysSo()
    {
        var (ok, text) = await LmuPitMenu.TestAsync(CancellationToken.None);
        Assert.That(ok, Is.False);
        Assert.That(text, Does.Contain("port 6397"));
    }

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
