#nullable enable
namespace SecondMonitor.Hub.Tests;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using NUnit.Framework;

/// <summary>
/// Utilisation à trois ordinateurs, rejouée sur une seule machine avec de vrais SecondMonitor (processus séparés) :
///  - un faux Ollama (HttpListener) tient lieu de l'Ollama de l'ordinateur 3 ;
///  - « Calcul » : SecondMonitor en mode serveur de calcul (ordinateur 3), relié au faux Ollama ;
///  - « Affichage » : SecondMonitor qui affiche la course (ordinateur 2), sans Ollama à lui.
/// Ports pris au hasard (jamais 8686, 8687, 8692, 9333, 11434 ni 8700-8760), données dans un dossier temporaire,
/// processus arrêtés par leur PID.
/// </summary>
[TestFixture]
[NonParallelizable]
public class ServeurCalculBoutEnBoutTests
{
    private FakeOllama _ollama = null!;
    private HubProcess _calcul = null!;
    private HubProcess _affichage = null!;
    private int _discoveryPort;

    [OneTimeSetUp]
    public async Task Demarrer()
    {
        string dll = HubProcess.HubDll();
        if (!File.Exists(dll))
        {
            Assert.Fail($"SecondMonitor n'est pas construit : {dll}");
        }

        _ollama = new FakeOllama();
        _discoveryPort = ServeurCalculTests.FreeUdpPort();
        _calcul = HubProcess.Start("calcul", new JsonObject
        {
            ["mode"] = "calcul",
            ["ai"] = new JsonObject { ["ollamaUrl"] = _ollama.Url },
            ["calcul"] = new JsonObject { ["name"] = "Calcul essai", ["discoveryPort"] = _discoveryPort },
        });
        _affichage = HubProcess.Start("affichage", new JsonObject
        {
            ["mode"] = "affichage",
            // Aucun Ollama sur « cet ordinateur » : seul le serveur de calcul a des modèles.
            ["ai"] = new JsonObject { ["ollamaUrl"] = $"http://127.0.0.1:{HubProcess.FreeTcpPort()}", ["localNodeName"] = "Mac" },
            ["calcul"] = new JsonObject { ["discoveryPort"] = _discoveryPort },
        });
        await Task.WhenAll(_calcul.WaitReadyAsync(), _affichage.WaitReadyAsync());
    }

    [OneTimeTearDown]
    public void Arreter()
    {
        _affichage?.Dispose();
        _calcul?.Dispose();
        _ollama?.Dispose();
    }

    /// <summary>Code d'appairage lu comme sur l'écran du serveur (depuis l'ordinateur lui-même).</summary>
    private async Task<string> CodeAsync() =>
        (await _calcul.GetJsonAsync("/api/calcul/etat"))["code"]!.GetValue<string>();

    private async Task<string> PairDirectlyAsync(string clientId, string name)
    {
        using var resp = await _calcul.Http.PostAsJsonAsync("/calcul/appairer", new { code = await CodeAsync(), id = clientId, nom = name });
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK), await resp.Content.ReadAsStringAsync());
        return (await resp.Content.ReadFromJsonAsync<JsonNode>())!["jeton"]!.GetValue<string>();
    }

    private HttpRequestMessage Relay(HttpMethod method, string path, string? token, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, "/calcul" + path) { Content = content };
        if (token != null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    [Test]
    public async Task Jeton_RefuseOuAccepteParLeRelais()
    {
        // Sans jeton, avec un faux jeton : refusé, et rien n'arrive à Ollama.
        int before = _ollama.Requests.Count;
        using (var resp = await _calcul.Http.SendAsync(Relay(HttpMethod.Get, "/api/tags", null)))
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        using (var resp = await _calcul.Http.SendAsync(Relay(HttpMethod.Get, "/api/tags", "faux-jeton-de-quarante-caracteres-0123456789")))
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(await resp.Content.ReadAsStringAsync(), Does.Contain("Jeton refusé"));
        }

        Assert.That(_ollama.Requests.Count, Is.EqualTo(before));

        // Mauvais code : refusé.
        using (var resp = await _calcul.Http.PostAsJsonAsync("/calcul/appairer", new { code = "12345", id = "intrus", nom = "Intrus" }))
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        // Bon code : jeton accepté.
        string token = await PairDirectlyAsync("essai-jeton", "Essai jeton");
        using (var resp = await _calcul.Http.SendAsync(Relay(HttpMethod.Get, "/api/tags", token)))
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        // Retiré sur l'écran du serveur : refusé de nouveau.
        using (var resp = await _calcul.Http.DeleteAsync("/api/calcul/clients/essai-jeton"))
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        using (var resp = await _calcul.Http.SendAsync(Relay(HttpMethod.Get, "/api/tags", token)))
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }
    }

    [Test]
    public async Task Relais_VersLeFauxOllama()
    {
        string token = await PairDirectlyAsync("essai-relais", "Essai relais");

        // Liste des modèles, telle quelle.
        using (var resp = await _calcul.Http.SendAsync(Relay(HttpMethod.Get, "/api/tags", token)))
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            JsonNode tags = (await resp.Content.ReadFromJsonAsync<JsonNode>())!;
            Assert.That(tags["models"]!.AsArray().Select(m => m!["name"]!.GetValue<string>()), Is.EquivalentTo(new[] { FakeOllama.ChatModel, FakeOllama.VoiceModel }));
        }

        // Conversation : le corps arrive intact à Ollama, la réponse revient intacte, le jeton reste sur le serveur.
        string chatBody = $$"""{"model":"{{FakeOllama.ChatModel}}","stream":false,"messages":[{"role":"user","content":"Combien de tours d'essence ?"}]}""";
        using (var resp = await _calcul.Http.SendAsync(Relay(HttpMethod.Post, "/api/chat", token, new StringContent(chatBody, Encoding.UTF8, "application/json"))))
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            JsonNode answer = (await resp.Content.ReadFromJsonAsync<JsonNode>())!;
            Assert.That(answer["message"]!["content"]!.GetValue<string>(), Is.EqualTo(FakeOllama.Answer));
        }

        FakeOllama.Received chat = _ollama.Requests.Last(r => r.Path == "/api/chat");
        Assert.That(Encoding.UTF8.GetString(chat.Body), Is.EqualTo(chatBody));
        Assert.That(chat.ContentType, Does.StartWith("application/json"));
        Assert.That(chat.Authorization, Is.Null, "le jeton ne doit pas partir vers Ollama");

        // Fiche d'un modèle.
        using (var resp = await _calcul.Http.SendAsync(Relay(HttpMethod.Post, "/api/show", token, JsonContent.Create(new { model = FakeOllama.VoiceModel }))))
        {
            JsonNode show = (await resp.Content.ReadFromJsonAsync<JsonNode>())!;
            Assert.That(show["capabilities"]!.AsArray().Select(c => c!.GetValue<string>()), Does.Contain("speech"));
        }

        // Transcription : formulaire multipart (fichier audio) relayé avec sa limite de parties.
        using (var form = new MultipartFormDataContent())
        {
            form.Add(new StringContent(FakeOllama.VoiceModel), "model");
            var wav = new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            wav.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            form.Add(wav, "file", "radio.wav");
            using var resp = await _calcul.Http.SendAsync(Relay(HttpMethod.Post, "/v1/audio/transcriptions", token, form));
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That((await resp.Content.ReadFromJsonAsync<JsonNode>())!["text"]!.GetValue<string>(), Is.EqualTo(FakeOllama.Transcript));
        }

        FakeOllama.Received transcription = _ollama.Requests.Last(r => r.Path == "/v1/audio/transcriptions");
        Assert.That(transcription.ContentType, Does.StartWith("multipart/form-data").And.Contain("boundary="));
        Assert.That(Encoding.UTF8.GetString(transcription.Body), Does.Contain("radio.wav").And.Contain(FakeOllama.VoiceModel));

        // Synthèse vocale : octets WAV rendus tels quels.
        using (var resp = await _calcul.Http.SendAsync(Relay(HttpMethod.Post, "/v1/audio/speech", token, JsonContent.Create(new { model = FakeOllama.VoiceModel, input = "Box, box", voice = "default", response_format = "wav" }))))
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("audio/wav"));
            Assert.That(await resp.Content.ReadAsByteArrayAsync(), Is.EqualTo(FakeOllama.Wav));
        }

        // Une erreur d'Ollama (modèle absent) revient avec son code.
        using (var resp = await _calcul.Http.SendAsync(Relay(HttpMethod.Post, "/api/chat", token, JsonContent.Create(new { model = "absent:1b", messages = Array.Empty<object>() }))))
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(await resp.Content.ReadAsStringAsync(), Does.Contain("not found"));
        }

        // Rien d'autre d'Ollama n'est joignable, même avec le jeton (télécharger, effacer un modèle…).
        foreach (var (method, path) in new[] { (HttpMethod.Post, "/api/pull"), (HttpMethod.Delete, "/api/delete"), (HttpMethod.Post, "/api/generate"), (HttpMethod.Get, "/api/tags/../ps") })
        {
            using var resp = await _calcul.Http.SendAsync(Relay(method, path, token, method == HttpMethod.Get ? null : JsonContent.Create(new { model = FakeOllama.ChatModel })));
            Assert.That((int)resp.StatusCode, Is.AnyOf(404, 405), $"{method} {path}");
        }

        Assert.That(_ollama.Requests.Select(r => r.Path), Has.None.EqualTo("/api/pull").And.None.EqualTo("/api/delete").And.None.EqualTo("/api/generate").And.None.EqualTo("/api/ps"));

        // L'écran du serveur voit ce SecondMonitor et sa dernière demande.
        JsonNode client = (await _calcul.GetJsonAsync("/api/calcul/etat"))["clients"]!.AsArray().First(c => c!["id"]!.GetValue<string>() == "essai-relais")!;
        Assert.That(client["enLigne"]!.GetValue<bool>(), Is.True);
        Assert.That(client["requetes"]!.GetValue<int>(), Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public async Task ServeurDeCalcul_FermeAuReseauSaufLeRelais()
    {
        // Le serveur répond « 127.0.0.1 » pour lui-même ; on le joint par son adresse du réseau local, s'il en a une.
        string? lan = (await _calcul.GetJsonAsync("/api/calcul/etat"))["adresses"]!.AsArray().Select(a => a!.GetValue<string>()).FirstOrDefault();
        if (lan == null)
        {
            Assert.Ignore("Pas d'adresse réseau sur cette machine.");
        }

        using var http = new HttpClient { BaseAddress = new Uri($"http://{lan}:{_calcul.WebPort}"), Timeout = TimeSpan.FromSeconds(10) };
        using (var resp = await http.GetAsync("/api/settings"))
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), "réglages fermés au réseau");
        }

        using (var resp = await http.GetAsync("/api/calcul/etat"))
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), "le code d'appairage ne s'affiche que sur le serveur");
        }

        using (var resp = await http.GetAsync("/calcul/api/tags"))
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), "relais joignable, mais jeton exigé");
        }
    }

    [Test]
    public async Task DeuxSecondMonitor_DecouverteAppairageEtModeles()
    {
        // 1) L'ordinateur 2 entend l'annonce du serveur de calcul.
        JsonNode? server = null;
        for (int i = 0; i < 60 && server == null; i++)
        {
            JsonNode found = await _affichage.GetJsonAsync("/api/calcul/decouverts");
            server = found["serveurs"]!.AsArray().FirstOrDefault(s => s!["nom"]!.GetValue<string>() == "Calcul essai");
            if (server == null)
            {
                await Task.Delay(250);
            }
        }

        Assert.That(server, Is.Not.Null, $"serveur de calcul non découvert. Calcul : {_calcul.Output} — Affichage : {_affichage.Output}");
        Assert.That(server!["port"]!.GetValue<int>(), Is.EqualTo(_calcul.WebPort));
        Assert.That(server["modeles"]!.GetValue<int>(), Is.EqualTo(2));
        Assert.That(server["ollama"]!.GetValue<bool>(), Is.True);
        string address = server["adresse"]!.GetValue<string>();

        // 2) Mauvais code : refusé, rien n'est ajouté.
        using (var resp = await _affichage.Http.PostAsJsonAsync("/api/calcul/relier", new { adresse = address, port = _calcul.WebPort, code = "000 001" }))
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(await resp.Content.ReadAsStringAsync(), Does.Contain("Code incorrect"));
        }

        Assert.That((await _affichage.GetJsonAsync("/api/settings"))["ai"]!["nodes"]!.AsArray(), Is.Empty);

        // 3) Bon code, saisi comme il est affiché (« 123 456 ») : le serveur devient une machine IA.
        using (var resp = await _affichage.Http.PostAsJsonAsync("/api/calcul/relier", new { adresse = address, port = _calcul.WebPort, code = await CodeAsync() }))
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK), await resp.Content.ReadAsStringAsync());
            Assert.That((await resp.Content.ReadFromJsonAsync<JsonNode>())!["machine"]!.GetValue<string>(), Is.EqualTo("Calcul essai"));
        }

        // Le jeton reste dans settings.json de l'ordinateur 2, jamais dans ce que reçoit l'interface.
        JsonNode settings = await _affichage.GetJsonAsync("/api/settings");
        JsonNode node = settings["ai"]!["nodes"]!.AsArray().Single()!;
        Assert.That(node["name"]!.GetValue<string>(), Is.EqualTo("Calcul essai"));
        Assert.That(node["url"]!.GetValue<string>(), Is.EqualTo($"http://{address}:{_calcul.WebPort}/calcul"));
        Assert.That(node["token"]!.GetValue<string>(), Is.EqualTo("relié"));
        string onDisk = JsonNode.Parse(File.ReadAllText(Path.Combine(_affichage.DataDir, "settings.json")))!["ai"]!["nodes"]![0]!["token"]!.GetValue<string>();
        Assert.That(onDisk, Has.Length.GreaterThanOrEqualTo(40));

        // 4) Ses modèles apparaissent « nom@machine » dans les listes existantes.
        JsonNode models = await _affichage.GetJsonAsync("/api/ai/models");
        string[] ids = models["models"]!.AsArray().Select(m => m!["id"]!.GetValue<string>()).ToArray();
        Assert.That(ids, Is.EquivalentTo(new[] { $"{FakeOllama.ChatModel}@Calcul essai", $"{FakeOllama.VoiceModel}@Calcul essai" }));
        JsonNode voice = models["models"]!.AsArray().First(m => m!["name"]!.GetValue<string>() == FakeOllama.VoiceModel)!;
        Assert.That(voice["capabilities"]!.AsArray().Select(c => c!.GetValue<string>()), Does.Contain("speech"), "fiche lue par le relais (/api/show)");
        JsonNode nodeStatus = models["nodes"]!.AsArray().First(n => n!["name"]!.GetValue<string>() == "Calcul essai")!;
        Assert.That(nodeStatus["ok"]!.GetValue<bool>(), Is.True);

        // 5) L'interface enregistre ses réglages (elle ne connaît pas le jeton) : le jeton est gardé.
        settings["ai"]!["thinkModel"] = $"{FakeOllama.ChatModel}@Calcul essai";
        settings["ai"]!["speakModel"] = $"{FakeOllama.VoiceModel}@Calcul essai";
        using (var resp = await _affichage.Http.PutAsync("/api/settings", new StringContent(settings.ToJsonString(), Encoding.UTF8, "application/json")))
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(await resp.Content.ReadAsStringAsync(), Does.Not.Contain(onDisk));
        }

        // 6) L'ingénieur de l'ordinateur 2 pose une question : elle part au serveur de calcul, qui la passe à Ollama.
        int chatsBefore = _ollama.Requests.Count(r => r.Path == "/api/chat");
        JsonNode think = await _affichage.PostJsonAsync("/api/ai/test", new { step = "think" });
        Assert.That(think["ok"]!.GetValue<bool>(), Is.True, think.ToJsonString());
        Assert.That(think["detail"]!.GetValue<string>(), Does.Contain("cinq sur cinq"));
        FakeOllama.Received chat = _ollama.Requests.Last(r => r.Path == "/api/chat");
        Assert.That(_ollama.Requests.Count(r => r.Path == "/api/chat"), Is.GreaterThan(chatsBefore));
        Assert.That(JsonNode.Parse(chat.Body)!["model"]!.GetValue<string>(), Is.EqualTo(FakeOllama.ChatModel), "le nom réel, sans « @machine »");
        Assert.That(chat.Authorization, Is.Null);

        JsonNode speak = await _affichage.PostJsonAsync("/api/ai/test", new { step = "speak" });
        Assert.That(speak["ok"]!.GetValue<bool>(), Is.True, speak.ToJsonString());
        Assert.That(Convert.FromBase64String(speak["audioBase64"]!.GetValue<string>()), Is.EqualTo(FakeOllama.Wav));

        // 7) L'écran du serveur montre à quel SecondMonitor il est relié, et ce qu'il lui a demandé en dernier.
        string clientId = settings["calcul"]!["id"]!.GetValue<string>();
        JsonNode linked = (await _calcul.GetJsonAsync("/api/calcul/etat"))["clients"]!.AsArray().Single(c => c!["id"]!.GetValue<string>() == clientId)!;
        Assert.That(linked["enLigne"]!.GetValue<bool>(), Is.True);
        Assert.That(linked["nom"]!.GetValue<string>(), Is.EqualTo(SecondMonitor.Hub.Core.Compute.ComputeAnnouncement.CleanName(Environment.MachineName)));
        Assert.That(linked["requetes"]!.GetValue<int>(), Is.GreaterThanOrEqualTo(2));
        Assert.That(linked["derniereRequete"]!.GetValue<string>(), Is.EqualTo($"voix · {FakeOllama.VoiceModel}"));

        // 8) Retiré sur le serveur : l'ordinateur 2 voit la machine en erreur (jeton refusé).
        using (var resp = await _calcul.Http.DeleteAsync($"/api/calcul/clients/{clientId}"))
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        JsonNode after = (await _affichage.GetJsonAsync("/api/ai/models"))["nodes"]!.AsArray().First(n => n!["name"]!.GetValue<string>() == "Calcul essai")!;
        Assert.That(after["ok"]!.GetValue<bool>(), Is.False);
        Assert.That(after["error"]!.GetValue<string>(), Does.Contain("401"));
    }

    /// <summary>Faux Ollama : répond aux cinq routes relayées et note tout ce qu'il reçoit.</summary>
    private sealed class FakeOllama : IDisposable
    {
        public const string ChatModel = "faux-modele:7b";
        public const string VoiceModel = "faux-voix:1b";
        public const string Answer = "Je vous reçois cinq sur cinq.";
        public const string Transcript = "combien de tours d'essence";
        public static readonly byte[] Wav = Encoding.ASCII.GetBytes("RIFF____WAVEfmt ").Concat(Enumerable.Range(0, 300).Select(i => (byte)i)).ToArray();

        private readonly HttpListener _listener = new();

        public FakeOllama()
        {
            Url = $"http://127.0.0.1:{HubProcess.FreeTcpPort()}";
            _listener.Prefixes.Add(Url + "/");
            _listener.Start();
            _ = Task.Run(LoopAsync);
        }

        public string Url { get; }

        public ConcurrentQueue<Received> Requests { get; } = new();

        public void Dispose()
        {
            try
            {
                _listener.Stop();
                _listener.Close();
            }
            catch (Exception)
            {
            }
        }

        private async Task LoopAsync()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await _listener.GetContextAsync();
                }
                catch (Exception)
                {
                    return;
                }

                _ = Task.Run(() => Handle(ctx));
            }
        }

        private void Handle(HttpListenerContext ctx)
        {
            try
            {
                using var ms = new MemoryStream();
                ctx.Request.InputStream.CopyTo(ms);
                var r = new Received(ctx.Request.HttpMethod, ctx.Request.Url!.AbsolutePath, ctx.Request.Headers["Authorization"], ctx.Request.ContentType, ms.ToArray());
                Requests.Enqueue(r);
                var (status, type, body) = Respond(r);
                ctx.Response.StatusCode = status;
                ctx.Response.ContentType = type;
                ctx.Response.ContentLength64 = body.Length;
                ctx.Response.OutputStream.Write(body);
                ctx.Response.Close();
            }
            catch (Exception)
            {
                // Connexion coupée par l'appelant.
            }
        }

        private static (int Status, string Type, byte[] Body) Respond(Received r)
        {
            static (int, string, byte[]) Json(string s, int status = 200) => (status, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(s));
            string model = "";
            if (r.ContentType?.StartsWith("application/json") == true && r.Body.Length > 0)
            {
                model = JsonNode.Parse(r.Body)?["model"]?.GetValue<string>() ?? "";
            }

            return (r.Method, r.Path) switch
            {
                ("GET", "/api/tags") => Json($$"""{"models":[{"name":"{{ChatModel}}","size":4200000000},{"name":"{{VoiceModel}}","size":1000000000}]}"""),
                ("POST", "/api/show") when model == ChatModel => Json("""{"capabilities":["completion"]}"""),
                ("POST", "/api/show") when model == VoiceModel => Json("""{"capabilities":["completion","audio","speech"],"voices":["default","female-french"]}"""),
                ("POST", "/api/chat") when model == ChatModel => Json($$"""{"model":"{{ChatModel}}","message":{"role":"assistant","content":"{{Answer}}"},"done":true}"""),
                ("POST", "/v1/audio/transcriptions") => Json($$"""{"text":"{{Transcript}}"}"""),
                ("POST", "/v1/audio/speech") => (200, "audio/wav", Wav),
                _ => Json($$"""{"error":"model '{{model}}' not found"}""", 404),
            };
        }

        public sealed record Received(string Method, string Path, string? Authorization, string? ContentType, byte[] Body);
    }

    /// <summary>Un SecondMonitor lancé pour l'essai : « dotnet SecondMonitorHub.dll --sans-fenetre », données à part.</summary>
    private sealed class HubProcess : IDisposable
    {
        private readonly Process _process;
        private readonly StringBuilder _output = new();

        private HubProcess(string name, JsonObject settings)
        {
            DataDir = Path.Combine(Path.GetTempPath(), $"sm-calcul-{name}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(DataDir);
            WebPort = FreeTcpPort();
            settings["webPort"] = WebPort;
            settings["httpsPort"] = FreeTcpPort();
            settings["enableHttps"] = false;
            settings["gamePort"] = ServeurCalculTests.FreeUdpPort();
            File.WriteAllText(Path.Combine(DataDir, "settings.json"), settings.ToJsonString());
            // Pas de vérification de mise à jour pendant l'essai.
            File.WriteAllText(Path.Combine(DataDir, "mise-a-jour.json"), """{"automatic":false}""");

            var start = new ProcessStartInfo(DotnetHost())
            {
                WorkingDirectory = DataDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            start.ArgumentList.Add(HubDll());
            start.ArgumentList.Add("--sans-fenetre");
            start.Environment["SMHUB_DATA"] = DataDir;
            start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
            _process = Process.Start(start)!;
            _process.OutputDataReceived += (_, e) => { lock (_output) { _output.AppendLine(e.Data); } };
            _process.ErrorDataReceived += (_, e) => { lock (_output) { _output.AppendLine(e.Data); } };
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            Http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{WebPort}"), Timeout = TimeSpan.FromSeconds(30) };
        }

        public string DataDir { get; }

        public int WebPort { get; }

        public HttpClient Http { get; }

        public string Output
        {
            get { lock (_output) { return _output.ToString(); } }
        }

        public static HubProcess Start(string name, JsonObject settings) => new(name, settings);

        public async Task WaitReadyAsync()
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < TimeSpan.FromSeconds(40))
            {
                if (_process.HasExited)
                {
                    Assert.Fail($"SecondMonitor s'est arrêté (code {_process.ExitCode}) : {Output}");
                }

                try
                {
                    using var resp = await Http.GetAsync("/api/status");
                    if (resp.IsSuccessStatusCode)
                    {
                        return;
                    }
                }
                catch (HttpRequestException)
                {
                }

                await Task.Delay(200);
            }

            Assert.Fail($"SecondMonitor ne répond pas sur le port {WebPort} : {Output}");
        }

        public async Task<JsonNode> GetJsonAsync(string path)
        {
            using var resp = await Http.GetAsync(path);
            string text = await resp.Content.ReadAsStringAsync();
            Assert.That(resp.IsSuccessStatusCode, Is.True, $"{path} : {(int)resp.StatusCode} {text}");
            return JsonNode.Parse(text)!;
        }

        public async Task<JsonNode> PostJsonAsync(string path, object body)
        {
            using var resp = await Http.PostAsJsonAsync(path, body);
            string text = await resp.Content.ReadAsStringAsync();
            Assert.That(resp.IsSuccessStatusCode, Is.True, $"{path} : {(int)resp.StatusCode} {text}");
            return JsonNode.Parse(text)!;
        }

        public void Dispose()
        {
            Http.Dispose();
            try
            {
                // Arrêt par le processus lui-même (son PID), jamais par nom.
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                    _process.WaitForExit(10000);
                }
            }
            catch (Exception)
            {
            }

            _process.Dispose();
            try
            {
                Directory.Delete(DataDir, true);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>SecondMonitorHub.dll construit à côté des tests (même configuration : Debug ou Release).</summary>
        public static string HubDll()
        {
            var testDir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); // …/Hub/tests/SecondMonitor.Hub.Tests/bin/Debug/net10.0
            string configuration = testDir.Parent!.Name;
            string hub = Path.GetFullPath(Path.Combine(testDir.FullName, "..", "..", "..", "..", ".."));
            return Path.Combine(hub, "src", "SecondMonitor.Hub", "bin", configuration, testDir.Name, "SecondMonitorHub.dll");
        }

        private static string DotnetHost()
        {
            string exe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";
            string? fromCli = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
            if (!string.IsNullOrEmpty(fromCli) && File.Exists(fromCli))
            {
                return fromCli;
            }

            // …/shared/Microsoft.NETCore.App/10.0.x/ → racine de .NET.
            string root = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", ".."));
            string candidate = Path.Combine(root, exe);
            return File.Exists(candidate) ? candidate : exe;
        }

        /// <summary>Port TCP libre, hors des ports de SecondMonitor, d'Ollama et des bancs d'essai de cette machine.</summary>
        public static int FreeTcpPort()
        {
            while (true)
            {
                var l = new System.Net.Sockets.TcpListener(IPAddress.Any, 0);
                l.Start();
                int port = ((IPEndPoint)l.LocalEndpoint).Port;
                l.Stop();
                if (port is not (8686 or 8687 or 8692 or 9333 or 11434) and not (>= 8700 and <= 8760))
                {
                    return port;
                }
            }
        }
    }
}
